using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Items/Item Database")]
public class ItemDatabase : ScriptableObject
{
    private Dictionary<string, ItemData> lookup;

    private void OnEnable() => lookup = null;

    private void Build()
    {
        lookup = new Dictionary<string, ItemData>(StringComparer.Ordinal);
        foreach (ItemData data in Resources.LoadAll<ItemData>("PlayerState/ProcessedItems"))
        {
            if (string.IsNullOrWhiteSpace(data.id))
            {
                Debug.LogWarning($"ItemDatabase: skipped an entry with no id ({(data != null ? data.name : "null")}).", this);
                continue;
            }

            string key = data.id.Trim();
            if (!lookup.TryAdd(key, data))
            {
                Debug.LogWarning($"ItemDatabase: duplicate id '{key}' on {data.name}.", data);
            }
        }
    }

    public bool TryGet(string id, out ItemData def)
    {
        def = null;
        if (string.IsNullOrWhiteSpace(id)) return false;
        if (lookup == null) Build();
        return lookup.TryGetValue(id.Trim(), out def);
    }

    public ItemData Get(string id)
    {
        if (TryGet(id, out ItemData def)) return def;
        if (!string.IsNullOrWhiteSpace(id)) Debug.LogWarning($"ItemDatabase: unknown item id '{id}'.", this);
        return null;
    }

#if UNITY_EDITOR
    private void OnValidate() => lookup = null;     //Picks up edits made in the Inspector
#endif
}