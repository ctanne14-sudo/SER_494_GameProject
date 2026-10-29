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
            IReadOnlyDictionary<string, int> startingItems = null,
            EncounterRewardDefinition rewards = null)
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
            Rewards = rewards ?? EncounterRewardDefinition.None;
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

        public EncounterRewardDefinition Rewards { get; }

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

    public sealed class EncounterRewardDefinition
    {
        public static readonly EncounterRewardDefinition None =
            new EncounterRewardDefinition(0, 0, Array.Empty<WeightedMaterialReward>());

        private readonly WeightedMaterialReward[] _materialDrops;

        public EncounterRewardDefinition(
            int gold,
            int materialRollCount,
            IReadOnlyList<WeightedMaterialReward> materialDrops)
        {
            if (gold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gold));
            }

            if (materialRollCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(materialRollCount));
            }

            if (materialDrops == null)
            {
                throw new ArgumentNullException(nameof(materialDrops));
            }

            var copiedDrops = new WeightedMaterialReward[materialDrops.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            int totalWeight = 0;
            for (int index = 0; index < materialDrops.Count; index++)
            {
                WeightedMaterialReward drop = materialDrops[index]
                    ?? throw new ArgumentException(
                        "Encounter reward entries cannot be null.",
                        nameof(materialDrops));
                if (!ids.Add(drop.ItemId))
                {
                    throw new ArgumentException(
                        $"Encounter reward item '{drop.ItemId}' is duplicated.",
                        nameof(materialDrops));
                }

                totalWeight = checked(totalWeight + drop.Weight);
                copiedDrops[index] = drop;
            }

            if (materialRollCount > 0 && totalWeight <= 0)
            {
                throw new ArgumentException(
                    "Encounter material rolls require at least one weighted reward.",
                    nameof(materialDrops));
            }

            Gold = gold;
            MaterialRollCount = materialRollCount;
            TotalMaterialWeight = totalWeight;
            _materialDrops = copiedDrops;
        }

        public int Gold { get; }

        public int MaterialRollCount { get; }

        public int TotalMaterialWeight { get; }

        public IReadOnlyList<WeightedMaterialReward> MaterialDrops => _materialDrops;

        public IReadOnlyDictionary<string, int> RollMaterials(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var results = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int rollIndex = 0; rollIndex < MaterialRollCount; rollIndex++)
            {
                int roll = random.Next(TotalMaterialWeight);
                int cumulativeWeight = 0;
                for (int dropIndex = 0; dropIndex < _materialDrops.Length; dropIndex++)
                {
                    WeightedMaterialReward drop = _materialDrops[dropIndex];
                    cumulativeWeight += drop.Weight;
                    if (roll >= cumulativeWeight)
                    {
                        continue;
                    }

                    results.TryGetValue(drop.ItemId, out int current);
                    results[drop.ItemId] = current + 1;
                    break;
                }
            }

            return results;
        }
    }

    public sealed class WeightedMaterialReward
    {
        public WeightedMaterialReward(string itemId, int weight)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException(
                    "A reward item requires a stable ID.",
                    nameof(itemId));
            }

            if (weight <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(weight),
                    weight,
                    "Reward weight must be positive.");
            }

            ItemId = itemId.Trim();
            Weight = weight;
        }

        public string ItemId { get; }

        public int Weight { get; }
    }
}
