using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.Equipment;
using SER494.Gameplay.Combat.Authoring.Weapons;
using SER494.Gameplay.Combat.Damage;
using SER494.Gameplay.Combat.Weapons;
using UnityEditor;
using UnityEngine;

namespace SER494.Gameplay.Combat.Editor
{
    [InitializeOnLoad]
    public static class ProcessingShopContentCreator
    {
        private const string ArmorFolder = "Assets/GameData/Resources/PlayerState/ProcessedArmor";
        private const string WeaponFolder = "Assets/GameData/Resources/PlayerState/ProcessedWeapons";
        private const string AccessoryFolder = "Assets/GameData/Resources/PlayerState/ProcessedAccessories";
        private const string CatalogPath = "Assets/GameData/Resources/PlayerState/PlayerEquipmentCatalog.asset";

        private readonly struct ArmorSpec
        {
            public ArmorSpec(
                string series,
                string name,
                int[] defenses,
                ArmorEffectType[] effectTypes,
                DamageType[] damageTypes,
                float[] amounts,
                string[] descriptions,
                string setDescription)
            {
                Series = series;
                Name = name;
                Defenses = defenses;
                EffectTypes = effectTypes;
                DamageTypes = damageTypes;
                Amounts = amounts;
                Descriptions = descriptions;
                SetDescription = setDescription;
            }

            public string Series { get; }
            public string Name { get; }
            public int[] Defenses { get; }
            public ArmorEffectType[] EffectTypes { get; }
            public DamageType[] DamageTypes { get; }
            public float[] Amounts { get; }
            public string[] Descriptions { get; }
            public string SetDescription { get; }
        }

        private readonly struct WeaponSpec
        {
            public WeaponSpec(string series, string name, int greatsword, int swordAndShield, int glaiveAndShield)
            {
                Series = series;
                Name = name;
                Attacks = new[] { greatsword, swordAndShield, glaiveAndShield };
            }

            public string Series { get; }
            public string Name { get; }
            public int[] Attacks { get; }
        }

        private readonly struct AccessorySpec
        {
            public AccessorySpec(string id, string name, string description)
            {
                Id = id;
                Name = name;
                Description = description;
            }

            public string Id { get; }
            public string Name { get; }
            public string Description { get; }
        }

        private static readonly string[] SlotSuffixes = { "head", "upper", "lower" };
        private static readonly string[] SlotIds = { "armor.head", "armor.upper-body", "armor.lower-body" };
        private static readonly string[] SlotNames = { "Headpiece", "Upper Armor", "Lower Armor" };
        private static readonly WeaponType[] WeaponTypes =
        {
            WeaponType.Greatsword,
            WeaponType.SwordAndShield,
            WeaponType.GlaiveAndShield
        };
        private static readonly string[] WeaponTypeIds =
        {
            "greatsword",
            "sword-and-shield",
            "glaive-and-shield"
        };
        private static readonly string[] WeaponTypeNames =
        {
            "Greatsword",
            "Sword and Shield",
            "Glaive and Shield"
        };

