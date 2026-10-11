using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.Combat.Damage;
using UnityEngine;

namespace SER494.Gameplay.Combat.Authoring.SoloCombo
{
    [CreateAssetMenu(
        fileName = "NewComboAction",
        menuName = "SER494/Solo Combo/Action")]
    public sealed class ComboActionAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable internal ID. It must be unique across all owned Actions.")]
        private string actionId = string.Empty;

        [SerializeField]
        private string displayName = string.Empty;

        [Header("Combo Role")]
        [SerializeField]
        private ComboActionCategory category = ComboActionCategory.Opening;

        [Header("Costs")]
        [SerializeField]
        [Min(1)]
        private int loadCost = 1;

        [SerializeField]
        [Min(0)]
        [Tooltip("Fixed Resolution Point in Action Local Time. Zero resolves when the Action starts.")]
        private int resolutionOffset = 1;

        [SerializeField]
        [Min(0)]
        private int manaCost;

        [SerializeField]
        [Min(0)]
        private int staminaCost;

        [SerializeField]
        [Min(0)]
        private int cooldown;

        [Header("Prototype Effect")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Ordinary damage multiplier. For example, 0.50 means 50% of relevant Weapon Attack before the remaining damage buckets.")]
        private float damageMultiplier;

        [SerializeField]
        private DamageType damageType = DamageType.Slashing;

        [SerializeField]
        [Tooltip("Prototype effect resolved at the fixed Resolution Point. Damage requires a positive Damage Multiplier.")]
        private ActionResolutionEffect resolutionEffect = ActionResolutionEffect.None;

        [SerializeField]
        [Tooltip("Tags that alter how a Damage Resolution Point interacts with Guard and Evade.")]
        private DamageResolutionTag damageTags = DamageResolutionTag.None;

        public string ActionId => actionId;

        public string DisplayName => displayName;

        public ComboActionCategory Category => category;

        public int LoadCost => loadCost;

        public int ManaCost => manaCost;

        public int ResolutionOffset => resolutionOffset;

        public int StaminaCost => staminaCost;

        public int Cooldown => cooldown;

        public float DamageMultiplier => damageMultiplier;

        public DamageType DamageType => damageType;

        public ActionResolutionEffect ResolutionEffect => resolutionEffect;

        public DamageResolutionTag DamageTags => damageTags;

        public bool TryCreateDefinition(
            out ComboActionDefinition definition,
            out string validationError)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(actionId))
            {
                validationError = $"{name} has an empty Action ID.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                validationError = $"{name} has an empty Display Name.";
                return false;
            }

            if (loadCost <= 0)
            {
                validationError = $"{name} must have a Load Cost greater than zero.";
                return false;
            }

            if (resolutionOffset < 0 || resolutionOffset > loadCost)
            {
                validationError = $"{name} has a Resolution Offset outside its Action Load.";
                return false;
            }

            if (manaCost < 0 || staminaCost < 0 || cooldown < 0 || damageMultiplier < 0f)
            {
                validationError = $"{name} contains a negative cost or Damage Multiplier value.";
                return false;
            }

            if (damageMultiplier > 0f
                && resolutionEffect != ActionResolutionEffect.Damage)
            {
                validationError = $"{name} has damage but its Resolution Effect is not Damage.";
                return false;
            }

            if (damageMultiplier <= 0f
                && resolutionEffect == ActionResolutionEffect.Damage)
            {
                validationError = $"{name} uses a Damage Resolution Effect without damage.";
                return false;
            }

            if (damageMultiplier <= 0f && damageTags != DamageResolutionTag.None)
            {
                validationError = $"{name} uses Damage Tags without damage.";
                return false;
            }

            if (resolutionEffect == ActionResolutionEffect.ApplyBuff
                || resolutionEffect == ActionResolutionEffect.ApplyDebuff)
            {
                validationError = $"{name} requires Status authoring, which is not yet "
                    + "available on player Combo Action assets.";
                return false;
            }

            definition = new ComboActionDefinition(
                actionId.Trim(),
                displayName.Trim(),
                category,
                loadCost,
                manaCost,
                staminaCost,
                cooldown,
                damageMultiplier,
                resolutionOffset,
                damageType,
                resolutionEffect,
                damageTags);

            validationError = null;
            return true;
        }

        private void OnValidate()
        {
            loadCost = Mathf.Max(1, loadCost);
            resolutionOffset = Mathf.Clamp(resolutionOffset, 0, loadCost);
            manaCost = Mathf.Max(0, manaCost);
            staminaCost = Mathf.Max(0, staminaCost);
            cooldown = Mathf.Max(0, cooldown);
            damageMultiplier = Mathf.Max(0f, damageMultiplier);
        }
    }
}
