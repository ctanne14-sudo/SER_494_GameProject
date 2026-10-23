using System;
using System.Collections.Generic;

namespace SER494.Gameplay.PlayerState
{
    public sealed class PlayerActionLibraryState
    {
        private readonly HashSet<string> _ownedActionIds =
            new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyCollection<string> OwnedActionIds => _ownedActionIds;

        public bool Learn(string actionId)
        {
            return _ownedActionIds.Add(PlayerStateId.Require(actionId, nameof(actionId)));
        }

        public bool Owns(string actionId)
        {
            return _ownedActionIds.Contains(PlayerStateId.Require(actionId, nameof(actionId)));
        }
    }
}
