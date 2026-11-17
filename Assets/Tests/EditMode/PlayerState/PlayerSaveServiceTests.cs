using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;

namespace SER494.Gameplay.PlayerState.Tests
{
    [Category("PlayerState")]
    [Category("Persistence")]
    public sealed class PlayerSaveServiceTests
    {
        private static readonly MethodInfo PlayerStateAwake =
            typeof(PlayerStateService).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo PlayerSaveAwake =
            typeof(PlayerSaveService).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo StorageField =
            typeof(PlayerSaveService).GetField(
                "_storage",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<GameObject> _createdObjects = new List<GameObject>();
        private string _saveDirectory;

        [SetUp]
        public void SetUp()
        {
            _saveDirectory = Path.Combine(
                Path.GetTempPath(),
                "SER494-PlayerSaveServiceTests",
                Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = _createdObjects.Count - 1; index >= 0; index--)
            {
                if (_createdObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[index]);
                }
            }

            _createdObjects.Clear();
            if (Directory.Exists(_saveDirectory))
            {
                Directory.Delete(_saveDirectory, true);
            }
        }

        [Test]
        public void SaveThenLoad_ReplacesAuthoritativePlayerState()
        {
            (PlayerStateService stateService, PlayerSaveService saveService) =
                CreateServices();
            stateService.State.SetCurrentHealth(64);

            saveService.SaveSlot(1);
            stateService.State.SetCurrentHealth(12);
            PlayerState beforeLoad = stateService.State;

            LoadedPlayerSave loaded = saveService.LoadSlot(1);

            Assert.That(stateService.State, Is.SameAs(loaded.State));
            Assert.That(stateService.State, Is.Not.SameAs(beforeLoad));
            Assert.That(stateService.State.CurrentHealth, Is.EqualTo(64));
        }

        [Test]
        public void FailedLoad_DoesNotReplaceAuthoritativePlayerState()
        {
            (PlayerStateService stateService, PlayerSaveService saveService) =
                CreateServices();
            PlayerState stateBeforeLoad = stateService.State;

            Directory.CreateDirectory(_saveDirectory);
            var storage = (PlayerSaveStorage)StorageField.GetValue(saveService);
            File.WriteAllText(storage.GetSavePath(1), "broken save");

            Assert.Throws<InvalidDataException>(() => saveService.LoadSlot(1));
            Assert.That(stateService.State, Is.SameAs(stateBeforeLoad));
        }

        [Test]
        public void SaveAtCheckpoint_UsesActiveSlotAndRestoresAllResources()
        {
            (PlayerStateService stateService, PlayerSaveService saveService) =
                CreateServices();
            saveService.BeginNewGameInSlot(2, "Floor1_Cell_A");
            stateService.State.SetCurrentHealth(12);
            stateService.State.SetCurrentMana(3);
            stateService.State.SetCurrentCombatStamina(7);

            saveService.SaveAtCheckpoint(new SavePointContext(
                "Floor1_Cell_A",
                "floor1-cell-a.save-point-01",
                new Vector3(4f, 1f, 2f),
                Quaternion.identity));

            Assert.That(saveService.ActiveSlotNumber, Is.EqualTo(2));
            Assert.That(stateService.State.CurrentHealth,
                Is.EqualTo(stateService.State.MaximumHealth));
            Assert.That(stateService.State.CurrentMana,
                Is.EqualTo(stateService.State.MaximumMana));
            Assert.That(stateService.State.CurrentCombatStamina,
                Is.EqualTo(stateService.State.MaximumCombatStamina));
            Assert.That(saveService.InspectSlot(1).State, Is.EqualTo(SaveSlotState.Empty));
            Assert.That(saveService.LoadSlot(2).Exploration.SavePointId,
                Is.EqualTo("floor1-cell-a.save-point-01"));
        }

        [Test]
        public void MutualDestruction_PreservesRuntimeProgressAndSetsResourcesToOne()
        {
            (PlayerStateService stateService, PlayerSaveService saveService) =
                CreateServices();
            saveService.BeginNewGameInSlot(1, "Floor1_Cell_A");
            saveService.MarkEncounterDefeated("Floor1_Cell_A", "wanderer-03");
            saveService.SetWorldObjectState(
                "Floor1_Cell_A",
                "treasure-chest-01",
                WorldObjectStateIds.Opened);

            saveService.PrepareMutualDestructionReturn();

            Assert.That(stateService.State.CurrentHealth, Is.EqualTo(1));
            Assert.That(stateService.State.CurrentMana, Is.EqualTo(1));
            Assert.That(stateService.State.CurrentCombatStamina, Is.EqualTo(1));
            Assert.That(
                saveService.IsEncounterDefeated("Floor1_Cell_A", "wanderer-03"),
                Is.True);
            Assert.That(
                saveService.TryGetWorldObjectState(
                    "Floor1_Cell_A",
                    "treasure-chest-01",
                    out string worldObjectState),
                Is.True);
            Assert.That(worldObjectState, Is.EqualTo(WorldObjectStateIds.Opened));
        }

