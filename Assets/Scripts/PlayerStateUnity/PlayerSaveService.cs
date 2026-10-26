using System;
using System.IO;
using UnityEngine;

namespace SER494.Gameplay.PlayerState.Unity
{
    [DisallowMultipleComponent]
    public sealed class PlayerSaveService : MonoBehaviour
    {
        private const string SaveDirectoryName = "Saves";

        private static PlayerSaveService _active;
        private PlayerSaveStorage _storage;

        public static PlayerSaveService Active => _active;

        public bool IsAuthority => ReferenceEquals(_active, this);

        public string SaveDirectory => Storage.RootDirectory;

        private PlayerSaveStorage Storage => _storage ??= new PlayerSaveStorage(
            Path.Combine(Application.persistentDataPath, SaveDirectoryName));

        private void Awake()
        {
            if (_active != null && !ReferenceEquals(_active, this))
            {
                enabled = false;
                if (Application.isPlaying)
                {
                    Destroy(this);
                }

                return;
            }

            _active = this;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(_active, this))
            {
                _active = null;
            }
        }

        public static bool TryGetActive(out PlayerSaveService service)
        {
            service = _active;
            return service != null;
        }

        public void SaveSlot(int slotNumber)
        {
            RequireAuthority();
            Storage.Save(slotNumber, RequirePlayerStateService().State);
        }

        public LoadedPlayerSave LoadSlot(int slotNumber)
        {
            RequireAuthority();

            // Storage fully deserializes and validates before the authoritative
            // runtime state is replaced. A failed load leaves the current state intact.
            LoadedPlayerSave loaded = Storage.Load(slotNumber);
            RequirePlayerStateService().RestoreState(loaded.State);
            return loaded;
        }

        public SaveSlotInfo InspectSlot(int slotNumber)
        {
            RequireAuthority();
            return Storage.Inspect(slotNumber);
        }

        public void DeleteSlot(int slotNumber)
        {
            RequireAuthority();
            Storage.Delete(slotNumber);
        }

        private void RequireAuthority()
        {
            if (!IsAuthority)
            {
                throw new InvalidOperationException(
                    "Only the active PlayerSaveService can access save slots.");
            }
        }

        private static PlayerStateService RequirePlayerStateService()
        {
            if (!PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                throw new InvalidOperationException(
                    "PlayerStateService must exist before save slots can be used.");
            }

            return service;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _active = null;
        }
    }
}
