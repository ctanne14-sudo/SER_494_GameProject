namespace SER494.Gameplay.ExplorationMenu.Unity
{
    public static class ExplorationMenuInputGate
    {
        public static bool IsOpen { get; internal set; }

        [UnityEngine.RuntimeInitializeOnLoadMethod(
            UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            IsOpen = false;
        }
    }
}
