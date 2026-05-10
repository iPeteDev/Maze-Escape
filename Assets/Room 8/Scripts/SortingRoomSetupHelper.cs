using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Utility script to help set up the sorting room
/// Provides tools for automatically generating IDs and configuring slots
/// </summary>
public class SortingRoomSetupHelper : MonoBehaviour
{
    [Header("Auto-Setup Tools")]
    [Tooltip("Prefix for auto-generated IDs (e.g., 'BOOK', 'ITEM')")]
    public string idPrefix = "ITEM";
    
    [Tooltip("Starting number for ID generation")]
    public int startingNumber = 1;
    
    [Header("References")]
    [Tooltip("Parent object containing all shelf slots")]
    public Transform shelfParent;
    
    [Tooltip("Parent object containing all items")]
    public Transform itemsParent;
    
    #if UNITY_EDITOR
    /// <summary>
    /// Editor button to auto-assign IDs to slots and items
    /// </summary>
    [ContextMenu("Auto-Assign IDs")]
    public void AutoAssignIDs()
    {
        if (shelfParent == null || itemsParent == null)
        {
            Debug.LogError("Please assign both Shelf Parent and Items Parent!");
            return;
        }
        
        // Get all shelf slots
        ShelfSlot[] slots = shelfParent.GetComponentsInChildren<ShelfSlot>();
        
        // Get all sortable items
        SortableItem[] items = itemsParent.GetComponentsInChildren<SortableItem>();
        
        if (slots.Length != items.Length)
        {
            Debug.LogWarning($"Mismatch: {slots.Length} slots but {items.Length} items. IDs may not pair correctly.");
        }
        
        int idCounter = startingNumber;
        
        // Assign IDs to slots
        foreach (ShelfSlot slot in slots)
        {
            slot.slotID = $"{idPrefix}_{idCounter:D3}";
            EditorUtility.SetDirty(slot);
            idCounter++;
        }
        
        // Reset counter and assign to items
        idCounter = startingNumber;
        
        foreach (SortableItem item in items)
        {
            if (item.itemData != null)
            {
                item.itemData.itemID = $"{idPrefix}_{idCounter:D3}";
                EditorUtility.SetDirty(item.itemData);
            }
            else
            {
                Debug.LogWarning($"Item {item.name} has no ItemData assigned!");
            }
            idCounter++;
        }
        
        Debug.Log($"Auto-assigned {slots.Length} slot IDs and {items.Length} item IDs");
    }
    
    /// <summary>
    /// Creates visual indicators for all slots
    /// </summary>
    [ContextMenu("Create Slot Indicators")]
    public void CreateSlotIndicators()
    {
        if (shelfParent == null)
        {
            Debug.LogError("Please assign Shelf Parent!");
            return;
        }
        
        ShelfSlot[] slots = shelfParent.GetComponentsInChildren<ShelfSlot>();
        
        foreach (ShelfSlot slot in slots)
        {
            if (slot.slotVisualIndicator == null)
            {
                // Create a simple cube as indicator
                GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
                indicator.name = "SlotIndicator";
                indicator.transform.SetParent(slot.transform);
                indicator.transform.localPosition = Vector3.zero;
                indicator.transform.localScale = new Vector3(0.9f, 0.9f, 0.1f);
                
                // Make it semi-transparent
                Renderer renderer = indicator.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(1f, 1f, 1f, 0.3f);
                
                // Enable transparent rendering
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                
                renderer.material = mat;
                
                // Remove collider from indicator
                DestroyImmediate(indicator.GetComponent<Collider>());
                
                // Assign to slot
                slot.slotVisualIndicator = indicator;
                EditorUtility.SetDirty(slot);
            }
        }
        
        Debug.Log($"Created indicators for {slots.Length} slots");
    }
    
    /// <summary>
    /// Validates the setup and reports any issues
    /// </summary>
    [ContextMenu("Validate Setup")]
    public void ValidateSetup()
    {
        int errorCount = 0;
        int warningCount = 0;
        
        // Check for SortingRoomManager
        SortingRoomManager manager = FindObjectOfType<SortingRoomManager>();
        if (manager == null)
        {
            Debug.LogError("No SortingRoomManager found in scene!");
            errorCount++;
        }
        
        // Check slots
        ShelfSlot[] slots = FindObjectsOfType<ShelfSlot>();
        foreach (ShelfSlot slot in slots)
        {
            if (string.IsNullOrEmpty(slot.slotID))
            {
                Debug.LogWarning($"Slot {slot.name} has no ID assigned!");
                warningCount++;
            }
            
            if (!slot.GetComponent<Collider>().isTrigger)
            {
                Debug.LogError($"Slot {slot.name} collider is not set to trigger!");
                errorCount++;
            }
        }
        
        // Check items
        SortableItem[] items = FindObjectsOfType<SortableItem>();
        foreach (SortableItem item in items)
        {
            if (item.itemData == null)
            {
                Debug.LogError($"Item {item.name} has no ItemData assigned!");
                errorCount++;
            }
            else if (string.IsNullOrEmpty(item.itemData.itemID))
            {
                Debug.LogWarning($"Item {item.name} ItemData has no ID!");
                warningCount++;
            }
            
            if (item.GetComponent<Rigidbody>() == null)
            {
                Debug.LogError($"Item {item.name} is missing Rigidbody!");
                errorCount++;
            }
        }
        
        // Check for PlayerInteraction
        PlayerInteraction playerInteraction = FindObjectOfType<PlayerInteraction>();
        if (playerInteraction == null)
        {
            Debug.LogError("No PlayerInteraction component found in scene!");
            errorCount++;
        }
        
        Debug.Log($"Validation complete: {errorCount} errors, {warningCount} warnings");
    }
    #endif
}
