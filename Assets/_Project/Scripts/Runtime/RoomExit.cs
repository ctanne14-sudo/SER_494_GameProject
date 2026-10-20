using UnityEngine;

namespace Roguelike
{
    [RequireComponent(typeof(Collider))]
    public class RoomExit : MonoBehaviour
    {
        public bool locked = false;

        private bool used;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (locked || used || !other.CompareTag("Player"))
            {
                return;
            }

            used = true;
            RunFlow.LoadNextFloor();
        }
    }
}
