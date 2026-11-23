using System.Linq;
using NUnit.Framework;

namespace SER494.Gameplay.PlayerState.Tests
{
    [Category("PlayerState")]
    public sealed class ProcessingShopTests
    {
        [Test]
        public void RecipeCatalog_DefinesSeventyOneUniqueRecipesAcrossAllEquipmentCategories()
        {
            Assert.That(ProcessingRecipeCatalog.All.Count, Is.EqualTo(71));
            Assert.That(
                ProcessingRecipeCatalog.All.Select(recipe => recipe.RecipeId).Distinct().Count(),
                Is.EqualTo(71));
            Assert.That(
                ProcessingRecipeCatalog.All.Select(recipe => recipe.EquipmentId).Distinct().Count(),
                Is.EqualTo(71));
            Assert.That(ProcessingRecipeCatalog.All.Count(recipe => recipe.Category == EquipmentCategory.Armor), Is.EqualTo(33));
            Assert.That(ProcessingRecipeCatalog.All.Count(recipe => recipe.Category == EquipmentCategory.Weapon), Is.EqualTo(27));
            Assert.That(ProcessingRecipeCatalog.All.Count(recipe => recipe.Category == EquipmentCategory.Accessory), Is.EqualTo(11));
            Assert.That(ProcessingRecipeCatalog.All.Any(recipe => recipe.EquipmentId == "accessory.mithril-ring"), Is.True);
        }

        [Test]
        public void RecipeCatalog_UsesApprovedAdvancedEquipmentCosts()
        {
            ProcessingRecipe weapon = FindRecipe("weapon.glaive-and-shield.mithril");
            Assert.That(weapon.GoldCost, Is.EqualTo(200));
            Assert.That(weapon.MaterialCosts.Count, Is.EqualTo(3));
            Assert.That(weapon.MaterialCosts.Single(cost => cost.ItemId == ProcessingRecipeCatalog.MithrilId).Amount, Is.EqualTo(3));
            Assert.That(weapon.MaterialCosts.Single(cost => cost.ItemId == ProcessingRecipeCatalog.CrystalId).Amount, Is.EqualTo(1));
            Assert.That(weapon.MaterialCosts.Single(cost => cost.ItemId == ProcessingRecipeCatalog.BrassId).Amount, Is.EqualTo(2));

            ProcessingRecipe armor = FindRecipe("armor.mithril.lower");
            Assert.That(armor.GoldCost, Is.EqualTo(300));
            Assert.That(armor.MaterialCosts.Single().ItemId, Is.EqualTo(ProcessingRecipeCatalog.MithrilId));
            Assert.That(armor.MaterialCosts.Single().Amount, Is.EqualTo(3));

            ProcessingRecipe accessory = FindRecipe("accessory.gold-ring");
            Assert.That(accessory.GoldCost, Is.EqualTo(490));
            Assert.That(accessory.MaterialCosts.Single().ItemId, Is.EqualTo(ProcessingRecipeCatalog.PureGoldId));
            Assert.That(accessory.MaterialCosts.Single().Amount, Is.EqualTo(2));

            ProcessingRecipe mithrilRing = FindRecipe("accessory.mithril-ring");
            Assert.That(mithrilRing.GoldCost, Is.EqualTo(490));
            Assert.That(mithrilRing.MaterialCosts.Single().ItemId, Is.EqualTo(ProcessingRecipeCatalog.MithrilId));
            Assert.That(mithrilRing.MaterialCosts.Single().Amount, Is.EqualTo(2));
        }

        [Test]
        public void TryCraft_ConsumesExactCostsAndGrantsOwnedArmorWithoutEquippingIt()
        {
            PlayerState state = CreateState();
            ProcessingRecipe recipe = FindRecipe("armor.white-copper.head");
            state.Currencies.Add(PlayerCurrencyState.GoldCurrencyId, 75);
            state.Inventory.Add(ProcessingRecipeCatalog.WhiteCopperId, 3);

            bool crafted = ProcessingShopService.TryCraft(state, recipe, out ProcessingCraftFailure failure);

            Assert.That(crafted, Is.True);
            Assert.That(failure, Is.EqualTo(ProcessingCraftFailure.None));
            Assert.That(state.Currencies.Gold, Is.EqualTo(25));
            Assert.That(state.Inventory.GetCount(ProcessingRecipeCatalog.WhiteCopperId), Is.EqualTo(1));
            Assert.That(state.Equipment.Owns(EquipmentCategory.Armor, recipe.EquipmentId), Is.True);
            Assert.That(state.Equipment.EquippedArmorBySlot.Count, Is.Zero);
        }

