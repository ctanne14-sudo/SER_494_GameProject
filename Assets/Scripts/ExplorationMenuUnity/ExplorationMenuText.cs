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
                case "combos.note": return "Select an unlocked loadout to inspect its four Combos and Timeline previews.";
                case "combos.back-loadouts": return "← Back to Loadouts";
                case "combos.back-combos": return "← Back to Combo List";
                case "combos.rename-loadout": return "Rename Loadout";
                case "combos.rename-combo": return "Rename Combo";
                case "combos.rename-save": return "Save Name";
                case "combos.rename-cancel": return "Cancel";
                case "combos.rename-required": return "A name is required.";
                case "combos.compact-timeline-legend": return "Mini Timelines: colors mark Resolution effects, gold marks a Charge-dependent Resolution Point, and stripes mark a Resolution Window.";
                case "combos.active": return "ACTIVE LOADOUT";
                case "combos.valid": return "Loadout configuration is valid.";
                case "combos.invalid": return "Loadout configuration is invalid.";
                case "combos.timeline": return "Timeline Preview";
                case "combos.timeline-legend": return "D Damage    G Guard    E Evade    B Buff / Debuff    H Heal    S Stamina    Diagonal stripes Resolution Window";
                case "combos.timeline-guide": return "Timeline Guide";
                case "combos.timeline-guide-heading": return "How to Read the Timeline";
                case "combos.timeline-guide-base": return "Each square is one Time Step. Brown squares belong to an Action; dark squares are outside that Action.";
                case "combos.timeline-guide-window": return "Diagonal stripes mark a Resolution Window instead of one fixed Resolution Point.";
                case "combos.timeline-guide-charge": return "Select a chargeable Action on the Timeline to preview its Charge Level. Added Time Steps move every later Action.";
                case "combos.timeline-guide-close": return "Back to Combo";
                case "combos.timeline-guide-damage": return "Damage Resolution Point";
                case "combos.timeline-guide-guard": return "Guard Resolution Point";
                case "combos.timeline-guide-evade": return "Evade Resolution Point";
                case "combos.timeline-guide-status": return "Buff or Debuff Resolution Point";
                case "combos.timeline-guide-heal": return "Healing Resolution Point";
                case "combos.timeline-guide-stamina": return "Stamina Recovery Resolution Point";
                case "combos.equipped-filter": return "Equipped";
                case "combos.charge-level": return "Preview Charge Level";
                case "combos.actions": return "Action Details";
                case "combos.configure-actions": return "Configure Actions";
                case "combos.assigned-actions": return "Assigned Actions";
                case "combos.action-details": return "Action Details";
                case "combos.action-library": return "Actions";
                case "combos.action-filter-shortcut": return "Q / X: Focus filters";
                case "combos.empty-slot": return "Empty";
                case "combos.clear-slot": return "Clear This Optional Slot";
                case "combos.current": return "Current";
                case "combos.used-elsewhere": return "Already assigned elsewhere in this Loadout";
                case "combos.weapon-warning": return "Warning: incompatible with the equipped Weapon";
                case "combos.no-owned-actions": return "No owned Actions are available for this slot.";
                case "combos.linked-placement": return "Linked Action Placement";
                case "combos.apply": return "Apply Changes";
                case "combos.revert": return "Revert Changes";
                case "combos.unsaved-heading": return "Unsaved Combo Changes";
                case "combos.unsaved-prompt": return "This Combo has unapplied changes. Save them before leaving?";
                case "combos.unsaved-save": return "Save Changes";
                case "combos.unsaved-discard": return "Discard Changes";
                case "combos.unsaved-cancel": return "Back to Editing";
                case "combos.activate": return "Set as Active Loadout";
                case "combos.fix-before-leaving": return "This Loadout cannot be left with an invalid draft.";
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
                case "footer.controls": return "Esc / B / Right Click: Back    F / Enter / A / Left Click: Confirm    WASD / Arrows / D-pad: Navigate    Wheel: Scroll";
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
