using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using UnityEditor;
using UnityEngine;

namespace SER494.Gameplay.Combat.Editor.SoloCombo
{
    public static class SampleComboActionAssetCreator
    {
        private const string GameDataFolder = "Assets/GameData";
        private const string SoloComboFolder = GameDataFolder + "/SoloCombo";
        private const string ActionFolder = SoloComboFolder + "/SampleActions";

        [MenuItem("Tools/SER494/Create Sample Solo Combo Actions")]
        public static void CreateSampleActions()
        {
            EnsureFolder("Assets", "GameData");
            EnsureFolder(GameDataFolder, "SoloCombo");
            EnsureFolder(SoloComboFolder, "SampleActions");

            var samples = new[]
            {
                new SampleAction(
                    "Sample_FocusStance",
                    "sample.focus-stance",
                    "Focus Stance",
                    ComboActionCategory.Opening,
                    loadCost: 2,
                    manaCost: 0,
                    staminaCost: 1,
                    cooldown: 1),
                new SampleAction(
                    "Sample_ArcaneSlash",
                    "sample.arcane-slash",
                    "Arcane Slash",
                    ComboActionCategory.Main,
                    loadCost: 5,
                    manaCost: 3,
                    staminaCost: 4,
                    cooldown: 3),
                new SampleAction(
                    "Sample_Backstep",
                    "sample.backstep",
                    "Backstep",
                    ComboActionCategory.Closing,
                    loadCost: 2,
                    manaCost: 0,
                    staminaCost: 2,
                    cooldown: 1),
                new SampleAction(
                    "Sample_FlameEcho",
                    "sample.flame-echo",
                    "Flame Echo",
                    ComboActionCategory.Linked,
                    loadCost: 2,
                    manaCost: 2,
                    staminaCost: 0,
                    cooldown: 2)
            };

            var createdAssets = new List<ComboActionAsset>();

            for (int index = 0; index < samples.Length; index++)
            {
                ComboActionAsset createdAsset = CreateIfMissing(samples[index]);
                if (createdAsset != null)
                {
                    createdAssets.Add(createdAsset);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (createdAssets.Count > 0)
            {
                Selection.activeObject = createdAssets[0];
                EditorGUIUtility.PingObject(createdAssets[0]);
            }

            Debug.Log(
                $"Created {createdAssets.Count} sample Solo Combo Action asset(s) in {ActionFolder}. "
                + "Existing assets were left unchanged.");
        }

        private static ComboActionAsset CreateIfMissing(SampleAction sample)
        {
            string assetPath = $"{ActionFolder}/{sample.FileName}.asset";
            Object existingAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (existingAsset != null)
            {
                return null;
            }

            ComboActionAsset asset = ScriptableObject.CreateInstance<ComboActionAsset>();
            asset.name = sample.FileName;

            var serializedAsset = new SerializedObject(asset);
            serializedAsset.FindProperty("actionId").stringValue = sample.ActionId;
            serializedAsset.FindProperty("displayName").stringValue = sample.DisplayName;
            serializedAsset.FindProperty("category").enumValueIndex = (int)sample.Category;
            serializedAsset.FindProperty("loadCost").intValue = sample.LoadCost;
            serializedAsset.FindProperty("manaCost").intValue = sample.ManaCost;
            serializedAsset.FindProperty("staminaCost").intValue = sample.StaminaCost;
            serializedAsset.FindProperty("cooldown").intValue = sample.Cooldown;
            serializedAsset.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, assetPath);
            Undo.RegisterCreatedObjectUndo(asset, "Create Sample Solo Combo Action");
            return asset;
        }

        private static void EnsureFolder(string parentFolder, string childFolderName)
        {
            string fullPath = $"{parentFolder}/{childFolderName}";
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parentFolder, childFolderName);
            }
        }

        private sealed class SampleAction
        {
            public SampleAction(
                string fileName,
                string actionId,
                string displayName,
                ComboActionCategory category,
                int loadCost,
                int manaCost,
                int staminaCost,
                int cooldown)
            {
                FileName = fileName;
                ActionId = actionId;
                DisplayName = displayName;
                Category = category;
                LoadCost = loadCost;
                ManaCost = manaCost;
                StaminaCost = staminaCost;
                Cooldown = cooldown;
            }

            public string FileName { get; }

            public string ActionId { get; }

            public string DisplayName { get; }

            public ComboActionCategory Category { get; }

            public int LoadCost { get; }

            public int ManaCost { get; }

            public int StaminaCost { get; }

            public int Cooldown { get; }
        }
    }
}
