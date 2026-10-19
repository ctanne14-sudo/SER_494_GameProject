using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject pausePanel;
    private bool isPaused;

    void Start()
    {
        pausePanel.SetActive(false);
    }

    void Update()
    {
        if (GameManager.Instance.gameComplete) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        SetCameraControl(false);
    }

    public void Resume()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        SetCameraControl(true);
    }

    public void RestartFloor()
    {
        Resume();
        GameManager.Instance.LoadFloor();
    }

    public void GoToMainMenu()
    {
        isPaused = false;
        GameManager.Instance.GoToMainMenu();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    void SetCameraControl(bool active)
    {
        CameraMovement cameraMovement = FindAnyObjectByType<CameraMovement>();
        if (cameraMovement != null) cameraMovement.enabled = active;

        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !active;
    }
}
