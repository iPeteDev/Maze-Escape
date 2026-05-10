using UnityEngine;

/// <summary>
/// UPDATED WireInteraction.cs - FIXES PREVIEW LINE COLOR ISSUE
/// 
/// Changes from original:
/// - Preview line now properly shows source endpoint color while dragging
/// - Material color is set correctly
/// - Uses Unlit/Color shader for proper color display
/// </summary>
public class WireInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactionDistance = 10f;
    [SerializeField] private LayerMask endpointLayer;

    [Header("Connection Settings")]
    [SerializeField] private GameObject wireConnectionPrefab;
    [SerializeField] private Transform wireConnectionParent;

    [Header("Visual Feedback")]
    [SerializeField] private bool showPreviewLine = true;
    [SerializeField] private Material previewLineMaterial;

    [Header("UI")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private UnityEngine.UI.Text interactionText;

    private Camera playerCamera;
    private WireEndpoint selectedEndpoint = null;
    private WireEndpoint hoveredEndpoint = null;
    private LineRenderer previewLine = null;
    private bool isDragging = false;

    void Start()
    {
        playerCamera = GetComponent<Camera>() ?? Camera.main;

        if (wireConnectionParent == null)
            wireConnectionParent = new GameObject("WireConnections").transform;

        if (showPreviewLine)
            CreatePreviewLine();

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    void Update()
    {
        HandleInput();
        PerformRaycast();

        if (isDragging && previewLine != null)
            UpdatePreviewLine();
    }

    /// <summary>
    /// Creates the preview LineRenderer with proper material setup
    /// FIXED: Now uses Unlit/Color shader for proper color display
    /// </summary>
    private void CreatePreviewLine()
    {
        GameObject previewObj = new GameObject("PreviewLine");
        previewObj.transform.SetParent(transform);
        previewLine = previewObj.AddComponent<LineRenderer>();

        // Basic setup
        previewLine.positionCount = 2;
        previewLine.startWidth = 0.03f;
        previewLine.endWidth = 0.03f;
        previewLine.useWorldSpace = true;

        // Material setup - FIXED!
        if (previewLineMaterial != null)
        {
            previewLine.material = previewLineMaterial;
        }
        else
        {
            // Create a simple unlit material that respects color
            Material mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = Color.white; // Will be overridden when dragging starts
            previewLine.material = mat;
        }

        // Start disabled
        previewLine.enabled = false;
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (hoveredEndpoint != null && !hoveredEndpoint.IsConnected())
                StartConnection(hoveredEndpoint);
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
                CompleteConnection();
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (hoveredEndpoint != null && hoveredEndpoint.IsConnected())
                DisconnectEndpoint(hoveredEndpoint);
        }
    }

    private void PerformRaycast()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (hoveredEndpoint != null && !isDragging)
        {
            hoveredEndpoint.Unhighlight();
            hoveredEndpoint = null;
        }

        if (Physics.Raycast(ray, out hit, interactionDistance, endpointLayer))
        {
            WireEndpoint endpoint = hit.collider.GetComponent<WireEndpoint>();
            if (endpoint != null)
            {
                hoveredEndpoint = endpoint;
                if (!isDragging)
                {
                    endpoint.Highlight();
                    ShowPrompt(endpoint.IsConnected() ? "Right Click to Disconnect" : "Left Click to Connect");
                }
            }
        }
        else if (!isDragging)
        {
            HidePrompt();
        }
    }

    /// <summary>
    /// Starts a wire connection from the selected endpoint
    /// FIXED: Properly sets preview line color to match source
    /// </summary>
    private void StartConnection(WireEndpoint endpoint)
    {
        selectedEndpoint = endpoint;
        isDragging = true;

        if (previewLine != null)
        {
            previewLine.enabled = true;

            // Get the color from the source endpoint
            Color wireColor = endpoint.GetWireColor();

            // Set both start and end color - FIXED!
            previewLine.startColor = wireColor;
            previewLine.endColor = wireColor;

            // Also set the material color - CRITICAL FIX!
            if (previewLine.material != null)
            {
                previewLine.material.color = wireColor;
            }

            Debug.Log($"Preview line color set to: {wireColor}");
        }
    }

    private void CompleteConnection()
    {
        if (selectedEndpoint != null && hoveredEndpoint != null &&
            selectedEndpoint.CanConnectTo(hoveredEndpoint))
        {
            CreateWireConnection(selectedEndpoint, hoveredEndpoint);
        }
        CancelConnection();
    }

    private void CancelConnection()
    {
        selectedEndpoint = null;
        isDragging = false;
        if (previewLine != null)
            previewLine.enabled = false;
    }

    private void CreateWireConnection(WireEndpoint source, WireEndpoint target)
    {
        WireEndpoint sourceEP = source.IsSource() ? source : target;
        WireEndpoint targetEP = source.IsSource() ? target : source;

        GameObject wireObj = Instantiate(wireConnectionPrefab, wireConnectionParent);
        WireConnection wire = wireObj.GetComponent<WireConnection>();

        if (wire != null)
        {
            wire.Initialize(sourceEP, targetEP);
            WireConnectionManager.Instance?.OnConnectionCreated(wire);
        }
    }

    private void DisconnectEndpoint(WireEndpoint endpoint)
    {
        LineRenderer line = endpoint.GetConnectionLine();
        if (line != null)
        {
            WireConnection wire = line.GetComponent<WireConnection>();
            if (wire != null)
            {
                WireConnectionManager.Instance?.OnConnectionRemoved(wire);
                wire.Disconnect();
            }
        }
    }

    private void UpdatePreviewLine()
    {
        Vector3 startPos = selectedEndpoint.GetConnectionPoint();
        Vector3 endPos;

        if (hoveredEndpoint != null && selectedEndpoint.CanConnectTo(hoveredEndpoint))
            endPos = hoveredEndpoint.GetConnectionPoint();
        else
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;
            endPos = Physics.Raycast(ray, out hit, interactionDistance) ? hit.point : ray.GetPoint(interactionDistance);
        }

        previewLine.SetPosition(0, startPos);
        previewLine.SetPosition(1, endPos);
    }

    private void ShowPrompt(string message)
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(true);
        if (interactionText != null)
            interactionText.text = message;
    }

    private void HidePrompt()
    {
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }
}