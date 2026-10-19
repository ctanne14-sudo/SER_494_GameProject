using System;
using SER494.Gameplay.Combat.Weapons;
using UnityEngine;

namespace SER494.Gameplay.Combat.Authoring.Weapons
{
    [CreateAssetMenu(
        fileName = "NewWeapon",
        menuName = "SER494/Combat/Weapon")]
    public sealed class WeaponDefinitionAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable internal ID. It must be unique across all Weapons.")]
        private string weaponId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [Header("Combat")]
        [SerializeField]
        private WeaponType weaponType = WeaponType.Greatsword;

        [SerializeField]
        [Min(0)]
        private int weaponAttack = 50;

        public string WeaponId => weaponId;

        public string DisplayName => displayName;

        public WeaponType WeaponType => weaponType;

        public int WeaponAttack => weaponAttack;

        public bool TryCreateDefinition(
            out WeaponDefinition definition,
            out string validationError)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(weaponId))
            {
                validationError = $"{name} has an empty Weapon ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                validationError = $"{name} has an empty Display Name.";
                return false;
            }

            if (!Enum.IsDefined(typeof(WeaponType), weaponType))
            {
                validationError = $"{name} has an unknown Weapon Type.";
                return false;
            }

            if (weaponAttack < 0)
            {
                validationError = $"{name} has a negative Weapon Attack.";
                return false;
            }

            definition = new WeaponDefinition(
                weaponId,
                displayName,
                weaponType,
                weaponAttack);
            validationError = null;
            return true;
        }

        private void OnValidate()
        {
            weaponAttack = Mathf.Max(0, weaponAttack);
        }
    }
}
