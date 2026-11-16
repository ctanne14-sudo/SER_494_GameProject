using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProcessingShopStationBootstrap
{
    private const string StationName = "Processing Shop (Prototype)";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureStationExists()
    {
        if (Object.FindAnyObjectByType<ProcessingShopAction>() != null)
        {
            return;
        }

        SavePointAction savePoint = Object.FindAnyObjectByType<SavePointAction>();
        if (savePoint == null)
        {
            return;
        }

        var station = new GameObject(StationName)
        {
            layer = savePoint.gameObject.layer
        };
        station.transform.position = savePoint.transform.position + new Vector3(3f, 0f, 0f);
        station.AddComponent<ProcessingShopAction>();
        var interactionCollider = station.AddComponent<BoxCollider>();
        interactionCollider.size = new Vector3(1.8f, 1.5f, 1.2f);

        CreatePart(station.transform, "Work Bench", new Vector3(0f, -0.05f, 0f),
            new Vector3(1.7f, 0.85f, 1.05f), new Color(0.28f, 0.17f, 0.09f));
        CreatePart(station.transform, "Metal Top", new Vector3(0f, 0.5f, 0f),
            new Vector3(1.9f, 0.22f, 1.2f), new Color(0.36f, 0.31f, 0.24f));
        CreatePart(station.transform, "Tool Block", new Vector3(0.35f, 0.82f, 0f),
            new Vector3(0.55f, 0.42f, 0.45f), new Color(0.25f, 0.27f, 0.29f));

        Debug.Log($"Created {StationName} beside '{savePoint.name}' in scene '{SceneManager.GetActiveScene().name}'.");
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureStationExists();
    }

    private static void CreatePart(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Color color)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.layer = parent.gameObject.layer;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        Object.Destroy(part.GetComponent<Collider>());
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.material.color = color;
    }
}
