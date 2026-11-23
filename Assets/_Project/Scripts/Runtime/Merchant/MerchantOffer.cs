using UnityEngine;

namespace Roguelike
{
    [CreateAssetMenu(fileName = "NewMerchantOffer", menuName = "Roguelike/Merchant Offer")]
    public class MerchantOffer : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Min(0)] public int basePrice = 50;
        [Min(0f)] public float weight = 1f;
        [Min(0)] public int minFloor = 0;
        public bool oncePerRun = false;

        public GameObject displayPrefab;

        public string Key => string.IsNullOrEmpty(id) ? name : id;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    }
}
