using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPersistance : MonoBehaviour
{
    private static GameObject[] persistantObjects = new GameObject[3];
    public int objectIndex;
    void Awake()
    {
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
