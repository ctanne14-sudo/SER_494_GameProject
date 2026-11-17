using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using UnityEditor;
using UnityEngine;

namespace SER494.Gameplay.Combat.Editor.SoloCombo
{
    public static class ComboActionAssetValidator
    {
        [MenuItem("Tools/SER494/Validate Solo Combo Actions")]
        public static void ValidateAllAssets()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:ComboActionAsset");
            var firstAssetPathById = new Dictionary<string, string>(StringComparer.Ordinal);
            int errorCount = 0;

            for (int index = 0; index < assetGuids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuids[index]);
                ComboActionAsset asset = AssetDatabase.LoadAssetAtPath<ComboActionAsset>(assetPath);

                if (asset == null)
                {
                    Debug.LogError($"Could not load Combo Action asset at {assetPath}.");
                    errorCount++;
                    continue;
                }

                if (!asset.TryCreateDefinition(
                        out ComboActionDefinition definition,
                        out string validationError))
                {
                    Debug.LogError(validationError, asset);
                    errorCount++;
                    continue;
                }

                if (firstAssetPathById.TryGetValue(definition.ActionId, out string firstAssetPath))
                {
                    Debug.LogError(
                        $"Duplicate Action ID '{definition.ActionId}' found in "
                        + $"{firstAssetPath} and {assetPath}.",
                        asset);
                    errorCount++;
                    continue;
                }

                firstAssetPathById.Add(definition.ActionId, assetPath);
            }

            if (errorCount == 0)
            {
                Debug.Log($"Validated {assetGuids.Length} Solo Combo Action assets. No errors found.");
                return;
            }

            Debug.LogError($"Solo Combo Action validation finished with {errorCount} error(s).");
        }
    }
}
