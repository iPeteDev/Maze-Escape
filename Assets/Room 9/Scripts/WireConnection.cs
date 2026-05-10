using UnityEngine;

/// <summary>
/// UPDATED WireConnection.cs - AUTO-DISCONNECTS INCORRECT CONNECTIONS
/// 
/// NEW FEATURE: If player connects wrong colors, wire automatically disconnects after delay
/// This prevents confusion and forces correct connections
/// </summary>
public class WireConnection : MonoBehaviour
{
    public WireEndpoint sourceEndpoint;
    public WireEndpoint targetEndpoint;

    [Header("Line Renderer Settings")]
    public float lineWidth = 0.05f;
    [Range(2, 50)] public int lineSegments = 20;
    [Range(0f, 5f)] public float wireSag = 0.5f;
    public Material wireMaterial;

    [Header("Animation")]
    public bool animateCreation = true;
    public float animationSpeed = 5f;

    [Header("Auto-Disconnect Settings")]
    [Tooltip("Auto-disconnect invalid connections?")]
    public bool autoDisconnectInvalid = true;

    [Tooltip("Delay before disconnecting invalid wire (seconds)")]
    public float disconnectDelay = 0.5f;

    [Tooltip("Flash invalid wire before disconnecting?")]
    public bool flashInvalidWire = true;

    private LineRenderer lineRenderer;
    private bool isValid = false;
    private float animationProgress = 0f;
    private bool isAnimating = false;
    private bool isScheduledForDisconnect = false;

    void Awake()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        ConfigureLineRenderer();
    }

    void Update()
    {
        if (sourceEndpoint != null && targetEndpoint != null)
            UpdateLinePositions();

        if (isAnimating && animateCreation)
            AnimateWireCreation();
    }

    private void ConfigureLineRenderer()
    {
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = lineSegments;
        lineRenderer.material = wireMaterial ?? new Material(Shader.Find("Sprites/Default"));
        lineRenderer.numCapVertices = 5;
        lineRenderer.numCornerVertices = 5;
        lineRenderer.useWorldSpace = true;
    }

    public void Initialize(WireEndpoint source, WireEndpoint target, bool animate = true)
    {
        sourceEndpoint = source;
        targetEndpoint = target;
        animateCreation = animate;

        if (lineRenderer != null && sourceEndpoint != null)
        {
            Color wireColor = sourceEndpoint.GetWireColor();
            lineRenderer.startColor = wireColor;
            lineRenderer.endColor = wireColor;
            lineRenderer.material.color = wireColor;
            lineRenderer.material.EnableKeyword("_EMISSION");
            lineRenderer.material.SetColor("_EmissionColor", wireColor * 0.3f);
        }

        // Validate connection
        isValid = sourceEndpoint.ValidateConnection(targetEndpoint);

        // Log validation result
        if (isValid)
        {
            Debug.Log($"<color=green>✓ Valid connection: {sourceEndpoint.endpointID} → {targetEndpoint.endpointID}</color>");
        }
        else
        {
            Debug.Log($"<color=red>✗ Invalid connection: {sourceEndpoint.endpointID} → {targetEndpoint.endpointID} (wrong colors!)</color>");
        }

        isAnimating = animateCreation;
        animationProgress = animateCreation ? 0f : 1f;

        sourceEndpoint?.ConnectTo(targetEndpoint, lineRenderer);
        targetEndpoint?.ConnectTo(sourceEndpoint, lineRenderer);

        UpdateLinePositions();

        // NEW: Auto-disconnect if invalid
        if (!isValid && autoDisconnectInvalid)
        {
            ScheduleAutoDisconnect();
        }
    }

    private void UpdateLinePositions()
    {
        Vector3 startPos = sourceEndpoint.GetConnectionPoint();
        Vector3 endPos = targetEndpoint.GetConnectionPoint();

        for (int i = 0; i < lineSegments; i++)
        {
            float t = i / (float)(lineSegments - 1);
            if (isAnimating) t *= animationProgress;

            Vector3 linearPos = Vector3.Lerp(startPos, endPos, t);
            float sagAmount = wireSag * (1f - Mathf.Pow(2f * t - 1f, 2f));
            linearPos.y -= sagAmount;

            lineRenderer.SetPosition(i, linearPos);
        }
    }

    private void AnimateWireCreation()
    {
        animationProgress += Time.deltaTime * animationSpeed;
        if (animationProgress >= 1f)
        {
            animationProgress = 1f;
            isAnimating = false;
            if (isValid)
                WireConnectionManager.Instance?.OnValidConnectionMade();
        }
    }

    /// <summary>
    /// NEW: Schedules auto-disconnect for invalid connections
    /// </summary>
    private void ScheduleAutoDisconnect()
    {
        if (isScheduledForDisconnect) return;

        isScheduledForDisconnect = true;

        // Show visual feedback if enabled
        if (flashInvalidWire)
        {
            StartCoroutine(FlashInvalidWire());
        }
        else
        {
            // Just disconnect after delay
            Invoke(nameof(AutoDisconnect), disconnectDelay);
        }
    }

    /// <summary>
    /// NEW: Flashes the wire red before disconnecting
    /// </summary>
    private System.Collections.IEnumerator FlashInvalidWire()
    {
        Color originalColor = sourceEndpoint.GetWireColor();
        Color flashColor = Color.red;

        float flashDuration = disconnectDelay;
        float flashSpeed = 4f; // Flashes per second
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            // Alternate between original color and red
            float t = Mathf.PingPong(Time.time * flashSpeed, 1f);
            Color currentColor = Color.Lerp(originalColor, flashColor, t);

            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = currentColor;
            lineRenderer.material.color = currentColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Disconnect after flashing
        AutoDisconnect();
    }

    /// <summary>
    /// NEW: Automatically disconnects this invalid wire
    /// </summary>
    private void AutoDisconnect()
    {
        if (!isValid)
        {
            Debug.Log($"<color=orange>Auto-disconnecting invalid wire: {sourceEndpoint.endpointID} → {targetEndpoint.endpointID}</color>");

            // Notify manager before destroying
            WireConnectionManager.Instance?.OnConnectionRemoved(this);

            // Disconnect
            Disconnect();
        }
    }

    public void Disconnect()
    {
        sourceEndpoint?.Disconnect();
        targetEndpoint?.Disconnect();
        Destroy(gameObject);
    }

    public bool IsValid() => isValid;
    public WireEndpoint GetSourceEndpoint() => sourceEndpoint;
    public WireEndpoint GetTargetEndpoint() => targetEndpoint;
}