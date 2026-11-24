using UnityEngine;

namespace Roguelike
{
    [RequireComponent(typeof(Collider))]
    public class RoomExit : MonoBehaviour
    {
        public bool locked = false;


        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (locked || !other.CompareTag("Player"))
            {
                return;
            }

            RunFlow.LoadNextFloor();
        }
    }
}
