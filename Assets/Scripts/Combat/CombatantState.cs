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
        private readonly List<DamageModifier> _permanentDamageDealtModifiers;
        private readonly List<DamageModifier> _permanentDamageTakenModifiers;
        private readonly List<CombatStatusInstance> _activeStatuses;
        private readonly ReadOnlyCollection<DamageModifier> _readOnlyDamageDealtModifiers;
        private readonly ReadOnlyCollection<DamageModifier> _readOnlyDamageTakenModifiers;
        private readonly ReadOnlyCollection<CombatStatusInstance> _readOnlyActiveStatuses;

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
            _permanentDamageDealtModifiers = new List<DamageModifier>();
            _permanentDamageTakenModifiers = new List<DamageModifier>();
            _activeStatuses = new List<CombatStatusInstance>();
            _readOnlyDamageDealtModifiers = _damageDealtModifiers.AsReadOnly();
            _readOnlyDamageTakenModifiers = _damageTakenModifiers.AsReadOnly();
            _readOnlyActiveStatuses = _activeStatuses.AsReadOnly();
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

        public IReadOnlyList<CombatStatusInstance> ActiveStatuses => _readOnlyActiveStatuses;

        public void AddDamageDealtModifier(DamageModifier modifier)
        {
            _permanentDamageDealtModifiers.Add(modifier);
            RebuildDamageModifiers();
        }

        public bool RemoveDamageDealtModifier(DamageModifier modifier)
        {
            bool removed = _permanentDamageDealtModifiers.Remove(modifier);
            RebuildDamageModifiers();
            return removed;
        }

        public void AddDamageTakenModifier(DamageModifier modifier)
        {
            _permanentDamageTakenModifiers.Add(modifier);
            RebuildDamageModifiers();
        }

        public bool RemoveDamageTakenModifier(DamageModifier modifier)
        {
            bool removed = _permanentDamageTakenModifiers.Remove(modifier);
            RebuildDamageModifiers();
            return removed;
        }

        public void ApplyStatus(CombatStatusDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definition.StackingPolicy == CombatStatusStackingPolicy.RefreshDuration)
            {
                for (int index = 0; index < _activeStatuses.Count; index++)
                {
                    CombatStatusInstance status = _activeStatuses[index];
                    if (string.Equals(
                            status.Definition.StatusId,
                            definition.StatusId,
                            StringComparison.Ordinal))
                    {
                        status.Refresh(definition);
                        RebuildDamageModifiers();
                        return;
                    }
                }
            }

            _activeStatuses.Add(new CombatStatusInstance(definition));
            RebuildDamageModifiers();
        }

        public bool HasStatus(string statusId)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                throw new ArgumentException("A status ID is required.", nameof(statusId));
            }

            for (int index = 0; index < _activeStatuses.Count; index++)
            {
                if (string.Equals(
                        _activeStatuses[index].Definition.StatusId,
                        statusId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ConsumeStatus(string statusId)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                throw new ArgumentException("A status ID is required.", nameof(statusId));
            }

            for (int index = 0; index < _activeStatuses.Count; index++)
            {
                if (!string.Equals(
                        _activeStatuses[index].Definition.StatusId,
                        statusId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                _activeStatuses.RemoveAt(index);
                RebuildDamageModifiers();
                return true;
            }

            return false;
        }

        public void AdvanceRoundStatuses()
        {
            AdvanceStatuses(1, CombatStatusDurationType.Rounds);
        }

        public void AdvanceTimeStepStatuses(int elapsedTimeSteps)
        {
            AdvanceStatuses(elapsedTimeSteps, CombatStatusDurationType.TimeSteps);
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

        public double GetStaminaRecoveryMultiplier()
        {
            double multiplier = 1d;
            for (int index = 0; index < _activeStatuses.Count; index++)
            {
                multiplier += _activeStatuses[index]
                    .Definition
                    .StaminaRecoveryMultiplierBonus;
            }

            return multiplier;
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

        private void AdvanceStatuses(int amount, CombatStatusDurationType durationType)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            for (int index = _activeStatuses.Count - 1; index >= 0; index--)
            {
                if (_activeStatuses[index].Advance(amount, durationType))
                {
                    _activeStatuses.RemoveAt(index);
                }
            }

            RebuildDamageModifiers();
        }

        private void RebuildDamageModifiers()
        {
            _damageDealtModifiers.Clear();
            _damageDealtModifiers.AddRange(_permanentDamageDealtModifiers);
            _damageTakenModifiers.Clear();
            _damageTakenModifiers.AddRange(_permanentDamageTakenModifiers);

            for (int index = 0; index < _activeStatuses.Count; index++)
            {
                CombatStatusDefinition status = _activeStatuses[index].Definition;
                if (status.ModifierDirection == CombatStatusModifierDirection.DamageDealt)
                {
                    _damageDealtModifiers.Add(status.Modifier);
                }
                else
                {
                    _damageTakenModifiers.Add(status.Modifier);
                }
            }
        }
    }
}
