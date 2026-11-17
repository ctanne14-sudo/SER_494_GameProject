using System;
using UnityEngine;
using UnityEngine.Events;

namespace SER494.Gameplay.PlayerState.Unity
{
    [DisallowMultipleComponent]
    public sealed class PersistentWorldObject : MonoBehaviour
    {
        [Tooltip("Stable within this scene. Do not change after the object ships.")]
        [SerializeField]
        private string persistentObjectId = string.Empty;

        [Tooltip("The saved state that makes this object completed or unavailable.")]
        [SerializeField]
        private string terminalStateId = WorldObjectStateIds.Removed;

        [Tooltip("Optional target to hide when the terminal state is restored.")]
        [SerializeField]
        private GameObject targetToDeactivate;

        [SerializeField]
        private bool deactivateAtTerminalState = true;

        [SerializeField]
        private UnityEvent terminalStateApplied = new UnityEvent();

        public string PersistentObjectId => persistentObjectId;

        public string TerminalStateId => terminalStateId;

        private void Start()
        {
            ApplyPersistedState();
        }

        public void Configure(
            string stableObjectId,
            string completedStateId,
            GameObject deactivationTarget = null,
            bool deactivateTarget = true)
        {
            persistentObjectId = RequireId(stableObjectId, nameof(stableObjectId));
            terminalStateId = RequireId(completedStateId, nameof(completedStateId));
            targetToDeactivate = deactivationTarget;
            deactivateAtTerminalState = deactivateTarget;
        }

        public void MarkCompleted()
        {
            MarkState(terminalStateId);
        }

        public void MarkState(string stateId)
        {
            if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService))
            {
                Debug.LogError(
                    $"Cannot persist '{name}' because PlayerSaveService is unavailable.",
                    this);
                return;
            }

            saveService.SetWorldObjectState(
                gameObject.scene.name,
                RequireId(persistentObjectId, nameof(persistentObjectId)),
                stateId);
            ApplyPersistedState();
        }

        public bool ApplyPersistedState()
        {
            if (!PlayerSaveService.TryGetActive(out PlayerSaveService saveService)
                || string.IsNullOrWhiteSpace(persistentObjectId)
                || !saveService.TryGetWorldObjectState(
                    gameObject.scene.name,
                    persistentObjectId,
                    out string stateId)
                || !string.Equals(stateId, terminalStateId, StringComparison.Ordinal))
            {
                return false;
            }

            terminalStateApplied?.Invoke();
            if (deactivateAtTerminalState)
            {
                GameObject target = targetToDeactivate != null
                    ? targetToDeactivate
                    : gameObject;
                target.SetActive(false);
            }

            return true;
        }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A stable ID is required.", parameterName);
            }

            return value.Trim();
        }
    }
}
