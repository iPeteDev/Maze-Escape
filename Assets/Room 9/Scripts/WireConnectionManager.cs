using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WireConnectionManager : MonoBehaviour
{
    public static WireConnectionManager Instance { get; private set; }

    [Header("Puzzle Configuration")]
    [SerializeField] private List<WireEndpoint> sourceEndpoints = new List<WireEndpoint>();
    [SerializeField] private List<WireEndpoint> targetEndpoints = new List<WireEndpoint>();
    [SerializeField] private int requiredConnections = 5;

    [Header("Door Settings")]
    [SerializeField] private GameObject door;
    [SerializeField] private Vector3 doorSlideOffset = new Vector3(3f, 0f, 0f);
    [SerializeField] private float doorSlideSpeed = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip connectionSound;
    [SerializeField] private AudioClip puzzleCompleteSound;

    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Text progressText;
    [SerializeField] private GameObject completionPanel;

    private List<WireConnection> activeConnections = new List<WireConnection>();
    private int validConnectionCount = 0;
    private bool puzzleComplete = false;
    private bool doorOpening = false;
    private Vector3 doorClosedPosition;
    private Vector3 doorOpenPosition;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        if (door != null)
        {
            doorClosedPosition = door.transform.position;
            doorOpenPosition = doorClosedPosition + doorSlideOffset;
        }

        if (sourceEndpoints.Count == 0)
            AutoFindEndpoints();

        if (requiredConnections == 0)
            requiredConnections = Mathf.Min(sourceEndpoints.Count, targetEndpoints.Count);

        UpdateProgressUI();

        if (completionPanel != null)
            completionPanel.SetActive(false);
    }

    void Update()
    {
        if (doorOpening && door != null)
        {
            door.transform.position = Vector3.Lerp(door.transform.position, doorOpenPosition, doorSlideSpeed * Time.deltaTime);
            if (Vector3.Distance(door.transform.position, doorOpenPosition) < 0.01f)
            {
                door.transform.position = doorOpenPosition;
                doorOpening = false;
            }
        }
    }

    private void AutoFindEndpoints()
    {
        WireEndpoint[] all = FindObjectsOfType<WireEndpoint>();
        sourceEndpoints = all.Where(e => e.IsSource()).ToList();
        targetEndpoints = all.Where(e => !e.IsSource()).ToList();
    }

    public void OnConnectionCreated(WireConnection connection)
    {
        activeConnections.Add(connection);
        if (audioSource && connectionSound)
            audioSource.PlayOneShot(connectionSound);

        if (connection.IsValid())
            validConnectionCount++;

        UpdateProgressUI();
        CheckCompletion();
    }

    public void OnConnectionRemoved(WireConnection connection)
    {
        if (connection.IsValid())
            validConnectionCount--;
        activeConnections.Remove(connection);
        UpdateProgressUI();
    }

    public void OnValidConnectionMade()
    {
        UpdateProgressUI();
        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (!puzzleComplete && validConnectionCount >= requiredConnections)
        {
            puzzleComplete = true;
            if (audioSource && puzzleCompleteSound)
                audioSource.PlayOneShot(puzzleCompleteSound);
            if (completionPanel)
                completionPanel.SetActive(true);
            Invoke("OpenDoor", 1f);
        }
    }

    private void OpenDoor()
    {
        doorOpening = true;
    }

    private void UpdateProgressUI()
    {
        if (progressText != null)
            progressText.text = $"Connections: {validConnectionCount}/{requiredConnections}";
    }

    public void ResetPuzzle()
    {
        foreach (var conn in activeConnections.ToList())
            if (conn != null)
                conn.Disconnect();

        activeConnections.Clear();
        validConnectionCount = 0;
        puzzleComplete = false;
        doorOpening = false;

        if (door != null)
            door.transform.position = doorClosedPosition;

        UpdateProgressUI();
        if (completionPanel != null)
            completionPanel.SetActive(false);
    }
}