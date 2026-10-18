using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HealthController : MonoBehaviour {
    [SerializeField]
    private PlayerMovement playerMovement;
    [SerializeField]
    private CameraMovement cameraMovement;
    [SerializeField]
    private GameObject deathPanel;
    [SerializeField]
    private TextMeshProUGUI hpText;
    [SerializeField]
    private Slider hpSlider;
    public int maxHP = 200;
    public int playerHP;
    private float displayHP;


    void Start() {
        playerHP = 100;
        displayHP = 100f;
        hpText.text = playerHP + " / " + maxHP;
        hpSlider.maxValue = maxHP;
        deathPanel.SetActive(false);
        hpSlider.value = playerHP;
    }

    void Update()
    {
        if (displayHP != playerHP)
        {
            displayHP = Mathf.MoveTowards(displayHP, playerHP, 25f * Time.deltaTime);
            hpSlider.value = displayHP;
        }
    }

    public void damagePlayer(int damage) {
        playerHP = Mathf.Max(playerHP - damage, 0);
        hpText.text = playerHP + " / " + maxHP;
        if (playerHP <= 0) die();
    }
    public void healPlayer(int heal) {
        float displayHP = playerHP;
        playerHP = Mathf.Min(playerHP + heal, maxHP);
        hpText.text = playerHP + " / " + maxHP;
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
