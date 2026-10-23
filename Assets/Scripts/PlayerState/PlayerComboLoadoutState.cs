using System;
using System.Collections.Generic;

namespace SER494.Gameplay.PlayerState
{
    public enum PreparedLinkedActionPlacement
    {
        AfterOpening,
        AfterMain,
        AfterClosing
    }

    public sealed class PreparedComboState
    {
        public PreparedComboState(
            string name,
            string openingActionId,
            string mainActionId,
            string closingActionId,
            string linkedActionId,
            PreparedLinkedActionPlacement? linkedPlacement)
        {
            Name = RequireName(name);
            OpeningActionId = PlayerStateId.Optional(openingActionId);
            MainActionId = PlayerStateId.Optional(mainActionId);
            ClosingActionId = PlayerStateId.Optional(closingActionId);
            LinkedActionId = PlayerStateId.Optional(linkedActionId);
            LinkedPlacement = ValidateLinkedPlacement(
                LinkedActionId,
                linkedPlacement,
                OpeningActionId,
                MainActionId,
                ClosingActionId);
        }

        public string Name { get; }

        public string OpeningActionId { get; }

        public string MainActionId { get; }

        public string ClosingActionId { get; }

        public string LinkedActionId { get; }

        public PreparedLinkedActionPlacement? LinkedPlacement { get; }

        public static PreparedComboState CreateEmpty(int oneBasedIndex)
        {
            if (oneBasedIndex <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(oneBasedIndex));
            }

            return new PreparedComboState(
                $"combo{oneBasedIndex}",
                openingActionId: null,
                mainActionId: null,
                closingActionId: null,
                linkedActionId: null,
                linkedPlacement: null);
        }

        public IReadOnlyList<string> GetAssignedActionIds()
        {
            var result = new List<string>(4);
            AddIfPresent(result, OpeningActionId);
            AddIfPresent(result, MainActionId);
            AddIfPresent(result, ClosingActionId);
            AddIfPresent(result, LinkedActionId);
            return result;
        }

        private static PreparedLinkedActionPlacement? ValidateLinkedPlacement(
            string linkedActionId,
            PreparedLinkedActionPlacement? linkedPlacement,
            string openingActionId,
            string mainActionId,
            string closingActionId)
        {
            if (linkedActionId == null)
            {
                if (linkedPlacement.HasValue)
                {
                    throw new ArgumentException(
                        "A Linked placement cannot exist without a Linked Action.",
                        nameof(linkedPlacement));
                }

                return null;
            }

            if (!linkedPlacement.HasValue)
            {
                throw new ArgumentException(
                    "A Linked Action requires a placement.",
                    nameof(linkedPlacement));
            }

            bool hasParent = linkedPlacement.Value == PreparedLinkedActionPlacement.AfterOpening
                ? openingActionId != null
                : linkedPlacement.Value == PreparedLinkedActionPlacement.AfterMain
                    ? mainActionId != null
                    : closingActionId != null;
            if (!hasParent)
            {
                throw new ArgumentException(
                    "A Linked Action must be placed after an assigned Action.",
                    nameof(linkedPlacement));
            }

            return linkedPlacement;
        }

        private static string RequireName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A Combo name is required.", nameof(name));
            }

            return name.Trim();
        }

        private static void AddIfPresent(ICollection<string> result, string actionId)
        {
            if (actionId != null)
            {
                result.Add(actionId);
            }
        }
    }

    public sealed class PlayerComboLoadoutState
    {
        public const int ComboCount = 4;

        private readonly PreparedComboState[] _combos;
        private readonly IReadOnlyList<PreparedComboState> _readOnlyCombos;

        public PlayerComboLoadoutState(string loadoutId, string name)
        {
            LoadoutId = PlayerStateId.Require(loadoutId, nameof(loadoutId));
            Name = RequireName(name);
            _combos = new PreparedComboState[ComboCount];
            for (int index = 0; index < _combos.Length; index++)
            {
                _combos[index] = PreparedComboState.CreateEmpty(index + 1);
            }

            _readOnlyCombos = Array.AsReadOnly(_combos);
        }

        public string LoadoutId { get; }

        public string Name { get; private set; }

        public IReadOnlyList<PreparedComboState> Combos => _readOnlyCombos;

        public void Rename(string name)
        {
            Name = RequireName(name);
        }

        public void SetCombo(int zeroBasedIndex, PreparedComboState combo)
        {
            if (zeroBasedIndex < 0 || zeroBasedIndex >= ComboCount)
            {
                throw new ArgumentOutOfRangeException(nameof(zeroBasedIndex));
            }

            _combos[zeroBasedIndex] = combo ?? throw new ArgumentNullException(nameof(combo));
        }

        private static string RequireName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A Loadout name is required.", nameof(name));
            }

            return name.Trim();
        }
    }

    public sealed class PlayerComboLoadoutCollection
    {
        private readonly List<PlayerComboLoadoutState> _loadouts =
            new List<PlayerComboLoadoutState>();
        private readonly IReadOnlyList<PlayerComboLoadoutState> _readOnlyLoadouts;

        public PlayerComboLoadoutCollection(int initiallyUnlockedSlots)
        {
            if (initiallyUnlockedSlots <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initiallyUnlockedSlots),
                    initiallyUnlockedSlots,
                    "At least one Loadout slot must be unlocked.");
            }

            _readOnlyLoadouts = _loadouts.AsReadOnly();
            for (int index = 0; index < initiallyUnlockedSlots; index++)
            {
                UnlockNext();
            }

            ActiveLoadoutId = _loadouts[0].LoadoutId;
        }

        public IReadOnlyList<PlayerComboLoadoutState> Loadouts => _readOnlyLoadouts;

        public int UnlockedSlotCount => _loadouts.Count;

        public string ActiveLoadoutId { get; private set; }

        public PlayerComboLoadoutState ActiveLoadout => Find(ActiveLoadoutId);

        public PlayerComboLoadoutState UnlockNext()
        {
            int oneBasedIndex = _loadouts.Count + 1;
            var loadout = new PlayerComboLoadoutState(
                $"loadout.{oneBasedIndex}",
                $"Loadout {oneBasedIndex}");
            _loadouts.Add(loadout);
            return loadout;
        }

        public void SetActive(string loadoutId)
        {
            PlayerComboLoadoutState loadout = Find(loadoutId);
            if (loadout == null)
            {
                throw new InvalidOperationException(
                    $"Cannot activate locked or unknown Loadout '{loadoutId}'.");
            }

            ActiveLoadoutId = loadout.LoadoutId;
        }

        public PlayerComboLoadoutState Find(string loadoutId)
        {
            string normalizedId = PlayerStateId.Require(loadoutId, nameof(loadoutId));
            for (int index = 0; index < _loadouts.Count; index++)
            {
                if (string.Equals(
                    _loadouts[index].LoadoutId,
                    normalizedId,
                    StringComparison.Ordinal))
                {
                    return _loadouts[index];
                }
            }

            return null;
        }
    }
}
