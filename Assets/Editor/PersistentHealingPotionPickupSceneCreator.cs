using System;
using SER494.Gameplay.PlayerState;
using SER494.Gameplay.PlayerState.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PersistentHealingPotionPickupSceneCreator
{
    private const string FloorScenePath = "Assets/_Project/Scenes/Floor1_Cell_A.unity";
    private const string PickupName = "Persistent Healing Potion Pickup";
    private const string PickupId = "floor1.healing-potion-pickup-01";

    [MenuItem("Tools/SER494/Add Persistent Healing Potion Pickup to Floor 1")]
    public static void CreateOrRepairPickup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(FloorScenePath, OpenSceneMode.Single);
        GameObject player = FindPlayer(scene);
        if (player == null)
        {
            Debug.LogError($"Could not find an active Player in {FloorScenePath}.");
            return;
        }

        GameObject pickup = FindRoot(scene, PickupName);
        if (pickup == null)
        {
            pickup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pickup.name = PickupName;
            Undo.RegisterCreatedObjectUndo(pickup, "Create Persistent Healing Potion Pickup");
            SceneManager.MoveGameObjectToScene(pickup, scene);
            pickup.transform.SetPositionAndRotation(
                player.transform.position
                + player.transform.forward * 2.5f
                + player.transform.right * 1.5f
                + Vector3.up * 0.35f,
                Quaternion.identity);
            pickup.transform.localScale = new Vector3(0.25f, 0.35f, 0.25f);
        }
        else
        {
            Undo.RecordObject(pickup, "Repair Persistent Healing Potion Pickup");
        }

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer < 0)
        {
            throw new InvalidOperationException("The Interactable layer is missing.");
        }

        pickup.layer = interactableLayer;
        Collider collider = pickup.GetComponent<Collider>();
        if (collider == null)
        {
            collider = Undo.AddComponent<CapsuleCollider>(pickup);
        }

        collider.isTrigger = false;
        EditorUtility.SetDirty(collider);

        PersistentWorldObject persistent =
            GetOrAddComponent<PersistentWorldObject>(pickup);
        persistent.Configure(
            PickupId,
            WorldObjectStateIds.Collected,
            pickup,
            deactivateTarget: true);
        EditorUtility.SetDirty(persistent);

        PersistentItemPickupAction pickupAction =
            GetOrAddComponent<PersistentItemPickupAction>(pickup);
        pickupAction.Configure(
            PlayerStateInitialValues.HealingPotionId,
            configuredAmount: 1,
            configuredPrompt: "E / A  Pick Up Healing Potion");
        EditorUtility.SetDirty(pickupAction);
        EditorUtility.SetDirty(pickup);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, FloorScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = pickup;
        EditorGUIUtility.PingObject(pickup);
        Debug.Log(
            $"Created or repaired '{PickupName}' in {FloorScenePath}. "
            + "Enter Play Mode and press E or the gamepad south button to collect it.");
    }

    private static GameObject FindPlayer(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.gameObject.activeInHierarchy
                    && candidate.gameObject.CompareTag("Player"))
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }

    private static GameObject FindRoot(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (string.Equals(root.name, objectName, StringComparison.Ordinal))
            {
                return root;
            }
        }

        return null;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject)
        where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(gameObject);
    }
}
