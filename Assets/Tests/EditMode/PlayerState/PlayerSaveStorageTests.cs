using System;
using System.IO;
using NUnit.Framework;
using SER494.Gameplay.PlayerState.Unity;

namespace SER494.Gameplay.PlayerState.Tests
{
    [Category("PlayerState")]
    [Category("Persistence")]
    public sealed class PlayerSaveStorageTests
    {
        private string _saveDirectory;

        [SetUp]
        public void SetUp()
        {
            _saveDirectory = Path.Combine(
                Path.GetTempPath(),
                "SER494-PlayerSaveStorageTests",
                Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_saveDirectory))
            {
                Directory.Delete(_saveDirectory, true);
            }
        }

        [Test]
        public void SaveAndLoad_PreservesPlayerStateAndMetadata()
        {
            DateTime savedAtUtc = new DateTime(
                2026,
                10,
                26,
                12,
                34,
                56,
                DateTimeKind.Utc);
            var storage = new PlayerSaveStorage(_saveDirectory, () => savedAtUtc);
            PlayerState original = CreatePopulatedState(currentHealth: 73);

            storage.Save(2, original);
            LoadedPlayerSave loaded = storage.Load(2);
            SaveSlotInfo info = storage.Inspect(2);

            Assert.That(loaded.State.CurrentHealth, Is.EqualTo(73));
            Assert.That(loaded.State.CurrentCombatStamina, Is.EqualTo(41));
            Assert.That(loaded.State.Inventory.GetCount("healing-potion"), Is.EqualTo(2));
            Assert.That(loaded.State.Equipment.EquippedWeaponId,
                Is.EqualTo("weapon.greatsword.test"));
            Assert.That(loaded.State.ActionLibrary.Owns("greatsword.main.test"), Is.True);
            Assert.That(loaded.State.ComboLoadouts.ActiveLoadout.Combos[0].MainActionId,
                Is.EqualTo("greatsword.main.test"));
            Assert.That(loaded.SavedAtUtc, Is.EqualTo(savedAtUtc));
            Assert.That(loaded.FromBackup, Is.False);
            Assert.That(info.State, Is.EqualTo(SaveSlotState.Valid));
            Assert.That(info.SavedAtUtc, Is.EqualTo(savedAtUtc));
        }

        [Test]
        public void SaveAndLoad_PreservesCheckpointDefeatedEncountersAndWorldObjects()
        {
            var storage = new PlayerSaveStorage(_saveDirectory);
            var exploration = new ExplorationSaveSnapshot
            {
                HasCheckpoint = true,
                SceneName = "Floor1_Cell_A",
                SavePointId = "floor1-cell-a.save-point-01",
                PositionX = 3f,
                PositionY = 1f,
                PositionZ = -2f,
                RotationW = 1f
            };
            exploration.DefeatedEncounterKeys.Add("Floor1_Cell_A::wanderer-01");
            exploration.WorldObjectStates.Add(new WorldObjectStateSnapshot
            {
                SceneName = "Floor1_Cell_A",
                ObjectId = "treasure-chest-01",
                StateId = WorldObjectStateIds.Opened
            });

            storage.Save(1, CreatePopulatedState(currentHealth: 73), exploration);
            LoadedPlayerSave loaded = storage.Load(1);

            Assert.That(loaded.Exploration.HasCheckpoint, Is.True);
            Assert.That(loaded.Exploration.SceneName, Is.EqualTo("Floor1_Cell_A"));
            Assert.That(loaded.Exploration.SavePointId,
                Is.EqualTo("floor1-cell-a.save-point-01"));
            Assert.That(loaded.Exploration.Position.x, Is.EqualTo(3f));
            Assert.That(loaded.Exploration.DefeatedEncounterKeys,
                Does.Contain("Floor1_Cell_A::wanderer-01"));
            Assert.That(loaded.Exploration.WorldObjectStates, Has.Count.EqualTo(1));
            Assert.That(loaded.Exploration.WorldObjectStates[0].ObjectId,
                Is.EqualTo("treasure-chest-01"));
            Assert.That(loaded.Exploration.WorldObjectStates[0].StateId,
                Is.EqualTo(WorldObjectStateIds.Opened));
        }

