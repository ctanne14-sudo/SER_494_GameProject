using System;
using SER494.Gameplay.Combat.Damage;
using UnityEngine;

namespace SER494.Gameplay.Combat.Authoring.Equipment
{
    public enum ArmorEffectType
    {
        None,
        DamageTakenMultiplier,
        MaximumHealthMultiplier,
        HealingPotionMultiplier,
        GuardCounterPiercingMultiplier,
        AllDamageDealtMultiplier
    }

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

        [Header("Series and Effect")]
        [SerializeField]
        [Tooltip("Stable series ID used to determine three-piece set bonuses.")]
        private string seriesId = string.Empty;

        [SerializeField]
        private ArmorEffectType effectType;

        [SerializeField]
        private DamageType effectDamageType;

        [SerializeField, Min(0f)]
        [Tooltip("Decimal modifier. For example, 0.04 means four percent.")]
        private float effectAmount;

        [SerializeField, TextArea]
        [Tooltip("Player-facing description. It may describe an effect whose runtime behavior is implemented by a later combat system.")]
        private string effectDescription = string.Empty;

        [SerializeField, TextArea]
        private string setEffectDescription = string.Empty;

        public string ArmorId => armorId;

        public string DisplayName => displayName;

        public string SlotId => slotId;

        public int Defense => defense;

        public string SeriesId => seriesId;

        public ArmorEffectType EffectType => effectType;

        public DamageType EffectDamageType => effectDamageType;

        public float EffectAmount => effectAmount;

        public string EffectDescription => effectDescription;

        public string SetEffectDescription => setEffectDescription;

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

            if (!Enum.IsDefined(typeof(ArmorEffectType), effectType))
            {
                validationError = $"{name} has an unknown Armor Effect Type.";
                return false;
            }

            if (effectAmount < 0f || float.IsNaN(effectAmount) || float.IsInfinity(effectAmount))
            {
                validationError = $"{name} has an invalid Armor Effect Amount.";
                return false;
            }

            if (effectType == ArmorEffectType.DamageTakenMultiplier
                && !Enum.IsDefined(typeof(DamageType), effectDamageType))
            {
                validationError = $"{name} has an unknown Armor Effect Damage Type.";
                return false;
            }

            validationError = null;
            return true;
        }

        private void OnValidate()
        {
            defense = Mathf.Max(0, defense);
            if (float.IsNaN(effectAmount) || float.IsInfinity(effectAmount))
            {
                effectAmount = 0f;
            }

            effectAmount = Mathf.Max(0f, effectAmount);
        }
    }
}
