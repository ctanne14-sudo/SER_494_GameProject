using UnityEngine;

namespace Roguelike
{
    public interface IMerchantCustomer
    {
        int Gold { get; }

        bool TrySpend(int amount);

        void Receive(MerchantOffer offer, GameObject display);
    }
}
