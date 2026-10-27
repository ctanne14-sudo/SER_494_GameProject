using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.Combat.Weapons;
using SER494.Gameplay.CombatIntegration.Unity;
using UnityEngine;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.CombatIntegration.Tests
{
    [Category("CombatIntegration")]
    public sealed class BattleSessionServiceTests
    {
        private static readonly MethodInfo AwakeMethod = typeof(BattleSessionService).GetMethod(
            "Awake",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _createdObjects.Count - 1; index >= 0; index--)
            {
                if (_createdObjects[index] != null)
                {
                    Object.DestroyImmediate(_createdObjects[index]);
                }
            }

            _createdObjects.Clear();
        }

        [Test]
        public void BeginBattle_CapturesEncounterLoadoutAndReturnTransform()
        {
            BattleSessionService service = CreateService("Battle Sessions");
            SoloComboEncounterDefinitionAsset encounter = CreateEncounter("enemy.wanderer");
            SoloComboLoadoutSession loadout = CreatePreparedLoadout();
            var position = new Vector3(3f, 2f, 1f);
            Quaternion rotation = Quaternion.Euler(0f, 125f, 0f);

            bool began = service.TryBeginBattle(
                encounter,
                loadout,
                "wanderer-01",
                "Floor1_Cell_A",
                "CombatSandbox",
                position,
                rotation,
                out string error);

            Assert.That(began, Is.True, error);
            Assert.That(service.CurrentSession.Encounter, Is.SameAs(encounter));
            Assert.That(service.CurrentSession.LoadoutSession, Is.SameAs(loadout));
            Assert.That(service.CurrentSession.EncounterInstanceId, Is.EqualTo("wanderer-01"));
            Assert.That(service.CurrentSession.SourceSceneName, Is.EqualTo("Floor1_Cell_A"));
            Assert.That(service.CurrentSession.ReturnPosition, Is.EqualTo(position));
            Assert.That(service.CurrentSession.ReturnRotation, Is.EqualTo(rotation));
        }

        [Test]
        public void BeginBattle_WithoutPreparedLoadout_IsRejected()
        {
            BattleSessionService service = CreateService("Battle Sessions");
            SoloComboLoadoutSession emptyLoadout = CreateLoadout();

            bool began = service.TryBeginBattle(
                CreateEncounter("enemy.wanderer"),
                emptyLoadout,
                "wanderer-01",
                "Floor1_Cell_A",
                "CombatSandbox",
                Vector3.zero,
                Quaternion.identity,
                out string error);

            Assert.That(began, Is.False);
            Assert.That(error, Does.Contain("prepared Combo Loadout"));
            Assert.That(service.CurrentSession, Is.Null);
        }

        [Test]
        public void BeginBattle_WhenSessionAlreadyActive_IsRejected()
        {
            BattleSessionService service = CreateService("Battle Sessions");
            BeginValidBattle(service);

            bool beganAgain = service.TryBeginBattle(
                CreateEncounter("enemy.second"),
                CreatePreparedLoadout(),
                "wanderer-02",
                "Floor1_Cell_A",
                "CombatSandbox",
                Vector3.zero,
                Quaternion.identity,
                out string error);

            Assert.That(beganAgain, Is.False);
            Assert.That(error, Does.Contain("already active"));
        }

        [TestCase(BattleOutcome.PlayerVictory)]
        [TestCase(BattleOutcome.Draw)]
        public void CompleteBattle_WhenEnemyDies_MarksMapEncounterDefeated(BattleOutcome outcome)
        {
            BattleSessionService service = CreateService("Battle Sessions");
            BeginValidBattle(service);

            bool completed = service.TryCompleteActiveBattle(outcome, out string error);

            Assert.That(completed, Is.True, error);
            Assert.That(service.CurrentSession.Outcome, Is.EqualTo(outcome));
            Assert.That(
                service.IsEncounterDefeated("Floor1_Cell_A", "wanderer-01"),
                Is.True);
        }

        [Test]
        public void CompleteBattle_WhenPlayerLoses_DoesNotMarkMapEncounterDefeated()
        {
            BattleSessionService service = CreateService("Battle Sessions");
            BeginValidBattle(service);

            service.TryCompleteActiveBattle(BattleOutcome.PlayerDefeat, out string error);

            Assert.That(error, Is.Null);
            Assert.That(
                service.IsEncounterDefeated("Floor1_Cell_A", "wanderer-01"),
                Is.False);
        }

        [Test]
        public void CanReturnToExploration_AfterVictory_ReturnsTrue()
        {
            BattleSessionService service = CreateService("Battle Sessions");
            BeginValidBattle(service);
            Assert.That(
                service.TryCompleteActiveBattle(
                    BattleOutcome.PlayerVictory,
                    out string completionError),
                Is.True,
                completionError);

            bool canReturn = service.CanReturnToExploration(out string returnError);

            Assert.That(canReturn, Is.True, returnError);
            Assert.That(returnError, Is.Null);
        }

        [TestCase(BattleOutcome.PlayerDefeat)]
        [TestCase(BattleOutcome.Draw)]
        public void CanReturnToExploration_WithoutVictory_IsRejected(BattleOutcome outcome)
        {
            BattleSessionService service = CreateService("Battle Sessions");
            BeginValidBattle(service);
            Assert.That(
                service.TryCompleteActiveBattle(outcome, out string completionError),
                Is.True,
                completionError);

            bool canReturn = service.CanReturnToExploration(out string returnError);

            Assert.That(canReturn, Is.False);
            Assert.That(returnError, Does.Contain("Only a player victory"));
        }

        [Test]
        public void CanReturnToExploration_DuringBattle_IsRejected()
        {
            BattleSessionService service = CreateService("Battle Sessions");
            BeginValidBattle(service);

            bool canReturn = service.CanReturnToExploration(out string returnError);

            Assert.That(canReturn, Is.False);
            Assert.That(returnError, Does.Contain("must be completed"));
        }

        [Test]
        public void DuplicateService_DoesNotReplaceAuthority()
        {
            BattleSessionService first = CreateService("Primary Battle Sessions");
            BattleSessionService duplicate = CreateService("Duplicate Battle Sessions");

            Assert.That(BattleSessionService.Active, Is.SameAs(first));
            Assert.That(first.IsAuthority, Is.True);
            Assert.That(duplicate.IsAuthority, Is.False);
            Assert.That(duplicate.enabled, Is.False);
        }

        [Test]
        public void BattleAdapter_CreatesBattleFromPersistentPlayerState()
        {
            BattleSessionService service = CreateService("Battle Sessions");
            var playerState = new PersistentPlayerState(120, 40, 30);
            EquipStartingGear(playerState);
            playerState.TakeDamage(35);
            Assert.That(playerState.TrySpendMana(7), Is.True);
            playerState.Inventory.Add("item.potion", 2);
            var weapon = new WeaponDefinition(
                "weapon.integration-test",
                "Integration Test Weapon",
                WeaponType.Greatsword,
                weaponAttack: 77);
            SoloComboLoadoutSession loadout = CreatePreparedLoadout(weapon);
            BeginBattle(service, CreateValidEncounter(), loadout);

            bool created = BattleSessionBattleAdapter.TryCreateBattle(
                service.CurrentSession,
                playerState,
                out SoloComboBattleState battle,
                out string error);

            Assert.That(created, Is.True, error);
            Assert.That(battle.Player.CurrentHealth, Is.EqualTo(85));
            Assert.That(battle.Player.CurrentMana, Is.EqualTo(33));
            Assert.That(battle.Player.CurrentStamina, Is.EqualTo(30));
            Assert.That(battle.Player.Stats.Attack, Is.EqualTo(50));
            Assert.That(battle.Player.Stats.Defense, Is.EqualTo(10));
            Assert.That(battle.GetItemCount("item.potion"), Is.EqualTo(2));
            Assert.That(battle.Enemy.DisplayName, Is.EqualTo("Wanderer"));
        }

        [Test]
        public void BattleAdapter_CommitsFinalResourcesItemsAndEncounterOutcome()
        {
            BattleSessionService service = CreateService("Battle Sessions");
            var playerState = new PersistentPlayerState(100, 30, 30);
            EquipStartingGear(playerState);
            playerState.Inventory.Add("item.potion", 1);
            SoloComboLoadoutSession loadout = CreatePreparedLoadout(
                weapon: null,
                damageMultiplier: 10d);
            BeginBattle(service, CreateValidEncounter(enemyMaximumHealth: 1), loadout);
            Assert.That(
                BattleSessionBattleAdapter.TryCreateBattle(
                    service.CurrentSession,
                    playerState,
                    out SoloComboBattleState battle,
                    out string createError),
                Is.True,
                createError);

            SoloComboTurnResult result = battle.ExecuteCombo(0);
            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            bool committed = BattleSessionBattleAdapter.TryCommitBattleResult(
                service,
                service.CurrentSession,
                playerState,
                battle,
                out string commitError);

            Assert.That(committed, Is.True, commitError);
            Assert.That(service.CurrentSession.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(
                service.IsEncounterDefeated("Floor1_Cell_A", "wanderer-01"),
                Is.True);
            Assert.That(playerState.CurrentHealth, Is.EqualTo(battle.Player.CurrentHealth));
            Assert.That(playerState.CurrentMana, Is.EqualTo(battle.Player.CurrentMana));
        }

        [Test]
        public void BattleAdapter_MutualDestruction_CommitsPlayerDeathAndEnemyDefeat()
        {
            BattleSessionService service = CreateService("Battle Sessions");
            var playerState = new PersistentPlayerState(100, 30, 30);
            EquipStartingGear(playerState);
            playerState.SetCurrentHealth(1);
            SoloComboLoadoutSession loadout = CreatePreparedLoadout(
                weapon: null,
                damageMultiplier: 10d);
            BeginBattle(
                service,
                CreateValidEncounter(
                    enemyMaximumHealth: 1,
                    enemyAttack: 100,
                    enemyDamageMultiplier: 1f),
                loadout);
            Assert.That(
                BattleSessionBattleAdapter.TryCreateBattle(
                    service.CurrentSession,
                    playerState,
                    out SoloComboBattleState battle,
                    out string createError),
                Is.True,
                createError);

            SoloComboTurnResult result = battle.ExecuteCombo(0);

            Assert.That(result.Outcome, Is.EqualTo(BattleOutcome.Draw));
            Assert.That(battle.Player.IsDefeated, Is.True);
            Assert.That(battle.Enemy.IsDefeated, Is.True);
            Assert.That(result.ActionResolutions, Has.Count.EqualTo(2));

            bool committed = BattleSessionBattleAdapter.TryCommitBattleResult(
                service,
                service.CurrentSession,
                playerState,
                battle,
                out string commitError);

            Assert.That(committed, Is.True, commitError);
            Assert.That(service.CurrentSession.Outcome, Is.EqualTo(BattleOutcome.Draw));
            Assert.That(playerState.CurrentHealth, Is.Zero);
            Assert.That(
                service.IsEncounterDefeated("Floor1_Cell_A", "wanderer-01"),
                Is.True);
            Assert.That(
                service.CanReturnToExploration(out string returnError),
                Is.False);
            Assert.That(returnError, Does.Contain("Mutual Destruction"));
        }

        private void BeginValidBattle(BattleSessionService service)
        {
            BeginBattle(
                service,
                CreateEncounter("enemy.wanderer"),
                CreatePreparedLoadout());
        }

        private static void BeginBattle(
            BattleSessionService service,
            SoloComboEncounterDefinitionAsset encounter,
            SoloComboLoadoutSession loadout)
        {
            bool began = service.TryBeginBattle(
                encounter,
                loadout,
                "wanderer-01",
                "Floor1_Cell_A",
                "CombatSandbox",
                Vector3.zero,
                Quaternion.identity,
                out string error);
            Assert.That(began, Is.True, error);
        }

        private BattleSessionService CreateService(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            BattleSessionService service = gameObject.AddComponent<BattleSessionService>();

            Assert.That(AwakeMethod, Is.Not.Null);
            AwakeMethod.Invoke(service, null);
            return service;
        }

        private SoloComboEncounterDefinitionAsset CreateEncounter(string encounterId)
        {
            var encounter = ScriptableObject.CreateInstance<SoloComboEncounterDefinitionAsset>();
            _createdObjects.Add(encounter);
            encounter.Configure(
                encounterId,
                "Wandering Enemy",
                "Test encounter",
                "Wanderer",
                100,
                50,
                50,
                1,
                30,
                new EncounterItemAuthoringData[0],
                new EnemySequenceAuthoringData[0]);
            return encounter;
        }

        private SoloComboEncounterDefinitionAsset CreateValidEncounter(
            int enemyMaximumHealth = 100,
            int enemyAttack = 1,
            float enemyDamageMultiplier = 0.01f)
        {
            var encounter = ScriptableObject.CreateInstance<SoloComboEncounterDefinitionAsset>();
            _createdObjects.Add(encounter);
            encounter.Configure(
                "enemy.wanderer",
                "Wandering Enemy",
                "Test encounter",
                "Wanderer",
                enemyMaximumHealth,
                enemyAttack,
                0,
                1,
                30,
                new EncounterItemAuthoringData[0],
                new[]
                {
                    new EnemySequenceAuthoringData(
                        "enemy.wait",
                        "Wait",
                        new EnemyActionAuthoringData(
                            "enemy.wait.action",
                            "Wait",
                            timelineCost: 1,
                            resolutionEffect: ActionResolutionEffect.Damage,
                            damageMultiplier: enemyDamageMultiplier))
                });
            return encounter;
        }

        private SoloComboLoadoutSession CreatePreparedLoadout(
            WeaponDefinition weapon = null,
            double damageMultiplier = 0.1d)
        {
            SoloComboLoadoutSession loadout = CreateLoadout();
            var main = new ComboActionDefinition(
                "main",
                "Main",
                ComboActionCategory.Main,
                timelineCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: damageMultiplier,
                resolutionOffset: 1);
            loadout.Prepare(
                new[]
                {
                    new ComboConfiguration("combo1", null, main, null, null, null)
                },
                weapon);
            return loadout;
        }

        private SoloComboLoadoutSession CreateLoadout()
        {
            var loadout = ScriptableObject.CreateInstance<SoloComboLoadoutSession>();
            _createdObjects.Add(loadout);
            return loadout;
        }

        private static void EquipStartingGear(PersistentPlayerState playerState)
        {
            playerState.Equipment.Grant(
                SER494.Gameplay.PlayerState.EquipmentCategory.Weapon,
                "weapon.greatsword.rusted-iron-sword");
            playerState.Equipment.EquipWeapon("weapon.greatsword.rusted-iron-sword");

            EquipArmor(playerState, "armor.head", "armor.linen-headpiece");
            EquipArmor(playerState, "armor.upper-body", "armor.linen-shirt");
            EquipArmor(playerState, "armor.lower-body", "armor.linen-hose");
        }

        private static void EquipArmor(
            PersistentPlayerState playerState,
            string slotId,
            string armorId)
        {
            playerState.Equipment.Grant(
                SER494.Gameplay.PlayerState.EquipmentCategory.Armor,
                armorId);
            playerState.Equipment.EquipArmor(slotId, armorId);
        }
    }
}
