using UnityEngine;
using SER494.Gameplay.ExplorationMenu.Unity;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerSpeed playerSpeed;
    private Vector3 checkPoint;
    private float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    private float verticalVelocity;
    private bool isGrounded;
    private bool inAir;

    void Start() {
        animator = GetComponent<Animator>();
        checkPoint = transform.position;
    }

    void Update() {
        if (ExplorationMenuInputGate.IsOpen)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
            return;
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
        
        bool isMoving = move != Vector3.zero && playerSpeed.currentSpeed != 0f;

        bool isRunning = playerSpeed.isRunning();

        if (isRunning) animator.SetBool("isRunning", isMoving);
        else animator.SetBool("isRunning", isRunning);
        

        if (isMoving)  animator.SetBool("isWalking", !isRunning);
        else animator.SetBool("isWalking", isMoving);
        
        

        if (isGrounded) {
            verticalVelocity = -2f;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && playerSpeed.stamina > 10)
        {
            playerSpeed.stamina -= 10;
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("isJump");
            inAir = true;
        }

        if (inAir && isGrounded && verticalVelocity < -0.1f) {
            animator.SetTrigger("isLand");
            inAir = false;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMove = move * playerSpeed.currentSpeed + new Vector3(0, verticalVelocity, 0);

        controller.Move(finalMove * Time.deltaTime);

        if (transform.position.y < -10f) {
            transform.position = checkPoint;
        }
    }

    public void updateCheckPoint(Vector3 point)
    {
        checkPoint = point;
    }

    public void stop()
    {
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
        this.enabled = false;
    }
}
