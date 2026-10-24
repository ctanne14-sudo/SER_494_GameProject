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

        private bool _transitionStarted;

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
            SceneManager.LoadScene(battleSceneName);
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
