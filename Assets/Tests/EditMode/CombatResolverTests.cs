using NUnit.Framework;

namespace SER494.Gameplay.Combat.Tests
{
    public sealed class CombatResolverTests
    {
        [Test]
        public void Execute_ValidAction_DealsDamageAndSpendsResources()
        {
            CombatantState actor = CreateCombatant("Player", attack: 8, defense: 2);
            CombatantState target = CreateCombatant("Enemy", attack: 5, defense: 3);
            CombatAction action = new CombatAction("Heavy Strike", power: 5, manaCost: 2, staminaCost: 4);

            CombatActionResult result = CombatResolver.Execute(actor, target, action);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.DamageDealt, Is.EqualTo(10));
            Assert.That(target.CurrentHealth, Is.EqualTo(90));
            Assert.That(actor.CurrentMana, Is.EqualTo(8));
            Assert.That(actor.CurrentStamina, Is.EqualTo(6));
        }

        [Test]
        public void Execute_InsufficientMana_DoesNotChangeCombatState()
        {
            CombatantState actor = CreateCombatant("Player", attack: 8, defense: 2);
            CombatantState target = CreateCombatant("Enemy", attack: 5, defense: 3);
            CombatAction action = new CombatAction("Expensive Spell", power: 20, manaCost: 11, staminaCost: 0);

            CombatActionResult result = CombatResolver.Execute(actor, target, action);

            Assert.That(result.Failure, Is.EqualTo(CombatActionFailure.InsufficientMana));
            Assert.That(target.CurrentHealth, Is.EqualTo(100));
            Assert.That(actor.CurrentMana, Is.EqualTo(10));
        }

        private static CombatantState CreateCombatant(string name, int attack, int defense)
        {
            CombatantStats stats = new CombatantStats(
                maxHealth: 100,
                maxMana: 10,
                maxStamina: 10,
                attack,
                defense,
                speed: 5);

            return new CombatantState(name, stats);
        }
    }
}
