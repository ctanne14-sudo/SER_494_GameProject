using UnityEngine;

namespace SER494.Gameplay.PlayerState.Unity
{
    public static class PlayerStateBootstrap
    {
        private const string ServiceObjectName = "Player State Service";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureServiceExists()
        {
            if (PlayerStateService.TryGetActive(out PlayerStateService playerStateService))
            {
                if (!PlayerSaveService.TryGetActive(out _))
                {
                    playerStateService.gameObject.AddComponent<PlayerSaveService>();
                }

                return;
            }

            var serviceObject = new GameObject(ServiceObjectName);
            serviceObject.AddComponent<PlayerStateService>();
            serviceObject.AddComponent<PlayerSaveService>();
        }
    }
}
