using System;
using SER494.Gameplay.Combat.Damage;

namespace SER494.Gameplay.Combat
{
    public enum CombatStatusDurationType
    {
        TimeSteps,
        Rounds
    }

    public enum CombatStatusModifierDirection
    {
        DamageDealt,
        DamageTaken
    }

    public enum CombatStatusStackingPolicy
    {
        RefreshDuration,
        IndependentInstances
    }

    public sealed class CombatStatusDefinition
    {
        public CombatStatusDefinition(
            string statusId,
            string displayName,
            CombatStatusDurationType durationType,
            int duration,
            CombatStatusModifierDirection modifierDirection,
            DamageModifier modifier,
            CombatStatusStackingPolicy stackingPolicy =
                CombatStatusStackingPolicy.RefreshDuration,
            double staminaRecoveryMultiplierBonus = 0d)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                throw new ArgumentException("A status must have a stable ID.", nameof(statusId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A status must have a display name.", nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(CombatStatusDurationType), durationType))
            {
                throw new ArgumentOutOfRangeException(nameof(durationType));
            }

            if (!Enum.IsDefined(typeof(CombatStatusModifierDirection), modifierDirection))
            {
                throw new ArgumentOutOfRangeException(nameof(modifierDirection));
            }

            if (duration <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    duration,
                    "Status duration must be greater than zero.");
            }

            if (!Enum.IsDefined(typeof(CombatStatusStackingPolicy), stackingPolicy))
            {
                throw new ArgumentOutOfRangeException(nameof(stackingPolicy));
            }

            if (double.IsNaN(staminaRecoveryMultiplierBonus)
                || double.IsInfinity(staminaRecoveryMultiplierBonus)
                || staminaRecoveryMultiplierBonus < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(staminaRecoveryMultiplierBonus));
            }

            StatusId = statusId;
            DisplayName = displayName;
            DurationType = durationType;
            Duration = duration;
            ModifierDirection = modifierDirection;
            Modifier = modifier;
            StackingPolicy = stackingPolicy;
            StaminaRecoveryMultiplierBonus = staminaRecoveryMultiplierBonus;
        }

        public string StatusId { get; }

        public string DisplayName { get; }

        public CombatStatusDurationType DurationType { get; }

        public int Duration { get; }

        public CombatStatusModifierDirection ModifierDirection { get; }

        public DamageModifier Modifier { get; }

        public CombatStatusStackingPolicy StackingPolicy { get; }

        public double StaminaRecoveryMultiplierBonus { get; }
    }

    public sealed class CombatStatusInstance
    {
        internal CombatStatusInstance(CombatStatusDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            RemainingDuration = definition.Duration;
        }

        public CombatStatusDefinition Definition { get; private set; }

        public int RemainingDuration { get; private set; }

        internal void Refresh(CombatStatusDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            RemainingDuration = definition.Duration;
        }

        internal bool Advance(int amount, CombatStatusDurationType durationType)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (Definition.DurationType == durationType)
            {
                RemainingDuration = Math.Max(0, RemainingDuration - amount);
            }

            return RemainingDuration == 0;
        }
    }
}
