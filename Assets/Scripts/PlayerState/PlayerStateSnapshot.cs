using System;
using System.Collections.Generic;

namespace SER494.Gameplay.PlayerState
{
    [Serializable]
    public sealed class IdAmountSnapshot
    {
        public string Id;
        public int Amount;
    }

    [Serializable]
    public sealed class SlotAssignmentSnapshot
    {
        public string SlotId;
        public string EquipmentId;
    }

    [Serializable]
    public sealed class PlayerEquipmentSnapshot
    {
        public List<string> OwnedWeaponIds = new List<string>();
        public List<string> OwnedArmorIds = new List<string>();
        public List<string> OwnedAccessoryIds = new List<string>();
        public string EquippedWeaponId;
        public List<SlotAssignmentSnapshot> EquippedArmor =
            new List<SlotAssignmentSnapshot>();
        public List<SlotAssignmentSnapshot> EquippedAccessories =
            new List<SlotAssignmentSnapshot>();
    }

    [Serializable]
    public sealed class PreparedComboSnapshot
    {
        public string Name;
        public string OpeningActionId;
        public string MainActionId;
        public string ClosingActionId;
        public string LinkedActionId;
        public int LinkedPlacement = -1;
    }

    [Serializable]
    public sealed class ComboLoadoutSnapshot
    {
        public string LoadoutId;
        public string Name;
        public List<PreparedComboSnapshot> Combos = new List<PreparedComboSnapshot>();
    }

