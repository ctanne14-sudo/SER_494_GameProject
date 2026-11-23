using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public static class MerchantStock
    {
        private static readonly HashSet<string> purchasedThisRun = new HashSet<string>();

        public static void MarkPurchased(MerchantOffer offer)
        {
            if (offer != null && offer.oncePerRun)
            {
                purchasedThisRun.Add(offer.Key);
            }
        }

        public static void ResetRun()
        {
            purchasedThisRun.Clear();
        }

        public static int PriceFor(MerchantOffer offer, int floor, float increasePerFloor)
        {
            return Mathf.RoundToInt(offer.basePrice * (1f + increasePerFloor * Mathf.Max(0, floor)));
        }

        public static List<MerchantOffer> Roll(IList<MerchantOffer> pool, int count, int floor, System.Random random)
        {
            var candidates = new List<MerchantOffer>();
            if (pool != null)
            {
                foreach (MerchantOffer offer in pool)
                {
                    if (IsAvailable(offer, floor) && !candidates.Contains(offer))
                    {
                        candidates.Add(offer);
                    }
                }
            }

            var picked = new List<MerchantOffer>();
            while (picked.Count < count && candidates.Count > 0)
            {
                float total = 0f;
                foreach (MerchantOffer offer in candidates)
                {
                    total += offer.weight;
                }

                float roll = (float)(random.NextDouble() * total);
                int index = candidates.Count - 1;
                for (int i = 0; i < candidates.Count; i++)
                {
                    roll -= candidates[i].weight;
                    if (roll < 0f)
                    {
                        index = i;
                        break;
                    }
                }

                picked.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
            return picked;
        }

        private static bool IsAvailable(MerchantOffer offer, int floor)
        {
            if (offer == null || offer.weight <= 0f || floor < offer.minFloor)
            {
                return false;
            }
            return !(offer.oncePerRun && purchasedThisRun.Contains(offer.Key));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            purchasedThisRun.Clear();
        }
    }
}
