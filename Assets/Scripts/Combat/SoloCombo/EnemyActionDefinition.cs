using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Damage;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public sealed class EnemyActionDefinition
    {
        public EnemyActionDefinition(
            string actionId,
            string displayName,
            int timelineCost,
            int resolutionOffset,
            double damageMultiplier = 0d,
            DamageType damageType = DamageType.Slashing,
            ActionResolutionEffect resolutionEffect = ActionResolutionEffect.None,
            DamageResolutionTag damageTags = DamageResolutionTag.None,
            CombatStatusDefinition statusEffect = null,
            ActionStatusTarget statusTarget = ActionStatusTarget.Self,
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
                timelineCost,
                resolutionOffset,
                CreateSingleDamageEffect(damageMultiplier, damageType),
                ResolveEffect(damageMultiplier, resolutionEffect),
                damageTags,
                statusEffect,
                statusTarget,
                activeWindows,
                onHitDisruption,
                disruptionResponseSegments,
                onGuardedDisruption,
                clash,
                onEvadedDisruption,
                sustainedAttack)
        {
        }

        public EnemyActionDefinition(
            string actionId,
            string displayName,
            int timelineCost,
            int resolutionOffset,
            DamageEffectDefinition damageEffect,
            DamageResolutionTag damageTags = DamageResolutionTag.None,
            CombatStatusDefinition statusEffect = null,
            ActionStatusTarget statusTarget = ActionStatusTarget.Self,
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
                timelineCost,
                resolutionOffset,
                damageEffect,
                damageEffect != null
                    ? ActionResolutionEffect.Damage
                    : ActionResolutionEffect.None,
                damageTags,
                statusEffect,
                statusTarget,
                activeWindows,
                onHitDisruption,
                disruptionResponseSegments,
                onGuardedDisruption,
                clash,
                onEvadedDisruption,
                sustainedAttack)
        {
        }

        private EnemyActionDefinition(
            string actionId,
            string displayName,
            int timelineCost,
            int resolutionOffset,
            DamageEffectDefinition damageEffect,
            ActionResolutionEffect resolutionEffect,
            DamageResolutionTag damageTags,
            CombatStatusDefinition statusEffect,
            ActionStatusTarget statusTarget,
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
                throw new ArgumentException("An enemy Action must have a stable ID.", nameof(actionId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException(
                    "An enemy Action must have a display name.",
                    nameof(displayName));
            }

            if (timelineCost <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timelineCost),
                    timelineCost,
                    "Timeline Cost must be greater than zero.");
            }

            if (resolutionOffset < 0 || resolutionOffset > timelineCost)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resolutionOffset),
                    resolutionOffset,
                    "Resolution Offset must be between zero and the Action Timeline Cost.");
            }

            ActionId = actionId;
            DisplayName = displayName;
            TimelineCost = timelineCost;
            ResolutionOffset = resolutionOffset;
            DamageEffect = damageEffect;
            ResolutionEffect = resolutionEffect;
            DamageTags = ValidateDamageTags(damageEffect, damageTags);
            StatusEffect = ValidateStatusEffect(resolutionEffect, statusEffect);
            StatusTarget = ValidateStatusTarget(statusTarget);
            ActiveWindows = CopyAndValidateActiveWindows(activeWindows, TimelineCost);
            if (onHitDisruption != null && damageEffect == null)
            {
                throw new ArgumentException(
                    "Only a damaging enemy Action can apply an On Hit Disruption.",
                    nameof(onHitDisruption));
            }

            OnHitDisruption = onHitDisruption;
            if (onGuardedDisruption != null && damageEffect == null)
            {
                throw new ArgumentException(
                    "Only a damaging enemy Action can apply an On Guarded Disruption.",
                    nameof(onGuardedDisruption));
            }

            OnGuardedDisruption = onGuardedDisruption;
            Clash = ValidateClash(damageEffect, DamageTags, clash);
            if (onEvadedDisruption != null
                && (DamageTags & DamageResolutionTag.Overcommit) == 0)
            {
                throw new ArgumentException(
                    "An enemy On Evaded Disruption requires the Overcommit Damage Tag.",
                    nameof(onEvadedDisruption));
            }

            if (onEvadedDisruption != null && damageEffect == null)
            {
                throw new ArgumentException(
                    "Only a damaging enemy Action can apply an On Evaded Disruption.",
                    nameof(onEvadedDisruption));
            }

            OnEvadedDisruption = onEvadedDisruption;
            sustainedAttack?.ValidateForAction(TimelineCost, damageEffect != null);
            SustainedAttack = sustainedAttack;
            DisruptionResponseSegments = ComboActionDefinition
                .CopyAndValidateDisruptionResponseSegments(
                    disruptionResponseSegments,
                    TimelineCost);
        }

        public string ActionId { get; }

        public string DisplayName { get; }

        public int TimelineCost { get; }

        public int ResolutionOffset { get; }

        public DamageEffectDefinition DamageEffect { get; }

        public bool HasDamage => DamageEffect != null;

        public ActionResolutionEffect ResolutionEffect { get; }

        public DamageResolutionTag DamageTags { get; }

        public CombatStatusDefinition StatusEffect { get; }

        public ActionStatusTarget StatusTarget { get; }

        public IReadOnlyList<ActionActiveWindowDefinition> ActiveWindows { get; }

        public ActionDisruptionDefinition OnHitDisruption { get; }

        public ActionDisruptionDefinition OnGuardedDisruption { get; }

        public DamageClashDefinition Clash { get; }

        public ActionDisruptionDefinition OnEvadedDisruption { get; }

        public IReadOnlyList<DisruptionResponseSegmentDefinition> DisruptionResponseSegments { get; }

        public SustainedAttackDefinition SustainedAttack { get; }

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
                        "A damaging enemy Action must use the Damage resolution effect.",
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
                    "Damage Resolution Tags require a damaging enemy Action.",
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
                    "Clash values require a damaging enemy Action.",
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
                        "One enemy Action cannot contain duplicate Active Window IDs.",
                        nameof(activeWindows));
                }

                copy[index] = window;
            }

            return Array.AsReadOnly(copy);
        }
    }
}
