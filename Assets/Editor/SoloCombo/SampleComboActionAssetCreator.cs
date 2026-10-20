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
                    timelineCost: 5,
                    resolutionOffset: 5,
                    manaCost: 0,
                    staminaCost: 1,
                    cooldown: 1,
                    damageMultiplier: 0f,
                    damageType: DamageType.Slashing,
                    resolutionEffect: ActionResolutionEffect.None,
                    activeWindows: new[]
                    {
                        new SampleActiveWindow(
                            "focus.defense",
                            "Focused Defense",
                            startOffset: 1,
                            endOffset: 5,
                            damageReduction: 0.30f)
                    }),
                new SampleAction(
                    "Sample_QuickGuard",
                    "sample.quick-guard",
                    "Quick Guard",
                    ComboActionCategory.Opening,
                    timelineCost: 2,
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
                    timelineCost: 2,
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
                    timelineCost: 2,
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
                    timelineCost: 5,
                    resolutionOffset: 4,
                    manaCost: 3,
                    staminaCost: 4,
                    cooldown: 3,
                    damageMultiplier: 0.70f,
                    damageType: DamageType.Slashing,
                    resolutionEffect: ActionResolutionEffect.Damage,
                    damageTags: DamageResolutionTag.Overcommit | DamageResolutionTag.Clash,
                    chargeLevels: new[]
                    {
                        new SampleChargeLevel(additionalTimelineCost: 3, damageMultiplierScale: 1.35f),
                        new SampleChargeLevel(additionalTimelineCost: 6, damageMultiplierScale: 1.70f)
                    },
                    clashDamageReduction: 0.50f,
                    clashSuperArmorLevel: 2),
                new SampleAction(
                    "Sample_GuardBreaker",
                    "sample.guard-breaker",
                    "Guard Breaker",
                    ComboActionCategory.Main,
                    timelineCost: 4,
                    resolutionOffset: 2,
                    manaCost: 0,
                    staminaCost: 5,
                    cooldown: 2,
                    damageMultiplier: 0.50f,
                    damageType: DamageType.Blunt,
                    resolutionEffect: ActionResolutionEffect.Damage,
                    damageTags: DamageResolutionTag.Unguardable,
                    onHitDisruption: new SampleDisruption(
                        ActionDisruptionType.Stagger,
                        duration: 2)),
                new SampleAction(
                    "Sample_IceLance",
                    "sample.ice-lance",
                    "Ice Lance",
                    ComboActionCategory.Main,
                    timelineCost: 5,
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
                    timelineCost: 4,
                    resolutionOffset: 4,
                    manaCost: 0,
                    staminaCost: 4,
                    cooldown: 2,
                    damageMultiplier: 0.60f,
                    damageType: DamageType.Piercing,
                    resolutionEffect: ActionResolutionEffect.Damage,
                    resolutionWindowStart: 2,
                    resolutionWindowEnd: 4),
                new SampleAction(
                    "Sample_Backstep",
                    "sample.backstep",
                    "Backstep",
                    ComboActionCategory.Closing,
                    timelineCost: 2,
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
                    timelineCost: 2,
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
                    timelineCost: 2,
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
                    timelineCost: 2,
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
                    timelineCost: 2,
                    resolutionOffset: 2,
                    manaCost: 2,
                    staminaCost: 0,
                    cooldown: 2,
                    damageMultiplier: 0.30f,
                    damageType: DamageType.Fire,
                    resolutionEffect: ActionResolutionEffect.Damage,
                    damageTags: DamageResolutionTag.IgnoresEvade),
                new SampleAction(
                    "Sample_WardEcho",
                    "sample.ward-echo",
                    "Ward Echo",
                    ComboActionCategory.Linked,
                    timelineCost: 2,
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
                    timelineCost: 2,
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
                    timelineCost: 2,
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
            serializedAsset.FindProperty("timelineCost").intValue = sample.TimelineCost;
            serializedAsset.FindProperty("resolutionOffset").intValue = sample.ResolutionOffset;
            serializedAsset.FindProperty("useResolutionWindow").boolValue =
                sample.ResolutionWindowStart.HasValue;
            serializedAsset.FindProperty("resolutionWindowStart").intValue =
                sample.ResolutionWindowStart ?? sample.ResolutionOffset;
            serializedAsset.FindProperty("resolutionWindowEnd").intValue =
                sample.ResolutionWindowEnd ?? sample.ResolutionOffset;
            SerializedProperty chargeLevels = serializedAsset.FindProperty("chargeLevels");
            chargeLevels.arraySize = sample.ChargeLevels.Length;
            for (int index = 0; index < sample.ChargeLevels.Length; index++)
            {
                SerializedProperty level = chargeLevels.GetArrayElementAtIndex(index);
                level.FindPropertyRelative("additionalTimelineCost").intValue =
                    sample.ChargeLevels[index].AdditionalTimelineCost;
                level.FindPropertyRelative("damageMultiplierScale").floatValue =
                    sample.ChargeLevels[index].DamageMultiplierScale;
            }
            SerializedProperty activeWindows = serializedAsset.FindProperty("activeWindows");
            activeWindows.arraySize = sample.ActiveWindows.Length;
            for (int index = 0; index < sample.ActiveWindows.Length; index++)
            {
                SerializedProperty window = activeWindows.GetArrayElementAtIndex(index);
                SampleActiveWindow sampleWindow = sample.ActiveWindows[index];
                window.FindPropertyRelative("windowId").stringValue = sampleWindow.WindowId;
                window.FindPropertyRelative("displayName").stringValue = sampleWindow.DisplayName;
                window.FindPropertyRelative("startOffset").intValue = sampleWindow.StartOffset;
                window.FindPropertyRelative("endOffset").intValue = sampleWindow.EndOffset;
                window.FindPropertyRelative("effect").enumValueIndex =
                    (int)sampleWindow.Effect;
                window.FindPropertyRelative("damageReduction").floatValue =
                    sampleWindow.DamageReduction;
                window.FindPropertyRelative("disruptionPolicy").enumValueIndex =
                    (int)sampleWindow.DisruptionPolicy;
                window.FindPropertyRelative("extendsThroughCharge").boolValue = false;
            }
            serializedAsset.FindProperty("manaCost").intValue = sample.ManaCost;
            serializedAsset.FindProperty("staminaCost").intValue = sample.StaminaCost;
            serializedAsset.FindProperty("cooldown").intValue = sample.Cooldown;
            serializedAsset.FindProperty("damageMultiplier").floatValue = sample.DamageMultiplier;
            serializedAsset.FindProperty("damageType").enumValueIndex = (int)sample.DamageType;
            serializedAsset.FindProperty("resolutionEffect").enumValueIndex =
                (int)sample.ResolutionEffect;
            serializedAsset.FindProperty("damageTags").intValue = (int)sample.DamageTags;
            serializedAsset.FindProperty("clashDamageReduction").floatValue =
                sample.ClashDamageReduction;
            serializedAsset.FindProperty("clashSuperArmorLevel").intValue =
                sample.ClashSuperArmorLevel;
            serializedAsset.FindProperty("appliesOnHitDisruption").boolValue =
                sample.OnHitDisruption != null;
            serializedAsset.FindProperty("onHitDisruptionType").enumValueIndex =
                (int)(sample.OnHitDisruption?.Type ?? ActionDisruptionType.Stagger) - 1;
            serializedAsset.FindProperty("onHitDisruptionDuration").intValue =
                sample.OnHitDisruption?.Duration ?? 1;
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
                int timelineCost,
                int resolutionOffset,
                int manaCost,
                int staminaCost,
                int cooldown,
                float damageMultiplier,
                DamageType damageType,
                ActionResolutionEffect resolutionEffect,
                DamageResolutionTag damageTags = DamageResolutionTag.None,
                int? resolutionWindowStart = null,
                int? resolutionWindowEnd = null,
                SampleChargeLevel[] chargeLevels = null,
                SampleActiveWindow[] activeWindows = null,
                SampleDisruption onHitDisruption = null,
                float clashDamageReduction = 0f,
                int clashSuperArmorLevel = 0)
            {
                FileName = fileName;
                ActionId = actionId;
                DisplayName = displayName;
                Category = category;
                TimelineCost = timelineCost;
                ResolutionOffset = resolutionOffset;
                ManaCost = manaCost;
                StaminaCost = staminaCost;
                Cooldown = cooldown;
                DamageMultiplier = damageMultiplier;
                DamageType = damageType;
                ResolutionEffect = resolutionEffect;
                DamageTags = damageTags;
                ResolutionWindowStart = resolutionWindowStart;
                ResolutionWindowEnd = resolutionWindowEnd;
                ChargeLevels = chargeLevels ?? new SampleChargeLevel[0];
                ActiveWindows = activeWindows ?? new SampleActiveWindow[0];
                OnHitDisruption = onHitDisruption;
                ClashDamageReduction = clashDamageReduction;
                ClashSuperArmorLevel = clashSuperArmorLevel;
            }

            public string FileName { get; }

            public string ActionId { get; }

            public string DisplayName { get; }

            public ComboActionCategory Category { get; }

            public int TimelineCost { get; }

            public int ResolutionOffset { get; }

            public int ManaCost { get; }

            public int StaminaCost { get; }

            public int Cooldown { get; }

            public float DamageMultiplier { get; }

            public DamageType DamageType { get; }

            public ActionResolutionEffect ResolutionEffect { get; }

            public DamageResolutionTag DamageTags { get; }

            public int? ResolutionWindowStart { get; }

            public int? ResolutionWindowEnd { get; }

            public SampleChargeLevel[] ChargeLevels { get; }

            public SampleActiveWindow[] ActiveWindows { get; }

            public SampleDisruption OnHitDisruption { get; }

            public float ClashDamageReduction { get; }

            public int ClashSuperArmorLevel { get; }
        }

        private sealed class SampleDisruption
        {
            public SampleDisruption(ActionDisruptionType type, int duration)
            {
                Type = type;
                Duration = duration;
            }

            public ActionDisruptionType Type { get; }

            public int Duration { get; }
        }

        private sealed class SampleActiveWindow
        {
            public SampleActiveWindow(
                string windowId,
                string displayName,
                int startOffset,
                int endOffset,
                float damageReduction,
                ActiveWindowEffect effect = ActiveWindowEffect.DefensiveState,
                ActiveWindowDisruptionPolicy disruptionPolicy =
                    ActiveWindowDisruptionPolicy.PersistThroughDisruption)
            {
                WindowId = windowId;
                DisplayName = displayName;
                StartOffset = startOffset;
                EndOffset = endOffset;
                DamageReduction = damageReduction;
                Effect = effect;
                DisruptionPolicy = disruptionPolicy;
            }

            public string WindowId { get; }

            public string DisplayName { get; }

            public int StartOffset { get; }

            public int EndOffset { get; }

            public float DamageReduction { get; }

            public ActiveWindowEffect Effect { get; }

            public ActiveWindowDisruptionPolicy DisruptionPolicy { get; }
        }

        private sealed class SampleChargeLevel
        {
            public SampleChargeLevel(int additionalTimelineCost, float damageMultiplierScale)
            {
                AdditionalTimelineCost = additionalTimelineCost;
                DamageMultiplierScale = damageMultiplierScale;
            }

            public int AdditionalTimelineCost { get; }

            public float DamageMultiplierScale { get; }
        }
    }
}
