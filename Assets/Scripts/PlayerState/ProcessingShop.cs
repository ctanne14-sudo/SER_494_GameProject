using System;
using System.Collections.Generic;

namespace SER494.Gameplay.PlayerState
{
    public readonly struct ProcessingMaterialCost
    {
        public ProcessingMaterialCost(string itemId, string displayName, int amount)
        {
            ItemId = PlayerStateId.Require(itemId, nameof(itemId));
            DisplayName = PlayerStateId.Require(displayName, nameof(displayName));
            Amount = amount > 0
                ? amount
                : throw new ArgumentOutOfRangeException(nameof(amount));
        }

        public string ItemId { get; }
        public string DisplayName { get; }
        public int Amount { get; }
    }

    public sealed class ProcessingRecipe
    {
        public ProcessingRecipe(
            string recipeId,
            string equipmentId,
            string displayName,
            string seriesDisplayName,
            string slotId,
            int goldCost,
            params ProcessingMaterialCost[] materialCosts)
        {
            RecipeId = PlayerStateId.Require(recipeId, nameof(recipeId));
            EquipmentId = PlayerStateId.Require(equipmentId, nameof(equipmentId));
            DisplayName = PlayerStateId.Require(displayName, nameof(displayName));
            SeriesDisplayName = PlayerStateId.Require(seriesDisplayName, nameof(seriesDisplayName));
            SlotId = PlayerStateId.Require(slotId, nameof(slotId));
            GoldCost = goldCost >= 0
                ? goldCost
                : throw new ArgumentOutOfRangeException(nameof(goldCost));
            MaterialCosts = Array.AsReadOnly(
                materialCosts ?? throw new ArgumentNullException(nameof(materialCosts)));
            if (MaterialCosts.Count == 0)
            {
                throw new ArgumentException("A processing recipe requires material costs.", nameof(materialCosts));
            }
        }

        public string RecipeId { get; }
        public string EquipmentId { get; }
        public string DisplayName { get; }
        public string SeriesDisplayName { get; }
        public string SlotId { get; }
        public int GoldCost { get; }
        public IReadOnlyList<ProcessingMaterialCost> MaterialCosts { get; }
    }

    public enum ProcessingCraftFailure
    {
        None,
        AlreadyOwned,
        InsufficientGold,
        InsufficientMaterials
    }

    public static class ProcessingShopService
    {
        public static bool TryCraft(
            PlayerState playerState,
            ProcessingRecipe recipe,
            out ProcessingCraftFailure failure)
        {
            if (playerState == null)
            {
                throw new ArgumentNullException(nameof(playerState));
            }

            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            if (playerState.Equipment.Owns(EquipmentCategory.Armor, recipe.EquipmentId))
            {
                failure = ProcessingCraftFailure.AlreadyOwned;
                return false;
            }

            if (playerState.Currencies.Gold < recipe.GoldCost)
            {
                failure = ProcessingCraftFailure.InsufficientGold;
                return false;
            }

            for (int index = 0; index < recipe.MaterialCosts.Count; index++)
            {
                ProcessingMaterialCost cost = recipe.MaterialCosts[index];
                if (playerState.Inventory.GetCount(cost.ItemId) < cost.Amount)
                {
                    failure = ProcessingCraftFailure.InsufficientMaterials;
                    return false;
                }
            }

            if (recipe.GoldCost > 0)
            {
                playerState.Currencies.TrySpend(PlayerCurrencyState.GoldCurrencyId, recipe.GoldCost);
            }

            for (int index = 0; index < recipe.MaterialCosts.Count; index++)
            {
                ProcessingMaterialCost cost = recipe.MaterialCosts[index];
                playerState.Inventory.TryConsume(cost.ItemId, cost.Amount);
            }

            playerState.Equipment.Grant(EquipmentCategory.Armor, recipe.EquipmentId);
            failure = ProcessingCraftFailure.None;
            return true;
        }
    }

    public static class ProcessingRecipeCatalog
    {
        public const string WhiteCopperId = "material.white-copper";
        public const string BronzeId = "material.bronze";
        public const string BrassId = "material.brass";
        public const string ApeBeastPeltId = "material.ape-beast-pelt";
        public const string ApeBeastArmguardId = "material.ape-beast-armguard";
        public const string ApeBeastBlackFurId = "material.ape-beast-black-fur";
        public const string SlimeChunkId = "material.slime-chunk";
        public const string YellowSilkId = "material.yellow-silk";
        public const string RedSilkId = "material.red-silk";
        public const string ThornId = "material.thorn";
        public const string CrystalId = "material.crystal";
        public const string PureGoldId = "material.pure-gold";

