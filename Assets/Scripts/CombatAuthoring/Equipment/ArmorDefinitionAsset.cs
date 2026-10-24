using UnityEngine;

namespace SER494.Gameplay.Combat.Authoring.Equipment
{
    [CreateAssetMenu(
        fileName = "NewArmor",
        menuName = "SER494/Combat/Armor")]
    public sealed class ArmorDefinitionAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable internal ID. It must be unique across all Armor.")]
        private string armorId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [SerializeField]
        [Tooltip("Stable Player State slot ID, such as armor.head.")]
        private string slotId = string.Empty;

        [Header("Combat")]
        [SerializeField, Min(0)]
        private int defense;

        public string ArmorId => armorId;

        public string DisplayName => displayName;

        public string SlotId => slotId;

        public int Defense => defense;

        public bool TryValidate(out string validationError)
        {
            if (string.IsNullOrWhiteSpace(armorId))
            {
                validationError = $"{name} has an empty Armor ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                validationError = $"{name} has an empty Display Name.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(slotId))
            {
                validationError = $"{name} has an empty Armor Slot ID.";
                return false;
            }

            if (defense < 0)
            {
                validationError = $"{name} has negative Defense.";
                return false;
            }

            validationError = null;
            return true;
        }

        private void OnValidate()
        {
            defense = Mathf.Max(0, defense);
        }
    }
}
