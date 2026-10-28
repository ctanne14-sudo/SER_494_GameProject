using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace SER494.Gameplay.PlayerState.Unity
{
    [Serializable]
    internal sealed class PlayerSaveEnvelope
    {
        public const int CurrentVersion = 3;

        public int Version = CurrentVersion;
        public string SavedAtUtc;
        public PlayerStateSnapshot PlayerState;
        public ExplorationSaveSnapshot Exploration = new ExplorationSaveSnapshot();
    }

    [Serializable]
    public sealed class WorldObjectStateSnapshot
    {
        public string SceneName;
        public string ObjectId;
        public string StateId;

        public WorldObjectStateSnapshot Clone()
        {
            return new WorldObjectStateSnapshot
            {
                SceneName = SceneName,
                ObjectId = ObjectId,
                StateId = StateId
            };
        }
    }

    [Serializable]
    public sealed class ExplorationSaveSnapshot
    {
        public bool HasCheckpoint;
        public string SceneName;
        public string SavePointId;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float RotationX;
        public float RotationY;
        public float RotationZ;
        public float RotationW = 1f;
        public List<string> DefeatedEncounterKeys = new List<string>();
        public List<WorldObjectStateSnapshot> WorldObjectStates =
            new List<WorldObjectStateSnapshot>();

        public Vector3 Position => new Vector3(PositionX, PositionY, PositionZ);

        public Quaternion Rotation => new Quaternion(
            RotationX,
            RotationY,
            RotationZ,
            RotationW);

        public ExplorationSaveSnapshot Clone()
        {
            return new ExplorationSaveSnapshot
            {
                HasCheckpoint = HasCheckpoint,
                SceneName = SceneName,
                SavePointId = SavePointId,
                PositionX = PositionX,
                PositionY = PositionY,
                PositionZ = PositionZ,
                RotationX = RotationX,
                RotationY = RotationY,
                RotationZ = RotationZ,
                RotationW = RotationW,
                DefeatedEncounterKeys = DefeatedEncounterKeys == null
                    ? new List<string>()
                    : new List<string>(DefeatedEncounterKeys),
                WorldObjectStates = CloneWorldObjectStates(WorldObjectStates)
            };
        }

        private static List<WorldObjectStateSnapshot> CloneWorldObjectStates(
            IEnumerable<WorldObjectStateSnapshot> source)
        {
            var result = new List<WorldObjectStateSnapshot>();
            if (source == null)
            {
                return result;
            }

            foreach (WorldObjectStateSnapshot entry in source)
            {
                result.Add(entry?.Clone());
            }

            return result;
        }
    }

    public enum SaveSlotState
    {
        Empty,
        Valid,
        Corrupted,
        UnsupportedVersion
    }

    public sealed class SaveSlotInfo
    {
        public SaveSlotInfo(
            int slotNumber,
            SaveSlotState state,
            DateTime? savedAtUtc,
            bool usesBackup,
            string errorMessage)
        {
            SlotNumber = slotNumber;
            State = state;
            SavedAtUtc = savedAtUtc;
            UsesBackup = usesBackup;
            ErrorMessage = errorMessage;
        }

        public int SlotNumber { get; }

        public SaveSlotState State { get; }

        public DateTime? SavedAtUtc { get; }

        public bool UsesBackup { get; }

        public string ErrorMessage { get; }

        public bool CanLoad => State == SaveSlotState.Valid;
    }

    public sealed class LoadedPlayerSave
    {
        public LoadedPlayerSave(
            PlayerState state,
            ExplorationSaveSnapshot exploration,
            DateTime savedAtUtc,
            bool fromBackup)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Exploration = exploration?.Clone() ?? new ExplorationSaveSnapshot();
            SavedAtUtc = savedAtUtc;
            FromBackup = fromBackup;
        }

        public PlayerState State { get; }

        public ExplorationSaveSnapshot Exploration { get; }

        public DateTime SavedAtUtc { get; }

        public bool FromBackup { get; }
    }

    public sealed class PlayerSaveStorage
    {
        public const int SlotCount = 3;

        private const string SaveFilePrefix = "save-slot-";
        private const string SaveFileExtension = ".json";

        private readonly string _rootDirectory;
        private readonly Func<DateTime> _utcNow;

        public PlayerSaveStorage(string rootDirectory, Func<DateTime> utcNow = null)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException(
                    "A save directory is required.",
                    nameof(rootDirectory));
            }

            _rootDirectory = Path.GetFullPath(rootDirectory);
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public string RootDirectory => _rootDirectory;

        public void Save(int slotNumber, PlayerState state)
        {
            Save(slotNumber, state, new ExplorationSaveSnapshot());
        }

        public void Save(
            int slotNumber,
            PlayerState state,
            ExplorationSaveSnapshot exploration)
        {
            ValidateSlotNumber(slotNumber);
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (exploration == null)
            {
                throw new ArgumentNullException(nameof(exploration));
            }

            Directory.CreateDirectory(_rootDirectory);

            string savePath = GetSavePath(slotNumber);
            string backupPath = GetBackupPath(slotNumber);
            string temporaryPath = savePath + ".tmp";
            var envelope = new PlayerSaveEnvelope
            {
                SavedAtUtc = _utcNow().ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                PlayerState = PlayerStateSnapshotMapper.Capture(state),
                Exploration = exploration.Clone()
            };

            string json = JsonUtility.ToJson(envelope, true);
            try
            {
                File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));

                // Read the temporary file through the same path as a real load before
                // it is allowed to replace the current valid save.
                ReadSave(temporaryPath, fromBackup: false);

                if (File.Exists(savePath))
                {
                    File.Replace(temporaryPath, savePath, backupPath, true);
                }
                else
                {
                    File.Move(temporaryPath, savePath);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        public LoadedPlayerSave Load(int slotNumber)
        {
            ValidateSlotNumber(slotNumber);

            string savePath = GetSavePath(slotNumber);
            if (!File.Exists(savePath))
            {
                throw new FileNotFoundException(
                    $"Save slot {slotNumber} is empty.",
                    savePath);
            }

            try
            {
                return ReadSave(savePath, fromBackup: false);
            }
            catch (Exception primaryException) when (IsRecoverableReadFailure(primaryException))
            {
                string backupPath = GetBackupPath(slotNumber);
                if (!File.Exists(backupPath))
                {
                    throw;
                }

                try
                {
                    return ReadSave(backupPath, fromBackup: true);
                }
                catch (Exception backupException) when (IsRecoverableReadFailure(backupException))
                {
                    throw new InvalidDataException(
                        $"Save slot {slotNumber} and its backup cannot be loaded.",
                        new AggregateException(primaryException, backupException));
                }
            }
        }

        public SaveSlotInfo Inspect(int slotNumber)
        {
            ValidateSlotNumber(slotNumber);

            string savePath = GetSavePath(slotNumber);
            if (!File.Exists(savePath))
            {
                return new SaveSlotInfo(
                    slotNumber,
                    SaveSlotState.Empty,
                    savedAtUtc: null,
                    usesBackup: false,
                    errorMessage: null);
            }

            try
            {
                LoadedPlayerSave loaded = Load(slotNumber);
                return new SaveSlotInfo(
                    slotNumber,
                    SaveSlotState.Valid,
                    loaded.SavedAtUtc,
                    loaded.FromBackup,
                    errorMessage: null);
            }
            catch (NotSupportedException exception)
            {
                return new SaveSlotInfo(
                    slotNumber,
                    SaveSlotState.UnsupportedVersion,
                    savedAtUtc: null,
                    usesBackup: false,
                    exception.Message);
            }
            catch (Exception exception) when (IsRecoverableReadFailure(exception))
            {
                return new SaveSlotInfo(
                    slotNumber,
                    SaveSlotState.Corrupted,
                    savedAtUtc: null,
                    usesBackup: false,
                    exception.Message);
            }
        }

        public void Delete(int slotNumber)
        {
            ValidateSlotNumber(slotNumber);
            DeleteIfPresent(GetSavePath(slotNumber));
            DeleteIfPresent(GetBackupPath(slotNumber));
            DeleteIfPresent(GetSavePath(slotNumber) + ".tmp");
        }

        public string GetSavePath(int slotNumber)
        {
            ValidateSlotNumber(slotNumber);
            return Path.Combine(
                _rootDirectory,
                SaveFilePrefix + slotNumber + SaveFileExtension);
        }

        private string GetBackupPath(int slotNumber)
        {
            return GetSavePath(slotNumber) + ".bak";
        }

        private static LoadedPlayerSave ReadSave(string path, bool fromBackup)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidDataException("The save file is empty.");
            }

            PlayerSaveEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<PlayerSaveEnvelope>(json);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidDataException("The save file is not valid JSON.", exception);
            }

            if (envelope == null || envelope.PlayerState == null)
            {
                throw new InvalidDataException("The save file has no Player State data.");
            }

            if (envelope.Version < 1
                || envelope.Version > PlayerSaveEnvelope.CurrentVersion)
            {
                throw new NotSupportedException(
                    $"Save file version {envelope.Version} is not supported.");
            }

            if (!DateTime.TryParseExact(
                    envelope.SavedAtUtc,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime savedAtUtc))
            {
                throw new InvalidDataException("The save timestamp is invalid.");
            }

            try
            {
                PlayerState state = PlayerStateSnapshotMapper.Restore(envelope.PlayerState);
                ExplorationSaveSnapshot exploration = envelope.Version == 1
                    ? new ExplorationSaveSnapshot()
                    : envelope.Exploration ?? new ExplorationSaveSnapshot();
                ValidateExploration(exploration);
                return new LoadedPlayerSave(
                    state,
                    exploration,
                    savedAtUtc.ToUniversalTime(),
                    fromBackup);
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is OverflowException)
            {
                throw new InvalidDataException(
                    "The Player State data in the save file is invalid.",
                    exception);
            }
        }

        private static void ValidateExploration(ExplorationSaveSnapshot exploration)
        {
            if (exploration.WorldObjectStates == null)
            {
                exploration.WorldObjectStates = new List<WorldObjectStateSnapshot>();
                return;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (WorldObjectStateSnapshot entry in exploration.WorldObjectStates)
            {
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.SceneName)
                    || string.IsNullOrWhiteSpace(entry.ObjectId)
                    || string.IsNullOrWhiteSpace(entry.StateId))
                {
                    throw new ArgumentException(
                        "Saved World Object states require scene, object, and state IDs.");
                }

                string key = $"{entry.SceneName.Trim()}::{entry.ObjectId.Trim()}";
                if (!keys.Add(key))
                {
                    throw new ArgumentException(
                        $"Saved World Object state '{key}' is duplicated.");
                }
            }
        }

        private static bool IsRecoverableReadFailure(Exception exception)
        {
            return exception is InvalidDataException
                || exception is IOException
                || exception is UnauthorizedAccessException;
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void ValidateSlotNumber(int slotNumber)
        {
            if (slotNumber < 1 || slotNumber > SlotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotNumber),
                    slotNumber,
                    $"Save slot must be between 1 and {SlotCount}.");
            }
        }
    }
}