        [Test]
        public void Inspect_ReportsThreeIndependentEmptySlots()
        {
            var storage = new PlayerSaveStorage(_saveDirectory);

            for (int slotNumber = 1; slotNumber <= PlayerSaveStorage.SlotCount; slotNumber++)
            {
                Assert.That(storage.Inspect(slotNumber).State,
                    Is.EqualTo(SaveSlotState.Empty));
            }
        }

        [Test]
        public void ThreeSlots_SaveAndLoadIndependently()
        {
            var storage = new PlayerSaveStorage(_saveDirectory);
            storage.Save(1, CreatePopulatedState(currentHealth: 91));
            storage.Save(2, CreatePopulatedState(currentHealth: 62));
            storage.Save(3, CreatePopulatedState(currentHealth: 33));

            Assert.That(storage.Load(1).State.CurrentHealth, Is.EqualTo(91));
            Assert.That(storage.Load(2).State.CurrentHealth, Is.EqualTo(62));
            Assert.That(storage.Load(3).State.CurrentHealth, Is.EqualTo(33));
        }

        [Test]
        public void OverwriteAndCorruptPrimary_LoadsPreviousBackup()
        {
            var storage = new PlayerSaveStorage(_saveDirectory);
            storage.Save(1, CreatePopulatedState(currentHealth: 81));
            storage.Save(1, CreatePopulatedState(currentHealth: 42));
            File.WriteAllText(storage.GetSavePath(1), "not valid json");

            LoadedPlayerSave loaded = storage.Load(1);
            SaveSlotInfo info = storage.Inspect(1);

            Assert.That(loaded.State.CurrentHealth, Is.EqualTo(81));
            Assert.That(loaded.FromBackup, Is.True);
            Assert.That(info.State, Is.EqualTo(SaveSlotState.Valid));
            Assert.That(info.UsesBackup, Is.True);
        }

        [Test]
        public void CorruptedSaveWithoutBackup_IsReportedAndRejected()
        {
            var storage = new PlayerSaveStorage(_saveDirectory);
            Directory.CreateDirectory(_saveDirectory);
            File.WriteAllText(storage.GetSavePath(3), "not valid json");

            SaveSlotInfo info = storage.Inspect(3);

            Assert.That(info.State, Is.EqualTo(SaveSlotState.Corrupted));
            Assert.That(info.CanLoad, Is.False);
            Assert.Throws<InvalidDataException>(() => storage.Load(3));
        }

        [Test]
        public void Load_EmptySlot_IsRejectedWithoutCreatingSaveFiles()
        {
            var storage = new PlayerSaveStorage(_saveDirectory);

            Assert.Throws<FileNotFoundException>(() => storage.Load(2));
            Assert.That(storage.Inspect(2).State, Is.EqualTo(SaveSlotState.Empty));
            Assert.That(Directory.Exists(_saveDirectory), Is.False);
        }

        [Test]
        public void CorruptedPrimaryAndBackup_AreReportedAndRejectedTogether()
        {
            var storage = new PlayerSaveStorage(_saveDirectory);
            storage.Save(1, CreatePopulatedState(currentHealth: 81));
            storage.Save(1, CreatePopulatedState(currentHealth: 42));
            File.WriteAllText(storage.GetSavePath(1), "broken primary");
            File.WriteAllText(storage.GetSavePath(1) + ".bak", "broken backup");

            InvalidDataException exception = Assert.Throws<InvalidDataException>(
                () => storage.Load(1));
            SaveSlotInfo info = storage.Inspect(1);

            Assert.That(exception.Message, Does.Contain("and its backup cannot be loaded"));
            Assert.That(exception.InnerException, Is.TypeOf<AggregateException>());
            Assert.That(info.State, Is.EqualTo(SaveSlotState.Corrupted));
            Assert.That(info.CanLoad, Is.False);
            Assert.That(info.UsesBackup, Is.False);
        }

