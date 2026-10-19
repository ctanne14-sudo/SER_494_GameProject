using UnityEngine;
using TMPro;

public class FloorUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI floorText;
    [SerializeField]
    private TextMeshProUGUI messageText;
    private float messageTimer;

    void Start()
    {
        GameManager.Instance.OnChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnChanged -= Refresh;
    }

    void Update()
    {
        if (messageTimer > 0)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0) messageText.text = "";
        }
    }

    void Refresh()
    {
        GameManager game = GameManager.Instance;
        floorText.text = "Floor " + game.currentFloor + " / " + game.maxFloors + "  -  " + game.CurrentEnemyName();

        if (game.message != "")
        {
            messageText.text = game.message;
            messageTimer = 3f;
            game.message = "";
        }
    }
}
