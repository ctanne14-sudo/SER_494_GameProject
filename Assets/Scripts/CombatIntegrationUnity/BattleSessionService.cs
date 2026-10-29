using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using UnityEngine;
using UnityEngine.SceneManagement;
using SER494.Gameplay.PlayerState.Unity;

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

        private readonly ExplorationPresentationSuspension _explorationPresentation =
            new ExplorationPresentationSuspension();

        private bool _isReturningToSource;
        private string _returnSceneName;
        private Vector3 _returnPosition;
        private Quaternion _returnRotation;
        private bool _hasReturnTransform;

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
            _explorationPresentation.Resume();
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
            if (Application.isPlaying)
            {
                TrySuspendExplorationPresentation();
            }
            SessionStarted?.Invoke(CurrentSession);
            return true;
        }

        public bool TryCompleteActiveBattle(
            BattleOutcome outcome,
            out string validationError)
        {
            return TryCompleteActiveBattle(
                outcome,
                BattleRewardSummary.None,
                out validationError);
        }

        public bool TryCompleteActiveBattle(
            BattleOutcome outcome,
            BattleRewardSummary grantedRewards,
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

            if (grantedRewards == null)
            {
                validationError = "A Battle Reward summary is required.";
                return false;
            }

            if (outcome == BattleOutcome.PlayerDefeat
                && (grantedRewards.Gold != 0 || grantedRewards.TotalMaterialCount != 0))
            {
                validationError = "A defeated player cannot receive Encounter rewards.";
                return false;
            }

            CurrentSession.Complete(outcome, grantedRewards);
            if (outcome == BattleOutcome.PlayerVictory || outcome == BattleOutcome.Draw)
            {
                string encounterKey = CreateEncounterKey(
                    CurrentSession.SourceSceneName,
                    CurrentSession.EncounterInstanceId);
                _defeatedEncounterKeys.Add(encounterKey);
                if (PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
                {
                    saveService.MarkEncounterDefeated(
                        CurrentSession.SourceSceneName,
                        CurrentSession.EncounterInstanceId);
                }
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

            if (!TryPrepareReturnDestination(out validationError))
            {
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(_returnSceneName))
            {
                validationError =
                    $"Return scene '{_returnSceneName}' is not available in Build Settings.";
                return false;
            }

            _isReturningToSource = true;
            SceneManager.sceneLoaded += HandleSourceSceneLoaded;
            SceneManager.LoadScene(_returnSceneName);
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

            if (PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
            {
                return saveService.IsEncounterDefeated(sourceSceneName, encounterInstanceId);
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
            _explorationPresentation.Resume();
            CurrentSession = null;
        }

        private void HandleSourceSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (!_isReturningToSource
                || CurrentSession == null
                || !string.Equals(
                    scene.name,
                    _returnSceneName,
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

            if (_hasReturnTransform)
            {
                player.transform.SetPositionAndRotation(
                    _returnPosition,
                    _returnRotation);
            }

            if (controllerWasEnabled)
            {
                controller.enabled = true;
            }

            _explorationPresentation.Resume();
            CancelPendingReturn();
            CurrentSession = null;
        }

        private bool TryPrepareReturnDestination(out string validationError)
        {
            if (CurrentSession.Outcome == BattleOutcome.PlayerVictory)
            {
                _returnSceneName = CurrentSession.SourceSceneName;
                _returnPosition = CurrentSession.ReturnPosition;
                _returnRotation = CurrentSession.ReturnRotation;
                _hasReturnTransform = true;
                validationError = null;
                return true;
            }

            if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService)
                || !saveService.HasActiveSlot)
            {
                validationError =
                    "Defeat recovery requires an active Gameplay Save slot.";
                return false;
            }

            if (CurrentSession.Outcome == BattleOutcome.PlayerDefeat)
            {
                saveService.ReloadActiveSlot();
            }
            else if (CurrentSession.Outcome == BattleOutcome.Draw)
            {
                saveService.PrepareMutualDestructionReturn();
            }

            ExplorationSaveSnapshot exploration = saveService.Exploration;
            _returnSceneName = string.IsNullOrWhiteSpace(exploration.SceneName)
                ? CurrentSession.SourceSceneName
                : exploration.SceneName;
            _returnPosition = exploration.Position;
            _returnRotation = exploration.Rotation;
            _hasReturnTransform = exploration.HasCheckpoint;
            validationError = null;
            return true;
        }

        private void TrySuspendExplorationPresentation()
        {
            GameObject player;
            try
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }
            catch (UnityException exception)
            {
                Debug.LogWarning(
                    $"Battle started, but exploration presentation could not be suspended: "
                    + exception.Message,
                    this);
                return;
            }

            if (player == null)
            {
                Debug.LogWarning(
                    "Battle started, but no active Player object was found to suspend.",
                    this);
                return;
            }

            _explorationPresentation.Suspend(player.transform);
        }

        private void CancelPendingReturn()
        {
            SceneManager.sceneLoaded -= HandleSourceSceneLoaded;
            _isReturningToSource = false;
            _returnSceneName = null;
            _hasReturnTransform = false;
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

    /// <summary>
    /// Temporarily suspends exploration-only presentation and input while a battle scene is active.
    /// The exploration Player and HUD persist across scene loads, so their original enabled states
    /// must be restored explicitly when returning from combat.
    /// </summary>
    internal sealed class ExplorationPresentationSuspension
    {
        private readonly List<BehaviourState> _behaviourStates =
            new List<BehaviourState>();

        private CursorLockMode _cursorLockMode;
        private bool _cursorVisible;
        private bool _isSuspended;

        public void Suspend(Transform playerTransform)
        {
            if (_isSuspended || playerTransform == null)
            {
                return;
            }

            _behaviourStates.Clear();
            CapturePlayerBehaviours(playerTransform);
            CaptureExplorationCanvases();

            _cursorLockMode = Cursor.lockState;
            _cursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _isSuspended = true;
        }

        public void Resume()
        {
            if (!_isSuspended)
            {
                return;
            }

            for (int index = 0; index < _behaviourStates.Count; index++)
            {
                BehaviourState state = _behaviourStates[index];
                if (state.Behaviour != null)
                {
                    state.Behaviour.enabled = state.WasEnabled;
                }
            }

            _behaviourStates.Clear();
            Cursor.lockState = _cursorLockMode;
            Cursor.visible = _cursorVisible;
            _isSuspended = false;
        }

        private void CapturePlayerBehaviours(Transform playerTransform)
        {
            Behaviour[] behaviours = playerTransform.GetComponentsInChildren<Behaviour>(true);
            for (int index = 0; index < behaviours.Length; index++)
            {
                CaptureAndDisable(behaviours[index]);
            }
        }

        private void CaptureExplorationCanvases()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < canvases.Length; index++)
            {
                CaptureAndDisable(canvases[index]);
            }
        }

        private void CaptureAndDisable(Behaviour behaviour)
        {
            if (behaviour == null || Contains(behaviour))
            {
                return;
            }

            _behaviourStates.Add(new BehaviourState(behaviour, behaviour.enabled));
            behaviour.enabled = false;
        }

        private bool Contains(Behaviour behaviour)
        {
            for (int index = 0; index < _behaviourStates.Count; index++)
            {
                if (ReferenceEquals(_behaviourStates[index].Behaviour, behaviour))
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct BehaviourState
        {
            public BehaviourState(Behaviour behaviour, bool wasEnabled)
            {
                Behaviour = behaviour;
                WasEnabled = wasEnabled;
            }

            public Behaviour Behaviour { get; }

            public bool WasEnabled { get; }
        }
    }
}
