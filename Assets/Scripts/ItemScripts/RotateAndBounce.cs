using UnityEngine;
using System.Collections;

public class RotateAndBounce : MonoBehaviour
{
    public float rotSpeed;
    public float speed;

    private float baseY;

    void Start()
    {
        baseY = transform.localPosition.y;
    }

    public void Update()
    {
        transform.Rotate(Vector3.up * rotSpeed * Time.deltaTime, Space.World);

        Vector3 pos = transform.localPosition;
        pos.y += speed * Time.deltaTime;

        if (pos.y >= baseY + .25f && speed > 0f) speed *= -1f;
        else if (pos.y <= baseY && speed < 0f) speed *= -1f;
        
        transform.localPosition = pos;
    }

}