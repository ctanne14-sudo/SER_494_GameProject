using System;
using System.Collections.Generic;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public enum LinkedActionPlacement
    {
        AfterOpening,
        AfterMain,
        AfterClosing
    }

    public sealed class ComboConfiguration
    {
        public ComboConfiguration(
            string name,
            ComboActionDefinition openingAction,
            ComboActionDefinition mainAction,
            ComboActionDefinition closingAction,
            ComboActionDefinition linkedAction,
            LinkedActionPlacement? linkedActionPlacement)
        {
            Name = RequireName(name);
            OpeningAction = openingAction;
            MainAction = mainAction;
            ClosingAction = closingAction;
            LinkedAction = linkedAction;
            LinkedPlacement = linkedActionPlacement;
        }

        public string Name { get; private set; }

        public ComboActionDefinition OpeningAction { get; }

        public ComboActionDefinition MainAction { get; }

        public ComboActionDefinition ClosingAction { get; }

        public ComboActionDefinition LinkedAction { get; }

        public LinkedActionPlacement? LinkedPlacement { get; }

        public int TotalLoadCost => Sum(action => action.LoadCost);

        public int TotalManaCost => Sum(action => action.ManaCost);

        public int TotalStaminaCost => Sum(action => action.StaminaCost);

        // The current design uses the sum of all Action cooldowns. Keeping this
        // calculation here makes it easy to replace if the design changes later.
        public int TotalCooldown => Sum(action => action.Cooldown);

        public static ComboConfiguration CreateEmpty(int oneBasedIndex)
        {
            if (oneBasedIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(oneBasedIndex));
            }

            return new ComboConfiguration(
                $"combo{oneBasedIndex}",
                openingAction: null,
                mainAction: null,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
        }

        public void Rename(string newName)
        {
            Name = RequireName(newName);
        }

        public IReadOnlyList<ComboActionDefinition> BuildExecutionOrder()
        {
            var executionOrder = new List<ComboActionDefinition>(4);

            AddActionAndLinked(executionOrder, OpeningAction, LinkedActionPlacement.AfterOpening);
            AddActionAndLinked(executionOrder, MainAction, LinkedActionPlacement.AfterMain);
            AddActionAndLinked(executionOrder, ClosingAction, LinkedActionPlacement.AfterClosing);

            return executionOrder;
        }

        public IReadOnlyList<ComboActionDefinition> GetAssignedActions()
        {
            var actions = new List<ComboActionDefinition>(4);
            AddIfPresent(actions, OpeningAction);
            AddIfPresent(actions, MainAction);
            AddIfPresent(actions, ClosingAction);
            AddIfPresent(actions, LinkedAction);
            return actions;
        }

        private void AddActionAndLinked(
            ICollection<ComboActionDefinition> executionOrder,
            ComboActionDefinition action,
            LinkedActionPlacement placement)
        {
            AddIfPresent(executionOrder, action);

            if (LinkedAction != null && LinkedPlacement == placement)
            {
                executionOrder.Add(LinkedAction);
            }
        }

        private int Sum(Func<ComboActionDefinition, int> selector)
        {
            int total = 0;
            IReadOnlyList<ComboActionDefinition> actions = GetAssignedActions();

            for (int index = 0; index < actions.Count; index++)
            {
                total += selector(actions[index]);
            }

            return total;
        }

        private static void AddIfPresent(
            ICollection<ComboActionDefinition> actions,
            ComboActionDefinition action)
        {
            if (action != null)
            {
                actions.Add(action);
            }
        }

        private static string RequireName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A Combo must have a name.", nameof(name));
            }

            return name;
        }
    }
}
