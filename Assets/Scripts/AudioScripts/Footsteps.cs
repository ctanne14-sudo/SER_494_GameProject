using UnityEngine;

public class Footsteps : MonoBehaviour
{
    public float walkInterval = 0.5f;
    public float runInterval = 0.32f;

    private CharacterController controller;
    private float timer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (controller == null || AudioManager.Instance == null) return;

        Vector3 velocity = controller.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        if (!controller.isGrounded || speed < 0.5f || Time.timeScale == 0f)
        {
            timer = 0f;
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            AudioManager.Instance.PlayFootstep();
            timer = speed > 5f ? runInterval : walkInterval;
        }
    }
}
