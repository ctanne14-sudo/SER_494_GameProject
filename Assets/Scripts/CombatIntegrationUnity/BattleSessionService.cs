using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SER494.Gameplay.CombatIntegration.Unity
{
    [DisallowMultipleComponent]
    public sealed class BattleSessionService : MonoBehaviour
    {
        private static BattleSessionService _active;

        private readonly HashSet<string> _defeatedEncounterKeys =
            new HashSet<string>(StringComparer.Ordinal);

        [SerializeField]
        private bool persistAcrossScenes = true;

        private bool _isReturningToSource;

        public static BattleSessionService Active => _active;

        public BattleSession CurrentSession { get; private set; }

        public bool IsAuthority => ReferenceEquals(_active, this);

        public bool HasActiveSession => CurrentSession != null;

        public bool IsReturningToSource => _isReturningToSource;

        public event Action<BattleSession> SessionStarted;

        public event Action<BattleSession> SessionCompleted;

        private void Awake()
        {
            if (_active != null && !ReferenceEquals(_active, this))
            {
                enabled = false;
                if (Application.isPlaying)
                {
                    Destroy(this);
                }

                return;
            }

            _active = this;
            if (Application.isPlaying && persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            CancelPendingReturn();

            if (ReferenceEquals(_active, this))
            {
                _active = null;
            }
        }

        public static bool TryGetActive(out BattleSessionService service)
        {
            service = _active;
            return service != null;
        }

        public bool TryBeginBattle(
            SoloComboEncounterDefinitionAsset encounter,
            SoloComboLoadoutSession loadoutSession,
            string encounterInstanceId,
            string sourceSceneName,
            string battleSceneName,
            Vector3 returnPosition,
            Quaternion returnRotation,
            out string validationError)
        {
            validationError = ValidateBeginRequest(
                encounter,
                loadoutSession,
                encounterInstanceId,
                sourceSceneName,
                battleSceneName);
            if (validationError != null)
            {
                return false;
            }

            CurrentSession = new BattleSession(
                encounter,
                loadoutSession,
                encounterInstanceId,
                sourceSceneName,
                battleSceneName,
                returnPosition,
                returnRotation);
            SessionStarted?.Invoke(CurrentSession);
            return true;
        }

        public bool TryCompleteActiveBattle(
            BattleOutcome outcome,
            out string validationError)
        {
            if (!IsAuthority)
            {
                validationError = "Only the active BattleSessionService can complete a battle.";
                return false;
            }

            if (CurrentSession == null)
            {
                validationError = "There is no active Battle Session to complete.";
                return false;
            }

            if (CurrentSession.IsComplete)
            {
                validationError = "The active Battle Session is already complete.";
                return false;
            }

            if (outcome == BattleOutcome.Ongoing)
            {
                validationError = "A completed Battle Session requires a final outcome.";
                return false;
            }

            CurrentSession.Complete(outcome);
            if (outcome == BattleOutcome.PlayerVictory || outcome == BattleOutcome.Draw)
            {
                _defeatedEncounterKeys.Add(CreateEncounterKey(
                    CurrentSession.SourceSceneName,
                    CurrentSession.EncounterInstanceId));
            }

            validationError = null;
            SessionCompleted?.Invoke(CurrentSession);
            return true;
        }

        public bool CanReturnToExploration(out string validationError)
        {
            if (!IsAuthority)
            {
                validationError =
                    "Only the active BattleSessionService can return to exploration.";
                return false;
            }

            if (CurrentSession == null)
            {
                validationError = "There is no active Battle Session to return from.";
                return false;
            }

            if (!CurrentSession.IsComplete)
            {
                validationError = "The active battle must be completed before returning.";
                return false;
            }

            if (CurrentSession.Outcome != BattleOutcome.PlayerVictory)
            {
                validationError =
                    "Only a player victory can currently return to exploration. "
                    + "Defeat and Mutual Destruction await the finalized death rules.";
                return false;
            }

            if (_isReturningToSource)
            {
                validationError = "The return to exploration is already in progress.";
                return false;
            }

            validationError = null;
            return true;
        }

        public bool TryReturnToSourceScene(out string validationError)
        {
            if (!CanReturnToExploration(out validationError))
            {
                return false;
            }

            string sourceSceneName = CurrentSession.SourceSceneName;
            if (!Application.CanStreamedLevelBeLoaded(sourceSceneName))
            {
                validationError =
                    $"Source scene '{sourceSceneName}' is not available in Build Settings.";
                return false;
            }

            _isReturningToSource = true;
            SceneManager.sceneLoaded += HandleSourceSceneLoaded;
            SceneManager.LoadScene(sourceSceneName);
            validationError = null;
            return true;
        }

        public bool IsEncounterDefeated(string sourceSceneName, string encounterInstanceId)
        {
            if (string.IsNullOrWhiteSpace(sourceSceneName)
                || string.IsNullOrWhiteSpace(encounterInstanceId))
            {
                return false;
            }

            return _defeatedEncounterKeys.Contains(CreateEncounterKey(
                sourceSceneName,
                encounterInstanceId));
        }

        public void ClearCurrentSession()
        {
            if (!IsAuthority)
            {
                throw new InvalidOperationException(
                    "Only the active BattleSessionService can clear the current session.");
            }

            CancelPendingReturn();
            CurrentSession = null;
        }

        private void HandleSourceSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (!_isReturningToSource
                || CurrentSession == null
                || !string.Equals(
                    scene.name,
                    CurrentSession.SourceSceneName,
                    StringComparison.Ordinal))
            {
                return;
            }

            GameObject player;
            try
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }
            catch (UnityException exception)
            {
                Debug.LogError(
                    $"Could not restore the player after battle: {exception.Message}",
                    this);
                CancelPendingReturn();
                return;
            }

            if (player == null)
            {
                Debug.LogError(
                    $"Source scene '{scene.name}' loaded, but no Player object was found. "
                    + "The completed Battle Session was preserved for recovery.",
                    this);
                CancelPendingReturn();
                return;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled)
            {
                controller.enabled = false;
            }

            player.transform.SetPositionAndRotation(
                CurrentSession.ReturnPosition,
                CurrentSession.ReturnRotation);

            if (controllerWasEnabled)
            {
                controller.enabled = true;
            }

            CancelPendingReturn();
            CurrentSession = null;
        }

        private void CancelPendingReturn()
        {
            SceneManager.sceneLoaded -= HandleSourceSceneLoaded;
            _isReturningToSource = false;
        }

        private string ValidateBeginRequest(
            SoloComboEncounterDefinitionAsset encounter,
            SoloComboLoadoutSession loadoutSession,
            string encounterInstanceId,
            string sourceSceneName,
            string battleSceneName)
        {
            if (!IsAuthority)
            {
                return "Only the active BattleSessionService can begin a battle.";
            }

            if (CurrentSession != null)
            {
                return "A Battle Session is already active.";
            }

            if (encounter == null || string.IsNullOrWhiteSpace(encounter.EncounterId))
            {
                return "A valid Encounter asset with an ID is required.";
            }

            if (loadoutSession == null || !loadoutSession.HasPreparedLoadout)
            {
                return "A prepared Combo Loadout is required before entering battle.";
            }

            if (string.IsNullOrWhiteSpace(encounterInstanceId))
            {
                return "The map encounter needs a stable Instance ID.";
            }

            if (string.IsNullOrWhiteSpace(sourceSceneName))
            {
                return "The source scene name is required.";
            }

            if (string.IsNullOrWhiteSpace(battleSceneName))
            {
                return "The battle scene name is required.";
            }

            if (IsEncounterDefeated(sourceSceneName, encounterInstanceId))
            {
                return "This map encounter has already been defeated.";
            }

            return null;
        }

        private static string CreateEncounterKey(
            string sourceSceneName,
            string encounterInstanceId)
        {
            return $"{sourceSceneName.Trim()}::{encounterInstanceId.Trim()}";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _active = null;
        }
    }
}
