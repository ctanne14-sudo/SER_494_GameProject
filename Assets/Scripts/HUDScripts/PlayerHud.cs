using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SER494.Gameplay.PlayerState.Unity;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

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

    [Header("Gold")]
    [SerializeField] private TextMeshProUGUI goldText;
    private float displayGold;


    void Start()
    {
        displayHP = hc.playerHP;
        hpText.text = displayHP + " / " + hc.maxHP;
        hpSlider.maxValue = hc.maxHP;
        hpSlider.value = displayHP;

        displayStamina = ps.stamina;
        staminaText.text = displayStamina + " / " + ps.maxStamina;
        staminaSlider.maxValue = ps.maxStamina;
        staminaSlider.value = displayStamina;

        if (PlayerStateService.TryGetActive(out PlayerStateService service)
                && service.State != null)
            {
                PersistentPlayerState state = service.State;
                displayGold = state.Currencies.Gold;
                goldText.text = displayGold.ToString("0");
            }
    }


    void Update()
    {
        int currentHP = hc.playerHP;
        hpText.text = currentHP + " / " + hc.maxHP;
        hpSlider.maxValue = hc.maxHP;
        if (displayHP != currentHP)
        {
            displayHP = Mathf.MoveTowards(displayHP, currentHP, 100f * Time.deltaTime);
            hpSlider.value = displayHP;
        }

        float currentStamina = ps.stamina;
        staminaText.text = currentStamina.ToString("0") + " / " + ps.maxStamina;
        staminaSlider.maxValue = ps.maxStamina;
        if (displayStamina != currentStamina)
        {
            displayStamina = Mathf.MoveTowards(displayStamina, currentStamina, 25f * Time.deltaTime);
            staminaSlider.value = displayStamina;
        }


        if (PlayerStateService.TryGetActive(out PlayerStateService service)
                && service.State != null)
            {
                PersistentPlayerState state = service.State;
                int currentGold = state.Currencies.Gold;
                if (displayGold != currentGold)
                {
                    displayGold = Mathf.MoveTowards(displayGold, currentGold, 100f * Time.deltaTime);
                    goldText.text = displayGold.ToString("0");
                }
            }


    }
}
