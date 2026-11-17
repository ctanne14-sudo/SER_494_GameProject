using NUnit.Framework;
using SER494.Gameplay.Combat.Damage;

namespace SER494.Gameplay.Combat.Tests
{
    [Category("CoreCombat")]
    public sealed class CombatStatusTests
    {
        [Test]
        public void ApplyStatus_AddsModifierToConfiguredDirection()
        {
            CombatantState combatant = CreateCombatant();
            CombatStatusDefinition status = Status(
                "exposed",
                rounds: 2,
                CombatStatusModifierDirection.DamageTaken,
                0.20d);

            combatant.ApplyStatus(status);

            Assert.That(combatant.ActiveStatuses, Has.Count.EqualTo(1));
            Assert.That(combatant.DamageDealtModifiers, Is.Empty);
            Assert.That(combatant.DamageTakenModifiers, Has.Count.EqualTo(1));
            Assert.That(combatant.DamageTakenModifiers[0].Amount, Is.EqualTo(0.20d));
        }

        [Test]
        public void ApplyStatus_WithSameId_RefreshesInsteadOfStacking()
        {
            CombatantState combatant = CreateCombatant();
            CombatStatusDefinition status = Status(
                "focus",
                rounds: 2,
                CombatStatusModifierDirection.DamageDealt,
                0.10d);
            combatant.ApplyStatus(status);
            combatant.AdvanceRoundStatuses();

            combatant.ApplyStatus(status);

            Assert.That(combatant.ActiveStatuses, Has.Count.EqualTo(1));
            Assert.That(combatant.ActiveStatuses[0].RemainingDuration, Is.EqualTo(2));
            Assert.That(combatant.DamageDealtModifiers, Has.Count.EqualTo(1));
        }

        [Test]
        public void ApplyStatus_IndependentInstancesStackAndCanBeConsumedOneAtATime()
        {
            CombatantState combatant = CreateCombatant();
            var status = new CombatStatusDefinition(
                "power-sheathe",
                "Power Sheathe",
                CombatStatusDurationType.TimeSteps,
                duration: 30,
                CombatStatusModifierDirection.DamageDealt,
                DamageModifier.ForType(DamageType.Slashing, 0.10d),
                CombatStatusStackingPolicy.IndependentInstances);

            combatant.ApplyStatus(status);
            combatant.ApplyStatus(status);

            Assert.That(combatant.ActiveStatuses, Has.Count.EqualTo(2));
            Assert.That(combatant.DamageDealtModifiers, Has.Count.EqualTo(2));
            Assert.That(combatant.HasStatus("power-sheathe"), Is.True);
            Assert.That(combatant.ConsumeStatus("power-sheathe"), Is.True);
            Assert.That(combatant.ActiveStatuses, Has.Count.EqualTo(1));
            Assert.That(combatant.ConsumeStatus("power-sheathe"), Is.True);
            Assert.That(combatant.HasStatus("power-sheathe"), Is.False);
        }

        [Test]
        public void AdvanceRoundStatuses_RemovesStatusAtZero()
        {
            CombatantState combatant = CreateCombatant();
            combatant.ApplyStatus(Status(
                "brief",
                rounds: 1,
                CombatStatusModifierDirection.DamageTaken,
                0.20d));

            combatant.AdvanceRoundStatuses();

            Assert.That(combatant.ActiveStatuses, Is.Empty);
            Assert.That(combatant.DamageTakenModifiers, Is.Empty);
        }

        [Test]
        public void AdvanceTimeStepStatuses_OnlyAdvancesTimeStepDurations()
        {
            CombatantState combatant = CreateCombatant();
            var timed = new CombatStatusDefinition(
                "timed",
                "Timed",
                CombatStatusDurationType.TimeSteps,
                duration: 3,
                CombatStatusModifierDirection.DamageTaken,
                DamageModifier.All(0.10d));
            combatant.ApplyStatus(timed);

            combatant.AdvanceRoundStatuses();
            combatant.AdvanceTimeStepStatuses(2);

            Assert.That(combatant.ActiveStatuses[0].RemainingDuration, Is.EqualTo(1));
        }

        private static CombatantState CreateCombatant()
        {
            return new CombatantState(
                "Combatant",
                new CombatantStats(100, 10, 10, 50, 50));
        }

        private static CombatStatusDefinition Status(
            string id,
            int rounds,
            CombatStatusModifierDirection direction,
            double amount)
        {
            return new CombatStatusDefinition(
                id,
                id,
                CombatStatusDurationType.Rounds,
                rounds,
                direction,
                DamageModifier.All(amount));
        }
    }
}
