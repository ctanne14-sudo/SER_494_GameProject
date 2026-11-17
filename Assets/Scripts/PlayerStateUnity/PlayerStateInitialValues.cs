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
            "greatsword.opening.vertical-slash",
            "greatsword.main.charged-slash",
            "greatsword.main.clash-slash",
            "greatsword.main.rising-strong-strike",
            "greatsword.closing.empowered-sheathe",
            "greatsword.closing.sidestep-sheathe",
            "greatsword.closing.double-sweep",
            "greatsword.linked.shoulder-tackle",
            "greatsword.linked.guarding-charge",
            "greatsword.linked.vertical-slash"
        };

        private static readonly string[] InitialCommonActionIds =
        {
            "common.opening.prepare",
            "common.main.use-healing-potion",
            "common.closing.breather-1",
            "common.linked.use-vitality-tonic"
        };

        [SerializeField, Min(1)]
        private int maximumHealth = 500;

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

        private static void LearnAll(PlayerState state, string[] actionIds)
        {
            for (int index = 0; index < actionIds.Length; index++)
            {
                state.ActionLibrary.Learn(actionIds[index]);
            }
        }
    }
}
