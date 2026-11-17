using System.Collections.Generic;
using Roguelike;
using SER494.Gameplay.CombatIntegration.Unity;
using SER494.Gameplay.ExplorationMenu.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FloorGuide : MonoBehaviour
{
    private Canvas canvas;
    private TextMeshProUGUI floorText;
    private TextMeshProUGUI objectiveText;
    private TextMeshProUGUI exitText;
    private GameObject hintBox;
    private TextMeshProUGUI hintText;
    private float hintTimer;

    private List<ExplorationBattleTrigger> enemies = new List<ExplorationBattleTrigger>();
    private RoomExit exit;
    private Transform player;
    private bool clearedMessageShown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Setup()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        CreateIfFloor(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) CreateIfFloor(scene);
    }

    private static void CreateIfFloor(Scene scene)
    {
        if (!scene.name.StartsWith("Floor") || FindAnyObjectByType<FloorGuide>() != null) return;

        GameObject guideObject = new GameObject("Floor Guide");
        SceneManager.MoveGameObjectToScene(guideObject, scene);
        guideObject.AddComponent<FloorGuide>();
        guideObject.AddComponent<TutorialHints>();
        guideObject.AddComponent<Minimap>();
    }

    void Start()
    {
        BuildUI();

        foreach (ExplorationBattleTrigger enemy in FindObjectsByType<ExplorationBattleTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (enemy.gameObject.scene == gameObject.scene) enemies.Add(enemy);
        }

        exit = FindAnyObjectByType<RoomExit>();
        floorText.text = FloorTitle(gameObject.scene.name);
    }

    void Update()
    {
        if (player == null)
        {
            PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>();
            if (playerMovement != null) player = playerMovement.transform;
        }

        canvas.enabled = !IsHidden();
        if (!canvas.enabled) return;

        UpdateObjective();
        UpdateHint();
    }

    public bool IsHidden()
    {
        if (ExplorationMenuInputGate.IsOpen) return true;
        if (SceneManager.sceneCount > 1) return true;
        return BattleSessionService.TryGetActive(out BattleSessionService battle) && battle.HasActiveSession;
    }

    public Transform Player()
    {
        return player;
    }

    public int EnemiesLeft()
    {
        int left = 0;
        foreach (ExplorationBattleTrigger enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeSelf) left++;
        }
        return left;
    }

    public float DistanceTo(Transform target)
    {
        if (player == null || target == null) return -1f;
        return Vector3.Distance(player.position, target.position);
    }

    public Transform ClosestEnemy()
    {
        Transform closest = null;
        float best = float.MaxValue;
        foreach (ExplorationBattleTrigger enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeSelf) continue;

            float distance = DistanceTo(enemy.transform);
            if (distance >= 0f && distance < best)
            {
                best = distance;
                closest = enemy.transform;
            }
        }
        return closest;
    }

    public Transform Exit()
    {
        return exit != null ? exit.transform : null;
    }

    void UpdateObjective()
    {
        int left = EnemiesLeft();

        if (left > 0)
        {
            objectiveText.text = "Defeat the enemies  " + (enemies.Count - left) + " / " + enemies.Count;
        }
        else
        {
            objectiveText.text = "Find the exit to climb higher";

            if (enemies.Count > 0 && !clearedMessageShown)
            {
                clearedMessageShown = true;
                ShowHint("All enemies defeated! Find the exit.", 5f);
            }
        }

        float exitDistance = DistanceTo(Exit());
        exitText.text = exitDistance >= 0f ? "Exit: " + Mathf.RoundToInt(exitDistance) + " m away" : "";
    }

    public void ShowHint(string text, float seconds)
    {
        hintText.text = text;
        hintBox.SetActive(true);
        hintTimer = seconds;
    }

    public bool IsHintShowing()
    {
        return hintBox.activeSelf;
    }

    void UpdateHint()
    {
        if (!hintBox.activeSelf) return;

        hintTimer -= Time.unscaledDeltaTime;
        if (hintTimer <= 0f) hintBox.SetActive(false);
    }

    string FloorTitle(string sceneName)
    {
        RunSequence sequence = Resources.Load<RunSequence>("RunSequence");
        if (sequence != null && sequence.floors != null)
        {
            for (int i = 0; i < sequence.floors.Length; i++)
            {
                foreach (string variant in sequence.floors[i].variantScenes)
                {
                    if (variant == sceneName) return "Floor " + (i + 1) + "  -  " + sequence.floors[i].floorName;
                }
            }
        }
        return sceneName;
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("Floor Guide Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject panel = MakeBox("Objective Panel", canvasObject.transform, new Vector2(0, 1), new Vector2(30, -30), new Vector2(520, 150));
        floorText = MakeText("Floor", panel.transform, new Vector2(20, -14), 32, new Color(1f, 0.85f, 0.4f));
        objectiveText = MakeText("Objective", panel.transform, new Vector2(20, -62), 26, Color.white);
        exitText = MakeText("Exit Distance", panel.transform, new Vector2(20, -102), 22, new Color(0.75f, 0.75f, 0.75f));

        hintBox = MakeBox("Hint", canvasObject.transform, new Vector2(0.5f, 0), new Vector2(0, 230), new Vector2(1000, 80));
        hintText = MakeText("Hint Text", hintBox.transform, new Vector2(0, -20), 30, new Color(1f, 0.92f, 0.6f));
        hintText.alignment = TextAlignmentOptions.Center;
        RectTransform hintRect = hintText.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0, 1);
        hintRect.anchorMax = new Vector2(1, 1);
        hintRect.pivot = new Vector2(0.5f, 1);
        hintRect.anchoredPosition = new Vector2(0, -20);
        hintRect.sizeDelta = new Vector2(-40, 44);
        hintBox.SetActive(false);
    }

    GameObject MakeBox(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject box = new GameObject(name, typeof(RectTransform), typeof(Image));
        box.transform.SetParent(parent, false);
        box.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.03f, 0.7f);
        RectTransform rect = box.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return box;
    }

    TextMeshProUGUI MakeText(string name, Transform parent, Vector2 position, float size, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.color = color;
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(-40, size + 10);
        return text;
    }
}
