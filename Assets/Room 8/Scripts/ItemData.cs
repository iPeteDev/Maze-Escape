using UnityEngine;

/// <summary>
/// ScriptableObject that defines the properties of a sortable item
/// Each item has a unique ID that must match its corresponding shelf slot
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Sorting Room/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Identification")]
    [Tooltip("Unique identifier for this item - must match the shelf slot ID")]
    public string itemID;
    
    [Header("Visual Properties")]
    [Tooltip("Name displayed to the player")]
    public string itemName;
    
    [Tooltip("3D model prefab for this item")]
    public GameObject itemPrefab;
    
    [Header("Glow Effect Settings")]
    [Tooltip("Color of the glow when correctly placed")]
    public Color glowColor = Color.green;
    
    [Tooltip("Intensity of the glow effect")]
    [Range(0f, 5f)]
    public float glowIntensity = 2f;
}
