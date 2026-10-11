using System;
using System.Collections.Generic;

namespace SER494.Gameplay.Combat.Damage
{
    public readonly struct DamageFormulaInput
    {
        public DamageFormulaInput(
            double actionMultiplier,
            int weaponAttack,
            double attackModifier,
            int baseDefense,
            double defenseModifier,
            double applicableDamageModifier,
            double clashDamageReduction)
        {
            ActionMultiplier = RequireFiniteNonNegative(
                actionMultiplier,
                nameof(actionMultiplier));
            WeaponAttack = RequireNonNegative(weaponAttack, nameof(weaponAttack));
            AttackModifier = RequireFinite(attackModifier, nameof(attackModifier));
            BaseDefense = RequireNonNegative(baseDefense, nameof(baseDefense));
            DefenseModifier = RequireFinite(defenseModifier, nameof(defenseModifier));
            ApplicableDamageModifier = RequireFinite(
                applicableDamageModifier,
                nameof(applicableDamageModifier));
            ClashDamageReduction = RequireFinite(
                clashDamageReduction,
                nameof(clashDamageReduction));
        }

        public double ActionMultiplier { get; }

        public int WeaponAttack { get; }

        public double AttackModifier { get; }

        public int BaseDefense { get; }

        public double DefenseModifier { get; }

        // This is the additive sum of every applicable Damage Dealt and
        // Damage Taken modifier for one Damage Component.
        public double ApplicableDamageModifier { get; }

        public double ClashDamageReduction { get; }

        private static int RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }

            return value;
        }

        private static double RequireFiniteNonNegative(double value, string parameterName)
        {
            RequireFinite(value, parameterName);
            if (value < 0d)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }

            return value;
        }

        private static double RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be finite.");
            }

            return value;
        }
    }

    public readonly struct DamageFormulaResult
    {
        internal DamageFormulaResult(double unroundedDamage, int finalDamage)
        {
            UnroundedDamage = unroundedDamage;
            FinalDamage = finalDamage;
        }

        public double UnroundedDamage { get; }

        public int FinalDamage { get; }
    }

    public readonly struct DamageComponentResult
    {
        internal DamageComponentResult(
            DamageComponentDefinition component,
            double applicableDamageModifier,
            double unroundedDamage)
        {
            Component = component;
            ApplicableDamageModifier = applicableDamageModifier;
            UnroundedDamage = unroundedDamage;
        }

        public DamageComponentDefinition Component { get; }

        public double ApplicableDamageModifier { get; }

        public double UnroundedDamage { get; }
    }

    public sealed class DamageEffectResult
    {
        internal DamageEffectResult(
            IReadOnlyList<DamageComponentResult> components,
            double unroundedDamage,
            int finalDamage)
        {
            Components = components ?? throw new ArgumentNullException(nameof(components));
            UnroundedDamage = unroundedDamage;
            FinalDamage = finalDamage;
        }

        public IReadOnlyList<DamageComponentResult> Components { get; }

        public double UnroundedDamage { get; }

        public int FinalDamage { get; }
    }
}
