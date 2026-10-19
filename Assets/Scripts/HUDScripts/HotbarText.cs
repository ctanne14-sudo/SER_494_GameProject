using UnityEngine;
using TMPro;

public class SetHotbarText : MonoBehaviour
{
    [SerializeField] private GameObject hotbar;
    [SerializeField] private TextMeshProUGUI hotbarText;


    void Update()
    {
        int numSlots = hotbar.transform.childCount;
        string prompt = "";
        for (int i = 1; i <= numSlots; i++)
        {
            prompt += i + " ";
        }
        hotbarText.text = prompt.TrimEnd();
    }
}