        private static readonly ArmorSpec[] ArmorSpecs =
        {
            UniformArmor("white-copper", "White Copper", 10, 10, 10,
                ArmorEffectType.DamageTakenMultiplier, DamageType.Slashing, .02f, .04f, .04f,
                "Slashing damage taken -2%.", "Slashing damage taken -4%.", "Slashing damage taken -4%."),
            UniformArmor("bronze", "Bronze", 10, 10, 10,
                ArmorEffectType.DamageTakenMultiplier, DamageType.Piercing, .02f, .04f, .04f,
                "Piercing damage taken -2%.", "Piercing damage taken -4%.", "Piercing damage taken -4%."),
            UniformArmor("brass", "Brass", 10, 10, 10,
                ArmorEffectType.DamageTakenMultiplier, DamageType.Blunt, .02f, .04f, .04f,
                "Blunt damage taken -2%.", "Blunt damage taken -4%.", "Blunt damage taken -4%."),
            UniformArmor("ape-beast", "Ape Beast", 15, 20, 15,
                ArmorEffectType.MaximumHealthMultiplier, default, .02f, .04f, .04f,
                "Maximum HP +2%.", "Maximum HP +4%.", "Maximum HP +4%."),
            UniformArmor("slime", "Slime", 20, 30, 30,
                ArmorEffectType.HealingPotionMultiplier, default, .10f, .15f, .15f,
                "Healing and Greater Healing Potion recovery +10%.",
                "Healing and Greater Healing Potion recovery +15%.",
                "Healing and Greater Healing Potion recovery +15%."),
            UniformArmor("yellow-silk", "Yellow Silk", 30, 40, 40,
                ArmorEffectType.None, default, 0f, 0f, 0f,
                "No individual armor effect.", "No individual armor effect.", "No individual armor effect.",
                "Full set: Gold earned after battle +20%."),
            UniformArmor("red-silk", "Red Silk", 30, 40, 40,
                ArmorEffectType.None, default, 0f, 0f, 0f,
                "No individual armor effect.", "No individual armor effect.", "No individual armor effect.",
                "Full set: one additional material reward roll."),
            UniformArmor("thorn", "Thorn", 30, 40, 40,
                ArmorEffectType.GuardCounterPiercingMultiplier, DamageType.Piercing, .10f, .20f, .20f,
                "Successful Guard deals an extra 10% Piercing damage.",
                "Successful Guard deals an extra 20% Piercing damage.",
                "Successful Guard deals an extra 20% Piercing damage."),
            new ArmorSpec(
                "crystal", "Crystal",
                new[] { 50, 75, 75 },
                new[] { ArmorEffectType.AllDamageDealtMultiplier, ArmorEffectType.DamageTakenMultiplier, ArmorEffectType.StaminaRecoveryPerRoundFlat },
                new[] { default(DamageType), DamageType.Blunt, default(DamageType) },
                new[] { .05f, .05f, 3f },
                new[] { "All damage dealt +5%.", "Blunt damage taken -5%.", "Stamina recovery +3 each round." },
                "Full set: all damage dealt +10%."),
            new ArmorSpec(
                "pure-gold", "Pure Gold",
                new[] { 50, 75, 75 },
                new[] { ArmorEffectType.HealingPotionMultiplier, ArmorEffectType.DamageTakenMultiplier, ArmorEffectType.DefenseMultiplier },
                new[] { default(DamageType), DamageType.Slashing, default(DamageType) },
                new[] { .10f, .05f, .10f },
                new[] { "Healing and Greater Healing Potion recovery +10%.", "Slashing damage taken -5%.", "Defense +10%." },
                "Full set: successful Clash damage +50%."),
            new ArmorSpec(
                "mithril", "Mithril",
                new[] { 50, 75, 75 },
                new[] { ArmorEffectType.MaximumHealthMultiplier, ArmorEffectType.DamageTakenMultiplier, ArmorEffectType.MaximumStaminaFlat },
                new[] { default(DamageType), DamageType.Piercing, default(DamageType) },
                new[] { .04f, .05f, 10f },
                new[] { "Maximum HP +4%.", "Piercing damage taken -5%.", "Maximum Stamina +10." },
                "Full set: all damage taken -10%.")
        };

        private static readonly WeaponSpec[] WeaponSpecs =
        {
            new WeaponSpec("white-copper", "White Copper", 40, 60, 50),
            new WeaponSpec("bronze", "Bronze", 60, 50, 40),
            new WeaponSpec("brass", "Brass", 50, 40, 60),
            new WeaponSpec("ape-beast", "Ape Beast", 100, 80, 70),
            new WeaponSpec("slime", "Slime", 40, 60, 50),
            new WeaponSpec("thorn", "Thorn", 70, 80, 90),
            new WeaponSpec("crystal", "Crystal", 100, 120, 100),
            new WeaponSpec("pure-gold", "Pure Gold", 120, 100, 100),
            new WeaponSpec("mithril", "Mithril", 100, 100, 120)
        };

