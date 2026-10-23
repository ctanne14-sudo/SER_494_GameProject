using SER494.Gameplay.Combat.Authoring.SoloCombo;
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
            if (_transitionStarted || !other.CompareTag("Player"))
            {
                return;
            }

            string sourceSceneName = gameObject.scene.name;
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

            Transform playerTransform = other.attachedRigidbody != null
                ? other.attachedRigidbody.transform
                : other.transform;
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

        private void DisableIfAlreadyDefeated()
        {
            if (BattleSessionService.TryGetActive(out BattleSessionService service)
                && service.IsEncounterDefeated(gameObject.scene.name, encounterInstanceId))
            {
                gameObject.SetActive(false);
            }
        }
    }
}
