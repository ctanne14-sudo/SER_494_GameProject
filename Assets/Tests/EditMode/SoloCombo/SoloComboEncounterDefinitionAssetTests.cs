using NUnit.Framework;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using UnityEngine;

namespace SER494.Gameplay.Combat.SoloCombo.Tests
{
    [Category("CoreCombat")]
    [Category("Authoring")]
    public sealed class SoloComboEncounterDefinitionAssetTests
    {
        private SoloComboEncounterDefinitionAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<SoloComboEncounterDefinitionAsset>();
            _asset.name = "Test Encounter";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void TryCreateDefinition_ValidScriptedEncounter_CopiesRuntimeData()
        {
            _asset.Configure(
                "encounter.test",
                "Test Encounter",
                "A deterministic test.",
                "Training Target",
                newEnemyMaximumHealth: 450,
                newEnemyAttack: 45,
                newEnemyDefense: 55,
                newManaRecoveryPerTurn: 1,
                newStaminaRecoveryPerTurn: 30,
                newStartingItems: new[]
                {
                    new EncounterItemAuthoringData("healing-potion", 2)
                },
                newScriptedSequences: new[]
                {
                    new EnemySequenceAuthoringData(
                        "sequence.one",
                        "Round One",
                        new EnemyActionAuthoringData(
                            "action.damage",
                            "Damage",
                            5,
                            ActionResolutionEffect.Damage,
                            damageMultiplier: 0.5f)),
                    new EnemySequenceAuthoringData(
                        "sequence.two",
                        "Round Two",
                        new EnemyActionAuthoringData(
                            "action.guard",
                            "Guard",
                            3,
                            ActionResolutionEffect.Guard))
                });

            bool succeeded = _asset.TryCreateDefinition(
                out SoloComboEncounterDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.True, validationError);
            Assert.That(definition.EncounterId, Is.EqualTo("encounter.test"));
            Assert.That(definition.DisplayName, Is.EqualTo("Test Encounter"));
            Assert.That(definition.EnemyDisplayName, Is.EqualTo("Training Target"));
            Assert.That(definition.EnemyStats.MaxHealth, Is.EqualTo(450));
            Assert.That(definition.EnemyStats.Attack, Is.EqualTo(45));
            Assert.That(definition.EnemyStats.Defense, Is.EqualTo(55));
            Assert.That(definition.ManaRecoveryPerTurn, Is.EqualTo(1));
            Assert.That(definition.StaminaRecoveryPerTurn, Is.EqualTo(30));
            Assert.That(definition.StartingItems["healing-potion"], Is.EqualTo(2));
            var player = new CombatantState(
                "Player",
                new CombatantStats(100, 30, 30, 50, 50));
            var enemy = new CombatantState(
                definition.EnemyDisplayName,
                definition.EnemyStats);
            Assert.That(
                definition.EnemyBehavior.SelectSequence(
                    player,
                    enemy,
                    previousSequenceId: null,
                    normalizedRoll: 0d).SequenceId,
                Is.EqualTo("sequence.one"));
        }

        [Test]
        public void TryCreateDefinition_DuplicateStartingItem_ReturnsValidationError()
        {
            _asset.Configure(
                "encounter.test",
                "Test Encounter",
                string.Empty,
                "Training Target",
                100,
                50,
                50,
                1,
                1,
                new[]
                {
                    new EncounterItemAuthoringData("potion", 1),
                    new EncounterItemAuthoringData("potion", 2)
                },
                new[]
                {
                    new EnemySequenceAuthoringData(
                        "sequence.one",
                        "Round One",
                        new EnemyActionAuthoringData(
                            "action.guard",
                            "Guard",
                            3,
                            ActionResolutionEffect.Guard))
                });

            bool succeeded = _asset.TryCreateDefinition(
                out SoloComboEncounterDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.False);
            Assert.That(definition, Is.Null);
            Assert.That(validationError, Does.Contain("duplicate"));
        }
    }
}
