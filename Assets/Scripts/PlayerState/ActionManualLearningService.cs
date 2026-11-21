using System;

namespace SER494.Gameplay.PlayerState
{
    public enum ActionLearningFailure
    {
        None = 0,
        InvalidManual = 1,
        ManualNotOwned = 2,
        ActionAlreadyLearned = 3,
        UnknownAction = 4
    }

    public static class ActionManualLearningService
    {
        public const string ManualItemPrefix = "manual.action.";

        public static string CreateManualItemId(string actionId)
        {
            return ManualItemPrefix + PlayerStateId.Require(actionId, nameof(actionId));
        }

        public static bool TryGetActionId(string manualItemId, out string actionId)
        {
            actionId = null;
            if (string.IsNullOrWhiteSpace(manualItemId))
            {
                return false;
            }

            string normalizedId = manualItemId.Trim();
            if (!normalizedId.StartsWith(ManualItemPrefix, StringComparison.Ordinal)
                || normalizedId.Length == ManualItemPrefix.Length)
            {
                return false;
            }

            actionId = normalizedId.Substring(ManualItemPrefix.Length).Trim();
            return actionId.Length > 0;
        }

        public static bool TryLearn(
            PlayerState playerState,
            string manualItemId,
            Func<string, bool> isKnownAction,
            out string learnedActionId,
            out ActionLearningFailure failure)
        {
            if (playerState == null)
            {
                throw new ArgumentNullException(nameof(playerState));
            }

            if (isKnownAction == null)
            {
                throw new ArgumentNullException(nameof(isKnownAction));
            }

            learnedActionId = null;
            if (!TryGetActionId(manualItemId, out string actionId))
            {
                failure = ActionLearningFailure.InvalidManual;
                return false;
            }

            if (!isKnownAction(actionId))
            {
                failure = ActionLearningFailure.UnknownAction;
                return false;
            }

            if (playerState.ActionLibrary.Owns(actionId))
            {
                failure = ActionLearningFailure.ActionAlreadyLearned;
                return false;
            }

            if (playerState.Inventory.GetCount(manualItemId) < 1)
            {
                failure = ActionLearningFailure.ManualNotOwned;
                return false;
            }

            if (!playerState.Inventory.TryConsume(manualItemId, 1))
            {
                failure = ActionLearningFailure.ManualNotOwned;
                return false;
            }

            if (!playerState.ActionLibrary.Learn(actionId))
            {
                playerState.Inventory.Add(manualItemId, 1);
                failure = ActionLearningFailure.ActionAlreadyLearned;
                return false;
            }

            learnedActionId = actionId;
            failure = ActionLearningFailure.None;
            return true;
        }
    }
}
