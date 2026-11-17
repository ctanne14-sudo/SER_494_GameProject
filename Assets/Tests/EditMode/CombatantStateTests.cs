using NUnit.Framework;
using SER494.Gameplay.Combat.Damage;

namespace SER494.Gameplay.Combat.Tests
{
    [Category("CoreCombat")]
    public sealed class CombatantStateTests
    {
        [Test]
        public void Resources_CanBePaidAndRestoredButNeverExceedMaximums()
        {
            CombatantState actor = CreateCombatant();

            actor.Pay(manaCost: 4, staminaCost: 6);
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

        [Test]
        public void DamageModifiers_AreOwnedAndRemovedByDirection()
        {
            CombatantState combatant = CreateCombatant();
            DamageModifier dealt = DamageModifier.ForType(DamageType.Slashing, 0.20d);
            DamageModifier taken = DamageModifier.ForFamily(DamageFamily.Magical, -0.10d);

            combatant.AddDamageDealtModifier(dealt);
            combatant.AddDamageTakenModifier(taken);

            Assert.That(combatant.DamageDealtModifiers, Is.EqualTo(new[] { dealt }));
            Assert.That(combatant.DamageTakenModifiers, Is.EqualTo(new[] { taken }));
            Assert.That(combatant.RemoveDamageDealtModifier(dealt), Is.True);
            Assert.That(combatant.RemoveDamageTakenModifier(taken), Is.True);
            Assert.That(combatant.DamageDealtModifiers, Is.Empty);
            Assert.That(combatant.DamageTakenModifiers, Is.Empty);
        }

        private static CombatantState CreateCombatant()
        {
            return new CombatantState(
                "Combatant",
                new CombatantStats(100, 10, 10, 5, 2));
        }
    }
}