        private static readonly AccessorySpec[] AccessorySpecs =
        {
            new AccessorySpec("white-copper-ring", "White Copper Ring", "All enemy Guard effects are disabled during Round 1."),
            new AccessorySpec("bronze-ring", "Bronze Ring", "Defense +15%."),
            new AccessorySpec("brass-ring", "Brass Ring", "Defense +25% during Rounds 1-2, +5% during Rounds 3-4, and -15% from Round 5 onward."),
            new AccessorySpec("ape-beast-fresh-blood-ornament", "Ape Beast Fresh Blood Ornament", "The first time damage is dealt each round, recover HP equal to 20% of the actual damage dealt."),
            new AccessorySpec("slime-ornament", "Slime Ornament", "At battle start and every two rounds, gain one non-stacking use that nullifies the next incoming Blunt damage. Guard and Evade do not consume it; added control effects remain."),
            new AccessorySpec("yellow-handkerchief", "Yellow Handkerchief", "When using a Healing Potion, Greater Healing Potion, or Vitality Tonic in battle, it has a 35% chance not to be consumed."),
            new AccessorySpec("red-handkerchief", "Red Handkerchief", "At battle end, each of up to three extra material slots independently succeeds at min(total damage resolutions by both sides, 100)% even when attacks were Guarded or Evaded."),
            new AccessorySpec("thorn-bracer", "Thorn Bracer", "Lose 10 HP at each round end, but never below 1 HP. Restore HP to full when battle ends, including Mutual Destruction."),
            new AccessorySpec("crystal-pendant", "Crystal Pendant", "Chance to learn a new action after battle increases from 50% to 70%."),
            new AccessorySpec("gold-ring", "Gold Ring", "Material reward rolls -1, to a minimum of zero; Gold reward +300.")
        };

        static ProcessingShopContentCreator()
        {
            EditorApplication.delayCall += GenerateIfMissing;
        }

        [MenuItem("SER494/Processing Shop/Generate Equipment Content")]
        public static void Generate()
        {
            EnsureFolder(ArmorFolder);
            EnsureFolder(WeaponFolder);
            EnsureFolder(AccessoryFolder);

            List<ArmorDefinitionAsset> armor = GenerateArmor();
            List<WeaponDefinitionAsset> weapons = GenerateWeapons();
            List<AccessoryDefinitionAsset> accessories = GenerateAccessories();
            UpdateCatalog(armor, weapons, accessories);
            AssetDatabase.SaveAssets();
            Debug.Log("Generated 33 Armor, 27 Weapon, and 10 Accessory Processing Shop assets and updated the Player Equipment Catalog.");
        }

        private static List<ArmorDefinitionAsset> GenerateArmor()
        {
            var generated = new List<ArmorDefinitionAsset>(33);
            foreach (ArmorSpec spec in ArmorSpecs)
            {
                for (int slotIndex = 0; slotIndex < SlotSuffixes.Length; slotIndex++)
                {
                    string assetPath = $"{ArmorFolder}/{ToPascal(spec.Series)}{SlotNames[slotIndex].Replace(" ", string.Empty)}.asset";
                    ArmorDefinitionAsset asset = LoadOrCreate<ArmorDefinitionAsset>(assetPath);
                    var serialized = new SerializedObject(asset);
                    serialized.FindProperty("armorId").stringValue = $"armor.{spec.Series}.{SlotSuffixes[slotIndex]}";
                    serialized.FindProperty("displayName").stringValue = $"{spec.Name} {SlotNames[slotIndex]}";
                    serialized.FindProperty("slotId").stringValue = SlotIds[slotIndex];
                    serialized.FindProperty("defense").intValue = spec.Defenses[slotIndex];
                    serialized.FindProperty("seriesId").stringValue = $"armor-series.{spec.Series}";
                    serialized.FindProperty("effectType").enumValueIndex = (int)spec.EffectTypes[slotIndex];
                    serialized.FindProperty("effectDamageType").enumValueIndex = (int)spec.DamageTypes[slotIndex];
                    serialized.FindProperty("effectAmount").floatValue = spec.Amounts[slotIndex];
                    serialized.FindProperty("effectDescription").stringValue = spec.Descriptions[slotIndex];
                    serialized.FindProperty("setEffectDescription").stringValue = spec.SetDescription;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(asset);
                    generated.Add(asset);
                }
            }

            return generated;
        }

        private static List<WeaponDefinitionAsset> GenerateWeapons()
        {
            var generated = new List<WeaponDefinitionAsset>(27);
            foreach (WeaponSpec spec in WeaponSpecs)
            {
                for (int typeIndex = 0; typeIndex < WeaponTypes.Length; typeIndex++)
                {
                    string assetPath = $"{WeaponFolder}/{ToPascal(spec.Series)}{WeaponTypeNames[typeIndex].Replace(" ", string.Empty)}.asset";
                    WeaponDefinitionAsset asset = LoadOrCreate<WeaponDefinitionAsset>(assetPath);
                    var serialized = new SerializedObject(asset);
                    serialized.FindProperty("weaponId").stringValue = $"weapon.{WeaponTypeIds[typeIndex]}.{spec.Series}";
                    serialized.FindProperty("displayName").stringValue = $"{spec.Name} {WeaponTypeNames[typeIndex]}";
                    serialized.FindProperty("weaponType").enumValueIndex = (int)WeaponTypes[typeIndex];
                    serialized.FindProperty("weaponAttack").intValue = spec.Attacks[typeIndex];
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(asset);
                    generated.Add(asset);
                }
            }

            return generated;
        }

