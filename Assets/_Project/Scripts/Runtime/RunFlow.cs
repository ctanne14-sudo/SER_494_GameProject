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
            CurrentFloor = 0;
            MerchantStock.ResetRun();

            if (HasCutscene(0))
            {
                pendingFloor = 0;
                SceneManager.LoadScene(Sequence.cutScenes[0]);
                return;
            }

            pendingFloor = -1;
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
                LoadFloorOrFinish(target);
                return;
            }

            int cutIndex = IndexOf(sequence.cutScenes, activeScene);
            if (cutIndex >= 0)
            {
                int target = pendingFloor >= 0 ? pendingFloor : cutIndex;

                if (target > 0 && target < Sequence.floors.Length && UseMerchant())
                {
                    pendingFloor = target;
                    SceneManager.LoadScene(PickRandom(Sequence.merchantScenes));
                    return;
                }

                pendingFloor = -1;
                LoadFloorOrFinish(target);
                return;
            }

            int index = FindFloorIndex(activeScene);
            if (index >= 0)
            {
                CurrentFloor = index;
            }

            int next = CurrentFloor + 1;

            if (HasCutscene(next))
            {
                pendingFloor = next;
                SceneManager.LoadScene(Sequence.cutScenes[next]);
                return;
            }
            
            if (next < Sequence.floors.Length && UseMerchant())
            {
                pendingFloor = next;
                SceneManager.LoadScene(PickRandom(Sequence.merchantScenes));
                return;
            }

            pendingFloor = -1;
            LoadFloorOrFinish(next);
        }

        private static void LoadFloorOrFinish(int index)
        {
            if (index >= Sequence.floors.Length)
            {
                Debug.Log("RunFlow: run complete, the player escaped the tower");
                return;
            }
            LoadFloor(index);
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

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Sequence == null) return;
            bool isCutscene = Contains(Sequence.cutScenes, scene.name);
            PlayerPersistance.SetPersistentObjectsActive(!isCutscene);
        }

        private static bool UseMerchant()
        {
            return Sequence.merchantBetweenFloors
                && Sequence.merchantScenes != null
                && Sequence.merchantScenes.Length > 0;
        }

        private static bool HasCutscene(int index)
        {
            return Sequence.cutScenes != null  
                && index > 0
                && index < Sequence.cutScenes.Length
                && !string.IsNullOrEmpty(Sequence.cutScenes[index]);
        }

        private static int IndexOf(string[] scenes, string activeScene)
        {
            if (scenes == null) return -1;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i] == activeScene) return i;
            }
            return -1;
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
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }
}
