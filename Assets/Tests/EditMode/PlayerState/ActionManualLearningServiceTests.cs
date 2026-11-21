using NUnit.Framework;

namespace SER494.Gameplay.PlayerState.Tests
{
    [Category("PlayerState")]
    public sealed class ActionManualLearningServiceTests
    {
        private const string ActionId = "greatsword.opening.quick-draw-slash";

        [Test]
        public void TryLearn_ConsumesOneManualAndUnlocksAction()
        {
            PlayerState state = CreateState();
            string manualId = ActionManualLearningService.CreateManualItemId(ActionId);
            state.Inventory.Add(manualId, 2);

            bool learned = ActionManualLearningService.TryLearn(
                state,
                manualId,
                IsKnownAction,
                out string learnedActionId,
                out ActionLearningFailure failure);

            Assert.That(learned, Is.True);
            Assert.That(failure, Is.EqualTo(ActionLearningFailure.None));
            Assert.That(learnedActionId, Is.EqualTo(ActionId));
            Assert.That(state.ActionLibrary.Owns(ActionId), Is.True);
            Assert.That(state.Inventory.GetCount(manualId), Is.EqualTo(1));
        }

        [Test]
        public void TryLearn_AlreadyLearnedActionDoesNotConsumeManual()
        {
            PlayerState state = CreateState();
            string manualId = ActionManualLearningService.CreateManualItemId(ActionId);
            state.ActionLibrary.Learn(ActionId);
            state.Inventory.Add(manualId, 1);

            bool learned = ActionManualLearningService.TryLearn(
                state,
                manualId,
                IsKnownAction,
                out string learnedActionId,
                out ActionLearningFailure failure);

            Assert.That(learned, Is.False);
            Assert.That(failure, Is.EqualTo(ActionLearningFailure.ActionAlreadyLearned));
            Assert.That(learnedActionId, Is.Null);
            Assert.That(state.Inventory.GetCount(manualId), Is.EqualTo(1));
        }

        [Test]
        public void TryLearn_MissingManualDoesNotUnlockAction()
        {
            PlayerState state = CreateState();
            string manualId = ActionManualLearningService.CreateManualItemId(ActionId);

            bool learned = ActionManualLearningService.TryLearn(
                state,
                manualId,
                IsKnownAction,
                out _,
                out ActionLearningFailure failure);

            Assert.That(learned, Is.False);
            Assert.That(failure, Is.EqualTo(ActionLearningFailure.ManualNotOwned));
            Assert.That(state.ActionLibrary.Owns(ActionId), Is.False);
        }

        [Test]
        public void LearnedActionAndConsumedManualCount_PersistThroughSnapshot()
        {
            PlayerState state = CreateState();
            string manualId = ActionManualLearningService.CreateManualItemId(ActionId);
            state.Inventory.Add(manualId, 2);
            Assert.That(
                ActionManualLearningService.TryLearn(
                    state,
                    manualId,
                    IsKnownAction,
                    out _,
                    out _),
                Is.True);

            PlayerState restored = PlayerStateSnapshotMapper.Restore(
                PlayerStateSnapshotMapper.Capture(state));

            Assert.That(restored.ActionLibrary.Owns(ActionId), Is.True);
            Assert.That(restored.Inventory.GetCount(manualId), Is.EqualTo(1));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("item.healing-potion")]
        [TestCase("manual.action.")]
        [TestCase("manual.action.   ")]
        public void TryLearn_InvalidManualIdDoesNotMutatePlayerState(string manualId)
        {
            PlayerState state = CreateState();

            bool learned = ActionManualLearningService.TryLearn(
                state,
                manualId,
                IsKnownAction,
                out _,
                out ActionLearningFailure failure);

            Assert.That(learned, Is.False);
            Assert.That(failure, Is.EqualTo(ActionLearningFailure.InvalidManual));
            Assert.That(state.ActionLibrary.OwnedActionIds, Is.Empty);
            Assert.That(state.Inventory.ItemCounts, Is.Empty);
        }

        [Test]
        public void TryLearn_UnknownActionDoesNotConsumeManual()
        {
            PlayerState state = CreateState();
            string manualId = ActionManualLearningService.CreateManualItemId("missing.action");
            state.Inventory.Add(manualId, 1);

            bool learned = ActionManualLearningService.TryLearn(
                state,
                manualId,
                IsKnownAction,
                out _,
                out ActionLearningFailure failure);

            Assert.That(learned, Is.False);
            Assert.That(failure, Is.EqualTo(ActionLearningFailure.UnknownAction));
            Assert.That(state.Inventory.GetCount(manualId), Is.EqualTo(1));
            Assert.That(state.ActionLibrary.OwnedActionIds, Is.Empty);
        }

        private static bool IsKnownAction(string actionId)
        {
            return actionId == ActionId;
        }

        private static PlayerState CreateState()
        {
            return new PlayerState(
                maximumHealth: 100,
                maximumMana: 30,
                maximumCombatStamina: 80);
        }
    }
}
