using System;
using SER494.Gameplay.Combat.SoloCombo;
using UnityEngine;

namespace SER494.Gameplay.Combat.Authoring.SoloCombo
{
    [CreateAssetMenu(
        fileName = "ComboActionCatalog",
        menuName = "SER494/Solo Combo/Action Catalog")]
    public sealed class ComboActionCatalogAsset : ScriptableObject
    {
        public const string DefaultResourcesPath = "PlayerState/ComboActionCatalog";

        [SerializeField]
        private ComboActionAsset[] actions = Array.Empty<ComboActionAsset>();

        public static ComboActionCatalogAsset LoadDefault()
        {
            return Resources.Load<ComboActionCatalogAsset>(DefaultResourcesPath);
        }

        public bool TryCreateDefinition(
            string actionId,
            out ComboActionDefinition definition,
            out string validationError)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(actionId))
            {
                validationError = "An Action ID is required.";
                return false;
            }

            ComboActionAsset match = null;
            for (int index = 0; index < actions.Length; index++)
            {
                ComboActionAsset candidate = actions[index];
                if (candidate == null
                    || !string.Equals(
                        candidate.ActionId,
                        actionId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (match != null)
                {
                    validationError = $"Multiple Action assets use ID '{actionId}'.";
                    return false;
                }

                match = candidate;
            }

            if (match == null)
            {
                validationError = $"Action '{actionId}' is missing from the Action Catalog.";
                return false;
            }

            return match.TryCreateDefinition(out definition, out validationError);
        }
    }
}
