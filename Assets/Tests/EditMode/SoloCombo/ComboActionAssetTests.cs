using NUnit.Framework;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
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
                manaCost: 3,
                staminaCost: 2,
                cooldown: 4);

            bool succeeded = _asset.TryCreateDefinition(
                out ComboActionDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.True);
            Assert.That(validationError, Is.Null);
            Assert.That(definition.ActionId, Is.EqualTo("test-main"));
            Assert.That(definition.DisplayName, Is.EqualTo("Test Main"));
            Assert.That(definition.Category, Is.EqualTo(ComboActionCategory.Main));
            Assert.That(definition.LoadCost, Is.EqualTo(5));
            Assert.That(definition.ManaCost, Is.EqualTo(3));
            Assert.That(definition.StaminaCost, Is.EqualTo(2));
            Assert.That(definition.Cooldown, Is.EqualTo(4));
        }

        [Test]
        public void TryCreateDefinition_EmptyActionId_ReturnsValidationError()
        {
            ConfigureAsset(
                actionId: "",
                displayName: "Test Main",
                category: ComboActionCategory.Main,
                loadCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0);

            bool succeeded = _asset.TryCreateDefinition(
                out ComboActionDefinition definition,
                out string validationError);

            Assert.That(succeeded, Is.False);
            Assert.That(definition, Is.Null);
            Assert.That(validationError, Does.Contain("empty Action ID"));
        }

        private void ConfigureAsset(
            string actionId,
            string displayName,
            ComboActionCategory category,
            int loadCost,
            int manaCost,
            int staminaCost,
            int cooldown)
        {
            var serializedAsset = new SerializedObject(_asset);
            serializedAsset.FindProperty("actionId").stringValue = actionId;
            serializedAsset.FindProperty("displayName").stringValue = displayName;
            serializedAsset.FindProperty("category").enumValueIndex = (int)category;
            serializedAsset.FindProperty("loadCost").intValue = loadCost;
            serializedAsset.FindProperty("manaCost").intValue = manaCost;
            serializedAsset.FindProperty("staminaCost").intValue = staminaCost;
            serializedAsset.FindProperty("cooldown").intValue = cooldown;
            serializedAsset.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
