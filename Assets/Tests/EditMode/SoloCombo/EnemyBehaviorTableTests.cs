using System;
using NUnit.Framework;

namespace SER494.Gameplay.Combat.SoloCombo.Tests
{
    public sealed class EnemyBehaviorTableTests
    {
        [Test]
        public void SelectSequence_UsesConfiguredWeights()
        {
            EnemyActionSequence first = Sequence("first");
            EnemyActionSequence second = Sequence("second");
            EnemyBehaviorTable table = Table(
                new EnemyBehaviorEntry(first, selectionWeight: 1),
                new EnemyBehaviorEntry(second, selectionWeight: 3));
            CombatantState player = Combatant("Player");
            CombatantState enemy = Combatant("Enemy");

            Assert.That(
                table.SelectSequence(player, enemy, null, normalizedRoll: 0d),
                Is.SameAs(first));
            Assert.That(
                table.SelectSequence(player, enemy, null, normalizedRoll: 0.25d),
                Is.SameAs(second));
        }

        [Test]
        public void SelectSequence_FiltersByCurrentHealthRatios()
        {
            EnemyActionSequence healthy = Sequence("healthy");
            EnemyActionSequence desperate = Sequence("desperate");
            EnemyBehaviorTable table = Table(
                new EnemyBehaviorEntry(
                    healthy,
                    selectionWeight: 1,
                    minimumEnemyHealthRatio: 0.51d),
                new EnemyBehaviorEntry(
                    desperate,
                    selectionWeight: 1,
                    maximumEnemyHealthRatio: 0.50d));
            CombatantState player = Combatant("Player");
            CombatantState enemy = Combatant("Enemy");

            Assert.That(
                table.SelectSequence(player, enemy, null, normalizedRoll: 0d),
                Is.SameAs(healthy));

            enemy.ReceiveDamage(50);
            Assert.That(
                table.SelectSequence(player, enemy, healthy.SequenceId, normalizedRoll: 0d),
                Is.SameAs(desperate));
        }

        [Test]
        public void SelectSequence_CanPreventImmediateRepeat()
        {
            EnemyActionSequence first = Sequence("first");
            EnemyActionSequence second = Sequence("second");
            EnemyBehaviorTable table = Table(
                new EnemyBehaviorEntry(first, 100, preventImmediateRepeat: true),
                new EnemyBehaviorEntry(second, 1, preventImmediateRepeat: true));

            EnemyActionSequence selected = table.SelectSequence(
                Combatant("Player"),
                Combatant("Enemy"),
                previousSequenceId: first.SequenceId,
                normalizedRoll: 0d);

            Assert.That(selected, Is.SameAs(second));
        }

        [Test]
        public void SelectSequence_WhenNothingIsEligible_UsesMandatoryFallback()
        {
            EnemyActionSequence ordinary = Sequence("ordinary");
            EnemyActionSequence fallback = Sequence("fallback");
            var table = new EnemyBehaviorTable(
                new[]
                {
                    new EnemyBehaviorEntry(
                        ordinary,
                        selectionWeight: 1,
                        maximumEnemyHealthRatio: 0.5d)
                },
                fallback);

            EnemyActionSequence selected = table.SelectSequence(
                Combatant("Player"),
                Combatant("Enemy"),
                previousSequenceId: null,
                normalizedRoll: 0.5d);

            Assert.That(selected, Is.SameAs(fallback));
        }

        [Test]
        public void Constructor_RejectsDuplicateSequenceIds()
        {
            EnemyActionSequence sequence = Sequence("duplicate");

            Assert.That(
                () => new EnemyBehaviorTable(
                    new[]
                    {
                        new EnemyBehaviorEntry(sequence, 1),
                        new EnemyBehaviorEntry(sequence, 2)
                    },
                    Sequence("fallback")),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Scripted_SelectSequence_UsesConfiguredOrderAndLoops()
        {
            EnemyActionSequence first = Sequence("first");
            EnemyActionSequence second = Sequence("second");
            EnemyBehaviorTable table = EnemyBehaviorTable.Scripted(
                new[] { first, second });
            CombatantState player = Combatant("Player");
            CombatantState enemy = Combatant("Enemy");

            Assert.That(
                table.SelectSequence(player, enemy, null, normalizedRoll: 0.9d),
                Is.SameAs(first));
            Assert.That(
                table.SelectSequence(player, enemy, first.SequenceId, normalizedRoll: 0.1d),
                Is.SameAs(second));
            Assert.That(
                table.SelectSequence(player, enemy, second.SequenceId, normalizedRoll: 0.5d),
                Is.SameAs(first));
        }

        [Test]
        public void Scripted_RejectsDuplicateSequenceIds()
        {
            EnemyActionSequence sequence = Sequence("duplicate");

            Assert.That(
                () => EnemyBehaviorTable.Scripted(new[] { sequence, sequence }),
                Throws.TypeOf<ArgumentException>());
        }

        private static EnemyBehaviorTable Table(params EnemyBehaviorEntry[] entries)
        {
            return new EnemyBehaviorTable(entries, Sequence("fallback"));
        }

        private static EnemyActionSequence Sequence(string id)
        {
            return new EnemyActionSequence(
                id,
                id,
                new[]
                {
                    new EnemyActionDefinition(
                        $"{id}-action",
                        id,
                        timelineCost: 1,
                        resolutionOffset: 1,
                        damageMultiplier: 0d)
                });
        }

        private static CombatantState Combatant(string name)
        {
            return new CombatantState(
                name,
                new CombatantStats(100, 0, 0, 50, 50));
        }
    }
}
