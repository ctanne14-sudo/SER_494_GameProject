using UnityEngine;

namespace Roguelike
{
    public class MerchantPedestal : MonoBehaviour
    {
        public Transform displayPoint;
        public GameObject placeholder;

        public MerchantOffer Offer { get; private set; }
        public int Price { get; private set; }
        public bool HasOffer => Offer != null;

        public event System.Action<MerchantPedestal> Changed;

        private GameObject display;

        public void SetOffer(MerchantOffer offer, int price)
        {
            ClearDisplay();
            Offer = offer;
            Price = price;

            if (offer != null && offer.displayPrefab != null)
            {
                Transform point = displayPoint != null ? displayPoint : transform;
                display = Instantiate(offer.displayPrefab, point.position, point.rotation, point);
                Freeze(display);
            }

            if (placeholder != null)
            {
                placeholder.SetActive(offer != null && display == null);
            }
            Changed?.Invoke(this);
        }

        public bool TryBuy(IMerchantCustomer customer)
        {
            if (Offer == null || customer == null || !customer.TrySpend(Price))
            {
                return false;
            }

            MerchantOffer bought = Offer;
            GameObject boughtDisplay = display;
            display = null;
            Offer = null;
            Price = 0;

            if (boughtDisplay != null)
            {
                boughtDisplay.transform.SetParent(null, true);
            }

            MerchantStock.MarkPurchased(bought);
            customer.Receive(bought, boughtDisplay);

            if (boughtDisplay != null)
            {
                Destroy(boughtDisplay);
            }
            if (placeholder != null)
            {
                placeholder.SetActive(false);
            }
            Changed?.Invoke(this);
            return true;
        }

        [ContextMenu("Test Buy")]
        private void TestBuy()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            var customer = FindAnyObjectByType<MerchantTestCustomer>();
            if (customer == null)
            {
                Debug.LogWarning("No MerchantTestCustomer in the scene.");
                return;
            }
            string offerName = HasOffer ? Offer.DisplayName : "nothing";
            Debug.Log(TryBuy(customer) ? "Bought " + offerName : "Could not buy " + offerName);
        }

        private void ClearDisplay()
        {
            if (display != null)
            {
                Destroy(display);
            }
            display = null;
        }

        private static void Freeze(GameObject target)
        {
            foreach (Collider collider in target.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
            foreach (Rigidbody body in target.GetComponentsInChildren<Rigidbody>())
            {
                body.isKinematic = true;
            }
        }
    }
}
