using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public class MerchantShop : MonoBehaviour
    {
        public MerchantOfferPool pool;
        public MerchantPedestal[] pedestals;

        [Min(0f)] public float priceIncreasePerFloor = 0.25f;
        [Min(0)] public int rerollCost = 25;
        [Min(0)] public int rerollsPerVisit = 1;
        public int seed = 0;

        private System.Random random;
        private int rerollsUsed;

        public int RerollsLeft => Mathf.Max(0, rerollsPerVisit - rerollsUsed);

        private void Start()
        {
            if (pool == null)
            {
                pool = Resources.Load<MerchantOfferPool>("MerchantOfferPool");
            }
            random = seed != 0 ? new System.Random(seed) : new System.Random();
            Restock();
        }

        [ContextMenu("Restock")]
        public void Restock()
        {
            if (!Application.isPlaying || pedestals == null)
            {
                return;
            }
            if (random == null)
            {
                random = new System.Random();
            }

            int floor = RunFlow.CurrentFloor;
            List<MerchantOffer> offers = MerchantStock.Roll(pool != null ? pool.offers : null, pedestals.Length, floor, random);

            for (int i = 0; i < pedestals.Length; i++)
            {
                if (pedestals[i] == null)
                {
                    continue;
                }
                MerchantOffer offer = i < offers.Count ? offers[i] : null;
                int price = offer != null ? MerchantStock.PriceFor(offer, floor, priceIncreasePerFloor) : 0;
                pedestals[i].SetOffer(offer, price);
            }
        }

        public bool TryReroll(IMerchantCustomer customer)
        {
            if (customer == null || RerollsLeft <= 0 || !customer.TrySpend(rerollCost))
            {
                return false;
            }
            rerollsUsed++;
            Restock();
            return true;
        }
    }
}
