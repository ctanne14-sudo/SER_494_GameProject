using System;
using System.Collections.Generic;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public sealed class SoloComboEncounterDefinition
    {
        private readonly IReadOnlyDictionary<string, int> _startingItems;

        public SoloComboEncounterDefinition(
            string encounterId,
            string displayName,
            string description,
            string enemyDisplayName,
            CombatantStats enemyStats,
            EnemyBehaviorTable enemyBehavior,
            int manaRecoveryPerTurn,
            int staminaRecoveryPerTurn,
            IReadOnlyDictionary<string, int> startingItems = null)
        {
            if (string.IsNullOrWhiteSpace(encounterId))
            {
                throw new ArgumentException(
                    "An Encounter must have a stable ID.",
                    nameof(encounterId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "An Encounter must have a display name.",
                    nameof(displayName));
            }

            if (enemyStats == null)
            {
                throw new ArgumentNullException(nameof(enemyStats));
            }

            if (string.IsNullOrWhiteSpace(enemyDisplayName))
            {
                throw new ArgumentException(
                    "An Encounter enemy must have a display name.",
                    nameof(enemyDisplayName));
            }

            if (enemyBehavior == null)
            {
                throw new ArgumentNullException(nameof(enemyBehavior));
            }

            if (manaRecoveryPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(manaRecoveryPerTurn));
            }

            if (staminaRecoveryPerTurn < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(staminaRecoveryPerTurn));
            }

            EncounterId = encounterId.Trim();
            DisplayName = displayName.Trim();
            Description = description?.Trim() ?? string.Empty;
            EnemyDisplayName = enemyDisplayName.Trim();
            EnemyStats = enemyStats;
            EnemyBehavior = enemyBehavior;
            ManaRecoveryPerTurn = manaRecoveryPerTurn;
            StaminaRecoveryPerTurn = staminaRecoveryPerTurn;
            _startingItems = CopyItems(startingItems);
        }

        public string EncounterId { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public string EnemyDisplayName { get; }

        public CombatantStats EnemyStats { get; }

        public EnemyBehaviorTable EnemyBehavior { get; }

        public int ManaRecoveryPerTurn { get; }

        public int StaminaRecoveryPerTurn { get; }

        public IReadOnlyDictionary<string, int> StartingItems => _startingItems;

        private static IReadOnlyDictionary<string, int> CopyItems(
            IReadOnlyDictionary<string, int> source)
        {
            var copy = new Dictionary<string, int>(StringComparer.Ordinal);
            if (source == null)
            {
                return copy;
            }

            foreach (KeyValuePair<string, int> pair in source)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    throw new ArgumentException(
                        "Encounter item IDs cannot be empty.",
                        nameof(source));
                }

                if (pair.Value <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(source),
                        "Encounter item quantities must be positive.");
                }

                copy.Add(pair.Key.Trim(), pair.Value);
            }

            return copy;
        }
    }
}
