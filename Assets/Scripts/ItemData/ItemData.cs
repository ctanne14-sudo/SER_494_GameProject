using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/Item Data")]
public class ItemData : ScriptableObject
{
    public string id;
    public string displayName;
    public Texture icon;
    public GameObject prefab;

    public string DisplayNameOrId => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
}