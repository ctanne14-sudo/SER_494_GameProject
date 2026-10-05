using System;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public enum ComboActionCategory
    {
        Opening,
        Main,
        Closing,
        Linked
    }

    public sealed class ComboActionDefinition
    {
        public ComboActionDefinition(
            string actionId,
            string displayName,
            ComboActionCategory category,
            int loadCost,
            int manaCost,
            int staminaCost,
            int cooldown)
        {
            if (string.IsNullOrWhiteSpace(actionId))
            {
                throw new ArgumentException("An Action must have a stable ID.", nameof(actionId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("An Action must have a display name.", nameof(displayName));
            }

            ActionId = actionId;
            DisplayName = displayName;
            Category = category;
            LoadCost = RequireNonNegative(loadCost, nameof(loadCost));
            ManaCost = RequireNonNegative(manaCost, nameof(manaCost));
            StaminaCost = RequireNonNegative(staminaCost, nameof(staminaCost));
            Cooldown = RequireNonNegative(cooldown, nameof(cooldown));
        }

        public string ActionId { get; }

        public string DisplayName { get; }

        public ComboActionCategory Category { get; }

        public int LoadCost { get; }

        public int ManaCost { get; }

        public int StaminaCost { get; }

        public int Cooldown { get; }

        private static int RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }

            return value;
        }
    }
}
