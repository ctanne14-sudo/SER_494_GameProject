using NUnit.Framework;
using SER494.Gameplay.Combat.Authoring.Weapons;
using SER494.Gameplay.Combat.Weapons;
using UnityEditor;
using UnityEngine;

namespace SER494.Gameplay.Combat.SoloCombo.Tests
{
    [Category("CoreCombat")]
    [Category("Authoring")]
    public sealed class WeaponDefinitionAssetTests
    {
        private WeaponDefinitionAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<WeaponDefinitionAsset>();
            _asset.name = "Test Weapon Asset";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void TryCreateDefinition_ValidAsset_CopiesWeaponData()
        {
            var serializedAsset = new SerializedObject(_asset);
            serializedAsset.FindProperty("weaponId").stringValue = "weapon.bladeguard";
            serializedAsset.FindProperty("displayName").stringValue = "Bladeguard";
            serializedAsset.FindProperty("weaponType").enumValueIndex =
                (int)WeaponType.Bladeguard;
            serializedAsset.FindProperty("weaponAttack").intValue = 50;
            serializedAsset.ApplyModifiedPropertiesWithoutUndo();

            bool succeeded = _asset.TryCreateDefinition(
                out WeaponDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.True);
            Assert.That(validationError, Is.Null);
            Assert.That(definition.WeaponId, Is.EqualTo("weapon.bladeguard"));
            Assert.That(definition.DisplayName, Is.EqualTo("Bladeguard"));
            Assert.That(definition.WeaponType, Is.EqualTo(WeaponType.Bladeguard));
            Assert.That(definition.WeaponAttack, Is.EqualTo(50));
        }

        [Test]
        public void TryCreateDefinition_MissingIdentity_ReturnsValidationError()
        {
            bool succeeded = _asset.TryCreateDefinition(
                out WeaponDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.False);
            Assert.That(definition, Is.Null);
            Assert.That(validationError, Does.Contain("Weapon ID"));
        }
    }
}
