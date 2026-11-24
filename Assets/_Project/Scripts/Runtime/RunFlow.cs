using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roguelike
{
    public static class RunFlow
    {
        private static RunSequence sequence;
        private static int pendingFloor = -1;

        public static int CurrentFloor { get; private set; }

        private static RunSequence Sequence
        {
            get
            {
                if (sequence == null)
                {
                    sequence = Resources.Load<RunSequence>("RunSequence");
                }
                return sequence;
            }
        }

        public static void StartRun()
        {
            pendingFloor = -1;
            MerchantStock.ResetRun();
            LoadFloor(0);
        }

        public static void LoadNextFloor()
        {
            if (Sequence == null || Sequence.floors == null || Sequence.floors.Length == 0)
            {
                Debug.LogWarning("RunFlow: no RunSequence asset found in a Resources folder.");
                return;
            }

            string activeScene = SceneManager.GetActiveScene().name;

            if (Contains(Sequence.merchantScenes, activeScene))
            {
                int target = pendingFloor >= 0 ? pendingFloor : CurrentFloor + 1;
                pendingFloor = -1;
                if (target >= Sequence.floors.Length)
                {
                    Debug.Log("RunFlow: run complete, the player escaped the tower.");
                    return;
                }
                LoadFloor(target);
                return;
            }

            int index = FindFloorIndex(activeScene);
            if (index >= 0)
            {
                CurrentFloor = index;
            }

            int next = CurrentFloor + 1;
            if (next >= Sequence.floors.Length)
            {
                Debug.Log("RunFlow: run complete, the player escaped the tower.");
                return;
            }

            if (Sequence.merchantBetweenFloors && Sequence.merchantScenes != null && Sequence.merchantScenes.Length > 0)
            {
                pendingFloor = next;
                SceneManager.LoadScene(PickRandom(Sequence.merchantScenes));
                return;
            }

            LoadFloor(next);
        }

        private static void LoadFloor(int index)
        {
            string[] variants = Sequence.floors[index].variantScenes;
            if (variants == null || variants.Length == 0)
            {
                Debug.LogWarning("RunFlow: floor " + index + " has no variant scenes.");
                return;
            }

            CurrentFloor = index;
            SceneManager.LoadScene(PickRandom(variants));
        }

        private static string PickRandom(string[] scenes)
        {
            return scenes[Random.Range(0, scenes.Length)];
        }

        private static bool Contains(string[] scenes, string sceneName)
        {
            if (scenes == null)
            {
                return false;
            }
            foreach (string scene in scenes)
            {
                if (scene == sceneName)
                {
                    return true;
                }
            }
            return false;
        }

        private static int FindFloorIndex(string sceneName)
        {
            for (int i = 0; i < Sequence.floors.Length; i++)
            {
                if (Contains(Sequence.floors[i].variantScenes, sceneName))
                {
                    return i;
                }
            }
            return -1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            sequence = null;
            pendingFloor = -1;
            CurrentFloor = 0;
        }
    }
}
