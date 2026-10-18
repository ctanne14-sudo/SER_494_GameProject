using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform target;
    public float sensitivity = 5f;
    public float distance = 3f;

    private float x = 0f;
    private float y = 0f;

    private bool initialized = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        distance = Vector3.Distance(transform.position, target.position);
    }

    void LateUpdate()
    {
        if (target == null) 
        {
            Debug.Log("Target is null");
            return;
        }

        if (!initialized) {
            initialized = true;
            return;
        }

            x += Input.GetAxis("Mouse X") * sensitivity;
            y -= Input.GetAxis("Mouse Y") * sensitivity;

            y = Mathf.Clamp(y, -20f, 80f);

            Quaternion rotation = Quaternion.Euler(y, x, 0);

            Vector3 positionOffset = rotation * new Vector3(0.0f, 0f, -distance);

            transform.rotation = rotation;
            transform.position = target.position + positionOffset;

            target.root.transform.rotation = Quaternion.Euler(0, x, 0);
        
    }
}