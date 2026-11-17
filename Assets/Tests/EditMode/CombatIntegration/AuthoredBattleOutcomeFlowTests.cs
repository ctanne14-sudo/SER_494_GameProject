using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.CombatIntegration.Unity;
using SER494.Gameplay.PlayerState;
using SER494.Gameplay.PlayerState.Unity;
using UnityEditor;
using UnityEngine;

namespace SER494.Gameplay.CombatIntegration.Tests
{
    [Category("CombatIntegration")]
    [Category("Persistence")]
    [Category("ContentContract")]
    public sealed class AuthoredBattleOutcomeFlowTests
    {
        private const string EncounterFolder =
            "Assets/GameData/Resources/SoloCombo/Encounters";

        private const string SceneName = "Floor1_Cell_A";
        private const int SaveSlot = 1;

        private static readonly MethodInfo PlayerStateAwake =
            typeof(PlayerStateService).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo PlayerSaveAwake =
            typeof(PlayerSaveService).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo BattleSessionAwake =
            typeof(BattleSessionService).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo StorageField =
            typeof(PlayerSaveService).GetField(
                "_storage",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo SetBattleOutcome =
            typeof(SoloComboBattleState)
                .GetProperty(nameof(SoloComboBattleState.Outcome))
                ?.GetSetMethod(nonPublic: true);

        private readonly List<UnityEngine.Object> _createdObjects =
            new List<UnityEngine.Object>();
        private string _saveDirectory;

        [SetUp]
        public void SetUp()
        {
            _saveDirectory = Path.Combine(
                Path.GetTempPath(),
                "SER494-AuthoredBattleOutcomeFlowTests",
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
                Directory.Delete(_saveDirectory, recursive: true);
            }
        }

        [TestCase(
            "DungeonGuardEncounter.asset",
            "floor1.dungeon-guard.01",
            150,
            3)]
        [TestCase(
            "ApeBeastEncounter.asset",
            "floor1.ape-beast.01",
            300,
            5)]
        [TestCase(
            "ArmoredGuardEncounter.asset",
            "floor1.armored-guard.01",
            250,
            5)]
        public void Victory_GrantsAuthoredRewardsOnceAndPersistsAtCheckpoint(
            string assetName,
            string encounterInstanceId,
            int expectedGold,
            int expectedMaterialCount)
        {
            TestContext context = CreateContext(assetName, encounterInstanceId);
            SoloComboBattleState battle = CreateBattle(context);
            battle.Enemy.ReceiveDamage(battle.Enemy.CurrentHealth);
            ForceOutcome(battle, BattleOutcome.PlayerVictory);

            Assert.That(
                BattleSessionBattleAdapter.TryCommitBattleResult(
                    context.BattleSessions,
                    context.BattleSessions.CurrentSession,
                    context.PlayerState.State,
                    battle,
                    new System.Random(594),
                    out string commitError),
                Is.True,
                commitError);

            AssertRewardAndEncounterState(
                context,
                expectedGold,
                expectedMaterialCount,
                encounterInstanceId);

            Assert.That(
                BattleSessionBattleAdapter.TryCommitBattleResult(
                    context.BattleSessions,
                    context.BattleSessions.CurrentSession,
                    context.PlayerState.State,
                    battle,
                    new System.Random(594),
                    out string duplicateError),
                Is.False);
            Assert.That(duplicateError, Does.Contain("already complete"));
            AssertRewardAndEncounterState(
                context,
                expectedGold,
                expectedMaterialCount,
                encounterInstanceId);

            SaveAndReloadCheckpoint(context);

            AssertRewardAndEncounterState(
                context,
                expectedGold,
                expectedMaterialCount,
                encounterInstanceId);
        }

        [TestCase(
            "DungeonGuardEncounter.asset",
            "floor1.dungeon-guard.01",
            150,
            3)]
        [TestCase(
            "ApeBeastEncounter.asset",
            "floor1.ape-beast.01",
            300,
            5)]
        [TestCase(
            "ArmoredGuardEncounter.asset",
            "floor1.armored-guard.01",
            250,
            5)]
        public void MutualDestruction_PreservesRewardsAndEnemyDefeatAtCheckpoint(
            string assetName,
            string encounterInstanceId,
            int expectedGold,
            int expectedMaterialCount)
        {
            TestContext context = CreateContext(assetName, encounterInstanceId);
            SoloComboBattleState battle = CreateBattle(context);
            battle.Player.ReceiveDamage(battle.Player.CurrentHealth);
            battle.Enemy.ReceiveDamage(battle.Enemy.CurrentHealth);
            ForceOutcome(battle, BattleOutcome.Draw);

            Assert.That(
                BattleSessionBattleAdapter.TryCommitBattleResult(
                    context.BattleSessions,
                    context.BattleSessions.CurrentSession,
                    context.PlayerState.State,
                    battle,
                    new System.Random(594),
                    out string commitError),
                Is.True,
                commitError);

            context.Saves.PrepareMutualDestructionReturn();

            Assert.That(context.PlayerState.State.CurrentHealth, Is.EqualTo(1));
            Assert.That(context.PlayerState.State.CurrentMana, Is.EqualTo(1));
            Assert.That(context.PlayerState.State.CurrentCombatStamina, Is.EqualTo(1));
            AssertRewardAndEncounterState(
                context,
                expectedGold,
                expectedMaterialCount,
                encounterInstanceId);

            SaveAndReloadCheckpoint(context);

            AssertRewardAndEncounterState(
                context,
                expectedGold,
                expectedMaterialCount,
                encounterInstanceId);
        }

        [TestCase("DungeonGuardEncounter.asset", "floor1.dungeon-guard.01")]
        [TestCase("ApeBeastEncounter.asset", "floor1.ape-beast.01")]
        [TestCase("ArmoredGuardEncounter.asset", "floor1.armored-guard.01")]
        public void Defeat_RestoresCheckpointWithoutRewardsOrEnemyRemoval(
            string assetName,
            string encounterInstanceId)
        {
            TestContext context = CreateContext(assetName, encounterInstanceId);
            context.PlayerState.State.Currencies.Add(
                PlayerCurrencyState.GoldCurrencyId,
                37);
            context.PlayerState.State.Inventory.Add("material.unsaved-test", 2);
            SoloComboBattleState battle = CreateBattle(context);
            battle.Player.ReceiveDamage(battle.Player.CurrentHealth);
            ForceOutcome(battle, BattleOutcome.PlayerDefeat);

            Assert.That(
                BattleSessionBattleAdapter.TryCommitBattleResult(
                    context.BattleSessions,
                    context.BattleSessions.CurrentSession,
                    context.PlayerState.State,
                    battle,
                    new System.Random(594),
                    out string commitError),
                Is.True,
                commitError);
            Assert.That(context.BattleSessions.CurrentSession.GrantedRewards.Gold, Is.Zero);
            Assert.That(
                context.BattleSessions.CurrentSession.GrantedRewards.TotalMaterialCount,
                Is.Zero);
            Assert.That(
                context.Saves.IsEncounterDefeated(SceneName, encounterInstanceId),
                Is.False);

            context.Saves.ReloadActiveSlot();

            Assert.That(context.PlayerState.State.Currencies.Gold, Is.Zero);
            Assert.That(
                context.PlayerState.State.Inventory.GetCount("material.unsaved-test"),
                Is.Zero);
            Assert.That(
                context.Saves.IsEncounterDefeated(SceneName, encounterInstanceId),
                Is.False);
            Assert.That(
                context.PlayerState.State.CurrentHealth,
                Is.EqualTo(context.PlayerState.State.MaximumHealth));
            Assert.That(
                context.PlayerState.State.CurrentCombatStamina,
                Is.EqualTo(context.PlayerState.State.MaximumCombatStamina));
        }

        private TestContext CreateContext(
            string encounterAssetName,
            string encounterInstanceId)
        {
            Assert.That(PlayerStateAwake, Is.Not.Null);
            Assert.That(PlayerSaveAwake, Is.Not.Null);
            Assert.That(BattleSessionAwake, Is.Not.Null);
            Assert.That(StorageField, Is.Not.Null);
            Assert.That(SetBattleOutcome, Is.Not.Null);

            var servicesObject = new GameObject("Authored Battle Outcome Services");
            _createdObjects.Add(servicesObject);

            PlayerStateService playerState =
                servicesObject.AddComponent<PlayerStateService>();
            PlayerStateAwake.Invoke(playerState, null);

            PlayerSaveService saves = servicesObject.AddComponent<PlayerSaveService>();
            PlayerSaveAwake.Invoke(saves, null);
            StorageField.SetValue(saves, new PlayerSaveStorage(_saveDirectory));
            saves.BeginNewGameInSlot(SaveSlot, SceneName);
            saves.SaveAtCheckpoint(CreateCheckpoint());

            BattleSessionService battleSessions =
                servicesObject.AddComponent<BattleSessionService>();
            BattleSessionAwake.Invoke(battleSessions, null);

            SoloComboEncounterDefinitionAsset encounter =
                AssetDatabase.LoadAssetAtPath<SoloComboEncounterDefinitionAsset>(
                    $"{EncounterFolder}/{encounterAssetName}");
            Assert.That(encounter, Is.Not.Null, $"Missing authored Encounter '{encounterAssetName}'.");

            SoloComboLoadoutSession loadout = CreatePreparedLoadout();
            Assert.That(
                battleSessions.TryBeginBattle(
                    encounter,
                    loadout,
                    encounterInstanceId,
                    SceneName,
                    "CombatSandbox",
                    Vector3.zero,
                    Quaternion.identity,
                    out string beginError),
                Is.True,
                beginError);

            Assert.That(
                encounter.TryCreateDefinition(
                    out SoloComboEncounterDefinition definition,
                    out string definitionError),
                Is.True,
                definitionError);

            return new TestContext(
                playerState,
                saves,
                battleSessions,
                definition,
                encounterInstanceId);
        }

        private SoloComboBattleState CreateBattle(TestContext context)
        {
            Assert.That(
                BattleSessionBattleAdapter.TryCreateBattle(
                    context.BattleSessions.CurrentSession,
                    context.PlayerState.State,
                    out SoloComboBattleState battle,
                    out string createError),
                Is.True,
                createError);
            return battle;
        }

        private SoloComboLoadoutSession CreatePreparedLoadout()
        {
            var loadout = ScriptableObject.CreateInstance<SoloComboLoadoutSession>();
            _createdObjects.Add(loadout);
            var main = new ComboActionDefinition(
                "test.main",
                "Test Main",
                ComboActionCategory.Main,
                timelineCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.1d,
                resolutionOffset: 1);
            loadout.Prepare(
                new[]
                {
                    new ComboConfiguration("test.combo", null, main, null, null, null)
                });
            return loadout;
        }

        private static void ForceOutcome(
            SoloComboBattleState battle,
            BattleOutcome outcome)
        {
            SetBattleOutcome.Invoke(battle, new object[] { outcome });
        }

        private static SavePointContext CreateCheckpoint()
        {
            return new SavePointContext(
                SceneName,
                "floor1.save-point.01",
                new Vector3(3f, 0f, 4f),
                Quaternion.identity);
        }

        private static void SaveAndReloadCheckpoint(TestContext context)
        {
            context.Saves.SaveAtCheckpoint(CreateCheckpoint());
            context.Saves.LoadSlot(SaveSlot);
        }

        private static void AssertRewardAndEncounterState(
            TestContext context,
            int expectedGold,
            int expectedMaterialCount,
            string encounterInstanceId)
        {
            Assert.That(context.PlayerState.State.Currencies.Gold, Is.EqualTo(expectedGold));
            Assert.That(
                CountRewardMaterials(context.PlayerState.State, context.Encounter),
                Is.EqualTo(expectedMaterialCount));
            Assert.That(
                context.Saves.IsEncounterDefeated(SceneName, encounterInstanceId),
                Is.True);
        }

        private static int CountRewardMaterials(
            SER494.Gameplay.PlayerState.PlayerState state,
            SoloComboEncounterDefinition encounter)
        {
            int total = 0;
            var countedIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<WeightedMaterialReward> drops =
                encounter.Rewards.MaterialDrops;
            for (int index = 0; index < drops.Count; index++)
            {
                if (countedIds.Add(drops[index].ItemId))
                {
                    total += state.Inventory.GetCount(drops[index].ItemId);
                }
            }

            return total;
        }

        private sealed class TestContext
        {
            public TestContext(
                PlayerStateService playerState,
                PlayerSaveService saves,
                BattleSessionService battleSessions,
                SoloComboEncounterDefinition encounter,
                string encounterInstanceId)
            {
                PlayerState = playerState;
                Saves = saves;
                BattleSessions = battleSessions;
                Encounter = encounter;
                EncounterInstanceId = encounterInstanceId;
            }

            public PlayerStateService PlayerState { get; }
            public PlayerSaveService Saves { get; }
            public BattleSessionService BattleSessions { get; }
            public SoloComboEncounterDefinition Encounter { get; }
            public string EncounterInstanceId { get; }
        }
    }
}
