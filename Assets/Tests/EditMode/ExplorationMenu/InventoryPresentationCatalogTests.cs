using NUnit.Framework;
using SER494.Gameplay.ExplorationMenu.Unity;

namespace SER494.Gameplay.ExplorationMenu.Tests
{
    public sealed class InventoryPresentationCatalogTests
    {
        [TestCase("healing-potion", InventoryItemCategory.Consumable)]
        [TestCase("material.white-copper", InventoryItemCategory.Material)]
        [TestCase("material.ape-beast-pelt", InventoryItemCategory.Material)]
        public void KnownItems_HaveExpectedCategory(
            string itemId,
            InventoryItemCategory expectedCategory)
        {
            Assert.That(
                InventoryPresentationCatalog.TryGet(itemId, out InventoryItemPresentation item),
                Is.True);
            Assert.That(item.Category, Is.EqualTo(expectedCategory));
            Assert.That(item.DisplayName, Is.Not.Empty);
        }

        [Test]
        public void MatchesFilter_AllIncludesConsumablesAndMaterials()
        {
            InventoryPresentationCatalog.TryGet(
                "healing-potion",
                out InventoryItemPresentation consumable);
            InventoryPresentationCatalog.TryGet(
                "material.white-copper",
                out InventoryItemPresentation material);

            Assert.That(
                InventoryPresentationCatalog.MatchesFilter(
                    consumable,
                    InventoryItemCategory.All),
                Is.True);
            Assert.That(
                InventoryPresentationCatalog.MatchesFilter(
                    material,
                    InventoryItemCategory.All),
                Is.True);
            Assert.That(
                InventoryPresentationCatalog.MatchesFilter(
                    material,
                    InventoryItemCategory.Consumable),
                Is.False);
        }

        [TestCase(1, "×1")]
        [TestCase(27, "×27")]
        [TestCase(999, "×999")]
        [TestCase(1000, "999+")]
        public void FormatCountBadge_UsesCompactStackCount(int count, string expected)
        {
            Assert.That(InventoryPresentationCatalog.FormatCountBadge(count), Is.EqualTo(expected));
        }
    }
}
