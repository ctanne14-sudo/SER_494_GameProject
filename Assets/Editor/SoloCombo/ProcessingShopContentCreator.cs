using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.Equipment;
using SER494.Gameplay.Combat.Damage;
using UnityEditor;
using UnityEngine;

namespace SER494.Gameplay.Combat.Editor
{
    [InitializeOnLoad]
    public static class ProcessingShopContentCreator
    {
        private const string OutputFolder = "Assets/GameData/Resources/PlayerState/ProcessedArmor";
        private const string CatalogPath = "Assets/GameData/Resources/PlayerState/PlayerEquipmentCatalog.asset";

        private readonly struct ArmorSpec
        {
            public ArmorSpec(string series, string name, int headDefense, int upperDefense, int lowerDefense,
                ArmorEffectType effectType, DamageType damageType, float headAmount, float upperAmount, float lowerAmount)
            {
                Series = series;
                Name = name;
                Defenses = new[] { headDefense, upperDefense, lowerDefense };
                EffectType = effectType;
                DamageType = damageType;
                Amounts = new[] { headAmount, upperAmount, lowerAmount };
            }

            public string Series { get; }
            public string Name { get; }
            public int[] Defenses { get; }
            public ArmorEffectType EffectType { get; }
            public DamageType DamageType { get; }
            public float[] Amounts { get; }
        }

        private static readonly ArmorSpec[] Specs =
        {
            new ArmorSpec("white-copper", "White Copper", 10, 10, 10, ArmorEffectType.DamageTakenMultiplier, DamageType.Slashing, .02f, .04f, .04f),
            new ArmorSpec("bronze", "Bronze", 10, 10, 10, ArmorEffectType.DamageTakenMultiplier, DamageType.Piercing, .02f, .04f, .04f),
            new ArmorSpec("brass", "Brass", 10, 10, 10, ArmorEffectType.DamageTakenMultiplier, DamageType.Blunt, .02f, .04f, .04f),
            new ArmorSpec("ape-beast", "Ape Beast", 15, 20, 15, ArmorEffectType.MaximumHealthMultiplier, default, .02f, .04f, .04f),
            new ArmorSpec("slime", "Slime", 20, 30, 30, ArmorEffectType.HealingPotionMultiplier, default, .10f, .15f, .15f),
            new ArmorSpec("yellow-silk", "Yellow Silk", 30, 40, 40, ArmorEffectType.None, default, 0f, 0f, 0f),
            new ArmorSpec("red-silk", "Red Silk", 30, 40, 40, ArmorEffectType.None, default, 0f, 0f, 0f),
            new ArmorSpec("thorn", "Thorn", 30, 40, 40, ArmorEffectType.GuardCounterPiercingMultiplier, DamageType.Piercing, .10f, .20f, .20f),
            new ArmorSpec("crystal", "Crystal", 50, 75, 75, ArmorEffectType.AllDamageDealtMultiplier, default, .05f, .05f, .05f),
            new ArmorSpec("pure-gold", "Pure Gold", 50, 75, 75, ArmorEffectType.DamageTakenMultiplier, DamageType.Piercing, .05f, .05f, .05f)
        };

        private static readonly string[] SlotSuffixes = { "head", "upper", "lower" };
        private static readonly string[] SlotIds = { "armor.head", "armor.upper-body", "armor.lower-body" };
        private static readonly string[] SlotNames = { "Headpiece", "Upper Armor", "Lower Armor" };

        static ProcessingShopContentCreator()
        {
            EditorApplication.delayCall += GenerateIfMissing;
        }

        [MenuItem("SER494/Processing Shop/Generate Armor Content")]
        public static void Generate()
        {
            EnsureFolder(OutputFolder);
            var generated = new List<ArmorDefinitionAsset>(30);
            foreach (ArmorSpec spec in Specs)
            {
                for (int slotIndex = 0; slotIndex < SlotSuffixes.Length; slotIndex++)
                {
                    string assetPath = $"{OutputFolder}/{ToPascal(spec.Series)}{SlotNames[slotIndex].Replace(" ", string.Empty)}.asset";
                    ArmorDefinitionAsset asset = AssetDatabase.LoadAssetAtPath<ArmorDefinitionAsset>(assetPath);
                    if (asset == null)
                    {
                        asset = ScriptableObject.CreateInstance<ArmorDefinitionAsset>();
                        AssetDatabase.CreateAsset(asset, assetPath);
                    }

                    var serialized = new SerializedObject(asset);
                    serialized.FindProperty("armorId").stringValue = $"armor.{spec.Series}.{SlotSuffixes[slotIndex]}";
                    serialized.FindProperty("displayName").stringValue = $"{spec.Name} {SlotNames[slotIndex]}";
                    serialized.FindProperty("slotId").stringValue = SlotIds[slotIndex];
                    serialized.FindProperty("defense").intValue = spec.Defenses[slotIndex];
                    serialized.FindProperty("seriesId").stringValue = $"armor-series.{spec.Series}";
                    serialized.FindProperty("effectType").enumValueIndex = (int)spec.EffectType;
                    serialized.FindProperty("effectDamageType").enumValueIndex = (int)spec.DamageType;
                    serialized.FindProperty("effectAmount").floatValue = spec.Amounts[slotIndex];
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(asset);
                    generated.Add(asset);
                }
            }

            PlayerEquipmentCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<PlayerEquipmentCatalogAsset>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"Processing Shop could not find {CatalogPath}.");
                return;
            }

            var catalogSerialized = new SerializedObject(catalog);
            SerializedProperty armor = catalogSerialized.FindProperty("armor");
            var combined = new List<ArmorDefinitionAsset>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < armor.arraySize; index++)
            {
                var existing = armor.GetArrayElementAtIndex(index).objectReferenceValue as ArmorDefinitionAsset;
                if (existing != null && !IsGenerated(existing) && seen.Add(existing.ArmorId))
                {
                    combined.Add(existing);
                }
            }
            foreach (ArmorDefinitionAsset asset in generated)
            {
                if (seen.Add(asset.ArmorId))
                {
                    combined.Add(asset);
                }
            }

            armor.arraySize = combined.Count;
            for (int index = 0; index < combined.Count; index++)
            {
                armor.GetArrayElementAtIndex(index).objectReferenceValue = combined[index];
            }
            catalogSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Generated 30 Processing Shop Armor assets and updated the Player Equipment Catalog.");
        }

        private static void GenerateIfMissing()
        {
            if (AssetDatabase.IsValidFolder(OutputFolder)
                && AssetDatabase.FindAssets("t:ArmorDefinitionAsset", new[] { OutputFolder }).Length == 30)
            {
                return;
            }
            Generate();
        }

        private static bool IsGenerated(ArmorDefinitionAsset asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            return path.StartsWith(OutputFolder + "/", StringComparison.Ordinal);
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }

        private static string ToPascal(string value)
        {
            string[] parts = value.Split('-');
            for (int index = 0; index < parts.Length; index++)
            {
                parts[index] = char.ToUpperInvariant(parts[index][0]) + parts[index].Substring(1);
            }
            return string.Concat(parts);
        }
    }
}
