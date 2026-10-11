using NUnit.Framework;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Damage;
using UnityEditor;
using UnityEngine;

namespace SER494.Gameplay.Combat.SoloCombo.Tests
{
    public sealed class ComboActionAssetTests
    {
        private ComboActionAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _asset = ScriptableObject.CreateInstance<ComboActionAsset>();
            _asset.name = "Test Action Asset";
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void TryCreateDefinition_ValidAsset_CopiesSerializedValues()
        {
            ConfigureAsset(
                actionId: "test-main",
                displayName: "Test Main",
                category: ComboActionCategory.Main,
                loadCost: 5,
                resolutionOffset: 3,
                manaCost: 3,
                staminaCost: 2,
                cooldown: 4,
                damageMultiplier: 0.60f,
                damageType: DamageType.Fire,
                resolutionEffect: ActionResolutionEffect.Damage,
                damageTags: DamageResolutionTag.Area | DamageResolutionTag.Unguardable);

            bool succeeded = _asset.TryCreateDefinition(
                out ComboActionDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.True);
            Assert.That(validationError, Is.Null);
            Assert.That(definition.ActionId, Is.EqualTo("test-main"));
            Assert.That(definition.DisplayName, Is.EqualTo("Test Main"));
            Assert.That(definition.Category, Is.EqualTo(ComboActionCategory.Main));
            Assert.That(definition.LoadCost, Is.EqualTo(5));
            Assert.That(definition.ResolutionOffset, Is.EqualTo(3));
            Assert.That(definition.ManaCost, Is.EqualTo(3));
            Assert.That(definition.StaminaCost, Is.EqualTo(2));
            Assert.That(definition.Cooldown, Is.EqualTo(4));
            Assert.That(definition.DamageEffect.Components[0].ActionMultiplier,
                Is.EqualTo(0.60d).Within(0.000001d));
            Assert.That(definition.DamageEffect.Components[0].Type, Is.EqualTo(DamageType.Fire));
            Assert.That(definition.ResolutionEffect, Is.EqualTo(ActionResolutionEffect.Damage));
            Assert.That(
                definition.DamageTags,
                Is.EqualTo(DamageResolutionTag.Area | DamageResolutionTag.Unguardable));
        }

        [Test]
        public void TryCreateDefinition_EmptyActionId_ReturnsValidationError()
        {
            ConfigureAsset(
                actionId: "",
                displayName: "Test Main",
                category: ComboActionCategory.Main,
                loadCost: 1,
                resolutionOffset: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0f,
                damageType: DamageType.Slashing,
                resolutionEffect: ActionResolutionEffect.None,
                damageTags: DamageResolutionTag.None);

            bool succeeded = _asset.TryCreateDefinition(
                out ComboActionDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.False);
            Assert.That(definition, Is.Null);
            Assert.That(validationError, Does.Contain("empty Action ID"));
        }

        [Test]
        public void TryCreateDefinition_GuardEffect_PreservesResolutionType()
        {
            ConfigureAsset(
                actionId: "test-guard",
                displayName: "Test Guard",
                category: ComboActionCategory.Opening,
                loadCost: 3,
                resolutionOffset: 2,
                manaCost: 0,
                staminaCost: 2,
                cooldown: 1,
                damageMultiplier: 0f,
                damageType: DamageType.Blunt,
                resolutionEffect: ActionResolutionEffect.Guard,
                damageTags: DamageResolutionTag.None);

            bool succeeded = _asset.TryCreateDefinition(
                out ComboActionDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.True);
            Assert.That(validationError, Is.Null);
            Assert.That(definition.DamageEffect, Is.Null);
            Assert.That(definition.ResolutionOffset, Is.EqualTo(2));
            Assert.That(definition.ResolutionEffect, Is.EqualTo(ActionResolutionEffect.Guard));
        }

        [Test]
        public void TryCreateDefinition_DamageEffectWithoutMultiplier_ReturnsValidationError()
        {
            ConfigureAsset(
                actionId: "invalid-damage",
                displayName: "Invalid Damage",
                category: ComboActionCategory.Main,
                loadCost: 2,
                resolutionOffset: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0f,
                damageType: DamageType.Slashing,
                resolutionEffect: ActionResolutionEffect.Damage,
                damageTags: DamageResolutionTag.None);

            bool succeeded = _asset.TryCreateDefinition(
                out ComboActionDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.False);
            Assert.That(definition, Is.Null);
            Assert.That(validationError, Does.Contain("without damage"));
        }

        [Test]
        public void TryCreateDefinition_DamageTagsWithoutDamage_ReturnsValidationError()
        {
            ConfigureAsset(
                actionId: "invalid-tags",
                displayName: "Invalid Tags",
                category: ComboActionCategory.Main,
                loadCost: 2,
                resolutionOffset: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0f,
                damageType: DamageType.Slashing,
                resolutionEffect: ActionResolutionEffect.None,
                damageTags: DamageResolutionTag.Area);

            bool succeeded = _asset.TryCreateDefinition(
                out ComboActionDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.False);
            Assert.That(definition, Is.Null);
            Assert.That(validationError, Does.Contain("Damage Tags without damage"));
        }

        private void ConfigureAsset(
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
            ActionResolutionEffect resolutionEffect,
            DamageResolutionTag damageTags)
        {
            var serializedAsset = new SerializedObject(_asset);
            serializedAsset.FindProperty("actionId").stringValue = actionId;
            serializedAsset.FindProperty("displayName").stringValue = displayName;
            serializedAsset.FindProperty("category").enumValueIndex = (int)category;
            serializedAsset.FindProperty("loadCost").intValue = loadCost;
            serializedAsset.FindProperty("resolutionOffset").intValue = resolutionOffset;
            serializedAsset.FindProperty("manaCost").intValue = manaCost;
            serializedAsset.FindProperty("staminaCost").intValue = staminaCost;
            serializedAsset.FindProperty("cooldown").intValue = cooldown;
            serializedAsset.FindProperty("damageMultiplier").floatValue = damageMultiplier;
            serializedAsset.FindProperty("damageType").enumValueIndex = (int)damageType;
            serializedAsset.FindProperty("resolutionEffect").enumValueIndex = (int)resolutionEffect;
            serializedAsset.FindProperty("damageTags").intValue = (int)damageTags;
            serializedAsset.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
