using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public static bool continueFromSave;

    public int currentFloor = 1;
    public int maxFloors = 3;
    public string[] floorScenes = { "FloorTest", "FloorTest", "FloorTest" };
    public string[] floorEnemies = { "Prisoners", "Guards", "Boss" };
    public string mainMenuScene = "MainMenu";
    public int gold;
    public bool gameComplete;
    public string message = "";

    public event System.Action OnChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (continueFromSave)
        {
            continueFromSave = false;
            currentFloor = PlayerPrefs.GetInt("SavedFloor", 1);
            gold = PlayerPrefs.GetInt("SavedGold", 0);
            message = "Welcome back to floor " + currentFloor;

            if (SceneManager.GetActiveScene().name != floorScenes[currentFloor - 1])
            {
                LoadFloor();
            }
        }
    }

    public string CurrentEnemyName()
    {
        int index = Mathf.Clamp(currentFloor - 1, 0, floorEnemies.Length - 1);
        return floorEnemies[index];
    }

    public void AddGold(int amount)
    {
        gold += amount;
        OnChanged?.Invoke();
    }

    public void ShowMessage(string text)
    {
        message = text;
        OnChanged?.Invoke();
    }

    public void NextFloor()
    {
        if (currentFloor >= maxFloors)
        {
            gameComplete = true;
            DeleteSave();
            OnChanged?.Invoke();
            return;
        }

        currentFloor++;
        message = "Floor " + currentFloor + ": " + CurrentEnemyName();
        SaveGame();
        OnChanged?.Invoke();
        LoadFloor();
    }

    public void LoadFloor()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(floorScenes[currentFloor - 1]);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Instance = null;
        Destroy(gameObject);
        SceneManager.LoadScene(mainMenuScene);
    }

    public void SaveGame()
    {
        PlayerPrefs.SetInt("SavedFloor", currentFloor);
        PlayerPrefs.SetInt("SavedGold", gold);
        PlayerPrefs.Save();
    }

    public static bool HasSave()
    {
        return PlayerPrefs.HasKey("SavedFloor");
    }

    public static void DeleteSave()
    {
        PlayerPrefs.DeleteKey("SavedFloor");
        PlayerPrefs.DeleteKey("SavedGold");
    }
}
