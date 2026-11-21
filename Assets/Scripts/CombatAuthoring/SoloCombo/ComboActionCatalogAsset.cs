using System;
using System.Collections.Generic;
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

        public bool TryCreateAllDefinitions(
            out IReadOnlyList<ComboActionDefinition> definitions,
            out string validationError)
        {
            definitions = null;
            ComboActionAsset[] authoredActions = actions ?? Array.Empty<ComboActionAsset>();
            var actionIds = new List<string>(authoredActions.Length);
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < authoredActions.Length; index++)
            {
                ComboActionAsset action = authoredActions[index];
                if (action == null)
                {
                    validationError = $"Action Catalog entry {index} is missing.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(action.ActionId))
                {
                    validationError = $"Action Catalog entry {index} has no stable ID.";
                    return false;
                }

                string actionId = action.ActionId.Trim();
                if (!uniqueIds.Add(actionId))
                {
                    validationError = $"Multiple Action assets use ID '{actionId}'.";
                    return false;
                }

                actionIds.Add(actionId);
            }

            return TryCreateDefinitions(actionIds, out definitions, out validationError);
        }

        public bool TryCreateDefinitions(
            IEnumerable<string> actionIds,
            out IReadOnlyList<ComboActionDefinition> definitions,
            out string validationError)
        {
            definitions = null;
            if (actionIds == null)
            {
                validationError = "An Action ID collection is required.";
                return false;
            }

            var requestedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string actionId in actionIds)
            {
                if (string.IsNullOrWhiteSpace(actionId))
                {
                    validationError = "An owned Action ID cannot be empty.";
                    return false;
                }

                requestedIds.Add(actionId.Trim());
            }

            var created = new List<ComboActionDefinition>(requestedIds.Count);
            foreach (string actionId in requestedIds)
            {
                if (!TryCreateDefinition(actionId, out ComboActionDefinition definition, out validationError))
                {
                    return false;
                }

                created.Add(definition);
            }

            created.Sort((left, right) =>
            {
                int categoryComparison = left.Category.CompareTo(right.Category);
                return categoryComparison != 0
                    ? categoryComparison
                    : string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
            });
            definitions = created.AsReadOnly();
            validationError = null;
            return true;
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
