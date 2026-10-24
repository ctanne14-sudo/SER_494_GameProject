using System;

namespace SER494.Gameplay.PlayerState
{
    public sealed class PlayerState
    {
        public PlayerState(
            int maximumHealth,
            int maximumMana,
            int maximumCombatStamina,
            int initiallyUnlockedLoadoutSlots = 1,
            int baseStaminaRecoveryPerRound = 10)
        {
            MaximumHealth = RequirePositive(maximumHealth, nameof(maximumHealth));
            MaximumMana = RequireNonNegative(maximumMana, nameof(maximumMana));
            MaximumCombatStamina = RequireNonNegative(
                maximumCombatStamina,
                nameof(maximumCombatStamina));
            BaseStaminaRecoveryPerRound = RequireNonNegative(
                baseStaminaRecoveryPerRound,
                nameof(baseStaminaRecoveryPerRound));
            CurrentHealth = MaximumHealth;
            CurrentMana = MaximumMana;

            Inventory = new PlayerInventoryState();
            Currencies = new PlayerCurrencyState();
            Equipment = new PlayerEquipmentState();
            ActionLibrary = new PlayerActionLibraryState();
            ComboLoadouts = new PlayerComboLoadoutCollection(initiallyUnlockedLoadoutSlots);
        }

        public int CurrentHealth { get; private set; }

        public int MaximumHealth { get; private set; }

        public int CurrentMana { get; private set; }

        public int MaximumMana { get; }

        // Stamina exists only during combat. A Battle State starts at this value
        // instead of carrying a current Stamina value through exploration.
        public int MaximumCombatStamina { get; }

        // Persistent base value. Equipment and battle statuses can modify the
        // actual recovery used by an individual battle without mutating this value.
        public int BaseStaminaRecoveryPerRound { get; }

        public PlayerInventoryState Inventory { get; }

        public PlayerCurrencyState Currencies { get; }

        public PlayerEquipmentState Equipment { get; }

        public PlayerActionLibraryState ActionLibrary { get; }

        public PlayerComboLoadoutCollection ComboLoadouts { get; }

        public void SetCurrentHealth(int value)
        {
            CurrentHealth = Clamp(value, 0, MaximumHealth);
        }

        public void TakeDamage(int amount)
        {
            RequireNonNegative(amount, nameof(amount));
            SetCurrentHealth(CurrentHealth - amount);
        }

        public void RestoreHealth(int amount)
        {
            RequireNonNegative(amount, nameof(amount));
            SetCurrentHealth(CurrentHealth + amount);
        }

        public void RestoreHealthToMaximum()
        {
            CurrentHealth = MaximumHealth;
        }

        public void IncreaseMaximumHealth(int amount)
        {
            MaximumHealth = checked(MaximumHealth + RequirePositive(amount, nameof(amount)));
        }

        public void SetCurrentMana(int value)
        {
            CurrentMana = Clamp(value, 0, MaximumMana);
        }

        public bool TrySpendMana(int amount)
        {
            RequireNonNegative(amount, nameof(amount));
            if (CurrentMana < amount)
            {
                return false;
            }

            CurrentMana -= amount;
            return true;
        }

        public void RestoreMana(int amount)
        {
            RequireNonNegative(amount, nameof(amount));
            SetCurrentMana(CurrentMana + amount);
        }

        public void RestoreManaToMaximum()
        {
            CurrentMana = MaximumMana;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }

        private static int RequirePositive(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value must be greater than zero.");
            }

            return value;
        }

        private static int RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Value cannot be negative.");
            }

            return value;
        }
    }
}
