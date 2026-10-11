using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Damage;
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
                    resolutionOffset: 2,
                    manaCost: 0,
                    staminaCost: 1,
                    cooldown: 1,
                    damageMultiplier: 0f,
                    damageType: DamageType.Slashing,
                    resolutionEffect: ActionResolutionEffect.None),
                new SampleAction(
                    "Sample_QuickGuard",
                    "sample.quick-guard",
                    "Quick Guard",
                    ComboActionCategory.Opening,
                    loadCost: 2,
                    resolutionOffset: 1,
                    manaCost: 0,
                    staminaCost: 2,
                    cooldown: 1,
                    damageMultiplier: 0f,
                    damageType: DamageType.Blunt,
                    resolutionEffect: ActionResolutionEffect.Guard),
                new SampleAction(
                    "Sample_Sidestep",
                    "sample.sidestep",
                    "Sidestep",
                    ComboActionCategory.Opening,
                    loadCost: 2,
                    resolutionOffset: 1,
                    manaCost: 0,
                    staminaCost: 2,
                    cooldown: 1,
                    damageMultiplier: 0f,
                    damageType: DamageType.Slashing,
                    resolutionEffect: ActionResolutionEffect.Evade),
                new SampleAction(
                    "Sample_EmberPrimer",
                    "sample.ember-primer",
                    "Ember Primer",
                    ComboActionCategory.Opening,
                    loadCost: 2,
                    resolutionOffset: 2,
                    manaCost: 2,
                    staminaCost: 0,
                    cooldown: 1,
                    damageMultiplier: 0.30f,
                    damageType: DamageType.Fire,
                    resolutionEffect: ActionResolutionEffect.Damage),
                new SampleAction(
                    "Sample_ArcaneSlash",
                    "sample.arcane-slash",
                    "Arcane Slash",
                    ComboActionCategory.Main,
                    loadCost: 5,
                    resolutionOffset: 4,
                    manaCost: 3,
                    staminaCost: 4,
                    cooldown: 3,
                    damageMultiplier: 0.70f,
                    damageType: DamageType.Slashing,
                    resolutionEffect: ActionResolutionEffect.Damage),
                new SampleAction(
                    "Sample_GuardBreaker",
                    "sample.guard-breaker",
                    "Guard Breaker",
                    ComboActionCategory.Main,
                    loadCost: 4,
                    resolutionOffset: 3,
                    manaCost: 0,
                    staminaCost: 5,
                    cooldown: 2,
                    damageMultiplier: 0.50f,
                    damageType: DamageType.Blunt,
                    resolutionEffect: ActionResolutionEffect.Damage),
                new SampleAction(
                    "Sample_IceLance",
                    "sample.ice-lance",
                    "Ice Lance",
                    ComboActionCategory.Main,
                    loadCost: 5,
                    resolutionOffset: 5,
                    manaCost: 4,
                    staminaCost: 0,
                    cooldown: 3,
                    damageMultiplier: 0.70f,
                    damageType: DamageType.Ice,
                    resolutionEffect: ActionResolutionEffect.Damage),
                new SampleAction(
                    "Sample_PreciseShot",
                    "sample.precise-shot",
                    "Precise Shot",
                    ComboActionCategory.Main,
                    loadCost: 4,
                    resolutionOffset: 4,
                    manaCost: 0,
                    staminaCost: 4,
                    cooldown: 2,
                    damageMultiplier: 0.60f,
                    damageType: DamageType.Piercing,
                    resolutionEffect: ActionResolutionEffect.Damage),
                new SampleAction(
                    "Sample_Backstep",
                    "sample.backstep",
                    "Backstep",
                    ComboActionCategory.Closing,
                    loadCost: 2,
                    resolutionOffset: 1,
                    manaCost: 0,
                    staminaCost: 2,
                    cooldown: 1,
                    damageMultiplier: 0f,
                    damageType: DamageType.Slashing,
                    resolutionEffect: ActionResolutionEffect.Evade),
                new SampleAction(
                    "Sample_GuardedRetreat",
                    "sample.guarded-retreat",
                    "Guarded Retreat",
                    ComboActionCategory.Closing,
                    loadCost: 2,
                    resolutionOffset: 1,
                    manaCost: 0,
                    staminaCost: 2,
                    cooldown: 1,
                    damageMultiplier: 0f,
                    damageType: DamageType.Blunt,
                    resolutionEffect: ActionResolutionEffect.Guard),
                new SampleAction(
                    "Sample_PartingSlash",
                    "sample.parting-slash",
                    "Parting Slash",
                    ComboActionCategory.Closing,
                    loadCost: 2,
                    resolutionOffset: 1,
                    manaCost: 0,
                    staminaCost: 2,
                    cooldown: 1,
                    damageMultiplier: 0.30f,
                    damageType: DamageType.Slashing,
                    resolutionEffect: ActionResolutionEffect.Damage),
                new SampleAction(
                    "Sample_SteadyFinish",
                    "sample.steady-finish",
                    "Steady Finish",
                    ComboActionCategory.Closing,
                    loadCost: 2,
                    resolutionOffset: 2,
                    manaCost: 0,
                    staminaCost: 1,
                    cooldown: 0,
                    damageMultiplier: 0f,
                    damageType: DamageType.Slashing,
                    resolutionEffect: ActionResolutionEffect.None),
                new SampleAction(
                    "Sample_FlameEcho",
                    "sample.flame-echo",
                    "Flame Echo",
                    ComboActionCategory.Linked,
                    loadCost: 2,
                    resolutionOffset: 2,
                    manaCost: 2,
                    staminaCost: 0,
                    cooldown: 2,
                    damageMultiplier: 0.30f,
                    damageType: DamageType.Fire,
                    resolutionEffect: ActionResolutionEffect.Damage),
                new SampleAction(
                    "Sample_WardEcho",
                    "sample.ward-echo",
                    "Ward Echo",
                    ComboActionCategory.Linked,
                    loadCost: 2,
                    resolutionOffset: 2,
                    manaCost: 1,
                    staminaCost: 0,
                    cooldown: 1,
                    damageMultiplier: 0f,
                    damageType: DamageType.Blunt,
                    resolutionEffect: ActionResolutionEffect.Guard),
                new SampleAction(
                    "Sample_VanishStep",
                    "sample.vanish-step",
                    "Vanish Step",
                    ComboActionCategory.Linked,
                    loadCost: 2,
                    resolutionOffset: 2,
                    manaCost: 0,
                    staminaCost: 1,
                    cooldown: 1,
                    damageMultiplier: 0f,
                    damageType: DamageType.Slashing,
                    resolutionEffect: ActionResolutionEffect.Evade),
                new SampleAction(
                    "Sample_SparkEcho",
                    "sample.spark-echo",
                    "Spark Echo",
                    ComboActionCategory.Linked,
                    loadCost: 2,
                    resolutionOffset: 2,
                    manaCost: 2,
                    staminaCost: 0,
                    cooldown: 1,
                    damageMultiplier: 0.30f,
                    damageType: DamageType.Lightning,
                    resolutionEffect: ActionResolutionEffect.Damage)
            };

            var createdAssets = new List<ComboActionAsset>();

            for (int index = 0; index < samples.Length; index++)
            {
                ComboActionAsset createdAsset = CreateOrUpdate(samples[index]);
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
                $"Created or synchronized {createdAssets.Count} sample Solo Combo Action asset(s) in {ActionFolder}.");
        }

        private static ComboActionAsset CreateOrUpdate(SampleAction sample)
        {
            string assetPath = $"{ActionFolder}/{sample.FileName}.asset";
            Object existingObject = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (existingObject != null && !(existingObject is ComboActionAsset))
            {
                Debug.LogError($"Cannot create sample Action because {assetPath} contains another asset type.");
                return null;
            }

            ComboActionAsset asset = existingObject as ComboActionAsset;
            bool isNewAsset = asset == null;
            if (isNewAsset)
            {
                asset = ScriptableObject.CreateInstance<ComboActionAsset>();
                asset.name = sample.FileName;
            }
            else
            {
                Undo.RecordObject(asset, "Synchronize Sample Solo Combo Action");
            }

            var serializedAsset = new SerializedObject(asset);
            serializedAsset.FindProperty("actionId").stringValue = sample.ActionId;
            serializedAsset.FindProperty("displayName").stringValue = sample.DisplayName;
            serializedAsset.FindProperty("category").enumValueIndex = (int)sample.Category;
            serializedAsset.FindProperty("loadCost").intValue = sample.LoadCost;
            serializedAsset.FindProperty("resolutionOffset").intValue = sample.ResolutionOffset;
            serializedAsset.FindProperty("manaCost").intValue = sample.ManaCost;
            serializedAsset.FindProperty("staminaCost").intValue = sample.StaminaCost;
            serializedAsset.FindProperty("cooldown").intValue = sample.Cooldown;
            serializedAsset.FindProperty("damageMultiplier").floatValue = sample.DamageMultiplier;
            serializedAsset.FindProperty("damageType").enumValueIndex = (int)sample.DamageType;
            serializedAsset.FindProperty("resolutionEffect").enumValueIndex =
                (int)sample.ResolutionEffect;
            serializedAsset.ApplyModifiedPropertiesWithoutUndo();

            if (isNewAsset)
            {
                AssetDatabase.CreateAsset(asset, assetPath);
                Undo.RegisterCreatedObjectUndo(asset, "Create Sample Solo Combo Action");
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

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
                int resolutionOffset,
                int manaCost,
                int staminaCost,
                int cooldown,
                float damageMultiplier,
                DamageType damageType,
                ActionResolutionEffect resolutionEffect)
            {
                FileName = fileName;
                ActionId = actionId;
                DisplayName = displayName;
                Category = category;
                LoadCost = loadCost;
                ResolutionOffset = resolutionOffset;
                ManaCost = manaCost;
                StaminaCost = staminaCost;
                Cooldown = cooldown;
                DamageMultiplier = damageMultiplier;
                DamageType = damageType;
                ResolutionEffect = resolutionEffect;
            }

            public string FileName { get; }

            public string ActionId { get; }

            public string DisplayName { get; }

            public ComboActionCategory Category { get; }

            public int LoadCost { get; }

            public int ResolutionOffset { get; }

            public int ManaCost { get; }

            public int StaminaCost { get; }

            public int Cooldown { get; }

            public float DamageMultiplier { get; }

            public DamageType DamageType { get; }

            public ActionResolutionEffect ResolutionEffect { get; }
        }
    }
}
