using UnityEngine;

namespace SER494.Gameplay.PlayerState.Unity
{
    public static class PlayerStateBootstrap
    {
        private const string ServiceObjectName = "Player State Service";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureServiceExists()
        {
            if (PlayerStateService.TryGetActive(out _))
            {
                return;
            }

            var serviceObject = new GameObject(ServiceObjectName);
            serviceObject.AddComponent<PlayerStateService>();
        }
    }
}
