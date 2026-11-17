using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SER494.Gameplay.PlayerState
{
    public sealed class PlayerCurrencyState
    {
        public const string GoldCurrencyId = "currency.gold";

        private readonly Dictionary<string, int> _amounts =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly IReadOnlyDictionary<string, int> _readOnlyAmounts;

        public PlayerCurrencyState()
        {
            _readOnlyAmounts = new ReadOnlyDictionary<string, int>(_amounts);
        }

        public IReadOnlyDictionary<string, int> Amounts => _readOnlyAmounts;

        public int Gold => GetAmount(GoldCurrencyId);

        public int GetAmount(string currencyId)
        {
            string normalizedId = PlayerStateId.Require(currencyId, nameof(currencyId));
            return _amounts.TryGetValue(normalizedId, out int amount) ? amount : 0;
        }

        public void Add(string currencyId, int amount)
        {
            string normalizedId = PlayerStateId.Require(currencyId, nameof(currencyId));
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be positive.");
            }

            _amounts.TryGetValue(normalizedId, out int current);
            _amounts[normalizedId] = checked(current + amount);
        }

        public bool TrySpend(string currencyId, int amount)
        {
            string normalizedId = PlayerStateId.Require(currencyId, nameof(currencyId));
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be positive.");
            }

            int current = GetAmount(normalizedId);
            if (current < amount)
            {
                return false;
            }

            int remaining = current - amount;
            if (remaining == 0)
            {
                _amounts.Remove(normalizedId);
            }
            else
            {
                _amounts[normalizedId] = remaining;
            }

            return true;
        }
    }
}
