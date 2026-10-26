using System;
using System.Collections.Generic;

namespace SER494.Gameplay.PlayerState
{
    public enum PreparedComboActionSlot
    {
        Opening,
        Main,
        Closing,
        Linked
    }

    public sealed class PreparedComboDraft
    {
        internal PreparedComboDraft(PreparedComboState source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Name = source.Name;
            OpeningActionId = source.OpeningActionId;
            MainActionId = source.MainActionId;
            ClosingActionId = source.ClosingActionId;
            LinkedActionId = source.LinkedActionId;
            LinkedPlacement = source.LinkedPlacement;
        }

        public string Name { get; }

        public string OpeningActionId { get; private set; }

        public string MainActionId { get; private set; }

        public string ClosingActionId { get; private set; }

        public string LinkedActionId { get; private set; }

        public PreparedLinkedActionPlacement? LinkedPlacement { get; private set; }

        public string GetActionId(PreparedComboActionSlot slot)
        {
            switch (slot)
            {
                case PreparedComboActionSlot.Opening: return OpeningActionId;
                case PreparedComboActionSlot.Main: return MainActionId;
                case PreparedComboActionSlot.Closing: return ClosingActionId;
                case PreparedComboActionSlot.Linked: return LinkedActionId;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public void SetAction(PreparedComboActionSlot slot, string actionId)
        {
            string normalized = PlayerStateId.Optional(actionId);
            switch (slot)
            {
                case PreparedComboActionSlot.Opening:
                    OpeningActionId = normalized;
                    break;
                case PreparedComboActionSlot.Main:
                    MainActionId = normalized;
                    break;
                case PreparedComboActionSlot.Closing:
                    ClosingActionId = normalized;
                    break;
                case PreparedComboActionSlot.Linked:
                    LinkedActionId = normalized;
                    if (normalized == null)
                    {
                        LinkedPlacement = null;
                    }
                    else if (!LinkedPlacement.HasValue
                        || !HasPlacementParent(LinkedPlacement.Value))
                    {
                        LinkedPlacement = FindFirstAvailablePlacement();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot));
            }

            NormalizeLinkedPlacement();
        }

        public bool TrySetLinkedPlacement(
            PreparedLinkedActionPlacement placement,
            out string validationError)
        {
            if (LinkedActionId == null)
            {
                validationError = "A Linked Action must be assigned before choosing its placement.";
                return false;
            }

            if (!HasPlacementParent(placement))
            {
                validationError = $"The Linked Action cannot be placed {FormatPlacement(placement)} because that Action slot is empty.";
                return false;
            }

            LinkedPlacement = placement;
            validationError = null;
            return true;
        }

        public PreparedComboState CreatePreparedState()
        {
            return new PreparedComboState(
                Name,
                OpeningActionId,
                MainActionId,
                ClosingActionId,
                LinkedActionId,
                LinkedPlacement);
        }

        internal bool Matches(PreparedComboState state)
        {
            return state != null
                && string.Equals(Name, state.Name, StringComparison.Ordinal)
                && string.Equals(OpeningActionId, state.OpeningActionId, StringComparison.Ordinal)
                && string.Equals(MainActionId, state.MainActionId, StringComparison.Ordinal)
                && string.Equals(ClosingActionId, state.ClosingActionId, StringComparison.Ordinal)
                && string.Equals(LinkedActionId, state.LinkedActionId, StringComparison.Ordinal)
                && LinkedPlacement == state.LinkedPlacement;
        }

        private void NormalizeLinkedPlacement()
        {
            if (LinkedActionId == null)
            {
                LinkedPlacement = null;
                return;
            }

            if (!LinkedPlacement.HasValue || !HasPlacementParent(LinkedPlacement.Value))
            {
                LinkedPlacement = FindFirstAvailablePlacement();
            }
        }

        private PreparedLinkedActionPlacement? FindFirstAvailablePlacement()
        {
            if (OpeningActionId != null)
            {
                return PreparedLinkedActionPlacement.AfterOpening;
            }

            if (MainActionId != null)
            {
                return PreparedLinkedActionPlacement.AfterMain;
            }

            return ClosingActionId != null
                ? PreparedLinkedActionPlacement.AfterClosing
                : (PreparedLinkedActionPlacement?)null;
        }

        private bool HasPlacementParent(PreparedLinkedActionPlacement placement)
        {
            switch (placement)
            {
                case PreparedLinkedActionPlacement.AfterOpening:
                    return OpeningActionId != null;
                case PreparedLinkedActionPlacement.AfterMain:
                    return MainActionId != null;
                case PreparedLinkedActionPlacement.AfterClosing:
                    return ClosingActionId != null;
                default:
                    return false;
            }
        }

        private static string FormatPlacement(PreparedLinkedActionPlacement placement)
        {
            switch (placement)
            {
                case PreparedLinkedActionPlacement.AfterOpening: return "after Opening";
                case PreparedLinkedActionPlacement.AfterMain: return "after Main";
                case PreparedLinkedActionPlacement.AfterClosing: return "after Closing";
                default: return placement.ToString();
            }
        }
    }

    public sealed class PlayerComboLoadoutDraft
    {
        private readonly PlayerComboLoadoutState _source;
        private readonly PreparedComboDraft[] _combos;
        private readonly IReadOnlyList<PreparedComboDraft> _readOnlyCombos;

        public PlayerComboLoadoutDraft(PlayerComboLoadoutState source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _combos = new PreparedComboDraft[source.Combos.Count];
            for (int index = 0; index < _combos.Length; index++)
            {
                _combos[index] = new PreparedComboDraft(source.Combos[index]);
            }

            _readOnlyCombos = Array.AsReadOnly(_combos);
        }

        public string LoadoutId => _source.LoadoutId;

        public string Name => _source.Name;

        public IReadOnlyList<PreparedComboDraft> Combos => _readOnlyCombos;

        public bool HasChanges
        {
            get
            {
                for (int index = 0; index < _combos.Length; index++)
                {
                    if (!_combos[index].Matches(_source.Combos[index]))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Apply()
        {
            var prepared = new PreparedComboState[_combos.Length];
            for (int index = 0; index < _combos.Length; index++)
            {
                prepared[index] = _combos[index].CreatePreparedState();
            }

            for (int index = 0; index < prepared.Length; index++)
            {
                _source.SetCombo(index, prepared[index]);
            }
        }
    }
}