        [Test]
        public void UnsupportedSaveVersion_IsReportedAndRejected()
        {
            var storage = new PlayerSaveStorage(_saveDirectory);
            storage.Save(3, CreatePopulatedState(currentHealth: 73));
            string savePath = storage.GetSavePath(3);
            string json = File.ReadAllText(savePath);
            File.WriteAllText(
                savePath,
                json.Replace("\"Version\": 3", "\"Version\": 4"));

            SaveSlotInfo info = storage.Inspect(3);

            Assert.That(info.State, Is.EqualTo(SaveSlotState.UnsupportedVersion));
            Assert.That(info.CanLoad, Is.False);
            Assert.That(info.ErrorMessage, Does.Contain("version 4"));
            Assert.Throws<NotSupportedException>(() => storage.Load(3));
        }

        [Test]
        public void VersionOneSave_MigratesWithFullCombatStamina()
        {
            var storage = new PlayerSaveStorage(_saveDirectory);
            storage.Save(1, CreatePopulatedState(currentHealth: 73));
            string savePath = storage.GetSavePath(1);
            string json = File.ReadAllText(savePath);
            File.WriteAllText(
                savePath,
                json.Replace("\"Version\": 3", "\"Version\": 1")
                    .Replace("\"Version\": 2", "\"Version\": 1"));

            LoadedPlayerSave loaded = storage.Load(1);

            Assert.That(loaded.State.CurrentCombatStamina,
                Is.EqualTo(loaded.State.MaximumCombatStamina));
            Assert.That(loaded.Exploration.HasCheckpoint, Is.False);
        }

        [Test]
        public void VersionTwoSave_MigratesWithEmptyWorldObjectStates()
        {
            var storage = new PlayerSaveStorage(_saveDirectory);
            storage.Save(1, CreatePopulatedState(currentHealth: 73));
            string savePath = storage.GetSavePath(1);
            string json = File.ReadAllText(savePath);
            File.WriteAllText(
                savePath,
                json.Replace("\"Version\": 3", "\"Version\": 2"));

            LoadedPlayerSave loaded = storage.Load(1);

            Assert.That(loaded.Exploration.WorldObjectStates, Is.Empty);
        }

        [Test]
        public void Delete_RemovesPrimaryAndBackup()
        {
            var storage = new PlayerSaveStorage(_saveDirectory);
            storage.Save(1, CreatePopulatedState(currentHealth: 90));
            storage.Save(1, CreatePopulatedState(currentHealth: 80));

            storage.Delete(1);

            Assert.That(storage.Inspect(1).State, Is.EqualTo(SaveSlotState.Empty));
            Assert.That(File.Exists(storage.GetSavePath(1) + ".bak"), Is.False);
        }

        [TestCase(0)]
        [TestCase(4)]
        public void SlotOperations_RejectNumbersOutsideThreeSlots(int slotNumber)
        {
            var storage = new PlayerSaveStorage(_saveDirectory);

            Assert.Throws<ArgumentOutOfRangeException>(() => storage.Inspect(slotNumber));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                storage.Save(slotNumber, CreatePopulatedState(currentHealth: 100)));
        }

        private static PlayerState CreatePopulatedState(int currentHealth)
        {
            var state = new PlayerState(100, 30, 100);
            state.SetCurrentHealth(currentHealth);
            state.SetCurrentCombatStamina(41);
            state.Inventory.Add("healing-potion", 2);
            state.Equipment.Grant(
                EquipmentCategory.Weapon,
                "weapon.greatsword.test");
            state.Equipment.EquipWeapon("weapon.greatsword.test");
            state.ActionLibrary.Learn("greatsword.main.test");
            state.ComboLoadouts.ActiveLoadout.SetCombo(
                0,
                new PreparedComboState(
                    "Test Combo",
                    openingActionId: null,
                    mainActionId: "greatsword.main.test",
                    closingActionId: null,
                    linkedActionId: null,
                    linkedPlacement: null));
            return state;
        }
    }
}
