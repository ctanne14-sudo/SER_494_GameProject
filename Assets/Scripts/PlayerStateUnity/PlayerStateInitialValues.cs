using System;
using UnityEngine;

namespace SER494.Gameplay.PlayerState.Unity
{
    [Serializable]
    public sealed class PlayerStateInitialValues
    {
        [SerializeField, Min(1)]
        private int maximumHealth = 100;

        [SerializeField, Min(0)]
        private int maximumMana = 30;

        [SerializeField, Min(0)]
        private int maximumCombatStamina = 30;

        [SerializeField, Min(1)]
        private int initiallyUnlockedLoadoutSlots = 1;

        public PlayerStateInitialValues()
        {
        }

        public PlayerStateInitialValues(
            int maximumHealth,
            int maximumMana,
            int maximumCombatStamina,
            int initiallyUnlockedLoadoutSlots = 1)
        {
            this.maximumHealth = maximumHealth;
            this.maximumMana = maximumMana;
            this.maximumCombatStamina = maximumCombatStamina;
            this.initiallyUnlockedLoadoutSlots = initiallyUnlockedLoadoutSlots;
        }

        public int MaximumHealth => maximumHealth;

        public int MaximumMana => maximumMana;

        public int MaximumCombatStamina => maximumCombatStamina;

        public int InitiallyUnlockedLoadoutSlots => initiallyUnlockedLoadoutSlots;

        public PlayerState CreateState()
        {
            return new PlayerState(
                maximumHealth,
                maximumMana,
                maximumCombatStamina,
                initiallyUnlockedLoadoutSlots);
        }
    }
}