    [Serializable]
    public sealed class PlayerStateSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int CurrentHealth;
        public int BaseMaximumHealth;
        public int EquipmentMaximumHealthBonus;
        public int CurrentMana;
        public int MaximumMana;
        public int MaximumCombatStamina;
        public int BaseStaminaRecoveryPerRound;
        public List<IdAmountSnapshot> Inventory = new List<IdAmountSnapshot>();
        public List<IdAmountSnapshot> Currencies = new List<IdAmountSnapshot>();
        public PlayerEquipmentSnapshot Equipment = new PlayerEquipmentSnapshot();
        public List<string> OwnedActionIds = new List<string>();
        public List<ComboLoadoutSnapshot> ComboLoadouts =
            new List<ComboLoadoutSnapshot>();
        public string ActiveLoadoutId;
    }

    public static class PlayerStateSnapshotMapper
    {
        public static PlayerStateSnapshot Capture(PlayerState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new PlayerStateSnapshot
            {
                CurrentHealth = state.CurrentHealth,
                BaseMaximumHealth = state.BaseMaximumHealth,
                EquipmentMaximumHealthBonus = state.EquipmentMaximumHealthBonus,
                CurrentMana = state.CurrentMana,
                MaximumMana = state.MaximumMana,
                MaximumCombatStamina = state.MaximumCombatStamina,
                BaseStaminaRecoveryPerRound = state.BaseStaminaRecoveryPerRound,
                Inventory = CaptureAmounts(state.Inventory.ItemCounts),
                Currencies = CaptureAmounts(state.Currencies.Amounts),
                Equipment = CaptureEquipment(state.Equipment),
                OwnedActionIds = CaptureSortedIds(state.ActionLibrary.OwnedActionIds),
                ComboLoadouts = CaptureLoadouts(state.ComboLoadouts.Loadouts),
                ActiveLoadoutId = state.ComboLoadouts.ActiveLoadoutId
            };
        }

        public static PlayerState Restore(PlayerStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Version != PlayerStateSnapshot.CurrentVersion)
            {
                throw new NotSupportedException(
                    $"Player State Snapshot version {snapshot.Version} is not supported.");
            }

            int loadoutCount = snapshot.ComboLoadouts?.Count ?? 0;
            if (loadoutCount <= 0)
            {
                throw new ArgumentException(
                    "A Player State Snapshot must contain at least one Combo Loadout.",
                    nameof(snapshot));
            }

            int maximumHealth = checked(
                snapshot.BaseMaximumHealth + snapshot.EquipmentMaximumHealthBonus);
            RequireRange(snapshot.CurrentHealth, 0, maximumHealth, nameof(snapshot.CurrentHealth));
            RequireRange(snapshot.CurrentMana, 0, snapshot.MaximumMana, nameof(snapshot.CurrentMana));

            var state = new PlayerState(
                snapshot.BaseMaximumHealth,
                snapshot.MaximumMana,
                snapshot.MaximumCombatStamina,
                loadoutCount,
                snapshot.BaseStaminaRecoveryPerRound);

            RestoreAmounts(snapshot.Inventory, state.Inventory.SetCount);
            RestoreAmounts(snapshot.Currencies, state.Currencies.Add);
            RestoreEquipment(snapshot.Equipment, state.Equipment);
            RestoreActions(snapshot.OwnedActionIds, state.ActionLibrary);
            RestoreLoadouts(snapshot.ComboLoadouts, snapshot.ActiveLoadoutId, state.ComboLoadouts);

            state.SetEquipmentMaximumHealthBonus(snapshot.EquipmentMaximumHealthBonus);
            state.SetCurrentHealth(snapshot.CurrentHealth);
            state.SetCurrentMana(snapshot.CurrentMana);
            return state;
        }

        private static List<IdAmountSnapshot> CaptureAmounts(
            IReadOnlyDictionary<string, int> amounts)
        {
            var result = new List<IdAmountSnapshot>(amounts.Count);
            foreach (KeyValuePair<string, int> entry in amounts)
            {
                result.Add(new IdAmountSnapshot { Id = entry.Key, Amount = entry.Value });
            }

            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            return result;
        }

        private static PlayerEquipmentSnapshot CaptureEquipment(PlayerEquipmentState equipment)
        {
            return new PlayerEquipmentSnapshot
            {
                OwnedWeaponIds = CaptureSortedIds(equipment.OwnedWeaponIds),
                OwnedArmorIds = CaptureSortedIds(equipment.OwnedArmorIds),
                OwnedAccessoryIds = CaptureSortedIds(equipment.OwnedAccessoryIds),
                EquippedWeaponId = equipment.EquippedWeaponId,
                EquippedArmor = CaptureAssignments(equipment.EquippedArmorBySlot),
                EquippedAccessories = CaptureAssignments(
                    equipment.EquippedAccessoryBySlot)
            };
        }

        private static List<string> CaptureSortedIds(IEnumerable<string> source)
        {
            var result = new List<string>(source);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static List<SlotAssignmentSnapshot> CaptureAssignments(
            IReadOnlyDictionary<string, string> assignments)
        {
            var result = new List<SlotAssignmentSnapshot>(assignments.Count);
            foreach (KeyValuePair<string, string> entry in assignments)
            {
                result.Add(new SlotAssignmentSnapshot
                {
                    SlotId = entry.Key,
                    EquipmentId = entry.Value
                });
            }

            result.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.SlotId, right.SlotId));
            return result;
        }

        private static List<ComboLoadoutSnapshot> CaptureLoadouts(
            IReadOnlyList<PlayerComboLoadoutState> loadouts)
        {
            var result = new List<ComboLoadoutSnapshot>(loadouts.Count);
            for (int loadoutIndex = 0; loadoutIndex < loadouts.Count; loadoutIndex++)
            {
                PlayerComboLoadoutState loadout = loadouts[loadoutIndex];
                var savedLoadout = new ComboLoadoutSnapshot
                {
                    LoadoutId = loadout.LoadoutId,
                    Name = loadout.Name
                };
                for (int comboIndex = 0; comboIndex < loadout.Combos.Count; comboIndex++)
                {
                    PreparedComboState combo = loadout.Combos[comboIndex];
                    savedLoadout.Combos.Add(new PreparedComboSnapshot
                    {
                        Name = combo.Name,
                        OpeningActionId = combo.OpeningActionId,
                        MainActionId = combo.MainActionId,
                        ClosingActionId = combo.ClosingActionId,
                        LinkedActionId = combo.LinkedActionId,
                        LinkedPlacement = combo.LinkedPlacement.HasValue
                            ? (int)combo.LinkedPlacement.Value
                            : -1
                    });
                }

                result.Add(savedLoadout);
            }

            return result;
        }

        private static void RestoreAmounts(
            IEnumerable<IdAmountSnapshot> savedAmounts,
            Action<string, int> restore)
        {
            if (savedAmounts == null)
            {
                return;
            }

            foreach (IdAmountSnapshot entry in savedAmounts)
            {
                if (entry == null)
                {
                    throw new ArgumentException("Saved amount entries cannot be null.");
                }

                restore(entry.Id, entry.Amount);
            }
        }

        private static void RestoreEquipment(
            PlayerEquipmentSnapshot snapshot,
            PlayerEquipmentState equipment)
        {
            if (snapshot == null)
            {
                return;
            }

            GrantAll(equipment, EquipmentCategory.Weapon, snapshot.OwnedWeaponIds);
            GrantAll(equipment, EquipmentCategory.Armor, snapshot.OwnedArmorIds);
            GrantAll(equipment, EquipmentCategory.Accessory, snapshot.OwnedAccessoryIds);

            if (!string.IsNullOrWhiteSpace(snapshot.EquippedWeaponId))
            {
                equipment.EquipWeapon(snapshot.EquippedWeaponId);
            }

            RestoreAssignments(snapshot.EquippedArmor, equipment.EquipArmor);
            RestoreAssignments(snapshot.EquippedAccessories, equipment.EquipAccessory);
        }

        private static void GrantAll(
            PlayerEquipmentState equipment,
            EquipmentCategory category,
            IEnumerable<string> equipmentIds)
        {
            if (equipmentIds == null)
            {
                return;
            }

            foreach (string equipmentId in equipmentIds)
            {
                equipment.Grant(category, equipmentId);
            }
        }

        private static void RestoreAssignments(
            IEnumerable<SlotAssignmentSnapshot> assignments,
            Action<string, string> equip)
        {
            if (assignments == null)
            {
                return;
            }

            foreach (SlotAssignmentSnapshot assignment in assignments)
            {
                if (assignment == null)
                {
                    throw new ArgumentException("Equipment assignments cannot be null.");
                }

                equip(assignment.SlotId, assignment.EquipmentId);
            }
        }

        private static void RestoreActions(
            IEnumerable<string> actionIds,
            PlayerActionLibraryState actionLibrary)
        {
            if (actionIds == null)
            {
                return;
            }

            foreach (string actionId in actionIds)
            {
                actionLibrary.Learn(actionId);
            }
        }

        private static void RestoreLoadouts(
            IReadOnlyList<ComboLoadoutSnapshot> snapshots,
            string activeLoadoutId,
            PlayerComboLoadoutCollection loadouts)
        {
            for (int loadoutIndex = 0; loadoutIndex < snapshots.Count; loadoutIndex++)
            {
                ComboLoadoutSnapshot snapshot = snapshots[loadoutIndex]
                    ?? throw new ArgumentException("Combo Loadout entries cannot be null.");
                PlayerComboLoadoutState loadout = loadouts.Loadouts[loadoutIndex];
                if (!string.Equals(
                        snapshot.LoadoutId,
                        loadout.LoadoutId,
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"Expected Loadout ID '{loadout.LoadoutId}' but found "
                        + $"'{snapshot.LoadoutId}'.");
                }

                if (snapshot.Combos == null
                    || snapshot.Combos.Count != PlayerComboLoadoutState.ComboCount)
                {
                    throw new ArgumentException(
                        $"Loadout '{snapshot.LoadoutId}' must contain exactly "
                        + $"{PlayerComboLoadoutState.ComboCount} Combos.");
                }

                loadout.Rename(snapshot.Name);
                for (int comboIndex = 0; comboIndex < snapshot.Combos.Count; comboIndex++)
                {
                    loadout.SetCombo(
                        comboIndex,
                        RestoreCombo(snapshot.Combos[comboIndex]));
                }
            }

            loadouts.SetActive(activeLoadoutId);
        }

        private static PreparedComboState RestoreCombo(PreparedComboSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentException("Prepared Combo entries cannot be null.");
            }

            PreparedLinkedActionPlacement? placement = null;
            if (snapshot.LinkedPlacement >= 0)
            {
                if (!Enum.IsDefined(
                        typeof(PreparedLinkedActionPlacement),
                        snapshot.LinkedPlacement))
                {
                    throw new ArgumentException(
                        $"Unknown Linked Action placement '{snapshot.LinkedPlacement}'.");
                }

                placement = (PreparedLinkedActionPlacement)snapshot.LinkedPlacement;
            }

            return new PreparedComboState(
                snapshot.Name,
                snapshot.OpeningActionId,
                snapshot.MainActionId,
                snapshot.ClosingActionId,
                snapshot.LinkedActionId,
                placement);
        }

        private static void RequireRange(
            int value,
            int minimum,
            int maximum,
            string parameterName)
        {
            if (value < minimum || value > maximum)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, null);
            }
        }
    }
}
