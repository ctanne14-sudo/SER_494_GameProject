namespace SER494.Gameplay.ExplorationMenu.Unity
{
    internal static class ExplorationMenuText
    {
        public static string Get(string key)
        {
            switch (key)
            {
                case "menu.title": return "Exploration Menu";
                case "nav.status": return "Status";
                case "nav.combos": return "Combo Loadouts";
                case "nav.inventory": return "Inventory";
                case "nav.equipment": return "Equipment";
                case "nav.title": return "Return to Title";
                case "status.heading": return "Status Overview";
                case "status.hp": return "HP";
                case "status.weapon": return "Equipped Weapon";
                case "status.defense": return "Total DEF";
                case "status.loadout": return "Active Combo Loadout";
                case "combos.heading": return "Combo Loadouts";
                case "combos.single-heading": return "Single Combo Loadout";
                case "combos.actions-heading": return "Actions of a Combo";
                case "combos.note": return "Select an unlocked loadout to inspect its four Combos and Timeline previews.";
                case "combos.back-loadouts": return "Back to Loadouts";
                case "combos.back-combos": return "Back to Combo List";
                case "combos.active": return "ACTIVE LOADOUT";
                case "combos.inactive": return "INACTIVE LOADOUT";
                case "combos.valid": return "Loadout configuration is valid.";
                case "combos.invalid": return "Loadout configuration is invalid.";
                case "combos.timeline": return "Timeline Preview";
                case "combos.timeline-legend": return "D Damage    G Guard    E Evade    B Buff / Debuff    H Heal    S Stamina    Diagonal stripes Resolution Window";
                case "combos.actions": return "Action Details";
                case "combos.editor-next": return "Action replacement, draft validation, activation prompts, and Revert Changes are added in the next Combo Configuration stage.";
                case "inventory.heading": return "Inventory";
                case "inventory.consumables": return "Consumables";
                case "inventory.materials": return "Materials";
                case "inventory.empty": return "No items in this category.";
                case "equipment.heading": return "Equipment";
                case "equipment.current": return "Equipped Gear";
                case "equipment.summary": return "Current Equipment Summary";
                case "equipment.weapon": return "Weapon";
                case "equipment.weapons": return "Weapons";
                case "equipment.armor": return "Armor";
                case "equipment.accessories": return "Accessories";
                case "equipment.head": return "Head";
                case "equipment.upper": return "Upper Body";
                case "equipment.lower": return "Lower Body";
                case "equipment.accessory": return "Ring";
                case "equipment.attack": return "ATK";
                case "equipment.defense": return "DEF";
                case "equipment.effects": return "Combined Equipment Effects";
                case "equipment.no-effects": return "No equipment effects.";
                case "equipment.choose": return "Choose {0}";
                case "equipment.cancel": return "Back / Cancel Change";
                case "equipment.equip": return "Equip";
                case "equipment.equipped": return "Equipped";
                case "equipment.none": return "No owned equipment in this category.";
                case "title.unavailable": return "A title scene has not been connected yet.";
                case "footer.controls": return "Esc / B / Right Click: Back    F / Enter / A / Left Click: Confirm    WASD / Arrows / D-pad / Wheel: Navigate";
                default: return key;
            }
        }

        public static string EquipmentName(string equipmentId, string fallback)
        {
            switch (equipmentId)
            {
                case "weapon.greatsword.rusted-iron-sword": return "Rusted Iron Sword";
                case "weapon.greatsword.cast-iron-greatsword": return "Cast Iron Greatsword";
                case "armor.linen-headpiece": return "Linen Headpiece";
                case "armor.linen-shirt": return "Linen Shirt";
                case "armor.linen-hose": return "Linen Hose";
                case "armor.leather-headpiece": return "Leather Headpiece";
                case "armor.leather-shirt": return "Leather Shirt";
                case "armor.leather-hose": return "Leather Hose";
                case "accessory.rust-ring": return "Rust Ring";
                case "accessory.ruby-ring": return "Ruby Ring";
                default: return string.IsNullOrWhiteSpace(fallback) ? equipmentId : fallback;
            }
        }
    }
}
