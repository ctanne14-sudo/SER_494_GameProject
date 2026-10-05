using NUnit.Framework;

namespace SER494.Gameplay.Combat.Tests
{
    public sealed class BattleEvaluatorTests
    {
        [Test]
        public void Evaluate_LivingCombatantsOnBothSides_ReturnsOngoing()
        {
            BattleOutcome outcome = BattleEvaluator.Evaluate(
                new[] { CreateCombatant("Player") },
                new[] { CreateCombatant("Enemy") });

            Assert.That(outcome, Is.EqualTo(BattleOutcome.Ongoing));
        }

        [Test]
        public void Evaluate_AllEnemiesDefeated_ReturnsPlayerVictory()
        {
            CombatantState enemy = CreateCombatant("Enemy");
            enemy.ReceiveDamage(100);

            BattleOutcome outcome = BattleEvaluator.Evaluate(
                new[] { CreateCombatant("Player") },
                new[] { enemy });

            Assert.That(outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
        }

        [Test]
        public void Evaluate_AllPlayersDefeated_ReturnsPlayerDefeat()
        {
            CombatantState player = CreateCombatant("Player");
            player.ReceiveDamage(100);

            BattleOutcome outcome = BattleEvaluator.Evaluate(
                new[] { player },
                new[] { CreateCombatant("Enemy") });

            Assert.That(outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
        }

        private static CombatantState CreateCombatant(string name)
        {
            return new CombatantState(
                name,
                new CombatantStats(100, 10, 10, 5, 2, 5));
        }
    }
}
