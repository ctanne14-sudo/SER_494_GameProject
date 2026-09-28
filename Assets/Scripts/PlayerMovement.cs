using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private CharacterController controller;
    public float speed = 3f;
    private float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    private Vector3 homePosition;

    private float verticalVelocity;
    private bool isGrounded;
    private bool isRunning;
    private bool inAir;

    void Start() {
        controller = GetComponent<CharacterController>();
        homePosition = transform.position;
    }

    void Update() {

        // Handle Running and Walking
        if (Input.GetKeyDown(KeyCode.LeftShift)) {
            speed = 7f;
            isRunning = true;
        }
        if (Input.GetKey(KeyCode.S)) {
            speed = 2f;
            isRunning = false;
        }

        isGrounded = controller.isGrounded;

        // Handle Movement
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        Vector3 move = transform.right * x + transform.forward * z;

        if (move.magnitude > 1f) {
            move.Normalize();
        }
        
        bool isMoving = move != Vector3.zero && speed != 0f;

        if (isGrounded) {
            verticalVelocity = -2f;
        }

        // Handle Jumping
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            inAir = true;
        }

        if (inAir && isGrounded && verticalVelocity < -0.1f) {
            inAir = false;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMove = move * speed + new Vector3(0, verticalVelocity, 0);

        controller.Move(finalMove * Time.deltaTime);

        // Reset player position if they fall below a certain point
        if (transform.position.y < -20f) {
            transform.position = homePosition;
        }

    }

}
