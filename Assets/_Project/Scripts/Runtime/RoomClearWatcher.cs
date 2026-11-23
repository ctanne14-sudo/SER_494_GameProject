using UnityEngine;

namespace Roguelike
{
    public class RoomClearWatcher : MonoBehaviour
    {
        public GameObject[] gates;
        public string enemyTag = "Enemy";
        public float startDelay = 1f;

        private bool cleared;

        private void Update()
        {
            if (cleared || Time.timeSinceLevelLoad < startDelay)
            {
                return;
            }

            if (GameObject.FindGameObjectsWithTag(enemyTag).Length > 0)
            {
                return;
            }

            cleared = true;
            foreach (GameObject gate in gates)
            {
                if (gate != null)
                {
                    gate.SetActive(false);
                }
            }
        }
    }
}
