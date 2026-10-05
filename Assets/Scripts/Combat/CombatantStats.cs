using System;

namespace SER494.Gameplay.Combat
{
    public sealed class CombatantStats
    {
        public CombatantStats(
            int maxHealth,
            int maxMana,
            int maxStamina,
            int attack,
            int defense,
            int speed)
        {
            MaxHealth = RequirePositive(maxHealth, nameof(maxHealth));
            MaxMana = RequireNonNegative(maxMana, nameof(maxMana));
            MaxStamina = RequireNonNegative(maxStamina, nameof(maxStamina));
            Attack = RequireNonNegative(attack, nameof(attack));
            Defense = RequireNonNegative(defense, nameof(defense));
            Speed = RequireNonNegative(speed, nameof(speed));
        }

        public int MaxHealth { get; }

        public int MaxMana { get; }

        public int MaxStamina { get; }

        public int Attack { get; }

        public int Defense { get; }

        public int Speed { get; }

        private static int RequirePositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
            }

            return value;
        }

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
