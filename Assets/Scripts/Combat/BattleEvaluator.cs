using System;
using System.Collections.Generic;

namespace SER494.Gameplay.Combat
{
    public enum BattleOutcome
    {
        Ongoing,
        PlayerVictory,
        PlayerDefeat,
        Draw
    }

    public static class BattleEvaluator
    {
        public static BattleOutcome Evaluate(
            IReadOnlyList<CombatantState> playerCombatants,
            IReadOnlyList<CombatantState> enemyCombatants)
        {
            ValidateTeam(playerCombatants, nameof(playerCombatants));
            ValidateTeam(enemyCombatants, nameof(enemyCombatants));

            bool allPlayersDefeated = AreAllDefeated(playerCombatants);
            bool allEnemiesDefeated = AreAllDefeated(enemyCombatants);

            if (allPlayersDefeated && allEnemiesDefeated)
            {
                return BattleOutcome.Draw;
            }

            if (allEnemiesDefeated)
            {
                return BattleOutcome.PlayerVictory;
            }

            if (allPlayersDefeated)
            {
                return BattleOutcome.PlayerDefeat;
            }

            return BattleOutcome.Ongoing;
        }

        private static bool AreAllDefeated(IReadOnlyList<CombatantState> combatants)
        {
            for (int index = 0; index < combatants.Count; index++)
            {
                if (!combatants[index].IsDefeated)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidateTeam(IReadOnlyList<CombatantState> combatants, string parameterName)
        {
            if (combatants == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (combatants.Count == 0)
            {
                throw new ArgumentException("A battle team must contain at least one combatant.", parameterName);
            }

            for (int index = 0; index < combatants.Count; index++)
            {
                if (combatants[index] == null)
                {
                    throw new ArgumentException("A team cannot contain a null combatant.", parameterName);
                }
            }
        }
    }
}
