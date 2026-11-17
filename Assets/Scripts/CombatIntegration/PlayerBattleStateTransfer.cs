using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SER494.Gameplay.Combat;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.CombatIntegration
{
    public static class PlayerBattleStateTransfer
    {
        public static CombatantState CreateCombatant(
            PersistentPlayerState playerState,
            string displayName,
            int attack,
            int defense)
        {
            if (playerState == null)
            {
                throw new ArgumentNullException(nameof(playerState));
            }

            var stats = new CombatantStats(
                playerState.MaximumHealth,
                playerState.MaximumMana,
                playerState.MaximumCombatStamina,
                attack,
                defense);

            return new CombatantState(
                displayName,
                stats,
                playerState.CurrentHealth,
                playerState.CurrentMana,
                playerState.CurrentCombatStamina);
        }

        public static IReadOnlyDictionary<string, int> CaptureInventory(
            PersistentPlayerState playerState)
        {
            if (playerState == null)
            {
                throw new ArgumentNullException(nameof(playerState));
            }

            var copy = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, int> item in playerState.Inventory.ItemCounts)
            {
                copy.Add(item.Key, item.Value);
            }

            return new ReadOnlyDictionary<string, int>(copy);
        }

        public static void ApplyBattleResult(
            PersistentPlayerState playerState,
            CombatantState playerCombatant,
            IReadOnlyDictionary<string, int> remainingItems)
        {
            if (playerState == null)
            {
                throw new ArgumentNullException(nameof(playerState));
            }

            if (playerCombatant == null)
            {
                throw new ArgumentNullException(nameof(playerCombatant));
            }

            if (remainingItems == null)
            {
                throw new ArgumentNullException(nameof(remainingItems));
            }

            RequireMatchingPersistentMaximums(playerState, playerCombatant);
            ValidateItems(remainingItems);

            playerState.SetCurrentHealth(playerCombatant.CurrentHealth);
            playerState.SetCurrentMana(playerCombatant.CurrentMana);
            playerState.SetCurrentCombatStamina(playerCombatant.CurrentStamina);
            foreach (KeyValuePair<string, int> item in remainingItems)
            {
                playerState.Inventory.SetCount(item.Key, item.Value);
            }
        }

        private static void RequireMatchingPersistentMaximums(
            PersistentPlayerState playerState,
            CombatantState playerCombatant)
        {
            if (playerCombatant.Stats.MaxHealth != playerState.MaximumHealth
                || playerCombatant.Stats.MaxMana != playerState.MaximumMana
                || playerCombatant.Stats.MaxStamina != playerState.MaximumCombatStamina)
            {
                throw new InvalidOperationException(
                    "Battle results cannot be applied after the player's persistent "
                    + "Health, Mana, or Stamina maximum changed during battle.");
            }
        }

        private static void ValidateItems(IReadOnlyDictionary<string, int> items)
        {
            foreach (KeyValuePair<string, int> item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    throw new ArgumentException(
                        "Battle inventory contains an empty item ID.",
                        nameof(items));
                }

                if (item.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(items),
                        item.Value,
                        "Battle item counts cannot be negative.");
                }
            }
        }
    }
}
