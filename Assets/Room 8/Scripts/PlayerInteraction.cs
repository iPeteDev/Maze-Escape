using UnityEngine;

/// <summary>
/// Handles player interaction with sortable items using raycasting
/// Attach this to the Main Camera or player controller
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("Maximum distance for item interaction")]
    [SerializeField] private float interactionDistance = 3f;
    
    [Tooltip("Layer mask for interactable objects")]
    [SerializeField] private LayerMask interactableLayer;
    
    [Header("Carry Settings")]
    [Tooltip("Distance in front of camera to hold items")]
    [SerializeField] private float carryDistance = 2f;
    
    [Tooltip("Smooth speed for item following")]
    [SerializeField] private float carrySmoothing = 10f;
    
    [Header("UI Feedback")]
    [Tooltip("Crosshair or interaction prompt UI")]
    [SerializeField] private GameObject interactionPrompt;
    
    [Tooltip("Text component for interaction messages")]
    [SerializeField] private UnityEngine.UI.Text interactionText;
    
    // Private variables
    private Camera playerCamera;
    private SortableItem carriedItem = null;
    private Vector3 targetCarryPosition;
    private bool isCarrying = false;
    
    // Input tracking
    private bool holdingLeftClick = false;
    
    void Start()
    {
        // Get camera reference
        playerCamera = GetComponent<Camera>();
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        if (playerCamera == null)
        {
            Debug.LogError("PlayerInteraction: No camera found! Attach this script to the Main Camera.");
        }
        
        // Hide interaction prompt initially
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    void Update()
    {
        // Handle input
        HandleInput();
        
        // Perform raycast
        PerformRaycast();
        
        // Update carried item position
        if (isCarrying && carriedItem != null)
        {
            UpdateCarriedItemPosition();
        }
    }
    
    /// <summary>
    /// Handles mouse input for picking up and placing items
    /// </summary>
    private void HandleInput()
    {
        // Check for left mouse button down
        if (Input.GetMouseButtonDown(0))
        {
            holdingLeftClick = true;
            
            // Try to pick up an item
            if (!isCarrying)
            {
                TryPickupItem();
            }
        }
        
        // Check for left mouse button held
        if (Input.GetMouseButton(0))
        {
            holdingLeftClick = true;
        }
        
        // Check for left mouse button up
        if (Input.GetMouseButtonUp(0))
        {
            holdingLeftClick = false;
            
            // Try to place the item
            if (isCarrying)
            {
                TryPlaceItem();
            }
        }
    }
    
    /// <summary>
    /// Performs raycast to detect interactable objects
    /// </summary>
    private void PerformRaycast()
    {
        if (playerCamera == null) return;
        
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        
        // Perform the raycast
        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            // Check if we hit a sortable item
            SortableItem item = hit.collider.GetComponent<SortableItem>();
            
            if (item != null && !item.IsPlaced() && !isCarrying)
            {
                // Show interaction prompt
                ShowInteractionPrompt("Hold Left Click to Pick Up");
            }
            else if (isCarrying)
            {
                // Check if we're looking at a shelf slot
                ShelfSlot slot = hit.collider.GetComponent<ShelfSlot>();
                
                if (slot != null && !slot.IsFilled())
                {
                    ShowInteractionPrompt("Release to Place Item");
                }
                else
                {
                    ShowInteractionPrompt("Release to Drop Item");
                }
            }
            else
            {
                HideInteractionPrompt();
            }
        }
        else
        {
            // Not looking at anything interactable
            if (isCarrying)
            {
                ShowInteractionPrompt("Release to Drop Item");
            }
            else
            {
                HideInteractionPrompt();
            }
        }
    }
    
    /// <summary>
    /// Attempts to pick up an item the player is looking at
    /// </summary>
    private void TryPickupItem()
    {
        if (playerCamera == null) return;
        
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            SortableItem item = hit.collider.GetComponent<SortableItem>();
            
            if (item != null && !item.IsPlaced())
            {
                // Pick up the item
                carriedItem = item;
                isCarrying = true;
                
                // Notify the item
                carriedItem.OnPickup();
                
                Debug.Log($"Picked up item: {carriedItem.GetItemID()}");
            }
        }
    }
    
    /// <summary>
    /// Attempts to place the carried item
    /// </summary>
    private void TryPlaceItem()
    {
        if (!isCarrying || carriedItem == null) return;
        
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        
        bool placedSuccessfully = false;
        
        // Check if we're looking at a shelf slot
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            ShelfSlot slot = hit.collider.GetComponent<ShelfSlot>();
            
            if (slot != null)
            {
                // Try to place the item in the slot
                placedSuccessfully = slot.TryPlaceItem(carriedItem);
                
                if (placedSuccessfully)
                {
                    Debug.Log($"Item placed successfully in slot {slot.GetSlotID()}");
                    
                    // Clear carried item reference
                    carriedItem = null;
                    isCarrying = false;
                    return;
                }
                else
                {
                    Debug.Log("Item does not match this slot!");
                }
            }
        }
        
        // If placement failed or no slot found, just drop the item
        if (!placedSuccessfully)
        {
            DropItem();
        }
    }
    
    /// <summary>
    /// Drops the currently carried item
    /// </summary>
    private void DropItem()
    {
        if (carriedItem == null) return;
        
        // Notify the item it's being dropped
        carriedItem.OnDrop();
        
        Debug.Log($"Dropped item: {carriedItem.GetItemID()}");
        
        // Clear references
        carriedItem = null;
        isCarrying = false;
    }
    
    /// <summary>
    /// Updates the position of the carried item to follow the camera
    /// </summary>
    private void UpdateCarriedItemPosition()
    {
        if (carriedItem == null || playerCamera == null) return;
        
        // Calculate target position in front of camera
        targetCarryPosition = playerCamera.transform.position + 
                             playerCamera.transform.forward * carryDistance;
        
        // Smoothly move item to target position
        carriedItem.transform.position = Vector3.Lerp(
            carriedItem.transform.position,
            targetCarryPosition,
            carrySmoothing * Time.deltaTime
        );
        
        // Optionally rotate item to face camera
        carriedItem.transform.rotation = Quaternion.Lerp(
            carriedItem.transform.rotation,
            playerCamera.transform.rotation,
            carrySmoothing * Time.deltaTime
        );
    }
    
    /// <summary>
    /// Shows the interaction prompt with a message
    /// </summary>
    private void ShowInteractionPrompt(string message)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
        }
        
        if (interactionText != null)
        {
            interactionText.text = message;
        }
    }
    
    /// <summary>
    /// Hides the interaction prompt
    /// </summary>
    private void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    /// <summary>
    /// Public method to check if player is currently carrying an item
    /// </summary>
    public bool IsCarryingItem()
    {
        return isCarrying;
    }
}
