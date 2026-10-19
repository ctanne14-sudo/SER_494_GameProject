using System;
using System.Collections.Generic;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public sealed class EnemyBehaviorEntry
    {
        public EnemyBehaviorEntry(
            EnemyActionSequence sequence,
            int selectionWeight,
            double minimumEnemyHealthRatio = 0d,
            double maximumEnemyHealthRatio = 1d,
            double minimumPlayerHealthRatio = 0d,
            double maximumPlayerHealthRatio = 1d,
            bool preventImmediateRepeat = false)
        {
            Sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            if (selectionWeight <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(selectionWeight),
                    selectionWeight,
                    "Enemy Sequence selection weight must be positive.");
            }

            ValidateRatioRange(
                minimumEnemyHealthRatio,
                maximumEnemyHealthRatio,
                nameof(minimumEnemyHealthRatio),
                nameof(maximumEnemyHealthRatio));
            ValidateRatioRange(
                minimumPlayerHealthRatio,
                maximumPlayerHealthRatio,
                nameof(minimumPlayerHealthRatio),
                nameof(maximumPlayerHealthRatio));

            SelectionWeight = selectionWeight;
            MinimumEnemyHealthRatio = minimumEnemyHealthRatio;
            MaximumEnemyHealthRatio = maximumEnemyHealthRatio;
            MinimumPlayerHealthRatio = minimumPlayerHealthRatio;
            MaximumPlayerHealthRatio = maximumPlayerHealthRatio;
            PreventImmediateRepeat = preventImmediateRepeat;
        }

        public EnemyActionSequence Sequence { get; }

        public int SelectionWeight { get; }

        public double MinimumEnemyHealthRatio { get; }

        public double MaximumEnemyHealthRatio { get; }

        public double MinimumPlayerHealthRatio { get; }

        public double MaximumPlayerHealthRatio { get; }

        public bool PreventImmediateRepeat { get; }

        public bool IsEligible(
            CombatantState player,
            CombatantState enemy,
            string previousSequenceId)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (enemy == null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            if (PreventImmediateRepeat
                && string.Equals(
                    Sequence.SequenceId,
                    previousSequenceId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            double enemyHealthRatio = enemy.CurrentHealth / (double)enemy.Stats.MaxHealth;
            double playerHealthRatio = player.CurrentHealth / (double)player.Stats.MaxHealth;
            return enemyHealthRatio >= MinimumEnemyHealthRatio
                && enemyHealthRatio <= MaximumEnemyHealthRatio
                && playerHealthRatio >= MinimumPlayerHealthRatio
                && playerHealthRatio <= MaximumPlayerHealthRatio;
        }

        private static void ValidateRatioRange(
            double minimum,
            double maximum,
            string minimumName,
            string maximumName)
        {
            if (double.IsNaN(minimum) || minimum < 0d || minimum > 1d)
            {
                throw new ArgumentOutOfRangeException(minimumName, minimum, "Ratio must be from 0 to 1.");
            }

            if (double.IsNaN(maximum) || maximum < 0d || maximum > 1d)
            {
                throw new ArgumentOutOfRangeException(maximumName, maximum, "Ratio must be from 0 to 1.");
            }

            if (minimum > maximum)
            {
                throw new ArgumentException("Minimum health ratio cannot exceed maximum health ratio.");
            }
        }
    }

    public sealed class EnemyBehaviorTable
    {
        private readonly IReadOnlyList<EnemyBehaviorEntry> _entries;

        public EnemyBehaviorTable(
            IReadOnlyList<EnemyBehaviorEntry> entries,
            EnemyActionSequence fallbackSequence)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            FallbackSequence = fallbackSequence
                ?? throw new ArgumentNullException(nameof(fallbackSequence));
            var copy = new EnemyBehaviorEntry[entries.Count];
            var sequenceIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < entries.Count; index++)
            {
                EnemyBehaviorEntry entry = entries[index];
                if (entry == null)
                {
                    throw new ArgumentException(
                        "An Enemy Behavior Table cannot contain a null entry.",
                        nameof(entries));
                }

                if (!sequenceIds.Add(entry.Sequence.SequenceId))
                {
                    throw new ArgumentException(
                        $"Enemy Sequence ID '{entry.Sequence.SequenceId}' is duplicated.",
                        nameof(entries));
                }

                copy[index] = entry;
            }

            _entries = Array.AsReadOnly(copy);
        }

        public IReadOnlyList<EnemyBehaviorEntry> Entries => _entries;

        public EnemyActionSequence FallbackSequence { get; }

        public EnemyActionSequence SelectSequence(
            CombatantState player,
            CombatantState enemy,
            string previousSequenceId,
            double normalizedRoll)
        {
            if (double.IsNaN(normalizedRoll)
                || normalizedRoll < 0d
                || normalizedRoll >= 1d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(normalizedRoll),
                    normalizedRoll,
                    "Selection roll must be at least 0 and less than 1.");
            }

            var eligible = new List<EnemyBehaviorEntry>();
            int totalWeight = 0;
            for (int index = 0; index < _entries.Count; index++)
            {
                EnemyBehaviorEntry entry = _entries[index];
                if (!entry.IsEligible(player, enemy, previousSequenceId))
                {
                    continue;
                }

                eligible.Add(entry);
                totalWeight = checked(totalWeight + entry.SelectionWeight);
            }

            if (eligible.Count == 0)
            {
                return FallbackSequence;
            }

            double targetWeight = normalizedRoll * totalWeight;
            int cumulativeWeight = 0;
            for (int index = 0; index < eligible.Count; index++)
            {
                cumulativeWeight += eligible[index].SelectionWeight;
                if (targetWeight < cumulativeWeight)
                {
                    return eligible[index].Sequence;
                }
            }

            return eligible[eligible.Count - 1].Sequence;
        }

        public static EnemyBehaviorTable Always(EnemyActionSequence sequence)
        {
            return new EnemyBehaviorTable(
                Array.Empty<EnemyBehaviorEntry>(),
                sequence ?? throw new ArgumentNullException(nameof(sequence)));
        }
    }
}
