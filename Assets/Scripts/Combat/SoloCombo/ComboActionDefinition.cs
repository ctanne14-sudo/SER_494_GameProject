using System;
using SER494.Gameplay.Combat.Damage;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public enum ComboActionCategory
    {
        Opening,
        Main,
        Closing,
        Linked
    }

    public enum ActionResolutionEffect
    {
        None,
        Damage,
        Guard,
        Evade
    }

    [Flags]
    public enum DamageResolutionTag
    {
        None = 0,
        Area = 1 << 0,
        Unguardable = 1 << 1,
        Overcommit = 1 << 2
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
            int cooldown,
            double damageMultiplier = 0d,
            int? resolutionOffset = null,
            DamageType damageType = DamageType.Slashing,
            ActionResolutionEffect resolutionEffect = ActionResolutionEffect.None,
            DamageResolutionTag damageTags = DamageResolutionTag.None)
            : this(
                actionId,
                displayName,
                category,
                loadCost,
                manaCost,
                staminaCost,
                cooldown,
                CreateSingleDamageEffect(damageMultiplier, damageType),
                resolutionOffset,
                ResolveEffect(damageMultiplier, resolutionEffect),
                damageTags)
        {
        }

        public ComboActionDefinition(
            string actionId,
            string displayName,
            ComboActionCategory category,
            int loadCost,
            int manaCost,
            int staminaCost,
            int cooldown,
            DamageEffectDefinition damageEffect,
            int? resolutionOffset = null,
            DamageResolutionTag damageTags = DamageResolutionTag.None)
            : this(
                actionId,
                displayName,
                category,
                loadCost,
                manaCost,
                staminaCost,
                cooldown,
                damageEffect,
                resolutionOffset,
                damageEffect != null
                    ? ActionResolutionEffect.Damage
                    : ActionResolutionEffect.None,
                damageTags)
        {
        }

        private ComboActionDefinition(
            string actionId,
            string displayName,
            ComboActionCategory category,
            int loadCost,
            int manaCost,
            int staminaCost,
            int cooldown,
            DamageEffectDefinition damageEffect,
            int? resolutionOffset,
            ActionResolutionEffect resolutionEffect,
            DamageResolutionTag damageTags)
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
            LoadCost = RequirePositive(loadCost, nameof(loadCost));
            ManaCost = RequireNonNegative(manaCost, nameof(manaCost));
            StaminaCost = RequireNonNegative(staminaCost, nameof(staminaCost));
            Cooldown = RequireNonNegative(cooldown, nameof(cooldown));
            DamageEffect = damageEffect;
            ResolutionEffect = resolutionEffect;
            DamageTags = ValidateDamageTags(damageEffect, damageTags);
            ResolutionOffset = RequireResolutionOffset(
                resolutionOffset ?? LoadCost,
                LoadCost,
                nameof(resolutionOffset));
        }

        public string ActionId { get; }

        public string DisplayName { get; }

        public ComboActionCategory Category { get; }

        public int LoadCost { get; }

        public int ManaCost { get; }

        public int StaminaCost { get; }

        public int Cooldown { get; }

        public DamageEffectDefinition DamageEffect { get; }

        public bool HasDamage => DamageEffect != null;

        public ActionResolutionEffect ResolutionEffect { get; }

        public DamageResolutionTag DamageTags { get; }

        public int ResolutionOffset { get; }

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
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }

            return value;
        }

        private static DamageEffectDefinition CreateSingleDamageEffect(
            double damageMultiplier,
            DamageType damageType)
        {
            if (double.IsNaN(damageMultiplier)
                || double.IsInfinity(damageMultiplier)
                || damageMultiplier < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damageMultiplier),
                    damageMultiplier,
                    "Damage Multiplier must be finite and cannot be negative.");
            }

            return damageMultiplier > 0d
                ? DamageEffectDefinition.Single(damageType, damageMultiplier)
                : null;
        }

        private static ActionResolutionEffect ResolveEffect(
            double damageMultiplier,
            ActionResolutionEffect resolutionEffect)
        {
            if (!Enum.IsDefined(typeof(ActionResolutionEffect), resolutionEffect))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resolutionEffect),
                    resolutionEffect,
                    "Unknown Action Resolution Effect.");
            }

            if (damageMultiplier > 0d)
            {
                if (resolutionEffect != ActionResolutionEffect.None
                    && resolutionEffect != ActionResolutionEffect.Damage)
                {
                    throw new ArgumentException(
                        "A damaging Action must use the Damage resolution effect.",
                        nameof(resolutionEffect));
                }

                return ActionResolutionEffect.Damage;
            }

            if (resolutionEffect == ActionResolutionEffect.Damage)
            {
                throw new ArgumentException(
                    "A Damage resolution effect requires a positive Damage Multiplier.",
                    nameof(resolutionEffect));
            }

            return resolutionEffect;
        }

        private static DamageResolutionTag ValidateDamageTags(
            DamageEffectDefinition damageEffect,
            DamageResolutionTag damageTags)
        {
            const DamageResolutionTag supportedTags = DamageResolutionTag.Area
                | DamageResolutionTag.Unguardable
                | DamageResolutionTag.Overcommit;
            if ((damageTags & ~supportedTags) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damageTags),
                    damageTags,
                    "Unknown Damage Resolution Tag.");
            }

            if (damageEffect == null && damageTags != DamageResolutionTag.None)
            {
                throw new ArgumentException(
                    "Damage Resolution Tags require a damaging Action.",
                    nameof(damageTags));
            }

            return damageTags;
        }

        private static int RequireResolutionOffset(
            int value,
            int loadCost,
            string parameterName)
        {
            if (value < 0 || value > loadCost)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Resolution Offset must be between zero and the Action Load Cost.");
            }

            return value;
        }
    }
}
