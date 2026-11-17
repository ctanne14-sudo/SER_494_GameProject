using UnityEngine;

namespace SER494.Gameplay.CombatIntegration.Unity
{
    public static class BattleSessionBootstrap
    {
        private const string ServiceObjectName = "Battle Session Service";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureServiceExists()
        {
            if (BattleSessionService.TryGetActive(out _))
            {
                return;
            }

            var serviceObject = new GameObject(ServiceObjectName);
            serviceObject.AddComponent<BattleSessionService>();
        }
    }
}
