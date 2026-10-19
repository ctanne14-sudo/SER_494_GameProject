using System;

namespace SER494.Gameplay.Combat
{
    public sealed class CombatAction
    {
        public CombatAction(string displayName, int power, int manaCost, int staminaCost)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("An action must have a display name.", nameof(displayName));
            }

            if (power < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(power));
            }

            if (manaCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(manaCost));
            }

            if (staminaCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(staminaCost));
            }

            DisplayName = displayName;
            Power = power;
            ManaCost = manaCost;
            StaminaCost = staminaCost;
        }

        public string DisplayName { get; }

        public int Power { get; }

        public int ManaCost { get; }

        public int StaminaCost { get; }
    }
}
