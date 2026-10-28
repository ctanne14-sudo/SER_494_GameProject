using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SER494.Gameplay.PlayerState.Unity
{
    public static class WorldObjectStateIds
    {
        public const string Collected = "collected";
        public const string Opened = "opened";
        public const string Activated = "activated";
        public const string Depleted = "depleted";
        public const string Removed = "removed";
    }

    public sealed class SavePointContext
    {
        public SavePointContext(
            string sceneName,
            string savePointId,
            Vector3 position,
            Quaternion rotation)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("A checkpoint scene is required.", nameof(sceneName));
            }

            if (string.IsNullOrWhiteSpace(savePointId))
            {
                throw new ArgumentException("A stable Save Point ID is required.", nameof(savePointId));
            }

            SceneName = sceneName.Trim();
            SavePointId = savePointId.Trim();
            Position = position;
            Rotation = rotation;
        }

        public string SceneName { get; }
        public string SavePointId { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }

    [DisallowMultipleComponent]
    public sealed class PlayerSaveService : MonoBehaviour
    {
        private const string SaveDirectoryName = "Saves";
        private const string InitialSpawnSavePointId = "initial-spawn";

        private static PlayerSaveService _active;
        private PlayerSaveStorage _storage;
        private ExplorationSaveSnapshot _exploration = new ExplorationSaveSnapshot();
        private bool _checkpointInteractionPending;
        private bool _restoreCheckpointOnNextSceneLoad;

        public static PlayerSaveService Active => _active;

        public bool IsAuthority => ReferenceEquals(_active, this);

        public string SaveDirectory => Storage.RootDirectory;

        public int ActiveSlotNumber { get; private set; }

        public bool HasActiveSlot => ActiveSlotNumber >= 1;

        public ExplorationSaveSnapshot Exploration => _exploration.Clone();

        private PlayerSaveStorage Storage => _storage ??= new PlayerSaveStorage(
            Path.Combine(Application.persistentDataPath, SaveDirectoryName));

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
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(_active, this))
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                _active = null;
            }
        }

        public static bool TryGetActive(out PlayerSaveService service)
        {
            service = _active;
            return service != null;
        }

        public void SaveSlot(int slotNumber)
        {
            RequireAuthority();
            Storage.Save(slotNumber, RequirePlayerStateService().State, _exploration);
            ActiveSlotNumber = slotNumber;
        }

        public void BeginNewGameInSlot(int slotNumber, string initialSceneName)
        {
            RequireAuthority();
            var proposed = new ExplorationSaveSnapshot { SceneName = initialSceneName };

            // Do not expose a half-created active run if the initial disk write fails.
            Storage.Save(slotNumber, RequirePlayerStateService().State, proposed);
            _exploration = proposed;
            ActiveSlotNumber = slotNumber;
            _checkpointInteractionPending = false;
            _restoreCheckpointOnNextSceneLoad = false;
        }

        public void SaveAtCheckpoint(SavePointContext checkpoint)
        {
            RequireAuthority();
            if (!HasActiveSlot)
            {
                throw new InvalidOperationException(
                    "A save slot must be selected before using a Save Point.");
            }

            if (checkpoint == null)
            {
                throw new ArgumentNullException(nameof(checkpoint));
            }

            PlayerState state = RequirePlayerStateService().State;
            int previousHealth = state.CurrentHealth;
            int previousMana = state.CurrentMana;
            int previousStamina = state.CurrentCombatStamina;
            ExplorationSaveSnapshot previousExploration = _exploration.Clone();
            ExplorationSaveSnapshot proposed = CaptureCheckpoint(checkpoint);

            state.RestoreHealthToMaximum();
            state.RestoreManaToMaximum();
            state.RestoreCombatStaminaToMaximum();
            try
            {
                Storage.Save(ActiveSlotNumber, state, proposed);
                _exploration = proposed;
            }
            catch
            {
                state.SetCurrentHealth(previousHealth);
                state.SetCurrentMana(previousMana);
                state.SetCurrentCombatStamina(previousStamina);
                _exploration = previousExploration;
                throw;
            }
        }

        public LoadedPlayerSave LoadSlot(int slotNumber)
        {
            RequireAuthority();

            // Storage fully deserializes and validates before the authoritative
            // runtime state is replaced. A failed load leaves the current state intact.
            LoadedPlayerSave loaded = Storage.Load(slotNumber);
            RequirePlayerStateService().RestoreState(loaded.State);
            ActiveSlotNumber = slotNumber;
            _exploration = loaded.Exploration.Clone();
            _restoreCheckpointOnNextSceneLoad = true;
            return loaded;
        }

        public LoadedPlayerSave ReloadActiveSlot()
        {
            if (!HasActiveSlot)
            {
                throw new InvalidOperationException("There is no active save slot to reload.");
            }

            return LoadSlot(ActiveSlotNumber);
        }

        public void MarkEncounterDefeated(string sceneName, string encounterInstanceId)
        {
            string key = CreateEncounterKey(sceneName, encounterInstanceId);
            if (!_exploration.DefeatedEncounterKeys.Contains(key))
            {
                _exploration.DefeatedEncounterKeys.Add(key);
            }
        }

        public bool IsEncounterDefeated(string sceneName, string encounterInstanceId)
        {
            string key = CreateEncounterKey(sceneName, encounterInstanceId);
            return _exploration.DefeatedEncounterKeys.Contains(key);
        }

        public void SetWorldObjectState(
            string sceneName,
            string objectId,
            string stateId)
        {
            string normalizedScene = RequireId(sceneName, nameof(sceneName));
            string normalizedObject = RequireId(objectId, nameof(objectId));
            string normalizedState = RequireId(stateId, nameof(stateId));
            WorldObjectStateSnapshot existing = FindWorldObjectState(
                normalizedScene,
                normalizedObject);
            if (existing != null)
            {
                existing.StateId = normalizedState;
                return;
            }

            _exploration.WorldObjectStates.Add(new WorldObjectStateSnapshot
            {
                SceneName = normalizedScene,
                ObjectId = normalizedObject,
                StateId = normalizedState
            });
        }

        public bool TryGetWorldObjectState(
            string sceneName,
            string objectId,
            out string stateId)
        {
            string normalizedScene = RequireId(sceneName, nameof(sceneName));
            string normalizedObject = RequireId(objectId, nameof(objectId));
            WorldObjectStateSnapshot existing = FindWorldObjectState(
                normalizedScene,
                normalizedObject);
            stateId = existing?.StateId;
            return existing != null;
        }

        public bool ClearWorldObjectState(string sceneName, string objectId)
        {
            string normalizedScene = RequireId(sceneName, nameof(sceneName));
            string normalizedObject = RequireId(objectId, nameof(objectId));
            for (int index = 0; index < _exploration.WorldObjectStates.Count; index++)
            {
                WorldObjectStateSnapshot entry = _exploration.WorldObjectStates[index];
                if (string.Equals(entry.SceneName, normalizedScene, StringComparison.Ordinal)
                    && string.Equals(entry.ObjectId, normalizedObject, StringComparison.Ordinal))
                {
                    _exploration.WorldObjectStates.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        public void PrepareMutualDestructionReturn()
        {
            PlayerState state = RequirePlayerStateService().State;
            state.SetCurrentHealth(state.MaximumHealth > 0 ? 1 : 0);
            state.SetCurrentMana(state.MaximumMana > 0 ? 1 : 0);
            state.SetCurrentCombatStamina(state.MaximumCombatStamina > 0 ? 1 : 0);
            _checkpointInteractionPending = true;
            _restoreCheckpointOnNextSceneLoad = true;
        }

        public bool TryConsumeCheckpointInteractionRequest(out SavePointContext checkpoint)
        {
            checkpoint = null;
            if (!_checkpointInteractionPending || !_exploration.HasCheckpoint)
            {
                return false;
            }

            _checkpointInteractionPending = false;
            checkpoint = new SavePointContext(
                _exploration.SceneName,
                _exploration.SavePointId,
                _exploration.Position,
                _exploration.Rotation);
            return true;
        }

        public SaveSlotInfo InspectSlot(int slotNumber)
        {
            RequireAuthority();
            return Storage.Inspect(slotNumber);
        }

        public void DeleteSlot(int slotNumber)
        {
            RequireAuthority();
            Storage.Delete(slotNumber);
            if (ActiveSlotNumber == slotNumber)
            {
                ActiveSlotNumber = 0;
                _exploration = new ExplorationSaveSnapshot();
                _checkpointInteractionPending = false;
                _restoreCheckpointOnNextSceneLoad = false;
            }
        }

        private void RequireAuthority()
        {
            if (!IsAuthority)
            {
                throw new InvalidOperationException(
                    "Only the active PlayerSaveService can access save slots.");
            }
        }

        private static PlayerStateService RequirePlayerStateService()
        {
            if (!PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                throw new InvalidOperationException(
                    "PlayerStateService must exist before save slots can be used.");
            }

            return service;
        }

        private ExplorationSaveSnapshot CaptureCheckpoint(SavePointContext checkpoint)
        {
            ExplorationSaveSnapshot currentExploration = _exploration.Clone();
            return new ExplorationSaveSnapshot
            {
                HasCheckpoint = true,
                SceneName = checkpoint.SceneName,
                SavePointId = checkpoint.SavePointId,
                PositionX = checkpoint.Position.x,
                PositionY = checkpoint.Position.y,
                PositionZ = checkpoint.Position.z,
                RotationX = checkpoint.Rotation.x,
                RotationY = checkpoint.Rotation.y,
                RotationZ = checkpoint.Rotation.z,
                RotationW = checkpoint.Rotation.w,
                DefeatedEncounterKeys = currentExploration.DefeatedEncounterKeys,
                WorldObjectStates = currentExploration.WorldObjectStates
            };
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_restoreCheckpointOnNextSceneLoad
                || !string.Equals(scene.name, _exploration.SceneName, StringComparison.Ordinal))
            {
                return;
            }

            // A freshly-created game has a Gameplay Save but has not yet touched a
            // physical Save Point. Ordinary loading should use the scene's authored
            // spawn. Mutual Destruction still needs a concrete return context so the
            // automatic Save Point interaction can open there.
            if (!_exploration.HasCheckpoint && !_checkpointInteractionPending)
            {
                _restoreCheckpointOnNextSceneLoad = false;
                return;
            }

            GameObject player;
            try
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }
            catch (UnityException exception)
            {
                Debug.LogError($"Could not restore checkpoint position: {exception.Message}", this);
                return;
            }

            if (player == null)
            {
                Debug.LogError(
                    $"Scene '{scene.name}' loaded without a Player for checkpoint recovery.",
                    this);
                return;
            }

            if (!_exploration.HasCheckpoint)
            {
                _exploration = CaptureCheckpoint(new SavePointContext(
                    scene.name,
                    InitialSpawnSavePointId,
                    player.transform.position,
                    player.transform.rotation));
                _restoreCheckpointOnNextSceneLoad = false;
                return;
            }

            CharacterController controller = player.GetComponent<CharacterController>();
            bool controllerWasEnabled = controller != null && controller.enabled;
            if (controllerWasEnabled)
            {
                controller.enabled = false;
            }

            player.transform.SetPositionAndRotation(
                _exploration.Position,
                _exploration.Rotation);
            if (controllerWasEnabled)
            {
                controller.enabled = true;
            }

            _restoreCheckpointOnNextSceneLoad = false;
        }

        private static string CreateEncounterKey(string sceneName, string encounterInstanceId)
        {
            if (string.IsNullOrWhiteSpace(sceneName)
                || string.IsNullOrWhiteSpace(encounterInstanceId))
            {
                throw new ArgumentException("Scene and encounter IDs are required.");
            }

            return $"{sceneName.Trim()}::{encounterInstanceId.Trim()}";
        }

        private WorldObjectStateSnapshot FindWorldObjectState(
            string sceneName,
            string objectId)
        {
            for (int index = 0; index < _exploration.WorldObjectStates.Count; index++)
            {
                WorldObjectStateSnapshot entry = _exploration.WorldObjectStates[index];
                if (string.Equals(entry.SceneName, sceneName, StringComparison.Ordinal)
                    && string.Equals(entry.ObjectId, objectId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A stable ID is required.", parameterName);
            }

            return value.Trim();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _active = null;
        }
    }
}
