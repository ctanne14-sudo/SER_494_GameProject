using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Damage;
using SER494.Gameplay.Combat.Weapons;

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
        ApplyDebuff,
        Heal
    }

    public enum ActionStatusTarget
    {
        Self,
        Opponent
    }

    public sealed class ActionItemCostDefinition
    {
        public ActionItemCostDefinition(string itemId, string displayName, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentException("An item cost must have a stable ID.", nameof(itemId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("An item cost must have a display name.", nameof(displayName));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            ItemId = itemId;
            DisplayName = displayName;
            Amount = amount;
        }

        public string ItemId { get; }

        public string DisplayName { get; }

        public int Amount { get; }
    }

    public enum ActionHealingType
    {
        FlatAmount,
        MaximumHealthPercentage
    }

    public sealed class ActionHealingDefinition
    {
        private ActionHealingDefinition(ActionHealingType type, double value)
        {
            if (!Enum.IsDefined(typeof(ActionHealingType), type))
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (type == ActionHealingType.MaximumHealthPercentage && value > 1d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    "Maximum Health Percentage healing cannot exceed 100%.");
            }

            Type = type;
            Value = value;
        }

        public ActionHealingType Type { get; }

        public double Value { get; }

        public static ActionHealingDefinition Flat(int amount)
        {
            return new ActionHealingDefinition(ActionHealingType.FlatAmount, amount);
        }

        public static ActionHealingDefinition MaximumHealthPercentage(double percentage)
        {
            return new ActionHealingDefinition(
                ActionHealingType.MaximumHealthPercentage,
                percentage);
        }

        public int CalculateAmount(int maximumHealth)
        {
            if (maximumHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            }

            return Type == ActionHealingType.FlatAmount
                ? checked((int)Value)
                : checked((int)Math.Round(
                    maximumHealth * Value,
                    MidpointRounding.AwayFromZero));
        }
    }

    public sealed class PreviousActionStatusRequirementDefinition
    {
        public PreviousActionStatusRequirementDefinition(
            string statusId,
            string displayName)
        {
            if (string.IsNullOrWhiteSpace(statusId))
            {
                throw new ArgumentException(
                    "A status requirement must have a stable ID.",
                    nameof(statusId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "A status requirement must have a display name.",
                    nameof(displayName));
            }

            StatusId = statusId;
            DisplayName = displayName;
        }

        public string StatusId { get; }

        public string DisplayName { get; }
    }

    public enum ActiveWindowEffect
    {
        DefensiveState,
        SuperArmor
    }

    public enum ActiveWindowDisruptionPolicy
    {
        PersistThroughDisruption,
        BreakOnDisruption,
        SuppressDuringDisruption
    }

    public enum ActionDisruptionType
    {
        Flinch = 1,
        Stagger = 2,
        Knockdown = 3
    }

    public enum ActionDisruptionResponse
    {
        Continue,
        PauseAndResume,
        RestartAction,
        CancelAction
    }

    public sealed class ActionDisruptionDefinition
    {
        public ActionDisruptionDefinition(ActionDisruptionType type, int duration)
        {
            if (!Enum.IsDefined(typeof(ActionDisruptionType), type))
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            if (duration <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            Type = type;
            Duration = duration;
        }

        public ActionDisruptionType Type { get; }

        public int Duration { get; }
    }

    public static class SuperArmorRules
    {
        public static ActionDisruptionDefinition Resolve(
            ActionDisruptionDefinition disruption,
            int superArmorLevel)
        {
            if (disruption == null)
            {
                return null;
            }

            if (superArmorLevel < 0 || superArmorLevel > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(superArmorLevel));
            }

            if (superArmorLevel == 0)
            {
                return disruption;
            }

            switch (disruption.Type)
            {
                case ActionDisruptionType.Flinch:
                    return superArmorLevel >= 1 ? null : disruption;
                case ActionDisruptionType.Stagger:
                    if (superArmorLevel >= 3)
                    {
                        return null;
                    }

                    return superArmorLevel == 2
                        ? new ActionDisruptionDefinition(
                            ActionDisruptionType.Flinch,
                            disruption.Duration)
                        : disruption;
                case ActionDisruptionType.Knockdown:
                    if (superArmorLevel >= 5)
                    {
                        return null;
                    }

                    return superArmorLevel == 4
                        ? new ActionDisruptionDefinition(
                            ActionDisruptionType.Stagger,
                            disruption.Duration)
                        : disruption;
                default:
                    throw new ArgumentOutOfRangeException(nameof(disruption));
            }
        }
    }

    public sealed class DisruptionResponseSegmentDefinition
    {
        public DisruptionResponseSegmentDefinition(
            int startOffset,
            int endOffset,
            ActionDisruptionResponse flinchResponse,
            ActionDisruptionResponse staggerResponse,
            ActionDisruptionResponse knockdownResponse)
        {
            if (startOffset < 1 || endOffset < startOffset)
            {
                throw new ArgumentException(
                    "A Disruption Response Segment must use an ordered Local Time range beginning at 1.");
            }

            StartOffset = startOffset;
            EndOffset = endOffset;
            FlinchResponse = ValidateResponse(flinchResponse, nameof(flinchResponse));
            StaggerResponse = ValidateResponse(staggerResponse, nameof(staggerResponse));
            KnockdownResponse = ValidateResponse(knockdownResponse, nameof(knockdownResponse));
        }

        public int StartOffset { get; }

        public int EndOffset { get; }

        public ActionDisruptionResponse FlinchResponse { get; }

        public ActionDisruptionResponse StaggerResponse { get; }

        public ActionDisruptionResponse KnockdownResponse { get; }

        public ActionDisruptionResponse GetResponse(ActionDisruptionType type)
        {
            switch (type)
            {
                case ActionDisruptionType.Flinch:
                    return FlinchResponse;
                case ActionDisruptionType.Stagger:
                    return StaggerResponse;
                case ActionDisruptionType.Knockdown:
                    return KnockdownResponse;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static ActionDisruptionResponse ValidateResponse(
            ActionDisruptionResponse response,
            string parameterName)
        {
            if (!Enum.IsDefined(typeof(ActionDisruptionResponse), response))
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return response;
        }
    }

    public sealed class ActionActiveWindowDefinition
    {
        public ActionActiveWindowDefinition(
            string windowId,
            string displayName,
            int startOffset,
            int endOffset,
            ActiveWindowEffect effect,
            double damageReduction,
            ActiveWindowDisruptionPolicy disruptionPolicy,
            int superArmorLevel = 0)
        {
            if (string.IsNullOrWhiteSpace(windowId))
            {
                throw new ArgumentException("An Active Window must have a stable ID.", nameof(windowId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "An Active Window must have a display name.",
                    nameof(displayName));
            }

            if (startOffset < 1 || endOffset < startOffset)
            {
                throw new ArgumentException(
                    "An Active Window must use an ordered Local Time range beginning at 1 or later.");
            }

            if (!Enum.IsDefined(typeof(ActiveWindowEffect), effect))
            {
                throw new ArgumentOutOfRangeException(nameof(effect));
            }

            if (effect == ActiveWindowEffect.DefensiveState
                && (double.IsNaN(damageReduction)
                    || double.IsInfinity(damageReduction)
                    || damageReduction <= 0d
                    || damageReduction >= 1d))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damageReduction),
                    damageReduction,
                    "Defensive State reduction must be greater than zero and less than one.");
            }

            if (effect == ActiveWindowEffect.SuperArmor
                && (superArmorLevel < 1 || superArmorLevel > 5))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(superArmorLevel),
                    superArmorLevel,
                    "Super Armor Level must be from 1 to 5.");
            }

            if (!Enum.IsDefined(typeof(ActiveWindowDisruptionPolicy), disruptionPolicy))
            {
                throw new ArgumentOutOfRangeException(nameof(disruptionPolicy));
            }

            WindowId = windowId;
            DisplayName = displayName;
            StartOffset = startOffset;
            EndOffset = endOffset;
            Effect = effect;
            DamageReduction = effect == ActiveWindowEffect.DefensiveState
                ? damageReduction
                : 0d;
            SuperArmorLevel = effect == ActiveWindowEffect.SuperArmor
                ? superArmorLevel
                : 0;
            DisruptionPolicy = disruptionPolicy;
        }

        public string WindowId { get; }

        public string DisplayName { get; }

        public int StartOffset { get; }

        public int EndOffset { get; }

        public ActiveWindowEffect Effect { get; }

        public double DamageReduction { get; }

        public int SuperArmorLevel { get; }

        public ActiveWindowDisruptionPolicy DisruptionPolicy { get; }

        internal void ValidateForActionTimelineCost(int timelineCost)
        {
            if (EndOffset > timelineCost)
            {
                throw new ArgumentException(
                    $"Active Window '{DisplayName}' ends outside its Action Timeline Cost.");
            }
        }
    }

    public sealed class ComboActionChargeLevelDefinition
    {
        public ComboActionChargeLevelDefinition(
            int additionalTimelineCost,
            double damageMultiplierScale)
        {
            if (additionalTimelineCost <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(additionalTimelineCost),
                    additionalTimelineCost,
                    "A Charge Level must add at least one Timeline Cost.");
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

            AdditionalTimelineCost = additionalTimelineCost;
            DamageMultiplierScale = damageMultiplierScale;
        }

        public int AdditionalTimelineCost { get; }

        public double DamageMultiplierScale { get; }
    }

    public sealed class SustainedAttackDefinition
    {
        public SustainedAttackDefinition(
            int windowStartOffset,
            int windowEndOffset,
            int firstHitOffset,
            int hitInterval,
            int maximumHitCount)
        {
            if (windowStartOffset < 1 || windowEndOffset < windowStartOffset)
            {
                throw new ArgumentException(
                    "A Sustained Attack must use an ordered window beginning at Local Time 1 or later.");
            }

            if (firstHitOffset < windowStartOffset || firstHitOffset > windowEndOffset)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(firstHitOffset),
                    "The first Sustained Attack hit must be inside its Active Window.");
            }

            if (hitInterval <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hitInterval));
            }

            if (maximumHitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumHitCount));
            }

            WindowStartOffset = windowStartOffset;
            WindowEndOffset = windowEndOffset;
            FirstHitOffset = firstHitOffset;
            HitInterval = hitInterval;
            MaximumHitCount = maximumHitCount;
        }

        public int WindowStartOffset { get; }

        public int WindowEndOffset { get; }

        public int FirstHitOffset { get; }

        public int HitInterval { get; }

        public int MaximumHitCount { get; }

        public IReadOnlyList<int> BuildHitOffsets()
        {
            var offsets = new List<int>(MaximumHitCount);
            int offset = FirstHitOffset;
            while (offset <= WindowEndOffset && offsets.Count < MaximumHitCount)
            {
                offsets.Add(offset);
                offset = checked(offset + HitInterval);
            }

            return Array.AsReadOnly(offsets.ToArray());
        }

        internal void ValidateForAction(int timelineCost, bool hasDamage)
        {
            if (!hasDamage)
            {
                throw new ArgumentException("Only a damaging Action can be a Sustained Attack.");
            }

            if (WindowEndOffset > timelineCost)
            {
                throw new ArgumentException("A Sustained Attack window cannot extend beyond its Action Timeline Cost.");
            }
        }
    }

    [Flags]
    public enum DamageResolutionTag
    {
        None = 0,
        IgnoresEvade = 1 << 0,
        Unguardable = 1 << 1,
        Overcommit = 1 << 2,
        Clash = 1 << 3
    }

    public sealed class DamageClashDefinition
    {
        public DamageClashDefinition(
            double damageReduction,
            int superArmorLevel = 0)
        {
            if (double.IsNaN(damageReduction)
                || double.IsInfinity(damageReduction)
                || damageReduction <= 0d
                || damageReduction > 1d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(damageReduction),
                    damageReduction,
                    "Clash Damage Reduction must be finite and greater than zero, up to one.");
            }

            if (superArmorLevel < 0 || superArmorLevel > 5)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(superArmorLevel),
                    superArmorLevel,
                    "Clash Super Armor Level must be between zero and five.");
            }

            DamageReduction = damageReduction;
            SuperArmorLevel = superArmorLevel;
        }

        public double DamageReduction { get; }

        public int SuperArmorLevel { get; }
    }

    public sealed class ActionResolutionDefinition
    {
        public ActionResolutionDefinition(
            int offset,
            ActionResolutionEffect effect,
            DamageEffectDefinition damageEffect = null,
            DamageResolutionTag damageTags = DamageResolutionTag.None,
            CombatStatusDefinition statusEffect = null,
            ActionStatusTarget statusTarget = ActionStatusTarget.Self,
            ActionDisruptionDefinition onHitDisruption = null,
            ActionDisruptionDefinition onGuardedDisruption = null,
            DamageClashDefinition clash = null,
            ActionDisruptionDefinition onEvadedDisruption = null,
            int healingAmount = 0,
            ActionItemCostDefinition itemCost = null,
            ActionHealingDefinition healing = null)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (!Enum.IsDefined(typeof(ActionResolutionEffect), effect))
            {
                throw new ArgumentOutOfRangeException(nameof(effect));
            }

            Offset = offset;
            Effect = ValidateEffect(effect, damageEffect);
            DamageEffect = damageEffect;
            DamageTags = ValidateDamageTags(damageEffect, damageTags);
            StatusEffect = ValidateStatusEffect(effect, statusEffect);
            StatusTarget = ValidateStatusTarget(statusTarget);
            OnHitDisruption = ValidateDamageDisruption(
                damageEffect,
                onHitDisruption,
                "On Hit");
            OnGuardedDisruption = ValidateDamageDisruption(
                damageEffect,
                onGuardedDisruption,
                "On Guarded");
            Clash = ValidateClash(damageEffect, DamageTags, clash);
            OnEvadedDisruption = ValidateOnEvadedDisruption(
                damageEffect,
                DamageTags,
                onEvadedDisruption);
            Healing = ValidateHealing(effect, healingAmount, healing);
            ItemCost = itemCost;
        }

        public int Offset { get; }

        public ActionResolutionEffect Effect { get; }

        public DamageEffectDefinition DamageEffect { get; }

        public bool HasDamage => DamageEffect != null;

        public DamageResolutionTag DamageTags { get; }

        public CombatStatusDefinition StatusEffect { get; }

        public ActionStatusTarget StatusTarget { get; }

        public ActionDisruptionDefinition OnHitDisruption { get; }

        public ActionDisruptionDefinition OnGuardedDisruption { get; }

        public DamageClashDefinition Clash { get; }

        public ActionDisruptionDefinition OnEvadedDisruption { get; }

        public ActionHealingDefinition Healing { get; }

        public int HealingAmount => Healing?.Type == ActionHealingType.FlatAmount
            ? checked((int)Healing.Value)
            : 0;

        public ActionItemCostDefinition ItemCost { get; }

        internal void ValidateForActionTimelineCost(int timelineCost)
        {
            if (Offset > timelineCost)
            {
                throw new ArgumentException(
                    "A Resolution Event cannot occur after its Action Timeline Cost.");
            }
        }

        private static ActionResolutionEffect ValidateEffect(
            ActionResolutionEffect effect,
            DamageEffectDefinition damageEffect)
        {
            if (damageEffect != null && effect != ActionResolutionEffect.Damage)
            {
                throw new ArgumentException(
                    "A Resolution Event with damage must use the Damage effect.",
                    nameof(effect));
            }

            if (damageEffect == null && effect == ActionResolutionEffect.Damage)
            {
                throw new ArgumentException(
                    "A Damage Resolution Event requires damage values.",
                    nameof(damageEffect));
            }

            return effect;
        }

        private static DamageResolutionTag ValidateDamageTags(
            DamageEffectDefinition damageEffect,
            DamageResolutionTag damageTags)
        {
            const DamageResolutionTag supportedTags = DamageResolutionTag.IgnoresEvade
                | DamageResolutionTag.Unguardable
                | DamageResolutionTag.Overcommit
                | DamageResolutionTag.Clash;
            if ((damageTags & ~supportedTags) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damageTags));
            }

            if (damageEffect == null && damageTags != DamageResolutionTag.None)
            {
                throw new ArgumentException(
                    "Damage Resolution Tags require damage values.",
                    nameof(damageTags));
            }

            return damageTags;
        }

        private static CombatStatusDefinition ValidateStatusEffect(
            ActionResolutionEffect effect,
            CombatStatusDefinition statusEffect)
        {
            bool appliesStatus = effect == ActionResolutionEffect.ApplyBuff
                || effect == ActionResolutionEffect.ApplyDebuff;
            if (appliesStatus != (statusEffect != null))
            {
                throw new ArgumentException(
                    "Only Apply Buff and Apply Debuff events require a Status Effect.",
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

        private static ActionDisruptionDefinition ValidateDamageDisruption(
            DamageEffectDefinition damageEffect,
            ActionDisruptionDefinition disruption,
            string label)
        {
            if (damageEffect == null && disruption != null)
            {
                throw new ArgumentException($"{label} Disruption requires damage values.");
            }

            return disruption;
        }

        private static DamageClashDefinition ValidateClash(
            DamageEffectDefinition damageEffect,
            DamageResolutionTag damageTags,
            DamageClashDefinition clash)
        {
            bool hasClashTag = (damageTags & DamageResolutionTag.Clash) != 0;
            if (hasClashTag != (clash != null))
            {
                throw new ArgumentException(
                    "Clash values and the Clash Damage Tag must be authored together.",
                    nameof(clash));
            }

            if (damageEffect == null && clash != null)
            {
                throw new ArgumentException("Clash values require damage values.", nameof(clash));
            }

            return clash;
        }

        private static ActionDisruptionDefinition ValidateOnEvadedDisruption(
            DamageEffectDefinition damageEffect,
            DamageResolutionTag damageTags,
            ActionDisruptionDefinition disruption)
        {
            if (disruption == null)
            {
                return null;
            }

            if (damageEffect == null
                || (damageTags & DamageResolutionTag.Overcommit) == 0)
            {
                throw new ArgumentException(
                    "On Evaded Disruption requires Overcommit damage.",
                    nameof(disruption));
            }

            return disruption;
        }

        private static ActionHealingDefinition ValidateHealing(
            ActionResolutionEffect effect,
            int healingAmount,
            ActionHealingDefinition healing)
        {
            if (healingAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(healingAmount));
            }

            if (healingAmount > 0 && healing != null)
            {
                throw new ArgumentException(
                    "Specify either a legacy Healing Amount or a Healing Definition, not both.",
                    nameof(healing));
            }

            ActionHealingDefinition resolvedHealing = healingAmount > 0
                ? ActionHealingDefinition.Flat(healingAmount)
                : healing;
            if ((effect == ActionResolutionEffect.Heal) != (resolvedHealing != null))
            {
                throw new ArgumentException(
                    "A Heal Resolution Event requires Healing, and other effects cannot heal.",
                    nameof(healing));
            }

            return resolvedHealing;
        }
    }

    public sealed class ComboActionDefinition
    {
        public ComboActionDefinition(
            string actionId,
            string displayName,
            ComboActionCategory category,
            int timelineCost,
            int manaCost,
            int staminaCost,
            int cooldown,
            IReadOnlyList<ActionResolutionDefinition> resolutionEvents,
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels = null,
            IReadOnlyList<ActionActiveWindowDefinition> activeWindows = null,
            IReadOnlyList<DisruptionResponseSegmentDefinition> disruptionResponseSegments = null,
            CompatibleWeaponTypes compatibleWeaponTypes = CompatibleWeaponTypes.All,
            PreviousActionStatusRequirementDefinition previousActionStatusRequirement = null)
            : this(
                actionId,
                displayName,
                category,
                timelineCost,
                manaCost,
                staminaCost,
                cooldown,
                GetPrimaryResolution(resolutionEvents).DamageEffect,
                GetPrimaryResolution(resolutionEvents).Offset,
                GetPrimaryResolution(resolutionEvents).Effect,
                GetPrimaryResolution(resolutionEvents).DamageTags,
                GetPrimaryResolution(resolutionEvents).StatusEffect,
                GetPrimaryResolution(resolutionEvents).StatusTarget,
                null,
                null,
                chargeLevels,
                activeWindows,
                GetPrimaryResolution(resolutionEvents).OnHitDisruption,
                disruptionResponseSegments,
                GetPrimaryResolution(resolutionEvents).OnGuardedDisruption,
                GetPrimaryResolution(resolutionEvents).Clash,
                GetPrimaryResolution(resolutionEvents).OnEvadedDisruption,
                GetPrimaryResolution(resolutionEvents).Healing,
                null,
                compatibleWeaponTypes,
                previousActionStatusRequirement)
        {
            ResolutionEvents = CopyAndValidateResolutionEvents(
                resolutionEvents,
                TimelineCost,
                chargeLevels);
        }

        public ComboActionDefinition(
            string actionId,
            string displayName,
            ComboActionCategory category,
            int timelineCost,
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
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels = null,
            IReadOnlyList<ActionActiveWindowDefinition> activeWindows = null,
            ActionDisruptionDefinition onHitDisruption = null,
            IReadOnlyList<DisruptionResponseSegmentDefinition> disruptionResponseSegments = null,
            ActionDisruptionDefinition onGuardedDisruption = null,
            DamageClashDefinition clash = null,
            ActionDisruptionDefinition onEvadedDisruption = null,
            SustainedAttackDefinition sustainedAttack = null,
            CompatibleWeaponTypes compatibleWeaponTypes = CompatibleWeaponTypes.All,
            PreviousActionStatusRequirementDefinition previousActionStatusRequirement = null)
            : this(
                actionId,
                displayName,
                category,
                timelineCost,
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
                chargeLevels,
                activeWindows,
                onHitDisruption,
                disruptionResponseSegments,
                onGuardedDisruption,
                clash,
                onEvadedDisruption,
                null,
                sustainedAttack,
                compatibleWeaponTypes,
                previousActionStatusRequirement)
        {
        }

        public ComboActionDefinition(
            string actionId,
            string displayName,
            ComboActionCategory category,
            int timelineCost,
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
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels = null,
            IReadOnlyList<ActionActiveWindowDefinition> activeWindows = null,
            ActionDisruptionDefinition onHitDisruption = null,
            IReadOnlyList<DisruptionResponseSegmentDefinition> disruptionResponseSegments = null,
            ActionDisruptionDefinition onGuardedDisruption = null,
            DamageClashDefinition clash = null,
            ActionDisruptionDefinition onEvadedDisruption = null,
            SustainedAttackDefinition sustainedAttack = null,
            CompatibleWeaponTypes compatibleWeaponTypes = CompatibleWeaponTypes.All,
            PreviousActionStatusRequirementDefinition previousActionStatusRequirement = null)
            : this(
                actionId,
                displayName,
                category,
                timelineCost,
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
                chargeLevels,
                activeWindows,
                onHitDisruption,
                disruptionResponseSegments,
                onGuardedDisruption,
                clash,
                onEvadedDisruption,
                null,
                sustainedAttack,
                compatibleWeaponTypes,
                previousActionStatusRequirement)
        {
        }

        private ComboActionDefinition(
            string actionId,
            string displayName,
            ComboActionCategory category,
            int timelineCost,
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
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels,
            IReadOnlyList<ActionActiveWindowDefinition> activeWindows,
            ActionDisruptionDefinition onHitDisruption,
            IReadOnlyList<DisruptionResponseSegmentDefinition> disruptionResponseSegments,
            ActionDisruptionDefinition onGuardedDisruption,
            DamageClashDefinition clash,
            ActionDisruptionDefinition onEvadedDisruption,
            ActionHealingDefinition healing,
            SustainedAttackDefinition sustainedAttack,
            CompatibleWeaponTypes compatibleWeaponTypes,
            PreviousActionStatusRequirementDefinition previousActionStatusRequirement)
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
            TimelineCost = RequirePositive(timelineCost, nameof(timelineCost));
            ManaCost = RequireNonNegative(manaCost, nameof(manaCost));
            StaminaCost = RequireNonNegative(staminaCost, nameof(staminaCost));
            Cooldown = RequireNonNegative(cooldown, nameof(cooldown));
            CompatibleWeaponTypes = ValidateCompatibleWeaponTypes(compatibleWeaponTypes);
            DamageEffect = damageEffect;
            ResolutionEffect = resolutionEffect;
            DamageTags = ValidateDamageTags(damageEffect, damageTags);
            StatusEffect = ValidateStatusEffect(resolutionEffect, statusEffect);
            StatusTarget = ValidateStatusTarget(statusTarget);
            ResolutionOffset = RequireResolutionOffset(
                resolutionOffset ?? TimelineCost,
                TimelineCost,
                nameof(resolutionOffset));
            ValidateResolutionWindow(
                resolutionWindowStart,
                resolutionWindowEnd,
                TimelineCost);
            ResolutionWindowStart = resolutionWindowStart;
            ResolutionWindowEnd = resolutionWindowEnd;
            ChargeLevels = CopyAndValidateChargeLevels(
                chargeLevels,
                HasResolutionWindow);
            ActiveWindows = CopyAndValidateActiveWindows(activeWindows, TimelineCost);
            OnHitDisruption = ValidateOnHitDisruption(damageEffect, onHitDisruption);
            OnGuardedDisruption = ValidateDamageDisruption(
                damageEffect,
                onGuardedDisruption,
                "On Guarded");
            Clash = ValidateClash(damageEffect, DamageTags, clash);
            OnEvadedDisruption = ValidateOnEvadedDisruption(
                damageEffect,
                DamageTags,
                onEvadedDisruption);
            Healing = healing;
            PreviousActionStatusRequirement = previousActionStatusRequirement;
            DisruptionResponseSegments = CopyAndValidateDisruptionResponseSegments(
                disruptionResponseSegments,
                TimelineCost);
            sustainedAttack?.ValidateForAction(TimelineCost, damageEffect != null);
            if (sustainedAttack != null && (HasResolutionWindow || ChargeLevels.Count > 0))
            {
                throw new ArgumentException(
                    "The MVP does not combine Sustained Attacks with Resolution Windows or Charge Levels.");
            }

            SustainedAttack = sustainedAttack;
            ResolutionEvents = Array.AsReadOnly(new[]
            {
                new ActionResolutionDefinition(
                    ResolutionOffset,
                    ResolutionEffect,
                    DamageEffect,
                    DamageTags,
                    StatusEffect,
                    StatusTarget,
                    OnHitDisruption,
                    OnGuardedDisruption,
                    Clash,
                    OnEvadedDisruption,
                    healing: Healing)
            });
            if (ChargeLevels.Count > 0 && ActiveWindows.Count > 0)
            {
                throw new ArgumentException(
                    "The MVP does not yet combine Charge Levels and Active Windows on one Action.");
            }
        }

        public string ActionId { get; }

        public string DisplayName { get; }

        public ComboActionCategory Category { get; }

        public int TimelineCost { get; }

        public int ManaCost { get; }

        public int StaminaCost { get; }

        public int Cooldown { get; }

        public CompatibleWeaponTypes CompatibleWeaponTypes { get; }

        public bool IsCompatibleWith(WeaponType weaponType)
        {
            return CompatibleWeaponTypes.Supports(weaponType);
        }

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

        public IReadOnlyList<ActionActiveWindowDefinition> ActiveWindows { get; }

        public ActionDisruptionDefinition OnHitDisruption { get; }

        public ActionDisruptionDefinition OnGuardedDisruption { get; }

        public DamageClashDefinition Clash { get; }

        public ActionDisruptionDefinition OnEvadedDisruption { get; }

        public ActionHealingDefinition Healing { get; }

        public int HealingAmount => Healing?.Type == ActionHealingType.FlatAmount
            ? checked((int)Healing.Value)
            : 0;

        public PreviousActionStatusRequirementDefinition PreviousActionStatusRequirement { get; }

        public IReadOnlyList<DisruptionResponseSegmentDefinition> DisruptionResponseSegments { get; }

        public SustainedAttackDefinition SustainedAttack { get; }

        public IReadOnlyList<ActionResolutionDefinition> ResolutionEvents { get; private set; }

        private static ActionResolutionDefinition GetPrimaryResolution(
            IReadOnlyList<ActionResolutionDefinition> resolutionEvents)
        {
            if (resolutionEvents == null || resolutionEvents.Count == 0)
            {
                throw new ArgumentException(
                    "An Action must contain at least one Resolution Event.",
                    nameof(resolutionEvents));
            }

            return resolutionEvents[0]
                ?? throw new ArgumentException(
                    "A Resolution Event cannot be null.",
                    nameof(resolutionEvents));
        }

        private static IReadOnlyList<ActionResolutionDefinition> CopyAndValidateResolutionEvents(
            IReadOnlyList<ActionResolutionDefinition> resolutionEvents,
            int timelineCost,
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels)
        {
            if (resolutionEvents == null || resolutionEvents.Count == 0)
            {
                throw new ArgumentException(
                    "An Action must contain at least one Resolution Event.",
                    nameof(resolutionEvents));
            }

            if (resolutionEvents.Count > 1
                && chargeLevels != null
                && chargeLevels.Count > 0)
            {
                throw new ArgumentException(
                    "Charge timing for multiple Resolution Events is not yet defined.",
                    nameof(chargeLevels));
            }

            var copy = new ActionResolutionDefinition[resolutionEvents.Count];
            var exclusiveOffsets = new HashSet<int>();
            for (int index = 0; index < resolutionEvents.Count; index++)
            {
                ActionResolutionDefinition resolution = resolutionEvents[index]
                    ?? throw new ArgumentException(
                        "A Resolution Event cannot be null.",
                        nameof(resolutionEvents));
                resolution.ValidateForActionTimelineCost(timelineCost);
                if ((resolution.Effect == ActionResolutionEffect.Damage
                        || resolution.Effect == ActionResolutionEffect.Guard
                        || resolution.Effect == ActionResolutionEffect.Evade)
                    && !exclusiveOffsets.Add(resolution.Offset))
                {
                    throw new ArgumentException(
                        "Damage, Guard, and Evade Resolution Events cannot share an offset in one Action.",
                        nameof(resolutionEvents));
                }

                copy[index] = resolution;
            }

            return Array.AsReadOnly(copy);
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
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }

            return value;
        }

        private static CompatibleWeaponTypes ValidateCompatibleWeaponTypes(
            CompatibleWeaponTypes compatibleWeaponTypes)
        {
            if (compatibleWeaponTypes == CompatibleWeaponTypes.None
                || (compatibleWeaponTypes & ~CompatibleWeaponTypes.All) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(compatibleWeaponTypes),
                    compatibleWeaponTypes,
                    "An Action must support at least one known Weapon Type.");
            }

            return compatibleWeaponTypes;
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
            const DamageResolutionTag supportedTags = DamageResolutionTag.IgnoresEvade
                | DamageResolutionTag.Unguardable
                | DamageResolutionTag.Overcommit
                | DamageResolutionTag.Clash;
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

        private static DamageClashDefinition ValidateClash(
            DamageEffectDefinition damageEffect,
            DamageResolutionTag damageTags,
            DamageClashDefinition clash)
        {
            bool hasClashTag = (damageTags & DamageResolutionTag.Clash) != 0;
            if (hasClashTag && clash == null)
            {
                throw new ArgumentException(
                    "A Clash Damage Resolution Tag requires Clash values.",
                    nameof(clash));
            }

            if (!hasClashTag && clash != null)
            {
                throw new ArgumentException(
                    "Clash values require the Clash Damage Resolution Tag.",
                    nameof(clash));
            }

            if (clash != null && damageEffect == null)
            {
                throw new ArgumentException(
                    "Clash values require a damaging Action.",
                    nameof(clash));
            }

            return clash;
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
            int timelineCost,
            string parameterName)
        {
            if (value < 0 || value > timelineCost)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    value,
                    "Resolution Offset must be between zero and the Action Timeline Cost.");
            }

            return value;
        }

        private static void ValidateResolutionWindow(
            int? start,
            int? end,
            int timelineCost)
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

            RequireResolutionOffset(start.Value, timelineCost, nameof(start));
            RequireResolutionOffset(end.Value, timelineCost, nameof(end));
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
            int previousAdditionalTimelineCost = 0;
            for (int index = 0; index < chargeLevels.Count; index++)
            {
                ComboActionChargeLevelDefinition level = chargeLevels[index]
                    ?? throw new ArgumentException(
                        "Charge Level entries cannot be null.",
                        nameof(chargeLevels));
                if (level.AdditionalTimelineCost <= previousAdditionalTimelineCost)
                {
                    throw new ArgumentException(
                        "Charge Levels must add progressively more Timeline Cost.",
                        nameof(chargeLevels));
                }

                copy[index] = level;
                previousAdditionalTimelineCost = level.AdditionalTimelineCost;
            }

            return Array.AsReadOnly(copy);
        }

        private static IReadOnlyList<ActionActiveWindowDefinition> CopyAndValidateActiveWindows(
            IReadOnlyList<ActionActiveWindowDefinition> activeWindows,
            int timelineCost)
        {
            if (activeWindows == null || activeWindows.Count == 0)
            {
                return Array.Empty<ActionActiveWindowDefinition>();
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var copy = new ActionActiveWindowDefinition[activeWindows.Count];
            for (int index = 0; index < activeWindows.Count; index++)
            {
                ActionActiveWindowDefinition window = activeWindows[index]
                    ?? throw new ArgumentException(
                        "Active Window entries cannot be null.",
                        nameof(activeWindows));
                window.ValidateForActionTimelineCost(timelineCost);
                if (!ids.Add(window.WindowId))
                {
                    throw new ArgumentException(
                        "One Action cannot contain duplicate Active Window IDs.",
                        nameof(activeWindows));
                }

                copy[index] = window;
            }

            return Array.AsReadOnly(copy);
        }

        internal static IReadOnlyList<DisruptionResponseSegmentDefinition>
            CopyAndValidateDisruptionResponseSegments(
                IReadOnlyList<DisruptionResponseSegmentDefinition> segments,
                int timelineCost)
        {
            if (segments == null || segments.Count == 0)
            {
                return new[]
                {
                    new DisruptionResponseSegmentDefinition(
                        1,
                        timelineCost,
                        ActionDisruptionResponse.Continue,
                        ActionDisruptionResponse.Continue,
                        ActionDisruptionResponse.Continue)
                };
            }

            var copy = new DisruptionResponseSegmentDefinition[segments.Count];
            int expectedStart = 1;
            for (int index = 0; index < segments.Count; index++)
            {
                DisruptionResponseSegmentDefinition segment = segments[index]
                    ?? throw new ArgumentException(
                        "Disruption Response Segment entries cannot be null.",
                        nameof(segments));
                if (segment.StartOffset != expectedStart || segment.EndOffset > timelineCost)
                {
                    throw new ArgumentException(
                        "Disruption Response Segments must cover the complete Action Local Timeline without gaps or overlaps.",
                        nameof(segments));
                }

                copy[index] = segment;
                expectedStart = checked(segment.EndOffset + 1);
            }

            if (expectedStart != timelineCost + 1)
            {
                throw new ArgumentException(
                    "Disruption Response Segments must cover the complete Action Local Timeline.",
                    nameof(segments));
            }

            return Array.AsReadOnly(copy);
        }

        private static ActionDisruptionDefinition ValidateOnHitDisruption(
            DamageEffectDefinition damageEffect,
            ActionDisruptionDefinition disruption)
        {
            return ValidateDamageDisruption(damageEffect, disruption, "On Hit");
        }

        private static ActionDisruptionDefinition ValidateOnEvadedDisruption(
            DamageEffectDefinition damageEffect,
            DamageResolutionTag damageTags,
            ActionDisruptionDefinition disruption)
        {
            ValidateDamageDisruption(damageEffect, disruption, "On Evaded");
            if (disruption != null
                && (damageTags & DamageResolutionTag.Overcommit) == 0)
            {
                throw new ArgumentException(
                    "An On Evaded Disruption requires the Overcommit Damage Tag.",
                    nameof(disruption));
            }

            return disruption;
        }

        private static ActionDisruptionDefinition ValidateDamageDisruption(
            DamageEffectDefinition damageEffect,
            ActionDisruptionDefinition disruption,
            string responseName)
        {
            if (disruption != null && damageEffect == null)
            {
                throw new ArgumentException(
                    $"Only a damaging Action can apply an {responseName} Disruption.",
                    nameof(disruption));
            }

            return disruption;
        }
    }
}
