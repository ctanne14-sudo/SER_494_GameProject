using NUnit.Framework;

namespace SER494.Gameplay.Combat.Tests
{
    public sealed class CombatantStateTests
    {
        [Test]
        public void Resources_CanBeRestoredButNeverExceedMaximums()
        {
            CombatantState actor = CreateCombatant();
            CombatAction action = new CombatAction("Resource Test", power: 1, manaCost: 4, staminaCost: 6);
            CombatantState target = CreateCombatant();

            CombatResolver.Execute(actor, target, action);
            actor.RestoreMana(100);
            actor.RestoreStamina(100);

            Assert.That(actor.CurrentMana, Is.EqualTo(actor.Stats.MaxMana));
            Assert.That(actor.CurrentStamina, Is.EqualTo(actor.Stats.MaxStamina));
        }

        [Test]
        public void ReceiveDamage_LethalDamageClampsHealthToZero()
        {
            CombatantState combatant = CreateCombatant();

            combatant.ReceiveDamage(500);

            Assert.That(combatant.CurrentHealth, Is.Zero);
            Assert.That(combatant.IsDefeated, Is.True);
        }

        private static CombatantState CreateCombatant()
        {
            return new CombatantState(
                "Combatant",
                new CombatantStats(100, 10, 10, 5, 2, 5));
        }
    }
}
