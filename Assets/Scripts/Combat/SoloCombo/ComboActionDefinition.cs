using System;
using System.Collections.Generic;
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
        Evade,
        ApplyBuff,
        ApplyDebuff
    }

    public enum ActionStatusTarget
    {
        Self,
        Opponent
    }

    public sealed class ComboActionChargeLevelDefinition
    {
        public ComboActionChargeLevelDefinition(
            int additionalLoad,
            double damageMultiplierScale)
        {
            if (additionalLoad <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(additionalLoad),
                    additionalLoad,
                    "A Charge Level must add at least one Load.");
            }

            if (double.IsNaN(damageMultiplierScale)
                || double.IsInfinity(damageMultiplierScale)
                || damageMultiplierScale <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damageMultiplierScale),
                    damageMultiplierScale,
                    "A Charge Level damage scale must be finite and greater than zero.");
            }

            AdditionalLoad = additionalLoad;
            DamageMultiplierScale = damageMultiplierScale;
        }

        public int AdditionalLoad { get; }

        public double DamageMultiplierScale { get; }
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
            DamageResolutionTag damageTags = DamageResolutionTag.None,
            CombatStatusDefinition statusEffect = null,
            ActionStatusTarget statusTarget = ActionStatusTarget.Self,
            int? resolutionWindowStart = null,
            int? resolutionWindowEnd = null,
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels = null)
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
                damageTags,
                statusEffect,
                statusTarget,
                resolutionWindowStart,
                resolutionWindowEnd,
                chargeLevels)
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
            DamageResolutionTag damageTags = DamageResolutionTag.None,
            CombatStatusDefinition statusEffect = null,
            ActionStatusTarget statusTarget = ActionStatusTarget.Self,
            int? resolutionWindowStart = null,
            int? resolutionWindowEnd = null,
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels = null)
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
                damageTags,
                statusEffect,
                statusTarget,
                resolutionWindowStart,
                resolutionWindowEnd,
                chargeLevels)
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
            DamageResolutionTag damageTags,
            CombatStatusDefinition statusEffect,
            ActionStatusTarget statusTarget,
            int? resolutionWindowStart,
            int? resolutionWindowEnd,
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels)
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
            StatusEffect = ValidateStatusEffect(resolutionEffect, statusEffect);
            StatusTarget = ValidateStatusTarget(statusTarget);
            ResolutionOffset = RequireResolutionOffset(
                resolutionOffset ?? LoadCost,
                LoadCost,
                nameof(resolutionOffset));
            ValidateResolutionWindow(
                resolutionWindowStart,
                resolutionWindowEnd,
                LoadCost);
            ResolutionWindowStart = resolutionWindowStart;
            ResolutionWindowEnd = resolutionWindowEnd;
            ChargeLevels = CopyAndValidateChargeLevels(
                chargeLevels,
                HasResolutionWindow);
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

        public CombatStatusDefinition StatusEffect { get; }

        public ActionStatusTarget StatusTarget { get; }

        public int ResolutionOffset { get; }

        public int? ResolutionWindowStart { get; }

        public int? ResolutionWindowEnd { get; }

        public bool HasResolutionWindow => ResolutionWindowStart.HasValue;

        public IReadOnlyList<ComboActionChargeLevelDefinition> ChargeLevels { get; }

        public bool IsChargeable => ChargeLevels.Count > 0;

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

        private static CombatStatusDefinition ValidateStatusEffect(
            ActionResolutionEffect resolutionEffect,
            CombatStatusDefinition statusEffect)
        {
            bool appliesStatus = resolutionEffect == ActionResolutionEffect.ApplyBuff
                || resolutionEffect == ActionResolutionEffect.ApplyDebuff;
            if (appliesStatus && statusEffect == null)
            {
                throw new ArgumentException(
                    "Apply Buff and Apply Debuff effects require a Status Effect.",
                    nameof(statusEffect));
            }

            if (!appliesStatus && statusEffect != null)
            {
                throw new ArgumentException(
                    "Only Apply Buff and Apply Debuff effects may contain a Status Effect.",
                    nameof(statusEffect));
            }

            return statusEffect;
        }

        private static ActionStatusTarget ValidateStatusTarget(ActionStatusTarget statusTarget)
        {
            if (!Enum.IsDefined(typeof(ActionStatusTarget), statusTarget))
            {
                throw new ArgumentOutOfRangeException(nameof(statusTarget));
            }

            return statusTarget;
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

        private static void ValidateResolutionWindow(
            int? start,
            int? end,
            int loadCost)
        {
            if (start.HasValue != end.HasValue)
            {
                throw new ArgumentException(
                    "Resolution Window start and end must either both be set or both be omitted.");
            }

            if (!start.HasValue)
            {
                return;
            }

            RequireResolutionOffset(start.Value, loadCost, nameof(start));
            RequireResolutionOffset(end.Value, loadCost, nameof(end));
            if (start.Value >= end.Value)
            {
                throw new ArgumentException(
                    "A Resolution Window must contain at least two ordered Time Steps.");
            }
        }

        private static IReadOnlyList<ComboActionChargeLevelDefinition> CopyAndValidateChargeLevels(
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels,
            bool hasResolutionWindow)
        {
            if (chargeLevels == null || chargeLevels.Count == 0)
            {
                return Array.Empty<ComboActionChargeLevelDefinition>();
            }

            if (hasResolutionWindow)
            {
                throw new ArgumentException(
                    "The MVP does not allow one Action to use both a Resolution Window and Charge Levels.",
                    nameof(chargeLevels));
            }

            var copy = new ComboActionChargeLevelDefinition[chargeLevels.Count];
            int previousAdditionalLoad = 0;
            for (int index = 0; index < chargeLevels.Count; index++)
            {
                ComboActionChargeLevelDefinition level = chargeLevels[index]
                    ?? throw new ArgumentException(
                        "Charge Level entries cannot be null.",
                        nameof(chargeLevels));
                if (level.AdditionalLoad <= previousAdditionalLoad)
                {
                    throw new ArgumentException(
                        "Charge Levels must add progressively more Load.",
                        nameof(chargeLevels));
                }

                copy[index] = level;
                previousAdditionalLoad = level.AdditionalLoad;
            }

            return Array.AsReadOnly(copy);
        }
    }
}