        [Test]
        public void ReloadActiveSlot_DiscardsUnsavedProgressAndRestoresSavedResources()
        {
            (PlayerStateService stateService, PlayerSaveService saveService) =
                CreateServices();
            saveService.BeginNewGameInSlot(1, "Floor1_Cell_A");

            stateService.State.SetCurrentHealth(23);
            stateService.State.SetCurrentCombatStamina(17);
            saveService.MarkEncounterDefeated("Floor1_Cell_A", "wanderer-01");

            saveService.ReloadActiveSlot();

            Assert.That(stateService.State.CurrentHealth,
                Is.EqualTo(stateService.State.MaximumHealth));
            Assert.That(stateService.State.CurrentCombatStamina,
                Is.EqualTo(stateService.State.MaximumCombatStamina));
            Assert.That(
                saveService.IsEncounterDefeated("Floor1_Cell_A", "wanderer-01"),
                Is.False);
        }

        [Test]
        public void WorldObjectState_ReplacesByStableSceneObjectKeyAndCanBeCleared()
        {
            (PlayerStateService _, PlayerSaveService saveService) = CreateServices();

            saveService.SetWorldObjectState(
                "Floor1_Cell_A",
                "treasure-chest-01",
                WorldObjectStateIds.Opened);
            saveService.SetWorldObjectState(
                "Floor1_Cell_A",
                "treasure-chest-01",
                WorldObjectStateIds.Depleted);

            Assert.That(
                saveService.TryGetWorldObjectState(
                    "Floor1_Cell_A",
                    "treasure-chest-01",
                    out string stateId),
                Is.True);
            Assert.That(stateId, Is.EqualTo(WorldObjectStateIds.Depleted));
            Assert.That(
                saveService.ClearWorldObjectState(
                    "Floor1_Cell_A",
                    "treasure-chest-01"),
                Is.True);
            Assert.That(
                saveService.TryGetWorldObjectState(
                    "Floor1_Cell_A",
                    "treasure-chest-01",
                    out _),
                Is.False);
        }

        [Test]
        public void WorldObjectState_CheckpointPersistsAndDefeatDiscardsLaterChanges()
        {
            (PlayerStateService _, PlayerSaveService saveService) = CreateServices();
            saveService.BeginNewGameInSlot(1, "Floor1_Cell_A");
            saveService.SetWorldObjectState(
                "Floor1_Cell_A",
                "treasure-chest-01",
                WorldObjectStateIds.Opened);
            saveService.SaveAtCheckpoint(new SavePointContext(
                "Floor1_Cell_A",
                "save-point-01",
                Vector3.zero,
                Quaternion.identity));
            saveService.SetWorldObjectState(
                "Floor1_Cell_A",
                "pickup-01",
                WorldObjectStateIds.Collected);

            saveService.ReloadActiveSlot();

            Assert.That(
                saveService.TryGetWorldObjectState(
                    "Floor1_Cell_A",
                    "treasure-chest-01",
                    out string savedState),
                Is.True);
            Assert.That(savedState, Is.EqualTo(WorldObjectStateIds.Opened));
            Assert.That(
                saveService.TryGetWorldObjectState(
                    "Floor1_Cell_A",
                    "pickup-01",
                    out _),
                Is.False);
        }

        [Test]
        public void DeleteActiveSlot_RemovesSaveAndClearsActiveRuntimeSlot()
        {
            (PlayerStateService _, PlayerSaveService saveService) = CreateServices();
            saveService.BeginNewGameInSlot(2, "Floor1_Cell_A");
            saveService.MarkEncounterDefeated("Floor1_Cell_A", "wanderer-01");

            saveService.DeleteSlot(2);

            Assert.That(saveService.InspectSlot(2).State, Is.EqualTo(SaveSlotState.Empty));
            Assert.That(saveService.ActiveSlotNumber, Is.Zero);
            Assert.That(
                saveService.IsEncounterDefeated("Floor1_Cell_A", "wanderer-01"),
                Is.False);
        }

        [Test]
        public void BeginNewGame_ClearsPendingCheckpointInteractionFromPreviousRun()
        {
            (PlayerStateService _, PlayerSaveService saveService) = CreateServices();
            saveService.BeginNewGameInSlot(1, "Floor1_Cell_A");
            saveService.SaveAtCheckpoint(new SavePointContext(
                "Floor1_Cell_A",
                "old-run-save-point",
                Vector3.zero,
                Quaternion.identity));
            saveService.PrepareMutualDestructionReturn();

            saveService.BeginNewGameInSlot(2, "Floor1_Cell_A");
            saveService.SaveAtCheckpoint(new SavePointContext(
                "Floor1_Cell_A",
                "new-run-save-point",
                Vector3.one,
                Quaternion.identity));

            Assert.That(
                saveService.TryConsumeCheckpointInteractionRequest(out _),
                Is.False,
                "A new game must not inherit a checkpoint interaction request from an older run.");
        }

        private (PlayerStateService, PlayerSaveService) CreateServices()
        {
            var gameObject = new GameObject("Player State and Save Services");
            _createdObjects.Add(gameObject);

            Assert.That(PlayerStateAwake, Is.Not.Null);
            Assert.That(PlayerSaveAwake, Is.Not.Null);
            Assert.That(StorageField, Is.Not.Null);

            PlayerStateService stateService = gameObject.AddComponent<PlayerStateService>();
            PlayerStateAwake.Invoke(stateService, null);

            PlayerSaveService saveService = gameObject.AddComponent<PlayerSaveService>();
            PlayerSaveAwake.Invoke(saveService, null);

            StorageField.SetValue(saveService, new PlayerSaveStorage(_saveDirectory));
            return (stateService, saveService);
        }
    }
}
