using System;
using System.Collections.Generic;
using SER494.Gameplay.CombatIntegration.Unity;
using SER494.Gameplay.PlayerState;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;

namespace SER494.Gameplay.ExplorationMenu.Unity
{
    public enum InventoryItemCategory
    {
        All,
        Consumable,
        Material,
        Other
    }

    public sealed class InventoryItemPresentation
    {
        public InventoryItemPresentation(
            string itemId,
            string displayName,
            string description,
            InventoryItemCategory category,
            string iconResourcePath,
            Color32 iconTint)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Description = description;
            Category = category;
            IconResourcePath = iconResourcePath;
            IconTint = iconTint;
        }

        public string ItemId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public InventoryItemCategory Category { get; }
        public string IconResourcePath { get; }
        public Color32 IconTint { get; }
    }

    public static class InventoryPresentationCatalog
    {
        private static readonly Color32 White = new Color32(255, 255, 255, 255);

        private static readonly InventoryItemPresentation[] KnownItems =
        {
            Item(PlayerStateInitialValues.HealingPotionId, "Healing Potion",
                "Restores HP when used in battle.", InventoryItemCategory.Consumable, null),
            Item(PlayerStateInitialValues.GreaterHealingPotionId, "Greater Healing Potion",
                "Restores more HP than a standard Healing Potion.",
                InventoryItemCategory.Consumable, null),
            Item(PlayerStateInitialValues.VitalityTonicId, "Vitality Tonic",
                "Restores Stamina when used in battle.", InventoryItemCategory.Consumable, null),

            Item(BattleRewardSummary.WhiteCopperMaterialId, "White Copper",
                "A pale metal used by the Processing Shop.", InventoryItemCategory.Material,
                "InventoryIcons/MetalIngot", new Color32(225, 231, 235, 255)),
            Item(BattleRewardSummary.BronzeMaterialId, "Bronze",
                "A sturdy metal used by the Processing Shop.", InventoryItemCategory.Material,
                "InventoryIcons/MetalIngot", new Color32(191, 112, 63, 255)),
            Item(BattleRewardSummary.BrassMaterialId, "Brass",
                "A warm-colored alloy used by the Processing Shop.",
                InventoryItemCategory.Material, "InventoryIcons/MetalIngot",
                new Color32(220, 177, 78, 255)),
            Item(BattleRewardSummary.ApeBeastPeltMaterialId, "Ape Beast Pelt",
                "A durable pelt taken from an Ape Beast.", InventoryItemCategory.Material,
                "InventoryIcons/ApePelt"),
            Item(BattleRewardSummary.ApeBeastArmguardMaterialId, "Ape Beast Armguard",
                "A hardened natural armguard taken from an Ape Beast.",
                InventoryItemCategory.Material, "InventoryIcons/ApeArmguard"),
            Item(BattleRewardSummary.ApeBeastBlackFurMaterialId, "Ape Beast Black Fur",
                "Dense black fur taken from an Ape Beast.", InventoryItemCategory.Material,
                "InventoryIcons/ApeBlackFur"),
            Item(BattleRewardSummary.ApeBeastBloodMaterialId, "Ape Beast Blood",
                "Fresh blood used in specialized processing.", InventoryItemCategory.Material,
                "InventoryIcons/ApeBlood"),
            Item(BattleRewardSummary.SlimeChunkMaterialId, "Slime Chunk",
                "A translucent chunk left behind by a Slime.", InventoryItemCategory.Material,
                "InventoryIcons/Slime"),
            Item(BattleRewardSummary.ThornMaterialId, "Thorn",
                "A sharp organic material used for thorn equipment.",
                InventoryItemCategory.Material, "InventoryIcons/Thorn"),
            Item(BattleRewardSummary.CrystalMaterialId, "Crystal",
                "A luminous crystal used for advanced processing.",
                InventoryItemCategory.Material, "InventoryIcons/Crystal",
                new Color32(139, 220, 255, 255)),
            Item(BattleRewardSummary.PureGoldMaterialId, "Pure Gold",
                "A valuable metal used for high-grade equipment.",
                InventoryItemCategory.Material, "InventoryIcons/MetalIngot",
                new Color32(255, 199, 61, 255)),
            Item(BattleRewardSummary.MithrilMaterialId, "Mithril",
                "A rare light metal used for high-grade equipment.",
                InventoryItemCategory.Material, "InventoryIcons/MetalIngot",
                new Color32(150, 216, 231, 255)),
            Item(BattleRewardSummary.YellowSilkMaterialId, "Yellow Silk",
                "Fine yellow silk used for lightweight equipment.",
                InventoryItemCategory.Material, "InventoryIcons/Thread",
                new Color32(244, 205, 70, 255)),
            Item(BattleRewardSummary.RedSilkMaterialId, "Red Silk",
                "Fine red silk used for lightweight equipment.",
                InventoryItemCategory.Material, "InventoryIcons/Thread",
                new Color32(202, 67, 67, 255))
        };

        private static readonly Dictionary<string, InventoryItemPresentation> Lookup =
            BuildLookup();

        public static IReadOnlyList<InventoryItemPresentation> AllKnownItems => KnownItems;

        public static bool TryGet(string itemId, out InventoryItemPresentation item)
        {
            item = null;
            return !string.IsNullOrWhiteSpace(itemId)
                && Lookup.TryGetValue(itemId.Trim(), out item);
        }

        public static bool MatchesFilter(
            InventoryItemPresentation item,
            InventoryItemCategory filter)
        {
            if (item == null)
            {
                return false;
            }

            return filter == InventoryItemCategory.All || item.Category == filter;
        }

        public static string FormatCountBadge(int count)
        {
            return count > 999 ? "999+" : $"×{Mathf.Max(0, count)}";
        }

        private static InventoryItemPresentation Item(
            string itemId,
            string displayName,
            string description,
            InventoryItemCategory category,
            string iconResourcePath,
            Color32? iconTint = null)
        {
            return new InventoryItemPresentation(
                itemId,
                displayName,
                description,
                category,
                iconResourcePath,
                iconTint ?? White);
        }

        private static Dictionary<string, InventoryItemPresentation> BuildLookup()
        {
            var lookup = new Dictionary<string, InventoryItemPresentation>(StringComparer.Ordinal);
            for (int index = 0; index < KnownItems.Length; index++)
            {
                lookup.Add(KnownItems[index].ItemId, KnownItems[index]);
            }

            return lookup;
        }
    }
}
