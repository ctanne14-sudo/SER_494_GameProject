using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.Weapons;
using SER494.Gameplay.Combat.Weapons;
using UnityEditor;
using UnityEngine;

namespace SER494.Gameplay.Combat.Editor.SoloCombo
{
    public static class SampleWeaponAssetCreator
    {
        public const string WeaponFolder = "Assets/GameData/SoloCombo/SampleWeapons";

        [MenuItem("Tools/SER494/Create Sample Physical Weapons")]
        public static void CreateSampleWeapons()
        {
            WeaponDefinitionAsset[] weapons = SynchronizeSampleWeapons();
            Debug.Log($"Created or synchronized {weapons.Length} sample Weapon asset(s) in {WeaponFolder}.");
        }

        public static WeaponDefinitionAsset[] SynchronizeSampleWeapons()
        {
            EnsureFolder("Assets", "GameData");
            EnsureFolder("Assets/GameData", "SoloCombo");
            EnsureFolder("Assets/GameData/SoloCombo", "SampleWeapons");

            var definitions = new[]
            {
                new SampleWeapon("Greatsword", "weapon.greatsword", "Greatsword", WeaponType.Greatsword),
                new SampleWeapon("SwordAndShield", "weapon.sword-and-shield", "Sword and Shield", WeaponType.SwordAndShield),
                new SampleWeapon("SpearAndShield", "weapon.spear-and-shield", "Spear and Shield", WeaponType.SpearAndShield),
                new SampleWeapon("Bow", "weapon.bow", "Bow", WeaponType.Bow),
                new SampleWeapon("Bladeguard", "weapon.bladeguard", "Bladeguard", WeaponType.Bladeguard)
            };

            var assets = new List<WeaponDefinitionAsset>(definitions.Length);
            for (int index = 0; index < definitions.Length; index++)
            {
                assets.Add(CreateOrUpdate(definitions[index]));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return assets.ToArray();
        }

        private static WeaponDefinitionAsset CreateOrUpdate(SampleWeapon sample)
        {
            string path = $"{WeaponFolder}/{sample.FileName}.asset";
            WeaponDefinitionAsset asset = AssetDatabase.LoadAssetAtPath<WeaponDefinitionAsset>(path);
            bool isNew = asset == null;
            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<WeaponDefinitionAsset>();
                asset.name = sample.FileName;
            }

            var serializedAsset = new SerializedObject(asset);
            serializedAsset.FindProperty("weaponId").stringValue = sample.WeaponId;
            serializedAsset.FindProperty("displayName").stringValue = sample.DisplayName;
            serializedAsset.FindProperty("weaponType").enumValueIndex = (int)sample.WeaponType;
            serializedAsset.FindProperty("weaponAttack").intValue = 50;
            serializedAsset.ApplyModifiedPropertiesWithoutUndo();

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, path);
            }

            EditorUtility.SetDirty(asset);
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

        private readonly struct SampleWeapon
        {
            public SampleWeapon(
                string fileName,
                string weaponId,
                string displayName,
                WeaponType weaponType)
            {
                FileName = fileName;
                WeaponId = weaponId;
                DisplayName = displayName;
                WeaponType = weaponType;
            }

            public string FileName { get; }

            public string WeaponId { get; }

            public string DisplayName { get; }

            public WeaponType WeaponType { get; }
        }
    }
}
