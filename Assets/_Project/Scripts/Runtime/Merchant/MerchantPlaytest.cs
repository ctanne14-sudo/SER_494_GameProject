using UnityEngine;
using UnityEngine.InputSystem;

namespace Roguelike
{
    [RequireComponent(typeof(CharacterController))]
    public class MerchantPlaytest : MonoBehaviour, IMerchantCustomer
    {
        public int gold = 300;
        public float moveSpeed = 6f;
        public float buyRange = 2.5f;
        public Vector3 cameraOffset = new Vector3(-9f, 12f, -9f);

        private CharacterController controller;
        private Camera view;
        private MerchantPedestal nearest;
        private string lastMessage = "";

        public int Gold => gold;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            view = Camera.main;
            if (view == null)
            {
                view = new GameObject("PlaytestCamera").AddComponent<Camera>();
                view.tag = "MainCamera";
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed)
            {
                input.y += 1f;
            }
            if (keyboard.sKey.isPressed)
            {
                input.y -= 1f;
            }
            if (keyboard.dKey.isPressed)
            {
                input.x += 1f;
            }
            if (keyboard.aKey.isPressed)
            {
                input.x -= 1f;
            }
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 forward = view.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 move = (right * input.x + forward * input.y) * moveSpeed;
            move.y = -9.81f;
            controller.Move(move * Time.deltaTime);

            nearest = FindNearestPedestal();

            if (keyboard.eKey.wasPressedThisFrame && nearest != null)
            {
                string offerName = nearest.Offer.DisplayName;
                lastMessage = nearest.TryBuy(this) ? "Bought " + offerName : "Not enough gold for " + offerName;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                MerchantShop shop = FindAnyObjectByType<MerchantShop>();
                lastMessage = shop != null && shop.TryReroll(this) ? "Rerolled the shop" : "Can't reroll";
            }
        }

        private void LateUpdate()
        {
            view.transform.position = transform.position + cameraOffset;
            view.transform.LookAt(transform.position + Vector3.up);
        }

        private MerchantPedestal FindNearestPedestal()
        {
            MerchantPedestal best = null;
            float bestDistance = buyRange;
            foreach (MerchantPedestal pedestal in FindObjectsByType<MerchantPedestal>(FindObjectsSortMode.None))
            {
                if (!pedestal.HasOffer)
                {
                    continue;
                }
                Vector3 offset = pedestal.transform.position - transform.position;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = pedestal;
                }
            }
            return best;
        }

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
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold };
            GUIStyle buyStyle = new GUIStyle(style) { fontSize = 44 };
            buyStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);

            GUI.Box(new Rect(20f, 20f, 760f, 250f), GUIContent.none);
            GUI.Label(new Rect(40f, 30f, 720f, 50f), "Gold: " + gold, style);
            if (nearest != null && nearest.HasOffer)
            {
                GUI.Label(new Rect(40f, 85f, 720f, 60f), "[E] Buy " + nearest.Offer.DisplayName + " for " + nearest.Price, buyStyle);
            }
            GUI.Label(new Rect(40f, 150f, 720f, 50f), "[R] Reroll", style);
            GUI.Label(new Rect(40f, 205f, 720f, 50f), lastMessage, style);
        }
    }
}