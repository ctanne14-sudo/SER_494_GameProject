using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private LayerMask interactableLayer;

    private readonly Collider[] _colliders = new Collider[3];
    private int _numFound;
    private IInteractable _lastInteractable;

    private void Update()
    {
        _numFound = Physics.OverlapSphereNonAlloc(interactionPoint.position, interactionRadius, _colliders, interactableLayer);

        if (_numFound > 0)
        {
            var interactable = _colliders[0].GetComponent<IInteractable>();

            if (interactable != null)
            {
                // If this is a brand new object, show its UI
                if (interactable != _lastInteractable)
                {
                    ClearLastInteractable();
                    _lastInteractable = interactable;
                    
                    // If the object is a MonoBehavior, try to invoke its unique ShowUI method
                    if (interactable is MonoBehaviour mb && mb.TryGetComponent(out Chest chest))
                    {
                        chest.ShowUI(true);
                    }
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact(this);
                }
                return;
            }
        }

        // If we walk away from everything, clear the active UI
        ClearLastInteractable();
    }

    private void ClearLastInteractable()
    {
        if (_lastInteractable != null)
        {
            if (_lastInteractable is MonoBehaviour mb && mb.TryGetComponent(out Chest chest))
            {
                chest.ShowUI(false);
            }
            _lastInteractable = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (interactionPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(interactionPoint.position, interactionRadius);
    }
}
