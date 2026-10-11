using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SER494.Gameplay.Combat.Damage;

namespace SER494.Gameplay.Combat
{
    public sealed class CombatantState
    {
        private readonly List<DamageModifier> _damageDealtModifiers;
        private readonly List<DamageModifier> _damageTakenModifiers;
        private readonly ReadOnlyCollection<DamageModifier> _readOnlyDamageDealtModifiers;
        private readonly ReadOnlyCollection<DamageModifier> _readOnlyDamageTakenModifiers;

        public CombatantState(string displayName, CombatantStats stats)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A combatant must have a display name.", nameof(displayName));
            }

            DisplayName = displayName;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
            CurrentHealth = stats.MaxHealth;
            CurrentMana = stats.MaxMana;
            CurrentStamina = stats.MaxStamina;
            _damageDealtModifiers = new List<DamageModifier>();
            _damageTakenModifiers = new List<DamageModifier>();
            _readOnlyDamageDealtModifiers = _damageDealtModifiers.AsReadOnly();
            _readOnlyDamageTakenModifiers = _damageTakenModifiers.AsReadOnly();
        }

        public string DisplayName { get; }

        public CombatantStats Stats { get; }

        public int CurrentHealth { get; private set; }

        public int CurrentMana { get; private set; }

        public int CurrentStamina { get; private set; }

        public bool IsDefeated => CurrentHealth == 0;

        public IReadOnlyList<DamageModifier> DamageDealtModifiers =>
            _readOnlyDamageDealtModifiers;

        public IReadOnlyList<DamageModifier> DamageTakenModifiers =>
            _readOnlyDamageTakenModifiers;

        public void AddDamageDealtModifier(DamageModifier modifier)
        {
            _damageDealtModifiers.Add(modifier);
        }

        public bool RemoveDamageDealtModifier(DamageModifier modifier)
        {
            return _damageDealtModifiers.Remove(modifier);
        }

        public void AddDamageTakenModifier(DamageModifier modifier)
        {
            _damageTakenModifiers.Add(modifier);
        }

        public bool RemoveDamageTakenModifier(DamageModifier modifier)
        {
            return _damageTakenModifiers.Remove(modifier);
        }

        public bool CanPay(int manaCost, int staminaCost)
        {
            if (manaCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(manaCost));
            }

            if (staminaCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(staminaCost));
            }

            return CurrentMana >= manaCost && CurrentStamina >= staminaCost;
        }

        public void ReceiveDamage(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            CurrentHealth = Math.Max(0, CurrentHealth - amount);
        }

        public void RestoreHealth(int amount)
        {
            CurrentHealth = Restore(CurrentHealth, amount, Stats.MaxHealth);
        }

        public void RestoreMana(int amount)
        {
            CurrentMana = Restore(CurrentMana, amount, Stats.MaxMana);
        }

        public void RestoreStamina(int amount)
        {
            CurrentStamina = Restore(CurrentStamina, amount, Stats.MaxStamina);
        }

        internal void Pay(int manaCost, int staminaCost)
        {
            if (!CanPay(manaCost, staminaCost))
            {
                throw new InvalidOperationException("The combatant does not have enough resources.");
            }

            CurrentMana -= manaCost;
            CurrentStamina -= staminaCost;
        }

        private static int Restore(int currentValue, int amount, int maximumValue)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            return Math.Min(maximumValue, currentValue + amount);
        }
    }
}
