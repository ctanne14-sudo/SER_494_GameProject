using System.Collections;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SER494.Gameplay.CombatIntegration.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class ExplorationBattleTrigger : MonoBehaviour
    {
        [SerializeField]
        private SoloComboEncounterDefinitionAsset encounter;

        [SerializeField]
        private SoloComboLoadoutSession loadoutSession;

        [SerializeField]
        private string encounterInstanceId = string.Empty;

        [SerializeField]
        private string battleSceneName = "CombatSandbox";

        [Header("Battle Presentation")]
        [SerializeField, Min(0f)]
        private float transitionDuration = 0.75f;

        [SerializeField, Min(0.1f)]
        private float combatantSeparation = 4f;

        [Tooltip("Moves the Player toward the Camera and the Enemy away from it.")]
        [SerializeField, Min(0f)]
        private float playerCameraDepthAdvantage = 0.6f;

        [SerializeField, Min(0.1f)]
        private float battleCameraDistance = 7f;

        [SerializeField, Min(0f)]
        private float battleCameraHeight = 2.5f;

        [SerializeField, Min(0f)]
        private float battleCameraLookHeight = 1.1f;

        private bool _transitionStarted;
        private string _awaitedBattleSceneName = string.Empty;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Start()
        {
            DisableIfAlreadyDefeated();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_transitionStarted || !TryResolvePlayerTransform(other, out Transform playerTransform))
            {
                return;
            }

            Camera explorationCamera = ResolveExplorationCamera(playerTransform);
            if (explorationCamera == null)
            {
                Debug.LogError(
                    "Battle entry could not find the Player's exploration Camera.",
                    this);
                return;
            }

            string sourceSceneName = gameObject.scene.name;
            if (PlayerStateService.TryGetActive(out PlayerStateService playerStateService)
                && !PlayerStateLoadoutSessionBuilder.TryPrepare(
                    playerStateService.State,
                    loadoutSession,
                    SoloComboTestScenario.GreatswordTrial,
                    out string loadoutError))
            {
                Debug.LogWarning(
                    $"Battle entry could not prepare the active Player Loadout: {loadoutError}",
                    this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(battleSceneName))
            {
                Debug.LogError(
                    $"Battle scene '{battleSceneName}' is not available in Build Settings.",
                    this);
                return;
            }

            if (!BattleSessionService.TryGetActive(out BattleSessionService service))
            {
                Debug.LogError("BattleSessionService is unavailable.", this);
                return;
            }

            if (!service.TryBeginBattle(
                    encounter,
                    loadoutSession,
                    encounterInstanceId,
                    sourceSceneName,
                    battleSceneName,
                    playerTransform.position,
                    playerTransform.rotation,
                    out string validationError))
            {
                Debug.LogWarning($"Battle entry was rejected: {validationError}", this);
                DisableIfAlreadyDefeated();
                return;
            }

            _transitionStarted = true;
            StartCoroutine(TransitionToBattle(playerTransform, explorationCamera));
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void Configure(
            SoloComboEncounterDefinitionAsset encounterAsset,
            SoloComboLoadoutSession runtimeLoadoutSession,
            string stableEncounterInstanceId,
            string targetBattleSceneName)
        {
            encounter = encounterAsset;
            loadoutSession = runtimeLoadoutSession;
            encounterInstanceId = stableEncounterInstanceId ?? string.Empty;
            battleSceneName = targetBattleSceneName ?? string.Empty;
        }

        private void DisableIfAlreadyDefeated()
        {
            if (BattleSessionService.TryGetActive(out BattleSessionService service)
                && service.IsEncounterDefeated(gameObject.scene.name, encounterInstanceId))
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator TransitionToBattle(
            Transform playerTransform,
            Camera explorationCamera)
        {
            if (playerTransform == null || explorationCamera == null)
            {
                AbortTransition("Battle presentation lost its Player or exploration Camera.");
                yield break;
            }

            // BattleSessionService suspends exploration presentation before this coroutine
            // begins. Re-enable only the Camera and listener used for the transition; the
            // exploration movement/camera-control behaviours remain suspended.
            explorationCamera.enabled = true;
            AudioListener explorationListener = explorationCamera.GetComponent<AudioListener>();
            if (explorationListener != null)
            {
                explorationListener.enabled = true;
            }

            Transform enemyTransform = transform;
            Vector3 playerStartPosition = playerTransform.position;
            Quaternion playerStartRotation = playerTransform.rotation;
            Vector3 enemyStartPosition = enemyTransform.position;
            Quaternion enemyStartRotation = enemyTransform.rotation;
            Vector3 cameraStartPosition = explorationCamera.transform.position;
            Quaternion cameraStartRotation = explorationCamera.transform.rotation;

            BuildBattlePresentationTargets(
                playerStartPosition,
                enemyStartPosition,
                explorationCamera.transform,
                out Vector3 playerTargetPosition,
                out Quaternion playerTargetRotation,
                out Vector3 enemyTargetPosition,
                out Quaternion enemyTargetRotation,
                out Vector3 cameraTargetPosition,
                out Quaternion cameraTargetRotation);

            CharacterController playerController = playerTransform.GetComponent<CharacterController>();
            bool restorePlayerController = playerController != null && playerController.enabled;
            if (restorePlayerController)
            {
                playerController.enabled = false;
            }

            float duration = Mathf.Max(0f, transitionDuration);
            float elapsed = 0f;
            do
            {
                float normalizedTime = duration <= Mathf.Epsilon
                    ? 1f
                    : Mathf.Clamp01(elapsed / duration);
                float smoothedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);

                playerTransform.SetPositionAndRotation(
                    Vector3.LerpUnclamped(playerStartPosition, playerTargetPosition, smoothedTime),
                    Quaternion.SlerpUnclamped(playerStartRotation, playerTargetRotation, smoothedTime));
                enemyTransform.SetPositionAndRotation(
                    Vector3.LerpUnclamped(enemyStartPosition, enemyTargetPosition, smoothedTime),
                    Quaternion.SlerpUnclamped(enemyStartRotation, enemyTargetRotation, smoothedTime));

                // The exploration Camera is normally a child of the Player. Set its world
                // pose after moving the Player so its intended transition remains stable.
                explorationCamera.transform.SetPositionAndRotation(
                    Vector3.LerpUnclamped(cameraStartPosition, cameraTargetPosition, smoothedTime),
                    Quaternion.SlerpUnclamped(cameraStartRotation, cameraTargetRotation, smoothedTime));

                if (normalizedTime >= 1f)
                {
                    break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            while (true);

            if (restorePlayerController && playerController != null)
            {
                playerController.enabled = true;
            }

            _awaitedBattleSceneName = battleSceneName;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                battleSceneName,
                LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                _awaitedBattleSceneName = string.Empty;
                RestorePresentationAfterFailedLoad(
                    playerTransform,
                    playerStartPosition,
                    playerStartRotation,
                    enemyTransform,
                    enemyStartPosition,
                    enemyStartRotation,
                    explorationCamera,
                    cameraStartPosition,
                    cameraStartRotation);
                AbortTransition($"Battle scene '{battleSceneName}' could not begin loading.");
                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }
        }

        private void BuildBattlePresentationTargets(
            Vector3 playerStartPosition,
            Vector3 enemyStartPosition,
            Transform explorationCameraTransform,
            out Vector3 playerTargetPosition,
            out Quaternion playerTargetRotation,
            out Vector3 enemyTargetPosition,
            out Quaternion enemyTargetRotation,
            out Vector3 cameraTargetPosition,
            out Quaternion cameraTargetRotation)
        {
            Vector3 battleAxis = Vector3.ProjectOnPlane(
                enemyStartPosition - playerStartPosition,
                Vector3.up);
            if (battleAxis.sqrMagnitude <= 0.001f)
            {
                battleAxis = explorationCameraTransform.right;
                battleAxis.y = 0f;
            }

            if (battleAxis.sqrMagnitude <= 0.001f)
            {
                battleAxis = Vector3.right;
            }

            battleAxis.Normalize();
            Vector3 cameraForward = Vector3.Cross(battleAxis, Vector3.up).normalized;
            Vector3 currentCameraForward = Vector3.ProjectOnPlane(
                explorationCameraTransform.forward,
                Vector3.up);
            if (currentCameraForward.sqrMagnitude > 0.001f
                && Vector3.Dot(cameraForward, currentCameraForward.normalized) < 0f)
            {
                cameraForward = -cameraForward;
            }

            Vector3 battleCenter = (playerStartPosition + enemyStartPosition) * 0.5f;
            float halfSeparation = Mathf.Max(0.1f, combatantSeparation) * 0.5f;
            float depthAdvantage = Mathf.Max(0f, playerCameraDepthAdvantage);

            playerTargetPosition = battleCenter
                - (battleAxis * halfSeparation)
                - (cameraForward * depthAdvantage);
            playerTargetPosition.y = playerStartPosition.y;
            enemyTargetPosition = battleCenter
                + (battleAxis * halfSeparation)
                + (cameraForward * depthAdvantage);
            enemyTargetPosition.y = enemyStartPosition.y;

            Vector3 playerFacing = Vector3.ProjectOnPlane(
                enemyTargetPosition - playerTargetPosition,
                Vector3.up).normalized;
            Vector3 enemyFacing = -playerFacing;
            playerTargetRotation = Quaternion.LookRotation(playerFacing, Vector3.up);
            enemyTargetRotation = Quaternion.LookRotation(enemyFacing, Vector3.up);

            cameraTargetPosition = battleCenter
                - (cameraForward * Mathf.Max(0.1f, battleCameraDistance))
                + (Vector3.up * Mathf.Max(0f, battleCameraHeight));
            Vector3 lookTarget = battleCenter
                + (Vector3.up * Mathf.Max(0f, battleCameraLookHeight));
            cameraTargetRotation = Quaternion.LookRotation(
                lookTarget - cameraTargetPosition,
                Vector3.up);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (loadMode != LoadSceneMode.Additive
                || !string.Equals(scene.name, _awaitedBattleSceneName))
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            _awaitedBattleSceneName = string.Empty;
            DisableAdditiveBattleScenePresentation(scene);
        }

        private static void DisableAdditiveBattleScenePresentation(Scene battleScene)
        {
            foreach (GameObject root in battleScene.GetRootGameObjects())
            {
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                {
                    camera.enabled = false;
                }

                foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true))
                {
                    listener.enabled = false;
                }

                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    light.enabled = false;
                }
            }
        }

        private void AbortTransition(string errorMessage)
        {
            Debug.LogError(errorMessage, this);
            if (BattleSessionService.TryGetActive(out BattleSessionService service))
            {
                service.ClearCurrentSession();
            }

            _transitionStarted = false;
        }

        private static void RestorePresentationAfterFailedLoad(
            Transform playerTransform,
            Vector3 playerPosition,
            Quaternion playerRotation,
            Transform enemyTransform,
            Vector3 enemyPosition,
            Quaternion enemyRotation,
            Camera explorationCamera,
            Vector3 cameraPosition,
            Quaternion cameraRotation)
        {
            if (playerTransform != null)
            {
                playerTransform.SetPositionAndRotation(playerPosition, playerRotation);
            }

            if (enemyTransform != null)
            {
                enemyTransform.SetPositionAndRotation(enemyPosition, enemyRotation);
            }

            if (explorationCamera != null)
            {
                explorationCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            }
        }

        private static Camera ResolveExplorationCamera(Transform playerTransform)
        {
            Camera[] playerCameras = playerTransform.GetComponentsInChildren<Camera>(true);
            foreach (Camera playerCamera in playerCameras)
            {
                if (playerCamera.enabled)
                {
                    return playerCamera;
                }
            }

            if (playerCameras.Length > 0)
            {
                return playerCameras[0];
            }

            return Camera.main;
        }

        private static bool TryResolvePlayerTransform(
            Collider other,
            out Transform playerTransform)
        {
            playerTransform = null;
            if (other == null)
            {
                return false;
            }

            Transform candidate = other.attachedRigidbody != null
                ? other.attachedRigidbody.transform
                : other.transform;
            Transform current = candidate;
            while (current != null)
            {
                if (current.CompareTag("Player"))
                {
                    playerTransform = current;
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
