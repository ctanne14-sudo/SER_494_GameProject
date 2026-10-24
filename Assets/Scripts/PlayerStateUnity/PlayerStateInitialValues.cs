using System;
using UnityEngine;

namespace SER494.Gameplay.PlayerState.Unity
{
    [Serializable]
    public sealed class PlayerStateInitialValues
    {
        public const string InitialWeaponId = "weapon.greatsword.rusted-iron-sword";
        public const string HeadArmorSlotId = "armor.head";
        public const string UpperBodyArmorSlotId = "armor.upper-body";
        public const string LowerBodyArmorSlotId = "armor.lower-body";
        public const string LinenHeadpieceId = "armor.linen-headpiece";
        public const string LinenShirtId = "armor.linen-shirt";
        public const string LinenHoseId = "armor.linen-hose";
        public const string HealingPotionId = "healing-potion";
        public const string GreaterHealingPotionId = "greater-healing-potion";
        public const string VitalityTonicId = "vitality-tonic";

        private static readonly string[] InitialGreatswordActionIds =
        {
            "greatsword.opening.sweeping-slash",
            "greatsword.opening.draw-slash",
            "greatsword.opening.quick-draw-slash",
            "greatsword.opening.leaping-sweep",
            "greatsword.main.charged-slash",
            "greatsword.main.vertical-slash",
            "greatsword.main.clash-slash",
            "greatsword.main.strong-charged-slash",
            "greatsword.closing.empowered-sheathe",
            "greatsword.closing.double-sweep",
            "greatsword.closing.steadfast-slash",
            "greatsword.closing.sidestep-sheathe",
            "greatsword.linked.shoulder-tackle",
            "greatsword.linked.guarding-charge",
            "greatsword.linked.dragging-charge",
            "greatsword.linked.vertical-slash"
        };

        private static readonly string[] InitialCommonActionIds =
        {
            "common.opening.prepare",
            "common.main.focused-guard-1",
            "common.closing.breather-1",
            "common.linked.use-healing-potion",
            "common.linked.use-vitality-tonic"
        };

        [SerializeField, Min(1)]
        private int maximumHealth = 100;

        [SerializeField, Min(0)]
        private int maximumMana = 30;

        [SerializeField, Min(0)]
        private int maximumCombatStamina = 100;

        [SerializeField, Min(0)]
        private int baseStaminaRecoveryPerRound = 10;

        [SerializeField, Min(1)]
        private int initiallyUnlockedLoadoutSlots = 1;

        public PlayerStateInitialValues()
        {
        }

        public PlayerStateInitialValues(
            int maximumHealth,
            int maximumMana,
            int maximumCombatStamina,
            int initiallyUnlockedLoadoutSlots = 1,
            int baseStaminaRecoveryPerRound = 10)
        {
            this.maximumHealth = maximumHealth;
            this.maximumMana = maximumMana;
            this.maximumCombatStamina = maximumCombatStamina;
            this.initiallyUnlockedLoadoutSlots = initiallyUnlockedLoadoutSlots;
            this.baseStaminaRecoveryPerRound = baseStaminaRecoveryPerRound;
        }

        public int MaximumHealth => maximumHealth;

        public int MaximumMana => maximumMana;

        public int MaximumCombatStamina => maximumCombatStamina;

        public int InitiallyUnlockedLoadoutSlots => initiallyUnlockedLoadoutSlots;

        public int BaseStaminaRecoveryPerRound => baseStaminaRecoveryPerRound;

        public PlayerState CreateState()
        {
            var state = new PlayerState(
                maximumHealth,
                maximumMana,
                maximumCombatStamina,
                initiallyUnlockedLoadoutSlots,
                baseStaminaRecoveryPerRound);

            ApplyStartingEquipment(state);
            ApplyStartingInventory(state);
            ApplyStartingActions(state);
            ApplyStartingGreatswordLoadout(state);
            return state;
        }

        private static void ApplyStartingEquipment(PlayerState state)
        {
            state.Equipment.Grant(EquipmentCategory.Weapon, InitialWeaponId);
            state.Equipment.EquipWeapon(InitialWeaponId);

            state.Equipment.Grant(EquipmentCategory.Armor, LinenHeadpieceId);
            state.Equipment.Grant(EquipmentCategory.Armor, LinenShirtId);
            state.Equipment.Grant(EquipmentCategory.Armor, LinenHoseId);
            state.Equipment.EquipArmor(HeadArmorSlotId, LinenHeadpieceId);
            state.Equipment.EquipArmor(UpperBodyArmorSlotId, LinenShirtId);
            state.Equipment.EquipArmor(LowerBodyArmorSlotId, LinenHoseId);
        }

        private static void ApplyStartingInventory(PlayerState state)
        {
            state.Inventory.Add(HealingPotionId, 3);
            state.Inventory.Add(GreaterHealingPotionId, 1);
            state.Inventory.Add(VitalityTonicId, 3);
        }

        private static void ApplyStartingActions(PlayerState state)
        {
            LearnAll(state, InitialGreatswordActionIds);
            LearnAll(state, InitialCommonActionIds);
        }

        private static void ApplyStartingGreatswordLoadout(PlayerState state)
        {
            PlayerComboLoadoutState loadout = state.ComboLoadouts.ActiveLoadout;
            loadout.Rename("Greatsword Loadout");
            loadout.SetCombo(0, CreateCombo(
                "Charge Timing",
                "greatsword.opening.quick-draw-slash",
                "greatsword.main.charged-slash",
                "greatsword.closing.empowered-sheathe",
                "greatsword.linked.shoulder-tackle"));
            loadout.SetCombo(1, CreateCombo(
                "Clash and Guard",
                "greatsword.opening.leaping-sweep",
                "greatsword.main.clash-slash",
                "greatsword.closing.sidestep-sheathe",
                "greatsword.linked.guarding-charge"));
            loadout.SetCombo(2, CreateCombo(
                "Armor and Recovery",
                "greatsword.opening.sweeping-slash",
                "greatsword.main.strong-charged-slash",
                "greatsword.closing.steadfast-slash",
                "common.linked.use-healing-potion"));
            loadout.SetCombo(3, CreateCombo(
                "Split Damage",
                "greatsword.opening.draw-slash",
                "greatsword.main.vertical-slash",
                "greatsword.closing.double-sweep",
                "greatsword.linked.vertical-slash"));
        }

        private static PreparedComboState CreateCombo(
            string name,
            string openingActionId,
            string mainActionId,
            string closingActionId,
            string linkedActionId)
        {
            return new PreparedComboState(
                name,
                openingActionId,
                mainActionId,
                closingActionId,
                linkedActionId,
                PreparedLinkedActionPlacement.AfterClosing);
        }

        private static void LearnAll(PlayerState state, string[] actionIds)
        {
            for (int index = 0; index < actionIds.Length; index++)
            {
                state.ActionLibrary.Learn(actionIds[index]);
            }
        }
    }
}
