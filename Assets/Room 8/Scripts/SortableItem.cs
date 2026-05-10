using UnityEngine;

/// <summary>
/// Component attached to each sortable item in the scene
/// Handles pickup, placement, and visual feedback
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class SortableItem : MonoBehaviour
{
    [Header("Item Configuration")]
    [Tooltip("Reference to the item's data asset")]
    public ItemData itemData;
    
    [Header("Physics Settings")]
    [Tooltip("Layer for items when being carried")]
    public LayerMask carriedLayer;
    
    private Rigidbody rb;
    private Collider col;
    private bool isBeingCarried = false;
    private bool isPlaced = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    // Glow effect components
    private Material[] originalMaterials;
    private Material[] glowMaterials;
    private Renderer itemRenderer;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        itemRenderer = GetComponentInChildren<Renderer>();
        
        // Store original transform for reset if needed
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // Initialize materials for glow effect
        if (itemRenderer != null)
        {
            originalMaterials = itemRenderer.materials;
        }
    }
    
    void Start()
    {
        // Ensure rigidbody is configured correctly
        rb.useGravity = true;
        rb.isKinematic = false;
        
        // Ensure the item has a valid ID
        if (itemData == null)
        {
            Debug.LogError($"Item {gameObject.name} is missing ItemData reference!");
        }
    }
    
    /// <summary>
    /// Called when the player starts carrying this item
    /// </summary>
    public void OnPickup()
    {
        if (isPlaced) return; // Cannot pick up items that are correctly placed
        
        isBeingCarried = true;
        rb.useGravity = false;
        rb.isKinematic = true;
        
        // Disable collision with other items while carrying
        col.isTrigger = true;
    }
    
    /// <summary>
    /// Called when the player stops carrying this item
    /// </summary>
    public void OnDrop()
    {
        isBeingCarried = false;
        rb.useGravity = true;
        rb.isKinematic = false;
        col.isTrigger = false;
    }
    
    /// <summary>
    /// Called when item is successfully placed in correct slot
    /// </summary>
    public void OnCorrectPlacement()
    {
        isPlaced = true;
        isBeingCarried = false;
        
        // Lock the item in place
        rb.useGravity = false;
        rb.isKinematic = true;
        col.enabled = false;
        
        // Activate glow effect
        ActivateGlow();
        
        // Notify the room manager
        SortingRoomManager.Instance?.OnItemPlaced();
    }
    
    /// <summary>
    /// Activates the glow effect on this item
    /// </summary>
    private void ActivateGlow()
    {
        if (itemRenderer == null || itemData == null) return;
        
        // Create emissive materials
        Material[] newMaterials = new Material[originalMaterials.Length];
        
        for (int i = 0; i < originalMaterials.Length; i++)
        {
            // Create a new material instance
            newMaterials[i] = new Material(originalMaterials[i]);
            
            // Enable emission
            newMaterials[i].EnableKeyword("_EMISSION");
            
            // Set emissive color
            Color emissiveColor = itemData.glowColor * itemData.glowIntensity;
            newMaterials[i].SetColor("_EmissionColor", emissiveColor);
        }
        
        itemRenderer.materials = newMaterials;
    }
    
    /// <summary>
    /// Returns the unique ID of this item
    /// </summary>
    public string GetItemID()
    {
        return itemData != null ? itemData.itemID : "";
    }
    
    /// <summary>
    /// Check if item is currently being carried
    /// </summary>
    public bool IsBeingCarried()
    {
        return isBeingCarried;
    }
    
    /// <summary>
    /// Check if item is correctly placed
    /// </summary>
    public bool IsPlaced()
    {
        return isPlaced;
    }
}
