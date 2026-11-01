using NUnit.Framework;
using SER494.Gameplay.Combat.Damage;
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
                },
                newRewardGold: 200,
                newRewardMaterialRollCount: 3,
                newRewardMaterialDrops: new[]
                {
                    new EncounterRewardDropAuthoringData("material.white-copper", 30),
                    new EncounterRewardDropAuthoringData("material.bronze", 35),
                    new EncounterRewardDropAuthoringData("material.brass", 35)
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
            Assert.That(definition.Rewards.Gold, Is.EqualTo(200));
            Assert.That(definition.Rewards.MaterialRollCount, Is.EqualTo(3));
            Assert.That(definition.Rewards.TotalMaterialWeight, Is.EqualTo(100));
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
        public void TryCreateDefinition_DuplicateRewardMaterial_ReturnsValidationError()
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
                new EncounterItemAuthoringData[0],
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
                },
                newRewardGold: 200,
                newRewardMaterialRollCount: 3,
                newRewardMaterialDrops: new[]
                {
                    new EncounterRewardDropAuthoringData("material.bronze", 30),
                    new EncounterRewardDropAuthoringData("material.bronze", 70)
                });

            bool succeeded = _asset.TryCreateDefinition(
                out SoloComboEncounterDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.False);
            Assert.That(definition, Is.Null);
            Assert.That(validationError, Does.Contain("duplicated"));
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

        [Test]
        public void TryCreateDefinition_RichEnemyAction_PreservesEventsWindowsAndCancelResponse()
        {
            var damageReduction = new CombatStatusDefinition(
                "status.test.damage-reduction",
                "Damage Reduction",
                CombatStatusDurationType.TimeSteps,
                20,
                CombatStatusModifierDirection.DamageTaken,
                DamageModifier.All(-0.20d),
                CombatStatusStackingPolicy.IndependentInstances);
            _asset.Configure(
                "encounter.rich-enemy",
                "Rich Enemy",
                string.Empty,
                "Rich Enemy",
                250,
                50,
                30,
                1,
                10,
                new EncounterItemAuthoringData[0],
                new[]
                {
                    new EnemySequenceAuthoringData(
                        "round.one",
                        "Round One",
                        new EnemyActionAuthoringData(
                            "action.rich",
                            "Rich Action",
                            20,
                            new[]
                            {
                                new EnemyResolutionEventAuthoringData(
                                    3,
                                    ActionResolutionEffect.ApplyBuff,
                                    statusEffect: damageReduction,
                                    statusTarget: ActionStatusTarget.Self),
                                new EnemyResolutionEventAuthoringData(
                                    6,
                                    ActionResolutionEffect.Damage,
                                    1.00f,
                                    DamageType.Slashing),
                                new EnemyResolutionEventAuthoringData(
                                    9,
                                    ActionResolutionEffect.Guard)
                            },
                            new[]
                            {
                                new EnemyActiveWindowAuthoringData(
                                    "action.rich.super-armor",
                                    "Super Armor III",
                                    1,
                                    20,
                                    ActiveWindowEffect.SuperArmor,
                                    superArmorLevel: 3)
                            }))
                });

            bool succeeded = _asset.TryCreateDefinition(
                out SoloComboEncounterDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.True, validationError);
            var player = new CombatantState(
                "Player",
                new CombatantStats(100, 30, 30, 50, 50));
            var enemy = new CombatantState(
                definition.EnemyDisplayName,
                definition.EnemyStats);
            EnemyActionDefinition action = definition.EnemyBehavior.SelectSequence(
                player,
                enemy,
                previousSequenceId: null,
                normalizedRoll: 0d).Actions[0];

            Assert.That(action.ResolutionEvents.Count, Is.EqualTo(3));
            Assert.That(action.ResolutionEvents[0].Effect,
                Is.EqualTo(ActionResolutionEffect.ApplyBuff));
            Assert.That(action.ResolutionEvents[0].StatusEffect.Duration, Is.EqualTo(20));
            Assert.That(action.ResolutionEvents[1].Offset, Is.EqualTo(6));
            Assert.That(action.ResolutionEvents[2].Effect,
                Is.EqualTo(ActionResolutionEffect.Guard));
            Assert.That(action.ActiveWindows.Count, Is.EqualTo(1));
            Assert.That(action.ActiveWindows[0].SuperArmorLevel, Is.EqualTo(3));
            Assert.That(
                action.DisruptionResponseSegments[0].StaggerResponse,
                Is.EqualTo(ActionDisruptionResponse.CancelAction));
        }
    }
}
