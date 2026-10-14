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

        internal void ValidateForActionLoad(int loadCost)
        {
            if (EndOffset > loadCost)
            {
                throw new ArgumentException(
                    $"Active Window '{DisplayName}' ends outside its Action Load.");
            }
        }
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

        internal void ValidateForAction(int loadCost, bool hasDamage)
        {
            if (!hasDamage)
            {
                throw new ArgumentException("Only a damaging Action can be a Sustained Attack.");
            }

            if (WindowEndOffset > loadCost)
            {
                throw new ArgumentException("A Sustained Attack window cannot extend beyond its Action Load.");
            }
        }
    }

    [Flags]
    public enum DamageResolutionTag
    {
        None = 0,
        Area = 1 << 0,
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
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels = null,
            IReadOnlyList<ActionActiveWindowDefinition> activeWindows = null,
            ActionDisruptionDefinition onHitDisruption = null,
            IReadOnlyList<DisruptionResponseSegmentDefinition> disruptionResponseSegments = null,
            ActionDisruptionDefinition onGuardedDisruption = null,
            DamageClashDefinition clash = null,
            ActionDisruptionDefinition onEvadedDisruption = null,
            SustainedAttackDefinition sustainedAttack = null)
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
                chargeLevels,
                activeWindows,
                onHitDisruption,
                disruptionResponseSegments,
                onGuardedDisruption,
                clash,
                onEvadedDisruption,
                sustainedAttack)
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
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels = null,
            IReadOnlyList<ActionActiveWindowDefinition> activeWindows = null,
            ActionDisruptionDefinition onHitDisruption = null,
            IReadOnlyList<DisruptionResponseSegmentDefinition> disruptionResponseSegments = null,
            ActionDisruptionDefinition onGuardedDisruption = null,
            DamageClashDefinition clash = null,
            ActionDisruptionDefinition onEvadedDisruption = null,
            SustainedAttackDefinition sustainedAttack = null)
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
                chargeLevels,
                activeWindows,
                onHitDisruption,
                disruptionResponseSegments,
                onGuardedDisruption,
                clash,
                onEvadedDisruption,
                sustainedAttack)
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
            IReadOnlyList<ComboActionChargeLevelDefinition> chargeLevels,
            IReadOnlyList<ActionActiveWindowDefinition> activeWindows,
            ActionDisruptionDefinition onHitDisruption,
            IReadOnlyList<DisruptionResponseSegmentDefinition> disruptionResponseSegments,
            ActionDisruptionDefinition onGuardedDisruption,
            DamageClashDefinition clash,
            ActionDisruptionDefinition onEvadedDisruption,
            SustainedAttackDefinition sustainedAttack)
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
            ActiveWindows = CopyAndValidateActiveWindows(activeWindows, LoadCost);
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
            DisruptionResponseSegments = CopyAndValidateDisruptionResponseSegments(
                disruptionResponseSegments,
                LoadCost);
            sustainedAttack?.ValidateForAction(LoadCost, damageEffect != null);
            if (sustainedAttack != null && (HasResolutionWindow || ChargeLevels.Count > 0))
            {
                throw new ArgumentException(
                    "The MVP does not combine Sustained Attacks with Resolution Windows or Charge Levels.");
            }

            SustainedAttack = sustainedAttack;
            if (ChargeLevels.Count > 0 && ActiveWindows.Count > 0)
            {
                throw new ArgumentException(
                    "The MVP does not yet combine Charge Levels and Active Windows on one Action.");
            }
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

        public IReadOnlyList<ActionActiveWindowDefinition> ActiveWindows { get; }

        public ActionDisruptionDefinition OnHitDisruption { get; }

        public ActionDisruptionDefinition OnGuardedDisruption { get; }

        public DamageClashDefinition Clash { get; }

        public ActionDisruptionDefinition OnEvadedDisruption { get; }

        public IReadOnlyList<DisruptionResponseSegmentDefinition> DisruptionResponseSegments { get; }

        public SustainedAttackDefinition SustainedAttack { get; }

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

        private static IReadOnlyList<ActionActiveWindowDefinition> CopyAndValidateActiveWindows(
            IReadOnlyList<ActionActiveWindowDefinition> activeWindows,
            int loadCost)
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
                window.ValidateForActionLoad(loadCost);
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
                int loadCost)
        {
            if (segments == null || segments.Count == 0)
            {
                return new[]
                {
                    new DisruptionResponseSegmentDefinition(
                        1,
                        loadCost,
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
                if (segment.StartOffset != expectedStart || segment.EndOffset > loadCost)
                {
                    throw new ArgumentException(
                        "Disruption Response Segments must cover the complete Action Local Timeline without gaps or overlaps.",
                        nameof(segments));
                }

                copy[index] = segment;
                expectedStart = checked(segment.EndOffset + 1);
            }

            if (expectedStart != loadCost + 1)
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
