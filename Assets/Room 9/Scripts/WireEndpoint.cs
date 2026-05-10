using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WireEndpoint : MonoBehaviour
{
    [Header("Endpoint Configuration")]
    public string endpointID;
    public bool isSource = true;
    public Color wireColor = Color.white;

    [Header("Visual Settings")]
    public GameObject endpointVisual;
    public Material highlightMaterial;
    [Range(1f, 2f)] public float highlightScale = 1.2f;

    private bool isConnected = false;
    private WireEndpoint connectedEndpoint = null;
    private LineRenderer connectionLine = null;
    private bool isHighlighted = false;
    private Vector3 originalScale;
    private Material originalMaterial;
    private Renderer endpointRenderer;

    void Awake()
    {
        if (endpointVisual != null)
        {
            endpointRenderer = endpointVisual.GetComponent<Renderer>();
            originalScale = endpointVisual.transform.localScale;
            if (endpointRenderer != null)
                originalMaterial = endpointRenderer.material;
        }
    }

    void Start()
    {
        ApplyWireColor();
    }

    private void ApplyWireColor()
    {
        if (endpointRenderer != null)
        {
            Material mat = endpointRenderer.material;
            mat.color = wireColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", wireColor * 0.5f);
        }
    }

    public void Highlight()
    {
        if (isHighlighted || isConnected) return;
        isHighlighted = true;

        if (endpointVisual != null)
        {
            endpointVisual.transform.localScale = originalScale * highlightScale;
            if (highlightMaterial != null && endpointRenderer != null)
                endpointRenderer.material = highlightMaterial;
        }
    }

    public void Unhighlight()
    {
        if (!isHighlighted) return;
        isHighlighted = false;

        if (endpointVisual != null)
        {
            endpointVisual.transform.localScale = originalScale;
            if (endpointRenderer != null && originalMaterial != null)
            {
                endpointRenderer.material = originalMaterial;
                ApplyWireColor();
            }
        }
    }

    public void ConnectTo(WireEndpoint target, LineRenderer line)
    {
        isConnected = true;
        connectedEndpoint = target;
        connectionLine = line;
        Unhighlight();
    }

    public void Disconnect()
    {
        isConnected = false;
        connectedEndpoint = null;
        connectionLine = null;
    }

    public bool CanConnectTo(WireEndpoint target)
    {
        if (target == this) return false;
        if (isConnected || target.isConnected) return false;
        if (isSource && target.isSource) return false;
        if (!isSource && !target.isSource) return false;
        return true;
    }

    public bool ValidateConnection(WireEndpoint target)
    {
        float tolerance = 0.1f;
        return Mathf.Abs(wireColor.r - target.wireColor.r) < tolerance &&
               Mathf.Abs(wireColor.g - target.wireColor.g) < tolerance &&
               Mathf.Abs(wireColor.b - target.wireColor.b) < tolerance;
    }

    public Vector3 GetConnectionPoint() => transform.position;
    public Color GetWireColor() => wireColor;
    public bool IsConnected() => isConnected;
    public WireEndpoint GetConnectedEndpoint() => connectedEndpoint;
    public LineRenderer GetConnectionLine() => connectionLine;
    public bool IsSource() => isSource;
}