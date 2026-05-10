using UnityEngine;

/// <summary>
/// Component attached to each vacant slot on the bookshelf
/// Uses trigger collision to validate and accept matching items
/// </summary>
[RequireComponent(typeof(Collider))]
public class ShelfSlot : MonoBehaviour
{
    [Header("Slot Configuration")]
    [Tooltip("Unique identifier - must match the item's ID")]
    public string slotID;
    
    [Header("Visual Feedback")]
    [Tooltip("Visual indicator for the slot (optional)")]
    public GameObject slotVisualIndicator;
    
    [Tooltip("Color when slot is empty")]
    public Color emptyColor = new Color(1f, 1f, 1f, 0.3f);
    
    [Tooltip("Color when slot is filled")]
    public Color filledColor = new Color(0f, 1f, 0f, 0.3f);
    
    [Header("Placement Settings")]
    [Tooltip("Snap position offset from slot center")]
    public Vector3 snapOffset = Vector3.zero;
    
    [Tooltip("Snap rotation for placed items")]
    public Vector3 snapRotation = Vector3.zero;
    
    private bool isFilled = false;
    private SortableItem currentItem = null;
    private Collider slotCollider;
    private Renderer slotRenderer;
    
    void Awake()
    {
        slotCollider = GetComponent<Collider>();
        slotCollider.isTrigger = true; // Must be a trigger
        
        if (slotVisualIndicator != null)
        {
            slotRenderer = slotVisualIndicator.GetComponent<Renderer>();
            UpdateVisualFeedback();
        }
    }
    
    void Start()
    {
        // Validate slot has an ID
        if (string.IsNullOrEmpty(slotID))
        {
            Debug.LogError($"ShelfSlot {gameObject.name} is missing a slot ID!");
        }
    }
    
    /// <summary>
    /// Called when an item enters the slot's trigger zone
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Only process if slot is empty
        if (isFilled) return;
        
        // Check if the object is a sortable item
        SortableItem item = other.GetComponent<SortableItem>();
        
        if (item != null && !item.IsPlaced())
        {
            // Check if the item is being carried by the player
            if (item.IsBeingCarried())
            {
                // Don't do anything yet - wait for player to release
                return;
            }
        }
    }
    
    /// <summary>
    /// Attempts to place an item in this slot
    /// Called by the PlayerInteraction script when player releases an item over this slot
    /// </summary>
    /// <param name="item">The item to place</param>
    /// <returns>True if placement was successful, false otherwise</returns>
    public bool TryPlaceItem(SortableItem item)
    {
        // Check if slot is already filled
        if (isFilled)
        {
            Debug.Log($"Slot {slotID} is already filled");
            return false;
        }
        
        // Check if item is null
        if (item == null)
        {
            Debug.LogWarning("Attempted to place null item");
            return false;
        }
        
        // Validate ID match
        string itemID = item.GetItemID();
        
        if (itemID == slotID)
        {
            // IDs match! Place the item
            PlaceItem(item);
            return true;
        }
        else
        {
            // IDs don't match - placement fails
            Debug.Log($"Item ID ({itemID}) does not match Slot ID ({slotID})");
            return false;
        }
    }
    
    /// <summary>
    /// Places and locks the item in this slot
    /// </summary>
    private void PlaceItem(SortableItem item)
    {
        isFilled = true;
        currentItem = item;
        
        // Calculate snap position
        Vector3 snapPosition = transform.position + transform.TransformDirection(snapOffset);
        Quaternion snapRotationQuat = Quaternion.Euler(snapRotation);
        
        // Move item to snap position
        item.transform.position = snapPosition;
        item.transform.rotation = transform.rotation * snapRotationQuat;
        
        // Notify the item it was correctly placed
        item.OnCorrectPlacement();
        
        // Update visual feedback
        UpdateVisualFeedback();
        
        Debug.Log($"Successfully placed item {item.GetItemID()} in slot {slotID}");
    }
    
    /// <summary>
    /// Updates the visual indicator color
    /// </summary>
    private void UpdateVisualFeedback()
    {
        if (slotRenderer != null)
        {
            Material mat = slotRenderer.material;
            
            if (isFilled)
            {
                mat.color = filledColor;
            }
            else
            {
                mat.color = emptyColor;
            }
        }
    }
    
    /// <summary>
    /// Check if this slot is filled
    /// </summary>
    public bool IsFilled()
    {
        return isFilled;
    }
    
    /// <summary>
    /// Get the slot's unique ID
    /// </summary>
    public string GetSlotID()
    {
        return slotID;
    }
}
