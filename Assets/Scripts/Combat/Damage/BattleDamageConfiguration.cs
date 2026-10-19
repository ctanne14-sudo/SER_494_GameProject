using System;

namespace SER494.Gameplay.Combat.Damage
{
    public sealed class BattleDamageConfiguration
    {
        public BattleDamageConfiguration(double defenseConstant)
        {
            if (double.IsNaN(defenseConstant)
                || double.IsInfinity(defenseConstant)
                || defenseConstant <= 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(defenseConstant),
                    defenseConstant,
                    "The Defense constant must be a finite value greater than zero.");
            }

            DefenseConstant = defenseConstant;
        }

        public double DefenseConstant { get; }

        public static BattleDamageConfiguration CreateMvpDefault()
        {
            return new BattleDamageConfiguration(defenseConstant: 100d);
        }
    }
}
