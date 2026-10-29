using System.Diagnostics;
using SER494.Gameplay.PlayerState;
using SER494.Gameplay.PlayerState.Unity;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.ExplorationMenu.Unity
{
    internal static class ExplorationMenuEditorPreviewContent
    {
        private const string CastIronGreatswordId =
            "weapon.greatsword.cast-iron-greatsword";
        private const string LeatherHeadpieceId = "armor.leather-headpiece";
        private const string LeatherShirtId = "armor.leather-shirt";
        private const string LeatherHoseId = "armor.leather-hose";
        private const string RustRingId = "accessory.rust-ring";
        private const string RubyRingId = "accessory.ruby-ring";

        [Conditional("UNITY_EDITOR")]
        internal static void GrantTestEquipment()
        {
            if (!PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                return;
            }

            PlayerEquipmentState equipment = service.State.Equipment;
            equipment.Grant(EquipmentCategory.Weapon, CastIronGreatswordId);
            equipment.Grant(EquipmentCategory.Armor, LeatherHeadpieceId);
            equipment.Grant(EquipmentCategory.Armor, LeatherShirtId);
            equipment.Grant(EquipmentCategory.Armor, LeatherHoseId);
            equipment.Grant(EquipmentCategory.Accessory, RustRingId);
            equipment.Grant(EquipmentCategory.Accessory, RubyRingId);
        }

        [Conditional("UNITY_EDITOR")]
        internal static void GrantComboLoadouts()
        {
            if (!PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                return;
            }

            PersistentPlayerState state = service.State;
            PlayerComboLoadoutState glaiveLoadout = GetOrUnlockPreviewLoadout(
                state,
                2,
                "Glaive Preview Loadout");
            LearnAndConfigureGlaivePreview(state, glaiveLoadout);

            PlayerComboLoadoutState commonLoadout = GetOrUnlockPreviewLoadout(
                state,
                3,
                "Common Preview Loadout");
            LearnAndConfigureCommonPreview(state, commonLoadout);
        }

        private static PlayerComboLoadoutState GetOrUnlockPreviewLoadout(
            PersistentPlayerState state,
            int oneBasedSlot,
            string name)
        {
            while (state.ComboLoadouts.UnlockedSlotCount < oneBasedSlot)
            {
                state.ComboLoadouts.UnlockNext();
            }

            PlayerComboLoadoutState loadout = state.ComboLoadouts.Loadouts[oneBasedSlot - 1];
            loadout.Rename(name);
            return loadout;
        }

        private static void LearnAndConfigureGlaivePreview(
            PersistentPlayerState state,
            PlayerComboLoadoutState loadout)
        {
            string[] actionIds =
            {
                "glaive-and-shield.opening.restraining-slash",
                "glaive-and-shield.main.diagonal-slash",
                "glaive-and-shield.closing.turning-back-thrust",
                "glaive-and-shield.linked.counter-thrust",
                "glaive-and-shield.opening.guarded-thrust",
                "glaive-and-shield.main.flying-round-slash",
                "glaive-and-shield.closing.flying-round-slash",
                "glaive-and-shield.linked.bash",
                "glaive-and-shield.opening.leaping-slash",
                "glaive-and-shield.main.rising-slash-chain",
                "glaive-and-shield.closing.rising-slash-chain",
                "glaive-and-shield.linked.double-slash",
                "glaive-and-shield.opening.bash",
                "glaive-and-shield.main.guarded-chain-slash",
                "glaive-and-shield.closing.empowered-double-slash",
                "glaive-and-shield.linked.restraining-slash"
            };
            LearnActions(state, actionIds);

            loadout.SetCombo(0, CreatePreviewCombo(
                "Counter Resolution Window",
                actionIds[0], actionIds[1], actionIds[2], actionIds[3]));
            loadout.SetCombo(1, CreatePreviewCombo(
                "Guarded Advance",
                actionIds[4], actionIds[5], actionIds[6], actionIds[7]));
            loadout.SetCombo(2, CreatePreviewCombo(
                "Rising Chain",
                actionIds[8], actionIds[9], actionIds[10], actionIds[11]));
            loadout.SetCombo(3, CreatePreviewCombo(
                "Guarded Chain",
                actionIds[12], actionIds[13], actionIds[14], actionIds[15]));
        }

        private static void LearnAndConfigureCommonPreview(
            PersistentPlayerState state,
            PlayerComboLoadoutState loadout)
        {
            string[] actionIds =
            {
                "common.opening.roll",
                "common.main.focused-guard-1",
                "common.closing.breather-2",
                "common.linked.focused-guard",
                "common.opening.defensive-stance",
                "common.main.focused-guard-2",
                "common.closing.roll",
                "common.linked.jump",
                "common.opening.use-healing-potion",
                "common.main.use-healing-potion",
                "common.closing.use-healing-potion",
                "common.linked.breather-1",
                "common.opening.use-vitality-tonic",
                "common.main.use-vitality-tonic",
                "common.closing.use-vitality-tonic",
                "common.linked.use-vitality-tonic"
            };
            LearnActions(state, actionIds);

            loadout.SetCombo(0, CreatePreviewCombo(
                "Double Guard Window",
                actionIds[0], actionIds[1], actionIds[2], actionIds[3]));
            loadout.SetCombo(1, CreatePreviewCombo(
                "Shifted Guard Window",
                actionIds[4], actionIds[5], actionIds[6], actionIds[7]));
            loadout.SetCombo(2, CreatePreviewCombo(
                "Healing Sequence",
                actionIds[8], actionIds[9], actionIds[10], actionIds[11]));
            loadout.SetCombo(3, CreatePreviewCombo(
                "Vitality Sequence",
                actionIds[12], actionIds[13], actionIds[14], actionIds[15]));
        }

        private static PreparedComboState CreatePreviewCombo(
            string name,
            string openingActionId,
            string mainActionId,
            string closingActionId,
            string linkedActionId)
        {
            return new PreparedComboState(
                name,
                openingActionId,
                mainActionId,
                closingActionId,
                linkedActionId,
                PreparedLinkedActionPlacement.AfterMain);
        }

        private static void LearnActions(PersistentPlayerState state, string[] actionIds)
        {
            for (int index = 0; index < actionIds.Length; index++)
            {
                state.ActionLibrary.Learn(actionIds[index]);
            }
        }
    }
}
