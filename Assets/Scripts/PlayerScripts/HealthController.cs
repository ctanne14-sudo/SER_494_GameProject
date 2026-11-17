using System;
using SER494.Gameplay.PlayerState;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HealthController : MonoBehaviour {
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private GameObject deathPanel;

    public int maxHP => State.MaximumHealth;
    public int playerHP => State.CurrentHealth;


    void Start() {
        deathPanel.SetActive(false);
    }

    public void damagePlayer(int damage) {
        State.TakeDamage(damage);
        if (playerHP <= 0) die();
    }
    public void healPlayer(int heal) {
        State.RestoreHealth(heal);
    }

    public void increaseMaxHp(int increase)
    {
        State.IncreaseMaximumHealth(increase);
    }

    public void die()
    {
        playerMovement.stop();
        cameraMovement.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        deathPanel.SetActive(true);
    }

    public void reset()
    {
        State.RestoreHealthToMaximum();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private static PlayerState State
    {
        get
        {
            if (!PlayerStateService.TryGetActive(out PlayerStateService service))
            {
                throw new InvalidOperationException(
                    "PlayerStateService must be active before HealthController is used.");
            }

            return service.State;
        }
    }
}
