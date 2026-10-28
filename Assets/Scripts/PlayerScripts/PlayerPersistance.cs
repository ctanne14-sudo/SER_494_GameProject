using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PlayerPersistance : MonoBehaviour
{
    private const string TitleSceneName = "TitleMenu";
    private static GameObject[] persistantObjects = new GameObject[3];
    public int objectIndex;

    void Awake()
    {
        PlayerVisualOriginAlignment.Align(gameObject);

        if (persistantObjects[objectIndex] == null)
        {
            persistantObjects[objectIndex] = gameObject;
            DontDestroyOnLoad(gameObject);
        } else if (persistantObjects[objectIndex] != gameObject)
        {
            MoveToThis(persistantObjects[objectIndex]);
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name == TitleSceneName)
        {
            Destroy(gameObject);
        }
    }

    void MoveToThis(GameObject target)
    {
        var cc = target.GetComponent<CharacterController>();

        if (cc != null) cc.enabled = false;
        target.transform.SetPositionAndRotation(transform.position, transform.rotation);
        if (target.CompareTag("Player"))
            target.GetComponent<PlayerMovement>().updateCheckPoint(transform.position);
        if (cc != null) cc.enabled = true;

    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { persistantObjects = new GameObject[3]; }
}

internal static class PlayerVisualOriginAlignment
{
    private const float MinimumCorrection = 0.1f;

    public static void Align(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        SkinnedMeshRenderer[] renderers =
            player.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds visualBounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
        {
            visualBounds.Encapsulate(renderers[index].bounds);
        }

        Transform root = player.transform;
        Vector3 correction = new Vector3(
            visualBounds.center.x - root.position.x,
            0f,
            visualBounds.center.z - root.position.z);
        if (correction.sqrMagnitude < MinimumCorrection * MinimumCorrection)
        {
            return;
        }

        var childPoses = new List<ChildPose>(root.childCount);
        for (int index = 0; index < root.childCount; index++)
        {
            Transform child = root.GetChild(index);
            childPoses.Add(new ChildPose(child));
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;
        if (controllerWasEnabled)
        {
            controller.enabled = false;
        }

        root.position += correction;
        for (int index = 0; index < childPoses.Count; index++)
        {
            childPoses[index].Restore();
        }

        if (controllerWasEnabled)
        {
            controller.enabled = true;
        }
    }

    private readonly struct ChildPose
    {
        private readonly Transform _transform;
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;

        public ChildPose(Transform transform)
        {
            _transform = transform;
            _position = transform.position;
            _rotation = transform.rotation;
        }

        public void Restore()
        {
            if (_transform != null)
            {
                _transform.SetPositionAndRotation(_position, _rotation);
            }
        }
    }
}
