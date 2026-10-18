using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerHud : MonoBehaviour {
    [SerializeField]
    private Transform player;
    [SerializeField]
    private TextMeshProUGUI playerPos;

    void Update()
    {
        Vector3 pos = player.position;
        playerPos.text = "X: " + pos.x.ToString("0.00") + 
            "\nZ: " + pos.z.ToString("0.00") + 
            "\nY Rot: " + player.eulerAngles.y.ToString("0") + "°";
    }
}
