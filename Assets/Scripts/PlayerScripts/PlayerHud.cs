using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerHud : MonoBehaviour {

    [Header("Health")]
    [SerializeField] private HealthController hc;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Slider hpSlider;
    private float displayHP;

    [Header("Stamina")]
    [SerializeField] private PlayerSpeed ps;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private Slider staminaSlider;
    private float displayStamina;

    [Header("Mana")]
    //[SerializeField] private HealthController hc;
    [SerializeField] private TextMeshProUGUI manaText;
    [SerializeField] private Slider manaSlider;
    private float displayMana;


    void Start()
    {
        displayHP = hc.playerHP;
        hpText.text = displayHP + " / " + hc.maxHP;
        hpSlider.value = displayHP;

        displayStamina = ps.stamina;
        staminaText.text = displayStamina + " / " + ps.maxStamina;
        staminaSlider.value = displayStamina;
    }


    void Update()
    {
        int currentHP = hc.playerHP;
        hpText.text = currentHP + " / " + hc.maxHP;
        if (displayHP != currentHP)
        {
            displayHP = Mathf.MoveTowards(displayHP, currentHP, 25f * Time.deltaTime);
            hpSlider.value = displayHP;
        }

        float currentStamina = ps.stamina;
        staminaText.text = currentStamina.ToString("0") + " / " + ps.maxStamina;
        if (displayStamina != currentStamina)
        {
            displayStamina = Mathf.MoveTowards(displayStamina, currentStamina, 25f * Time.deltaTime);
            staminaSlider.value = displayStamina;
        }




    }
}