        private static List<AccessoryDefinitionAsset> GenerateAccessories()
        {
            var generated = new List<AccessoryDefinitionAsset>(10);
            foreach (AccessorySpec spec in AccessorySpecs)
            {
                string assetPath = $"{AccessoryFolder}/{ToPascal(spec.Id)}.asset";
                AccessoryDefinitionAsset asset = LoadOrCreate<AccessoryDefinitionAsset>(assetPath);
                var serialized = new SerializedObject(asset);
                serialized.FindProperty("accessoryId").stringValue = $"accessory.{spec.Id}";
                serialized.FindProperty("displayName").stringValue = spec.Name;
                serialized.FindProperty("effectType").enumValueIndex = (int)AccessoryEffectType.Custom;
                serialized.FindProperty("effectAmount").floatValue = 0f;
                serialized.FindProperty("effectDescription").stringValue = spec.Description;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                generated.Add(asset);
            }

            return generated;
        }

        private static void UpdateCatalog(
            IReadOnlyList<ArmorDefinitionAsset> generatedArmor,
            IReadOnlyList<WeaponDefinitionAsset> generatedWeapons,
            IReadOnlyList<AccessoryDefinitionAsset> generatedAccessories)
        {
            PlayerEquipmentCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<PlayerEquipmentCatalogAsset>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"Processing Shop could not find {CatalogPath}.");
                return;
            }

            var serialized = new SerializedObject(catalog);
            ReplaceGeneratedReferences(serialized.FindProperty("armor"), generatedArmor, ArmorFolder);
            ReplaceGeneratedReferences(serialized.FindProperty("weapons"), generatedWeapons, WeaponFolder);
            ReplaceGeneratedReferences(serialized.FindProperty("accessories"), generatedAccessories, AccessoryFolder);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void ReplaceGeneratedReferences<T>(
            SerializedProperty property,
            IReadOnlyList<T> generated,
            string generatedFolder)
            where T : UnityEngine.Object
        {
            var combined = new List<T>();
            var seen = new HashSet<T>();
            for (int index = 0; index < property.arraySize; index++)
            {
                T existing = property.GetArrayElementAtIndex(index).objectReferenceValue as T;
                if (existing != null
                    && !AssetDatabase.GetAssetPath(existing).StartsWith(generatedFolder + "/", StringComparison.Ordinal)
                    && seen.Add(existing))
                {
                    combined.Add(existing);
                }
            }

            foreach (T asset in generated)
            {
                if (asset != null && seen.Add(asset))
                {
                    combined.Add(asset);
                }
            }

            property.arraySize = combined.Count;
            for (int index = 0; index < combined.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = combined[index];
            }
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void GenerateIfMissing()
        {
            if (AssetDatabase.FindAssets("t:ArmorDefinitionAsset", new[] { ArmorFolder }).Length == 33
                && AssetDatabase.FindAssets("t:WeaponDefinitionAsset", new[] { WeaponFolder }).Length == 27
                && AssetDatabase.FindAssets("t:AccessoryDefinitionAsset", new[] { AccessoryFolder }).Length == 10)
            {
                return;
            }

            Generate();
        }

        private static ArmorSpec UniformArmor(
            string series,
            string name,
            int headDefense,
            int upperDefense,
            int lowerDefense,
            ArmorEffectType effectType,
            DamageType damageType,
            float headAmount,
            float upperAmount,
            float lowerAmount,
            string headDescription,
            string upperDescription,
            string lowerDescription,
            string setDescription = "")
        {
            return new ArmorSpec(
                series,
                name,
                new[] { headDefense, upperDefense, lowerDefense },
                new[] { effectType, effectType, effectType },
                new[] { damageType, damageType, damageType },
                new[] { headAmount, upperAmount, lowerAmount },
                new[] { headDescription, upperDescription, lowerDescription },
                setDescription);
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
