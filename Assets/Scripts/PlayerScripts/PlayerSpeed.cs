using UnityEngine;
using SER494.Gameplay.ExplorationMenu.Unity;

public class PlayerSpeed : MonoBehaviour {
    [SerializeField] private float runSpeed;
    [SerializeField] private float walkSpeed;
    public float speedMultiplyer = 1;
    public int maxStamina = 80;
    public float stamina = 80f;
    private int staminaSpeed = 5;
    bool sprinting;
    bool run;

    public float currentSpeed => (sprinting ? runSpeed : walkSpeed) * speedMultiplyer;

    void Update()
    {
        if (ExplorationMenuInputGate.IsOpen)
        {
            sprinting = false;
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift)) run = !run;
        else if (Input.GetKey(KeyCode.S)) run = false;

        bool moving = Input.GetAxis("Vertical") != 0;
        run = run && stamina > 0;

        sprinting = run && stamina > 0f;

        if (sprinting && moving)
        {
            stamina -= staminaSpeed * Time.deltaTime;
        } else if (stamina < maxStamina)
        {
            stamina += staminaSpeed * Time.deltaTime;
        }
    }

    public bool isRunning() => sprinting;

    public bool isWalking() => !sprinting;

    public void changeMultiplier(float multi) => speedMultiplyer = multi;

}
