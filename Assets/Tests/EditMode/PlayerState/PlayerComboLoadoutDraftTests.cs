using NUnit.Framework;

namespace SER494.Gameplay.PlayerState.Tests
{
    public sealed class PlayerComboLoadoutDraftTests
    {
        [Test]
        public void EditingDraft_DoesNotMutateSourceUntilApply()
        {
            PlayerComboLoadoutState source = CreateLoadout();
            var draft = new PlayerComboLoadoutDraft(source);

            draft.Combos[0].SetAction(
                PreparedComboActionSlot.Main,
                "action.main.replacement");

            Assert.That(source.Combos[0].MainActionId, Is.EqualTo("action.main.original"));
            Assert.That(draft.HasChanges, Is.True);

            draft.Apply();

            Assert.That(source.Combos[0].MainActionId, Is.EqualTo("action.main.replacement"));
            Assert.That(draft.HasChanges, Is.False);
        }

        [Test]
        public void ClearingLinkedParent_MovesPlacementToNextAvailableParent()
        {
            var draft = new PlayerComboLoadoutDraft(CreateLoadout());

            draft.Combos[0].SetAction(PreparedComboActionSlot.Opening, null);

            Assert.That(
                draft.Combos[0].LinkedPlacement,
                Is.EqualTo(PreparedLinkedActionPlacement.AfterMain));
        }

        [Test]
        public void LinkedPlacement_RejectsEmptyParent()
        {
            var draft = new PlayerComboLoadoutDraft(CreateLoadout());

            bool changed = draft.Combos[0].TrySetLinkedPlacement(
                PreparedLinkedActionPlacement.AfterClosing,
                out string validationError);

            Assert.That(changed, Is.False);
            Assert.That(validationError, Does.Contain("Closing"));
            Assert.That(
                draft.Combos[0].LinkedPlacement,
                Is.EqualTo(PreparedLinkedActionPlacement.AfterOpening));
        }

        [Test]
        public void RenamingDraft_IsTrackedAndAppliedWithComboName()
        {
            PlayerComboLoadoutState source = CreateLoadout();
            var draft = new PlayerComboLoadoutDraft(source);

            draft.Rename("Renamed Loadout");
            draft.Combos[0].Rename("Renamed Combo");

            Assert.That(draft.HasChanges, Is.True);
            Assert.That(source.Name, Is.EqualTo("Test Loadout"));
            Assert.That(source.Combos[0].Name, Is.EqualTo("Test Combo"));

            draft.Apply();

            Assert.That(source.Name, Is.EqualTo("Renamed Loadout"));
            Assert.That(source.Combos[0].Name, Is.EqualTo("Renamed Combo"));
            Assert.That(draft.HasChanges, Is.False);
        }

        [Test]
        public void RenameCombo_PreservesAssignedActions()
        {
            PlayerComboLoadoutState source = CreateLoadout();

            source.RenameCombo(0, "Immediate Rename");

            Assert.That(source.Combos[0].Name, Is.EqualTo("Immediate Rename"));
            Assert.That(source.Combos[0].MainActionId, Is.EqualTo("action.main.original"));
            Assert.That(
                source.Combos[0].LinkedPlacement,
                Is.EqualTo(PreparedLinkedActionPlacement.AfterOpening));
        }

        private static PlayerComboLoadoutState CreateLoadout()
        {
            var loadout = new PlayerComboLoadoutState("loadout.test", "Test Loadout");
            loadout.SetCombo(0, new PreparedComboState(
                "Test Combo",
                "action.opening.original",
                "action.main.original",
                closingActionId: null,
                "action.linked.original",
                PreparedLinkedActionPlacement.AfterOpening));
            return loadout;
        }
    }
}