        private static readonly IReadOnlyList<ProcessingRecipe> Recipes = BuildRecipes();

        public static IReadOnlyList<ProcessingRecipe> All => Recipes;

        private static IReadOnlyList<ProcessingRecipe> BuildRecipes()
        {
            var recipes = new List<ProcessingRecipe>(30);
            AddUniformSeries(recipes, "white-copper", "White Copper", WhiteCopperId, "White Copper", 2, 50);
            AddUniformSeries(recipes, "bronze", "Bronze", BronzeId, "Bronze", 2, 50);
            AddUniformSeries(recipes, "brass", "Brass", BrassId, "Brass", 2, 50);
            AddApeBeastSeries(recipes);
            AddUniformSeries(recipes, "slime", "Slime", SlimeChunkId, "Slime Chunk", 2, 80);
            AddUniformSeries(recipes, "yellow-silk", "Yellow Silk", YellowSilkId, "Yellow Silk", 3, 100);
            AddUniformSeries(recipes, "red-silk", "Red Silk", RedSilkId, "Red Silk", 3, 100);
            AddUniformSeries(recipes, "thorn", "Thorn", ThornId, "Thorn", 2, 80);
            AddUniformSeries(recipes, "crystal", "Crystal", CrystalId, "Crystal", 3, 300);
            AddUniformSeries(recipes, "pure-gold", "Pure Gold", PureGoldId, "Pure Gold", 3, 300);
            return recipes.AsReadOnly();
        }

        private static void AddUniformSeries(
            ICollection<ProcessingRecipe> recipes,
            string seriesId,
            string seriesName,
            string materialId,
            string materialName,
            int materialAmount,
            int goldCost)
        {
            AddRecipe(recipes, seriesId, seriesName, "head", "Headpiece", goldCost,
                new ProcessingMaterialCost(materialId, materialName, materialAmount));
            AddRecipe(recipes, seriesId, seriesName, "upper", "Upper Armor", goldCost,
                new ProcessingMaterialCost(materialId, materialName, materialAmount));
            AddRecipe(recipes, seriesId, seriesName, "lower", "Lower Armor", goldCost,
                new ProcessingMaterialCost(materialId, materialName, materialAmount));
        }

        private static void AddApeBeastSeries(ICollection<ProcessingRecipe> recipes)
        {
            AddRecipe(recipes, "ape-beast", "Ape Beast", "head", "Headpiece", 60,
                new ProcessingMaterialCost(ApeBeastBlackFurId, "Ape Beast Black Fur", 2));
            AddRecipe(recipes, "ape-beast", "Ape Beast", "upper", "Upper Armor", 60,
                new ProcessingMaterialCost(ApeBeastPeltId, "Ape Beast Pelt", 1),
                new ProcessingMaterialCost(ApeBeastArmguardId, "Ape Beast Armguard", 2));
            AddRecipe(recipes, "ape-beast", "Ape Beast", "lower", "Lower Armor", 60,
                new ProcessingMaterialCost(ApeBeastPeltId, "Ape Beast Pelt", 2),
                new ProcessingMaterialCost(ApeBeastArmguardId, "Ape Beast Armguard", 1));
        }

        private static void AddRecipe(
            ICollection<ProcessingRecipe> recipes,
            string seriesId,
            string seriesName,
            string slotSuffix,
            string slotName,
            int goldCost,
            params ProcessingMaterialCost[] costs)
        {
            string equipmentId = $"armor.{seriesId}.{slotSuffix}";
            recipes.Add(new ProcessingRecipe(
                $"recipe.{equipmentId}",
                equipmentId,
                $"{seriesName} {slotName}",
                seriesName,
                GetSlotId(slotSuffix),
                goldCost,
                costs));
        }

        private static string GetSlotId(string slotSuffix)
        {
            switch (slotSuffix)
            {
                case "head":
                    return "armor.head";
                case "upper":
                    return "armor.upper-body";
                case "lower":
                    return "armor.lower-body";
                default:
                    throw new ArgumentOutOfRangeException(nameof(slotSuffix));
            }
        }
    }
}
