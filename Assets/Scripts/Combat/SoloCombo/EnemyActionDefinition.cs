using System;
using SER494.Gameplay.Combat.Damage;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public sealed class EnemyActionDefinition
    {
        public EnemyActionDefinition(
            string actionId,
            string displayName,
            int loadCost,
            int resolutionOffset,
            double damageMultiplier = 0d,
            DamageType damageType = DamageType.Slashing,
            ActionResolutionEffect resolutionEffect = ActionResolutionEffect.None)
            : this(
                actionId,
                displayName,
                loadCost,
                resolutionOffset,
                CreateSingleDamageEffect(damageMultiplier, damageType),
                ResolveEffect(damageMultiplier, resolutionEffect))
        {
        }

        public EnemyActionDefinition(
            string actionId,
            string displayName,
            int loadCost,
            int resolutionOffset,
            DamageEffectDefinition damageEffect)
            : this(
                actionId,
                displayName,
                loadCost,
                resolutionOffset,
                damageEffect,
                damageEffect != null
                    ? ActionResolutionEffect.Damage
                    : ActionResolutionEffect.None)
        {
        }

        private EnemyActionDefinition(
            string actionId,
            string displayName,
            int loadCost,
            int resolutionOffset,
            DamageEffectDefinition damageEffect,
            ActionResolutionEffect resolutionEffect)
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

            if (loadCost <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(loadCost),
                    loadCost,
                    "Load Cost must be greater than zero.");
            }

            if (resolutionOffset < 0 || resolutionOffset > loadCost)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resolutionOffset),
                    resolutionOffset,
                    "Resolution Offset must be between zero and the Action Load Cost.");
            }

            ActionId = actionId;
            DisplayName = displayName;
            LoadCost = loadCost;
            ResolutionOffset = resolutionOffset;
            DamageEffect = damageEffect;
            ResolutionEffect = resolutionEffect;
        }

        public string ActionId { get; }

        public string DisplayName { get; }

        public int LoadCost { get; }

        public int ResolutionOffset { get; }

        public DamageEffectDefinition DamageEffect { get; }

        public bool HasDamage => DamageEffect != null;

        public ActionResolutionEffect ResolutionEffect { get; }

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
    }
}
