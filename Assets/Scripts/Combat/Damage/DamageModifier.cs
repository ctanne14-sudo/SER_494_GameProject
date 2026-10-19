using System;

namespace SER494.Gameplay.Combat.Damage
{
    public enum DamageModifierScope
    {
        All,
        Family,
        Type
    }

    public readonly struct DamageModifier
    {
        private DamageModifier(
            DamageModifierScope scope,
            double amount,
            DamageFamily family,
            DamageType type)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount))
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be finite.");
            }

            Scope = scope;
            Amount = amount;
            Family = family;
            Type = type;
        }

        public DamageModifierScope Scope { get; }

        public double Amount { get; }

        public DamageFamily Family { get; }

        public DamageType Type { get; }

        public bool AppliesTo(DamageComponentDefinition component)
        {
            switch (Scope)
            {
                case DamageModifierScope.All:
                    return true;
                case DamageModifierScope.Family:
                    return Family == component.Family;
                case DamageModifierScope.Type:
                    return Type == component.Type;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static DamageModifier All(double amount)
        {
            return new DamageModifier(
                DamageModifierScope.All,
                amount,
                default,
                default);
        }

        public static DamageModifier ForFamily(DamageFamily family, double amount)
        {
            return new DamageModifier(
                DamageModifierScope.Family,
                amount,
                family,
                default);
        }

        public static DamageModifier ForType(DamageType type, double amount)
        {
            return new DamageModifier(
                DamageModifierScope.Type,
                amount,
                default,
                type);
        }
    }
}
