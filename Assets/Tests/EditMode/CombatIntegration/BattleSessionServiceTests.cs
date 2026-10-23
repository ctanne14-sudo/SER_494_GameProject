using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.CombatIntegration.Unity;
using UnityEngine;

namespace SER494.Gameplay.CombatIntegration.Tests
{
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
        public void DuplicateService_DoesNotReplaceAuthority()
        {
            BattleSessionService first = CreateService("Primary Battle Sessions");
            BattleSessionService duplicate = CreateService("Duplicate Battle Sessions");

            Assert.That(BattleSessionService.Active, Is.SameAs(first));
            Assert.That(first.IsAuthority, Is.True);
            Assert.That(duplicate.IsAuthority, Is.False);
            Assert.That(duplicate.enabled, Is.False);
        }

        private void BeginValidBattle(BattleSessionService service)
        {
            bool began = service.TryBeginBattle(
                CreateEncounter("enemy.wanderer"),
                CreatePreparedLoadout(),
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

        private SoloComboLoadoutSession CreatePreparedLoadout()
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
                damageMultiplier: 0.1d);
            loadout.Prepare(new[]
            {
                new ComboConfiguration("combo1", null, main, null, null, null)
            });
            return loadout;
        }

        private SoloComboLoadoutSession CreateLoadout()
        {
            var loadout = ScriptableObject.CreateInstance<SoloComboLoadoutSession>();
            _createdObjects.Add(loadout);
            return loadout;
        }
    }
}
