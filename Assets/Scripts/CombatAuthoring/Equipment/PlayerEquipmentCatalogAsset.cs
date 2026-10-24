using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.Weapons;
using SER494.Gameplay.Combat.Weapons;
using UnityEngine;

namespace SER494.Gameplay.Combat.Authoring.Equipment
{
    [CreateAssetMenu(
        fileName = "PlayerEquipmentCatalog",
        menuName = "SER494/Combat/Player Equipment Catalog")]
    public sealed class PlayerEquipmentCatalogAsset : ScriptableObject
    {
        public const string DefaultResourcesPath = "PlayerState/PlayerEquipmentCatalog";

        [SerializeField]
        private WeaponDefinitionAsset[] weapons = Array.Empty<WeaponDefinitionAsset>();

        [SerializeField]
        private ArmorDefinitionAsset[] armor = Array.Empty<ArmorDefinitionAsset>();

        public static PlayerEquipmentCatalogAsset LoadDefault()
        {
            return Resources.Load<PlayerEquipmentCatalogAsset>(DefaultResourcesPath);
        }

        public bool TryGetWeapon(
            string weaponId,
            out WeaponDefinition definition,
            out string validationError)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                validationError = "The player has no equipped Weapon.";
                return false;
            }

            for (int index = 0; index < weapons.Length; index++)
            {
                WeaponDefinitionAsset asset = weapons[index];
                if (asset != null && string.Equals(
                        asset.WeaponId,
                        weaponId,
                        StringComparison.Ordinal))
                {
                    return asset.TryCreateDefinition(out definition, out validationError);
                }
            }

            validationError = $"Equipped Weapon '{weaponId}' is missing from the equipment catalog.";
            return false;
        }

        public bool TryGetTotalDefense(
            IReadOnlyDictionary<string, string> equippedArmorBySlot,
            out int totalDefense,
            out string validationError)
        {
            totalDefense = 0;
            if (equippedArmorBySlot == null)
            {
                validationError = "Equipped Armor data is unavailable.";
                return false;
            }

            foreach (KeyValuePair<string, string> equipped in equippedArmorBySlot)
            {
                ArmorDefinitionAsset match = FindArmor(equipped.Value);
                if (match == null)
                {
                    validationError =
                        $"Equipped Armor '{equipped.Value}' is missing from the equipment catalog.";
                    return false;
                }

                if (!match.TryValidate(out validationError))
                {
                    return false;
                }

                if (!string.Equals(match.SlotId, equipped.Key, StringComparison.Ordinal))
                {
                    validationError =
                        $"Armor '{match.ArmorId}' belongs to slot '{match.SlotId}', not '{equipped.Key}'.";
                    return false;
                }

                totalDefense = checked(totalDefense + match.Defense);
            }

            validationError = null;
            return true;
        }

        private ArmorDefinitionAsset FindArmor(string armorId)
        {
            for (int index = 0; index < armor.Length; index++)
            {
                ArmorDefinitionAsset asset = armor[index];
                if (asset != null && string.Equals(
                        asset.ArmorId,
                        armorId,
                        StringComparison.Ordinal))
                {
                    return asset;
                }
            }

            return null;
        }
    }
}
