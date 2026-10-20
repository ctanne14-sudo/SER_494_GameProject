using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roguelike
{
    public static class RunFlow
    {
        private static RunSequence sequence;

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
            LoadFloor(0);
        }

        public static void LoadNextFloor()
        {
            if (Sequence == null || Sequence.floors == null || Sequence.floors.Length == 0)
            {
                Debug.LogWarning("RunFlow: no RunSequence asset found in a Resources folder.");
                return;
            }

            int index = FindFloorIndex(SceneManager.GetActiveScene().name);
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
            string scene = variants[Random.Range(0, variants.Length)];
            SceneManager.LoadScene(scene);
        }

        private static int FindFloorIndex(string sceneName)
        {
            for (int i = 0; i < Sequence.floors.Length; i++)
            {
                string[] variants = Sequence.floors[i].variantScenes;
                if (variants == null)
                {
                    continue;
                }
                foreach (string variant in variants)
                {
                    if (variant == sceneName)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
    }
}
