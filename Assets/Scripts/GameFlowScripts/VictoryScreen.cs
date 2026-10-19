using UnityEngine;

public class VictoryScreen : MonoBehaviour
{
    [SerializeField]
    private GameObject victoryPanel;

    void Start()
    {
        victoryPanel.SetActive(false);
        GameManager.Instance.OnChanged += Refresh;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnChanged -= Refresh;
    }

    void Refresh()
    {
        if (!GameManager.Instance.gameComplete) return;

        victoryPanel.SetActive(true);
        Time.timeScale = 0f;

        CameraMovement cameraMovement = FindAnyObjectByType<CameraMovement>();
        if (cameraMovement != null) cameraMovement.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void BackToMainMenu()
    {
        GameManager.Instance.GoToMainMenu();
    }
}
