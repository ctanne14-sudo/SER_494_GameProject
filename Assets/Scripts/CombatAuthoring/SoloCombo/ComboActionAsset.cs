using SER494.Gameplay.Combat.SoloCombo;
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
        [Min(0)]
        private int loadCost;

        [SerializeField]
        [Min(0)]
        private int manaCost;

        [SerializeField]
        [Min(0)]
        private int staminaCost;

        [SerializeField]
        [Min(0)]
        private int cooldown;

        public string ActionId => actionId;

        public string DisplayName => displayName;

        public ComboActionCategory Category => category;

        public int LoadCost => loadCost;

        public int ManaCost => manaCost;

        public int StaminaCost => staminaCost;

        public int Cooldown => cooldown;

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

            if (loadCost < 0 || manaCost < 0 || staminaCost < 0 || cooldown < 0)
            {
                validationError = $"{name} contains a negative cost.";
                return false;
            }

            definition = new ComboActionDefinition(
                actionId.Trim(),
                displayName.Trim(),
                category,
                loadCost,
                manaCost,
                staminaCost,
                cooldown);

            validationError = null;
            return true;
        }

        private void OnValidate()
        {
            loadCost = Mathf.Max(0, loadCost);
            manaCost = Mathf.Max(0, manaCost);
            staminaCost = Mathf.Max(0, staminaCost);
            cooldown = Mathf.Max(0, cooldown);
        }
    }
}