        [Test]
        public void TryCraft_MixedMaterialRecipeConsumesEveryRequiredMaterial()
        {
            PlayerState state = CreateState();
            ProcessingRecipe recipe = FindRecipe("armor.ape-beast.upper");
            state.Currencies.Add(PlayerCurrencyState.GoldCurrencyId, 60);
            state.Inventory.Add(ProcessingRecipeCatalog.ApeBeastPeltId, 1);
            state.Inventory.Add(ProcessingRecipeCatalog.ApeBeastArmguardId, 2);

            bool crafted = ProcessingShopService.TryCraft(state, recipe, out ProcessingCraftFailure failure);

            Assert.That(crafted, Is.True);
            Assert.That(failure, Is.EqualTo(ProcessingCraftFailure.None));
            Assert.That(state.Currencies.Gold, Is.Zero);
            Assert.That(state.Inventory.GetCount(ProcessingRecipeCatalog.ApeBeastPeltId), Is.Zero);
            Assert.That(state.Inventory.GetCount(ProcessingRecipeCatalog.ApeBeastArmguardId), Is.Zero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TryCraft_WhenAnyCostIsMissing_DoesNotPartiallyConsumeResources(bool omitGold)
        {
            PlayerState state = CreateState();
            ProcessingRecipe recipe = FindRecipe("armor.ape-beast.upper");
            state.Currencies.Add(PlayerCurrencyState.GoldCurrencyId, omitGold ? 59 : 60);
            state.Inventory.Add(ProcessingRecipeCatalog.ApeBeastPeltId, 1);
            state.Inventory.Add(ProcessingRecipeCatalog.ApeBeastArmguardId, omitGold ? 2 : 1);

            bool crafted = ProcessingShopService.TryCraft(state, recipe, out ProcessingCraftFailure failure);

            Assert.That(crafted, Is.False);
            Assert.That(
                failure,
                Is.EqualTo(omitGold
                    ? ProcessingCraftFailure.InsufficientGold
                    : ProcessingCraftFailure.InsufficientMaterials));
            Assert.That(state.Currencies.Gold, Is.EqualTo(omitGold ? 59 : 60));
            Assert.That(state.Inventory.GetCount(ProcessingRecipeCatalog.ApeBeastPeltId), Is.EqualTo(1));
            Assert.That(state.Inventory.GetCount(ProcessingRecipeCatalog.ApeBeastArmguardId), Is.EqualTo(omitGold ? 2 : 1));
            Assert.That(state.Equipment.Owns(EquipmentCategory.Armor, recipe.EquipmentId), Is.False);
        }

        [Test]
        public void TryCraft_WhenArmorIsAlreadyOwned_DoesNotConsumeResources()
        {
            PlayerState state = CreateState();
            ProcessingRecipe recipe = FindRecipe("armor.slime.lower");
            state.Equipment.Grant(EquipmentCategory.Armor, recipe.EquipmentId);
            state.Currencies.Add(PlayerCurrencyState.GoldCurrencyId, 80);
            state.Inventory.Add(ProcessingRecipeCatalog.SlimeChunkId, 2);

            bool crafted = ProcessingShopService.TryCraft(state, recipe, out ProcessingCraftFailure failure);

            Assert.That(crafted, Is.False);
            Assert.That(failure, Is.EqualTo(ProcessingCraftFailure.AlreadyOwned));
            Assert.That(state.Currencies.Gold, Is.EqualTo(80));
            Assert.That(state.Inventory.GetCount(ProcessingRecipeCatalog.SlimeChunkId), Is.EqualTo(2));
        }

        [TestCase(EquipmentCategory.Weapon, "weapon.test")]
        [TestCase(EquipmentCategory.Accessory, "accessory.test")]
        public void TryCraft_NonArmorRecipeGrantsItsDeclaredEquipmentCategory(
            EquipmentCategory category,
            string equipmentId)
        {
            PlayerState state = CreateState();
            var recipe = new ProcessingRecipe(
                $"recipe.{equipmentId}",
                category,
                equipmentId,
                "Test Equipment",
                "Test Series",
                category == EquipmentCategory.Weapon ? "weapon" : "accessory.primary",
                25,
                new ProcessingMaterialCost("material.test", "Test Material", 2));
            state.Currencies.Add(PlayerCurrencyState.GoldCurrencyId, 25);
            state.Inventory.Add("material.test", 2);

            bool crafted = ProcessingShopService.TryCraft(
                state,
                recipe,
                out ProcessingCraftFailure failure);

            Assert.That(crafted, Is.True);
            Assert.That(failure, Is.EqualTo(ProcessingCraftFailure.None));
            Assert.That(state.Equipment.Owns(category, equipmentId), Is.True);
            Assert.That(state.Equipment.Owns(EquipmentCategory.Armor, equipmentId), Is.False);
            Assert.That(state.Currencies.Gold, Is.Zero);
            Assert.That(state.Inventory.GetCount("material.test"), Is.Zero);
        }

        private static ProcessingRecipe FindRecipe(string equipmentId)
        {
            return ProcessingRecipeCatalog.All.Single(recipe => recipe.EquipmentId == equipmentId);
        }

        private static PlayerState CreateState()
        {
            return new PlayerState(100, 30, 100);
        }
    }
}
