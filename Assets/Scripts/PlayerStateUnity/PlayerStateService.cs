using System;
using UnityEngine;

namespace SER494.Gameplay.PlayerState.Unity
{
    [DisallowMultipleComponent]
    public sealed class PlayerStateService : MonoBehaviour
    {
        private static PlayerStateService _active;

        [SerializeField]
        private bool persistAcrossScenes = true;

        [SerializeField]
        private PlayerStateInitialValues initialValues = new PlayerStateInitialValues();

        public static PlayerStateService Active => _active;

        public PlayerState State { get; private set; }

        public bool IsAuthority => ReferenceEquals(_active, this);

        public event Action<PlayerState> StateReplaced;

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
            if (Application.isPlaying && persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (State == null)
            {
                StartNewGame(initialValues);
            }
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(_active, this))
            {
                _active = null;
            }
        }

        public static bool TryGetActive(out PlayerStateService service)
        {
            service = _active;
            return service != null;
        }

        public void StartNewGame()
        {
            StartNewGame(initialValues);
        }

        public void StartNewGame(PlayerStateInitialValues values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            ReplaceState(values.CreateState());
        }

        public void RestoreState(PlayerState restoredState)
        {
            ReplaceState(restoredState ?? throw new ArgumentNullException(nameof(restoredState)));
        }

        private void ReplaceState(PlayerState state)
        {
            if (!IsAuthority)
            {
                throw new InvalidOperationException(
                    "Only the active PlayerStateService can replace player state.");
            }

            State = state;
            StateReplaced?.Invoke(State);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _active = null;
        }
    }
}
