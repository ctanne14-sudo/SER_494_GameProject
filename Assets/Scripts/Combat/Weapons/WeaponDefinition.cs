using System;

namespace SER494.Gameplay.Combat.Weapons
{
    public enum WeaponType
    {
        Greatsword,
        SwordAndShield,
        SpearAndShield,
        Bow,
        Bladeguard
    }

    [Flags]
    public enum CompatibleWeaponTypes
    {
        None = 0,
        Greatsword = 1 << 0,
        SwordAndShield = 1 << 1,
        SpearAndShield = 1 << 2,
        Bow = 1 << 3,
        Bladeguard = 1 << 4,
        All = Greatsword | SwordAndShield | SpearAndShield | Bow | Bladeguard
    }

    public sealed class WeaponDefinition
    {
        public WeaponDefinition(
            string weaponId,
            string displayName,
            WeaponType weaponType,
            int weaponAttack)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                throw new ArgumentException("A Weapon must have a stable ID.", nameof(weaponId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A Weapon must have a display name.", nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(WeaponType), weaponType))
            {
                throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, "Unknown Weapon Type.");
            }

            if (weaponAttack < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weaponAttack), weaponAttack, "Weapon Attack cannot be negative.");
            }

            WeaponId = weaponId.Trim();
            DisplayName = displayName.Trim();
            WeaponType = weaponType;
            WeaponAttack = weaponAttack;
        }

        public string WeaponId { get; }

        public string DisplayName { get; }

        public WeaponType WeaponType { get; }

        public int WeaponAttack { get; }
    }

    public static class WeaponCompatibility
    {
        public static CompatibleWeaponTypes ToCompatibilityFlag(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Greatsword:
                    return CompatibleWeaponTypes.Greatsword;
                case WeaponType.SwordAndShield:
                    return CompatibleWeaponTypes.SwordAndShield;
                case WeaponType.SpearAndShield:
                    return CompatibleWeaponTypes.SpearAndShield;
                case WeaponType.Bow:
                    return CompatibleWeaponTypes.Bow;
                case WeaponType.Bladeguard:
                    return CompatibleWeaponTypes.Bladeguard;
                default:
                    throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, "Unknown Weapon Type.");
            }
        }

        public static bool Supports(
            this CompatibleWeaponTypes compatibleWeaponTypes,
            WeaponType weaponType)
        {
            return (compatibleWeaponTypes & ToCompatibilityFlag(weaponType)) != 0;
        }
    }
}
