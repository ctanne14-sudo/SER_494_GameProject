using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SER494.Gameplay.PlayerState
{
    public sealed class PlayerInventoryState
    {
        private readonly Dictionary<string, int> _itemCounts =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly IReadOnlyDictionary<string, int> _readOnlyItemCounts;

        public PlayerInventoryState()
        {
            _readOnlyItemCounts = new ReadOnlyDictionary<string, int>(_itemCounts);
        }

        public IReadOnlyDictionary<string, int> ItemCounts => _readOnlyItemCounts;

        public int GetCount(string itemId)
        {
            string normalizedId = PlayerStateId.Require(itemId, nameof(itemId));
            return _itemCounts.TryGetValue(normalizedId, out int count) ? count : 0;
        }

        public void Add(string itemId, int amount)
        {
            string normalizedId = PlayerStateId.Require(itemId, nameof(itemId));
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be positive.");
            }

            _itemCounts.TryGetValue(normalizedId, out int current);
            _itemCounts[normalizedId] = checked(current + amount);
        }

        public bool TryConsume(string itemId, int amount)
        {
            string normalizedId = PlayerStateId.Require(itemId, nameof(itemId));
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be positive.");
            }

            int current = GetCount(normalizedId);
            if (current < amount)
            {
                return false;
            }

            SetCount(normalizedId, current - amount);
            return true;
        }

        public void SetCount(string itemId, int count)
        {
            string normalizedId = PlayerStateId.Require(itemId, nameof(itemId));
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Count cannot be negative.");
            }

            if (count == 0)
            {
                _itemCounts.Remove(normalizedId);
                return;
            }

            _itemCounts[normalizedId] = count;
        }
    }

    internal static class PlayerStateId
    {
        public static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A stable ID is required.", parameterName);
            }

            return value.Trim();
        }

        public static string Optional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
