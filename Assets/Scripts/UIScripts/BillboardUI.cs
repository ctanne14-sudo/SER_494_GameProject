using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Transform _mainCameraTransform;

    private void Start()
    {
        // Cache the main camera's transform for performance
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (_mainCameraTransform == null) return;

        // Force the UI to face the camera smoothly
        transform.LookAt(transform.position + _mainCameraTransform.forward);
    }
}
