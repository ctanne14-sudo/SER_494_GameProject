using UnityEngine;

namespace Roguelike
{
    public class MerchantTestCustomer : MonoBehaviour, IMerchantCustomer
    {
        public int gold = 200;

        public int Gold => gold;

        public bool TrySpend(int amount)
        {
            if (gold < amount)
            {
                return false;
            }
            gold -= amount;
            return true;
        }

        public void Receive(MerchantOffer offer, GameObject display)
        {
            Debug.Log("Received " + offer.DisplayName + ", gold left: " + gold);
        }
    }
}
