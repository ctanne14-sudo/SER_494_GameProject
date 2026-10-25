using System;
using UnityEngine;

namespace SER494.Gameplay.Combat.Authoring.Equipment
{
    public enum AccessoryEffectType
    {
        MaximumHealthFlat,
        AllDamageDealtMultiplier
    }

    [CreateAssetMenu(
        fileName = "NewAccessory",
        menuName = "SER494/Combat/Accessory")]
    public sealed class AccessoryDefinitionAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        private string accessoryId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [Header("Effect")]
        [SerializeField]
        private AccessoryEffectType effectType;

        [SerializeField]
        private float effectAmount;

        public string AccessoryId => accessoryId;

        public string DisplayName => displayName;

        public AccessoryEffectType EffectType => effectType;

        public float EffectAmount => effectAmount;

        public bool TryValidate(out string validationError)
        {
            if (string.IsNullOrWhiteSpace(accessoryId))
            {
                validationError = $"{name} has an empty Accessory ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                validationError = $"{name} has an empty Display Name.";
                return false;
            }

            if (!Enum.IsDefined(typeof(AccessoryEffectType), effectType))
            {
                validationError = $"{name} has an unknown Accessory Effect Type.";
                return false;
            }

            if (effectAmount < 0f || float.IsNaN(effectAmount) || float.IsInfinity(effectAmount))
            {
                validationError = $"{name} has an invalid Accessory Effect Amount.";
                return false;
            }

            validationError = null;
            return true;
        }

        private void OnValidate()
        {
            if (float.IsNaN(effectAmount) || float.IsInfinity(effectAmount))
            {
                effectAmount = 0f;
            }

            effectAmount = Mathf.Max(0f, effectAmount);
        }
    }
}
