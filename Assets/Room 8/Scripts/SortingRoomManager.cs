using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the sorting room puzzle logic
/// Tracks item placement progress and unlocks the door when complete
/// Singleton pattern for easy access from other scripts
/// </summary>
public class SortingRoomManager : MonoBehaviour
{
    // Singleton instance
    public static SortingRoomManager Instance { get; private set; }
    
    [Header("Puzzle Configuration")]
    [Tooltip("Total number of items that need to be placed")]
    [SerializeField] private int totalItems = 10;
    
    [Tooltip("All shelf slots in the room")]
    [SerializeField] private List<ShelfSlot> shelfSlots = new List<ShelfSlot>();
    
    [Header("Door Settings")]
    [Tooltip("The door GameObject that will slide open")]
    [SerializeField] private GameObject door;
    
    [Tooltip("Direction and distance the door slides")]
    [SerializeField] private Vector3 doorSlideOffset = new Vector3(3f, 0f, 0f);
    
    [Tooltip("Speed at which the door slides")]
    [SerializeField] private float doorSlideSpeed = 2f;
    
    [Header("Audio Feedback")]
    [Tooltip("Sound played when an item is correctly placed")]
    [SerializeField] private AudioClip itemPlacedSound;
    
    [Tooltip("Sound played when puzzle is completed")]
    [SerializeField] private AudioClip puzzleCompleteSound;
    
    [Tooltip("Sound played when door opens")]
    [SerializeField] private AudioClip doorOpenSound;
    
    [Header("UI Feedback")]
    [Tooltip("UI Text showing progress (e.g., '5/10 Items Placed')")]
    [SerializeField] private UnityEngine.UI.Text progressText;
    
    [Tooltip("UI Panel shown when puzzle is completed")]
    [SerializeField] private GameObject completionPanel;
    
    // Private variables
    private int itemsPlaced = 0;
    private bool puzzleComplete = false;
    private bool doorOpening = false;
    private Vector3 doorClosedPosition;
    private Vector3 doorOpenPosition;
    private AudioSource audioSource;
    
    void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple SortingRoomManagers detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Get or add AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    void Start()
    {
        // Initialize door positions
        if (door != null)
        {
            doorClosedPosition = door.transform.position;
            doorOpenPosition = doorClosedPosition + doorSlideOffset;
        }
        
        // Auto-find shelf slots if not assigned
        if (shelfSlots.Count == 0)
        {
            shelfSlots.AddRange(FindObjectsOfType<ShelfSlot>());
            Debug.Log($"Auto-found {shelfSlots.Count} shelf slots");
        }
        
        // Update initial UI
        UpdateProgressUI();
        
        // Hide completion panel
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }
    }
    
    void Update()
    {
        // Handle door sliding animation
        if (doorOpening && door != null)
        {
            door.transform.position = Vector3.Lerp(
                door.transform.position,
                doorOpenPosition,
                doorSlideSpeed * Time.deltaTime
            );
            
            // Check if door has reached target position
            if (Vector3.Distance(door.transform.position, doorOpenPosition) < 0.01f)
            {
                door.transform.position = doorOpenPosition;
                doorOpening = false;
            }
        }
    }
    
    /// <summary>
    /// Called by SortableItem when it's successfully placed
    /// </summary>
    public void OnItemPlaced()
    {
        itemsPlaced++;
        
        Debug.Log($"Items placed: {itemsPlaced}/{totalItems}");
        
        // Play placement sound
        PlaySound(itemPlacedSound);
        
        // Update UI
        UpdateProgressUI();
        
        // Check if puzzle is complete
        if (itemsPlaced >= totalItems && !puzzleComplete)
        {
            OnPuzzleComplete();
        }
    }
    
    /// <summary>
    /// Called when all items are correctly placed
    /// </summary>
    private void OnPuzzleComplete()
    {
        puzzleComplete = true;
        
        Debug.Log("Sorting puzzle completed!");
        
        // Play completion sound
        PlaySound(puzzleCompleteSound);
        
        // Show completion UI
        if (completionPanel != null)
        {
            completionPanel.SetActive(true);
        }
        
        // Open the door after a short delay
        Invoke(nameof(OpenDoor), 1f);
    }
    
    /// <summary>
    /// Opens the door by starting the slide animation
    /// </summary>
    private void OpenDoor()
    {
        if (door == null)
        {
            Debug.LogWarning("Door reference is missing!");
            return;
        }
        
        doorOpening = true;
        
        // Play door open sound
        PlaySound(doorOpenSound);
        
        Debug.Log("Door is opening...");
    }
    
    /// <summary>
    /// Updates the progress UI text
    /// </summary>
    private void UpdateProgressUI()
    {
        if (progressText != null)
        {
            progressText.text = $"Items Sorted: {itemsPlaced}/{totalItems}";
        }
    }
    
    /// <summary>
    /// Plays an audio clip if available
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Resets the puzzle (useful for testing)
    /// </summary>
    public void ResetPuzzle()
    {
        itemsPlaced = 0;
        puzzleComplete = false;
        doorOpening = false;
        
        if (door != null)
        {
            door.transform.position = doorClosedPosition;
        }
        
        UpdateProgressUI();
        
        if (completionPanel != null)
        {
            completionPanel.SetActive(false);
        }
        
        Debug.Log("Puzzle reset");
    }
    
    /// <summary>
    /// Returns the current progress
    /// </summary>
    public int GetItemsPlaced()
    {
        return itemsPlaced;
    }
    
    /// <summary>
    /// Returns the total number of items
    /// </summary>
    public int GetTotalItems()
    {
        return totalItems;
    }
    
    /// <summary>
    /// Checks if the puzzle is complete
    /// </summary>
    public bool IsPuzzleComplete()
    {
        return puzzleComplete;
    }
}
