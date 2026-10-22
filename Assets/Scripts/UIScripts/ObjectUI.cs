using UnityEngine;

public class ObjectUI : MonoBehaviour
{
    private Transform mainCamera;

    private void Start()
    {
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (mainCamera == null) return;

        transform.LookAt(transform.position + mainCamera.forward);
    }
}
