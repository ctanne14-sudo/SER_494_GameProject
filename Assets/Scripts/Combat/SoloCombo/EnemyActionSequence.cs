using System;
using System.Collections.Generic;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public sealed class EnemyActionSequence
    {
        private readonly IReadOnlyList<EnemyActionDefinition> _actions;

        public EnemyActionSequence(
            string sequenceId,
            string displayName,
            IReadOnlyList<EnemyActionDefinition> actions)
        {
            if (string.IsNullOrWhiteSpace(sequenceId))
            {
                throw new ArgumentException("An enemy Sequence must have a stable ID.", nameof(sequenceId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "An enemy Sequence must have a display name.",
                    nameof(displayName));
            }

            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            if (actions.Count == 0)
            {
                throw new ArgumentException(
                    "An enemy Sequence must contain at least one Action.",
                    nameof(actions));
            }

            var copy = new EnemyActionDefinition[actions.Count];
            int totalLoad = 0;
            for (int index = 0; index < actions.Count; index++)
            {
                EnemyActionDefinition action = actions[index];
                if (action == null)
                {
                    throw new ArgumentException(
                        "An enemy Sequence cannot contain a null Action.",
                        nameof(actions));
                }

                copy[index] = action;
                totalLoad = checked(totalLoad + action.LoadCost);
            }

            SequenceId = sequenceId;
            DisplayName = displayName;
            _actions = Array.AsReadOnly(copy);
            TotalLoadCost = totalLoad;
        }

        public string SequenceId { get; }

        public string DisplayName { get; }

        public IReadOnlyList<EnemyActionDefinition> Actions => _actions;

        public int TotalLoadCost { get; }
    }
}
