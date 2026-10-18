using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private CharacterController controller;
    private Animator animator;
    [SerializeField]
    public Vector3 homePosition;
    public float speed = 3f;
    private float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    private float verticalVelocity;
    private bool isGrounded;
    private bool isRunning;
    private bool inAir;

    void Start() {
        animator = GetComponent<Animator>();
        homePosition = transform.position;
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.LeftShift)) {
            speed = 7f;
            isRunning = true;
        }
        if (Input.GetKey(KeyCode.S)) {
            speed = 2f;
            isRunning = false;
        }
        
        if (Input.GetKeyDown(KeyCode.K))
        {
            GetComponent<HealthController>().damagePlayer(50);
        }

        isGrounded = controller.isGrounded;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        Vector3 move = transform.right * x + transform.forward * z;

        if (move.magnitude > 1f) {
            move.Normalize();
        }
        
        bool isMoving = move != Vector3.zero && speed != 0f;

        if (isRunning) {
            animator.SetBool("isRunning", isMoving);
        } else {
            animator.SetBool("isRunning", isRunning);
        }

        if (isMoving) {
            animator.SetBool("isWalking", !isRunning);
        } else {
            animator.SetBool("isWalking", isMoving);
        }
        

        if (isGrounded) {
            verticalVelocity = -2f;
        }

        // Handle Jumping
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("isJump");
            inAir = true;
        }

        if (inAir && isGrounded && verticalVelocity < -0.1f) {
            animator.SetTrigger("isLand");
            inAir = false;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMove = move * speed + new Vector3(0, verticalVelocity, 0);

        controller.Move(finalMove * Time.deltaTime);

        if (transform.position.y < -10f) {
            transform.position = homePosition;
        }
    }

    public void stop()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        speed = 0f;
        this.enabled = false;
    }
}
