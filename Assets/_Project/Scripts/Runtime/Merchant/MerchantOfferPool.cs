using UnityEngine;

namespace Roguelike
{
    [CreateAssetMenu(fileName = "MerchantOfferPool", menuName = "Roguelike/Merchant Offer Pool")]
    public class MerchantOfferPool : ScriptableObject
    {
        public MerchantOffer[] offers;
    }
}
