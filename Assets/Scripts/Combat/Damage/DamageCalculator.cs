using System;
using System.Collections.Generic;

namespace SER494.Gameplay.Combat.Damage
{
    public static class DamageCalculator
    {
        private static readonly IReadOnlyList<DamageModifier> NoModifiers =
            Array.Empty<DamageModifier>();

        public static DamageFormulaResult CalculateOrdinaryComponent(
            DamageFormulaInput input,
            BattleDamageConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            double attackMultiplier = Math.Max(0d, 1d + input.AttackModifier);
            double modifiedDefense = Math.Max(
                0d,
                input.BaseDefense * (1d + input.DefenseModifier));
            double defenseMultiplier = configuration.DefenseConstant
                / (configuration.DefenseConstant + modifiedDefense);
            double damageMultiplier = Math.Max(
                0d,
                1d + input.ApplicableDamageModifier);
            double clashMultiplier = Math.Max(
                0d,
                Math.Min(1d, 1d - input.ClashDamageReduction));

            double unroundedDamage = input.ActionMultiplier
                * input.WeaponAttack
                * attackMultiplier
                * defenseMultiplier
                * damageMultiplier
                * clashMultiplier;
            unroundedDamage = Math.Max(0d, unroundedDamage);

            int finalDamage = checked((int)Math.Round(
                unroundedDamage,
                MidpointRounding.AwayFromZero));
            return new DamageFormulaResult(unroundedDamage, finalDamage);
        }

        public static DamageEffectResult CalculateEffect(
            DamageEffectDefinition effect,
            int weaponAttack,
            double attackModifier,
            int baseDefense,
            double defenseModifier,
            IReadOnlyList<DamageModifier> damageDealtModifiers,
            IReadOnlyList<DamageModifier> damageTakenModifiers,
            double clashDamageReduction,
            BattleDamageConfiguration configuration,
            double additionalApplicableDamageModifier = 0d)
        {
            if (effect == null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            if (double.IsNaN(additionalApplicableDamageModifier)
                || double.IsInfinity(additionalApplicableDamageModifier))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(additionalApplicableDamageModifier));
            }

            IReadOnlyList<DamageModifier> dealt = damageDealtModifiers ?? NoModifiers;
            IReadOnlyList<DamageModifier> taken = damageTakenModifiers ?? NoModifiers;
            var componentResults = new DamageComponentResult[effect.Components.Count];
            double totalUnroundedDamage = 0d;

            for (int index = 0; index < effect.Components.Count; index++)
            {
                DamageComponentDefinition component = effect.Components[index];
                double applicableModifier = SumApplicable(dealt, component)
                    + SumApplicable(taken, component)
                    + additionalApplicableDamageModifier;
                DamageFormulaResult componentResult = CalculateOrdinaryComponent(
                    new DamageFormulaInput(
                        component.ActionMultiplier,
                        weaponAttack,
                        attackModifier,
                        baseDefense,
                        defenseModifier,
                        applicableModifier,
                        clashDamageReduction),
                    configuration);

                componentResults[index] = new DamageComponentResult(
                    component,
                    applicableModifier,
                    componentResult.UnroundedDamage);
                totalUnroundedDamage += componentResult.UnroundedDamage;
            }

            int finalDamage = checked((int)Math.Round(
                Math.Max(0d, totalUnroundedDamage),
                MidpointRounding.AwayFromZero));
            return new DamageEffectResult(
                Array.AsReadOnly(componentResults),
                totalUnroundedDamage,
                finalDamage);
        }

        private static double SumApplicable(
            IReadOnlyList<DamageModifier> modifiers,
            DamageComponentDefinition component)
        {
            double total = 0d;
            for (int index = 0; index < modifiers.Count; index++)
            {
                DamageModifier modifier = modifiers[index];
                if (modifier.AppliesTo(component))
                {
                    total += modifier.Amount;
                }
            }

            return total;
        }
    }
}
