using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string firstFloorScene = "FloorTest";
    [SerializeField]
    private GameObject continueButton;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        continueButton.SetActive(GameManager.HasSave());
    }

    public void NewGame()
    {
        GameManager.DeleteSave();
        GameManager.continueFromSave = false;
        SceneManager.LoadScene(firstFloorScene);
    }

    public void ContinueGame()
    {
        GameManager.continueFromSave = true;
        SceneManager.LoadScene(firstFloorScene);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
