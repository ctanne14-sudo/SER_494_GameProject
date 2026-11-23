using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroToStart : MonoBehaviour
{

    void Start()
    {
        Cursor.lockState = CursorLockMode.None; 
    }

    public void PlayGame()
    {
        Debug.Log("Play Game button clicked");
        SceneManager.LoadScene("Floor1_Cell_A");
    }
}
