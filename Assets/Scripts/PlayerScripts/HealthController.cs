using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HealthController : MonoBehaviour {
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CameraMovement cameraMovement;
    [SerializeField] private GameObject deathPanel;
    public int maxHP = 200;
    public int playerHP;


    void Start() {
        playerHP = 100;
        deathPanel.SetActive(false);
    }

    public void damagePlayer(int damage) {
        playerHP = Mathf.Max(playerHP - damage, 0);
        if (playerHP <= 0) die();
    }
    public void healPlayer(int heal) {
        playerHP = Mathf.Min(playerHP + heal, maxHP);
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
