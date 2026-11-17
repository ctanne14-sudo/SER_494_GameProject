using System;
using System.Collections.Generic;

namespace SER494.Gameplay.Combat.Damage
{
    public enum DamageFamily
    {
        Physical,
        Magical
    }

    public enum DamageType
    {
        Slashing,
        Blunt,
        Piercing,
        Fire,
        Ice,
        Lightning
    }

    public readonly struct DamageComponentDefinition
    {
        public DamageComponentDefinition(DamageType type, double actionMultiplier)
        {
            if (!Enum.IsDefined(typeof(DamageType), type))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(type),
                    type,
                    "Unknown Damage Type.");
            }

            if (double.IsNaN(actionMultiplier)
                || double.IsInfinity(actionMultiplier)
                || actionMultiplier <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actionMultiplier),
                    actionMultiplier,
                    "A Damage Component multiplier must be finite and greater than zero.");
            }

            Type = type;
            ActionMultiplier = actionMultiplier;
        }

        public DamageType Type { get; }

        public DamageFamily Family => Type.GetFamily();

        public double ActionMultiplier { get; }
    }

    public sealed class DamageEffectDefinition
    {
        private readonly IReadOnlyList<DamageComponentDefinition> _components;

        public DamageEffectDefinition(IReadOnlyList<DamageComponentDefinition> components)
        {
            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            if (components.Count == 0)
            {
                throw new ArgumentException(
                    "A Damage Effect must contain at least one Damage Component.",
                    nameof(components));
            }

            var copy = new DamageComponentDefinition[components.Count];
            for (int index = 0; index < components.Count; index++)
            {
                if (components[index].ActionMultiplier <= 0d
                    || double.IsNaN(components[index].ActionMultiplier)
                    || double.IsInfinity(components[index].ActionMultiplier))
                {
                    throw new ArgumentException(
                        "Every Damage Component must be fully initialized and valid.",
                        nameof(components));
                }

                components[index].Type.GetFamily();
                copy[index] = components[index];
            }

            _components = Array.AsReadOnly(copy);
        }

        public IReadOnlyList<DamageComponentDefinition> Components => _components;

        public static DamageEffectDefinition Single(DamageType type, double actionMultiplier)
        {
            return new DamageEffectDefinition(
                new[] { new DamageComponentDefinition(type, actionMultiplier) });
        }
    }

    public static class DamageTypeExtensions
    {
        public static DamageFamily GetFamily(this DamageType type)
        {
            switch (type)
            {
                case DamageType.Slashing:
                case DamageType.Blunt:
                case DamageType.Piercing:
                    return DamageFamily.Physical;
                case DamageType.Fire:
                case DamageType.Ice:
                case DamageType.Lightning:
                    return DamageFamily.Magical;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown Damage Type.");
            }
        }
    }
}
