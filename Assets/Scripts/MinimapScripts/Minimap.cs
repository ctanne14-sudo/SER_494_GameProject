using System.Collections.Generic;
using SER494.Gameplay.CombatIntegration.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    private const float SmallSize = 14f;
    private const float CameraHeight = 40f;
    private const float SliceAbovePlayer = 4f;

    private FloorGuide guide;
    private Camera mapCamera;
    private RenderTexture mapTexture;

    private Canvas canvas;
    private RectTransform frame;
    private RectTransform mapImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI legendText;
    private RectTransform playerIcon;
    private RectTransform exitIcon;
    private List<RectTransform> enemyIcons = new List<RectTransform>();
    private List<ExplorationBattleTrigger> enemies = new List<ExplorationBattleTrigger>();

    private Bounds floorBounds;
    private bool bigMap;
    private float mapTurn;

    void Start()
    {
        guide = GetComponent<FloorGuide>();

        foreach (ExplorationBattleTrigger enemy in FindObjectsByType<ExplorationBattleTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (enemy.gameObject.scene == gameObject.scene) enemies.Add(enemy);
        }

        floorBounds = FindFloorBounds();
        MakeCamera();
        BuildUI();
        SetBigMap(false);
    }

    void OnDestroy()
    {
        if (mapTexture != null) mapTexture.Release();
    }

    void Update()
    {
        bool hidden = guide.IsHidden() || guide.Player() == null;
        canvas.enabled = !hidden;
        mapCamera.enabled = !hidden;
        if (hidden) return;

        if (Input.GetKeyDown(KeyCode.M)) SetBigMap(!bigMap);

        MoveCamera();
        UpdateIcons();
    }

    void SetBigMap(bool big)
    {
        bigMap = big;

        if (big)
        {
            frame.anchorMin = new Vector2(0.5f, 0.5f);
            frame.anchorMax = new Vector2(0.5f, 0.5f);
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.anchoredPosition = Vector2.zero;
            frame.sizeDelta = new Vector2(820, 880);
            titleText.text = "Floor Map";
            legendText.text = "<color=#FFD966>You</color>     <color=#FF5A4A>Enemy</color>     <color=#5AE07A>Exit</color>     M - Close";
        }
        else
        {
            frame.anchorMin = new Vector2(1, 1);
            frame.anchorMax = new Vector2(1, 1);
            frame.pivot = new Vector2(1, 1);
            frame.anchoredPosition = new Vector2(-30, -30);
            frame.sizeDelta = new Vector2(320, 380);
            titleText.text = "Map";
            legendText.text = "M - Full map";
        }
    }

    void MoveCamera()
    {
        Vector3 playerPosition = guide.Player().position;
        float height = playerPosition.y + CameraHeight;

        if (bigMap)
        {
            mapCamera.transform.position = new Vector3(floorBounds.center.x, height, floorBounds.center.z);
            mapTurn = 0f;
            mapCamera.orthographicSize = Mathf.Max(floorBounds.extents.x, floorBounds.extents.z) + 3f;
        }
        else
        {
            mapCamera.transform.position = new Vector3(playerPosition.x, height, playerPosition.z);
            mapTurn = guide.Player().eulerAngles.y;
            mapCamera.orthographicSize = SmallSize;
        }

        mapCamera.transform.rotation = Quaternion.Euler(90f, mapTurn, 0f);
    }

    void UpdateIcons()
    {
        PlaceIcon(playerIcon, guide.Player().position, false);
        float turn = guide.Player().eulerAngles.y - mapTurn;
        playerIcon.localEulerAngles = new Vector3(0, 0, -turn);

        Transform exit = guide.Exit();
        exitIcon.gameObject.SetActive(exit != null);
        if (exit != null) PlaceIcon(exitIcon, exit.position, true);

        for (int i = 0; i < enemies.Count; i++)
        {
            bool alive = enemies[i] != null && enemies[i].gameObject.activeSelf;
            enemyIcons[i].gameObject.SetActive(alive);
            if (alive) PlaceIcon(enemyIcons[i], enemies[i].transform.position, false);
        }
    }

    void PlaceIcon(RectTransform icon, Vector3 worldPosition, bool keepOnEdge)
    {
        Vector3 viewport = mapCamera.WorldToViewportPoint(worldPosition);
        bool inside = viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;

        if (!inside && !keepOnEdge)
        {
            icon.gameObject.SetActive(false);
            return;
        }

        float x = Mathf.Clamp(viewport.x, 0.04f, 0.96f);
        float y = Mathf.Clamp(viewport.y, 0.04f, 0.96f);
        Vector2 size = mapImage.rect.size;
        icon.anchoredPosition = new Vector2((x - 0.5f) * size.x, (y - 0.5f) * size.y);
    }

    Bounds FindFloorBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool first = true;

        foreach (Renderer part in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (part.gameObject.scene != gameObject.scene) continue;
            if (part is ParticleSystemRenderer || part is TrailRenderer || part is LineRenderer) continue;

            if (first)
            {
                bounds = part.bounds;
                first = false;
            }
            else
            {
                bounds.Encapsulate(part.bounds);
            }
        }
        return bounds;
    }

    void MakeCamera()
    {
        mapTexture = new RenderTexture(512, 512, 16);
        mapTexture.name = "Minimap Texture";

        GameObject cameraObject = new GameObject("Minimap Camera");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        mapCamera = cameraObject.AddComponent<Camera>();
        mapCamera.orthographic = true;
        mapCamera.orthographicSize = SmallSize;
        mapCamera.nearClipPlane = CameraHeight - SliceAbovePlayer;
        mapCamera.farClipPlane = CameraHeight + 60f;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = new Color(0.06f, 0.05f, 0.04f);
        mapCamera.targetTexture = mapTexture;
        mapCamera.depth = -10;
    }

    void BuildUI()
    {
        GameObject canvasObject = new GameObject("Minimap Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject frameObject = new GameObject("Map Frame", typeof(RectTransform), typeof(Image));
        frameObject.transform.SetParent(canvasObject.transform, false);
        frameObject.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.03f, 0.85f);
        frame = frameObject.GetComponent<RectTransform>();

        titleText = MakeText("Map Title", frame, new Vector2(0, 1), new Vector2(0, -6), 26, new Color(1f, 0.85f, 0.4f));
        legendText = MakeText("Map Legend", frame, new Vector2(0, 0), new Vector2(0, 6), 20, new Color(0.8f, 0.8f, 0.8f));

        GameObject imageObject = new GameObject("Map Image", typeof(RectTransform), typeof(RawImage), typeof(Mask));
        imageObject.transform.SetParent(frame, false);
        imageObject.GetComponent<RawImage>().texture = mapTexture;
        mapImage = imageObject.GetComponent<RectTransform>();
        mapImage.anchorMin = new Vector2(0, 0);
        mapImage.anchorMax = new Vector2(1, 1);
        mapImage.offsetMin = new Vector2(10, 40);
        mapImage.offsetMax = new Vector2(-10, -40);

        Sprite circle = MakeSprite(false);
        Sprite triangle = MakeSprite(true);

        exitIcon = MakeIcon("Exit Icon", null, new Color(0.35f, 0.88f, 0.48f), 20);
        exitIcon.localEulerAngles = new Vector3(0, 0, 45);

        foreach (ExplorationBattleTrigger enemy in enemies)
        {
            enemyIcons.Add(MakeIcon("Enemy Icon", circle, new Color(1f, 0.35f, 0.29f), 18));
        }

        playerIcon = MakeIcon("Player Icon", triangle, new Color(1f, 0.85f, 0.4f), 26);
    }

    RectTransform MakeIcon(string name, Sprite sprite, Color color, float size)
    {
        GameObject iconObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(mapImage, false);
        Image image = iconObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);
        return rect;
    }

    TextMeshProUGUI MakeText(string name, RectTransform parent, Vector2 anchor, Vector2 position, float size, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, anchor.y);
        rect.anchorMax = new Vector2(1, anchor.y);
        rect.pivot = new Vector2(0.5f, anchor.y);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(0, size + 8);
        return text;
    }

    Sprite MakeSprite(bool triangle)
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);
        float middle = (size - 1) / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool filled;
                if (triangle)
                {
                    filled = Mathf.Abs(x - middle) <= (size - 1 - y) / 2f;
                }
                else
                {
                    filled = Vector2.Distance(new Vector2(x, y), new Vector2(middle, middle)) <= middle;
                }
                texture.SetPixel(x, y, filled ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
