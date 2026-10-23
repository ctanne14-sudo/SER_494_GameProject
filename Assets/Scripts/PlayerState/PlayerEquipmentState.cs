using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SER494.Gameplay.PlayerState
{
    public enum EquipmentCategory
    {
        Weapon,
        Armor,
        Accessory
    }

    public sealed class PlayerEquipmentState
    {
        private readonly HashSet<string> _ownedWeaponIds =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _ownedArmorIds =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _ownedAccessoryIds =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _equippedArmorBySlot =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _equippedAccessoryBySlot =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly IReadOnlyDictionary<string, string> _readOnlyEquippedArmor;
        private readonly IReadOnlyDictionary<string, string> _readOnlyEquippedAccessories;

        public PlayerEquipmentState()
        {
            _readOnlyEquippedArmor =
                new ReadOnlyDictionary<string, string>(_equippedArmorBySlot);
            _readOnlyEquippedAccessories =
                new ReadOnlyDictionary<string, string>(_equippedAccessoryBySlot);
        }

        public IReadOnlyCollection<string> OwnedWeaponIds => _ownedWeaponIds;

        public IReadOnlyCollection<string> OwnedArmorIds => _ownedArmorIds;

        public IReadOnlyCollection<string> OwnedAccessoryIds => _ownedAccessoryIds;

        public string EquippedWeaponId { get; private set; }

        public IReadOnlyDictionary<string, string> EquippedArmorBySlot =>
            _readOnlyEquippedArmor;

        public IReadOnlyDictionary<string, string> EquippedAccessoryBySlot =>
            _readOnlyEquippedAccessories;

        public bool Grant(EquipmentCategory category, string equipmentId)
        {
            string normalizedId = PlayerStateId.Require(equipmentId, nameof(equipmentId));
            return GetOwnedSet(category).Add(normalizedId);
        }

        public bool Owns(EquipmentCategory category, string equipmentId)
        {
            string normalizedId = PlayerStateId.Require(equipmentId, nameof(equipmentId));
            return GetOwnedSet(category).Contains(normalizedId);
        }

        public void EquipWeapon(string weaponId)
        {
            string normalizedId = RequireOwned(
                EquipmentCategory.Weapon,
                weaponId,
                nameof(weaponId));
            EquippedWeaponId = normalizedId;
        }

        public void UnequipWeapon()
        {
            EquippedWeaponId = null;
        }

        public void EquipArmor(string slotId, string armorId)
        {
            EquipInSlot(
                _equippedArmorBySlot,
                EquipmentCategory.Armor,
                slotId,
                armorId,
                nameof(armorId));
        }

        public void EquipAccessory(string slotId, string accessoryId)
        {
            EquipInSlot(
                _equippedAccessoryBySlot,
                EquipmentCategory.Accessory,
                slotId,
                accessoryId,
                nameof(accessoryId));
        }

        public bool UnequipArmor(string slotId)
        {
            return _equippedArmorBySlot.Remove(PlayerStateId.Require(slotId, nameof(slotId)));
        }

        public bool UnequipAccessory(string slotId)
        {
            return _equippedAccessoryBySlot.Remove(PlayerStateId.Require(slotId, nameof(slotId)));
        }

        private void EquipInSlot(
            IDictionary<string, string> assignments,
            EquipmentCategory category,
            string slotId,
            string equipmentId,
            string equipmentParameterName)
        {
            string normalizedSlotId = PlayerStateId.Require(slotId, nameof(slotId));
            string normalizedEquipmentId = RequireOwned(
                category,
                equipmentId,
                equipmentParameterName);

            string previousSlot = null;
            foreach (KeyValuePair<string, string> assignment in assignments)
            {
                if (string.Equals(
                    assignment.Value,
                    normalizedEquipmentId,
                    StringComparison.Ordinal))
                {
                    previousSlot = assignment.Key;
                    break;
                }
            }

            if (previousSlot != null)
            {
                assignments.Remove(previousSlot);
            }

            assignments[normalizedSlotId] = normalizedEquipmentId;
        }

        private string RequireOwned(
            EquipmentCategory category,
            string equipmentId,
            string parameterName)
        {
            string normalizedId = PlayerStateId.Require(equipmentId, parameterName);
            if (!GetOwnedSet(category).Contains(normalizedId))
            {
                throw new InvalidOperationException(
                    $"Cannot equip unowned {category.ToString().ToLowerInvariant()} '{normalizedId}'.");
            }

            return normalizedId;
        }

        private HashSet<string> GetOwnedSet(EquipmentCategory category)
        {
            switch (category)
            {
                case EquipmentCategory.Weapon:
                    return _ownedWeaponIds;
                case EquipmentCategory.Armor:
                    return _ownedArmorIds;
                case EquipmentCategory.Accessory:
                    return _ownedAccessoryIds;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }
    }
}
