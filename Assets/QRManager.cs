using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

public class QRCodeAlignment : MonoBehaviour
{
    [Header("Runtime Visualization")]
    [SerializeField] private GameObject qrMarkerPrefab;
    [SerializeField] private Color ulColor = Color.blue;
    [SerializeField] private Color lrColor = Color.red;

    private GameObject ulMarker;
    private GameObject lrMarker;
    private LineRenderer connectionLine;

    [Header("Alignment Settings")]
    [SerializeField] private Transform sceneRoot; // The root object to transform
    [SerializeField] private bool autoAlign = true;
    [SerializeField] private bool debugVisualization = true;
    
    [Header("Debug Info")]
    [SerializeField] private bool isAligned = false;
    [SerializeField] private Vector3 debugTranslation;
    [SerializeField] private float debugRotation;
    [SerializeField] private float debugScale;
    
    // QR code tracking
    private Dictionary<string, MRUKTrackable> detectedQRCodes = new Dictionary<string, MRUKTrackable>();
    
    // Transformation components
    private Vector3 translation;
    private Quaternion rotation;
    private float scale;
    
    void Start()
    {
        if (sceneRoot == null)
        {
            Debug.LogError("QRCodeAlignment: sceneRoot is not assigned! Please assign the root transform to align.");
        }
    }
    
    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
        {
            return;
        }
        
        string payload = trackable.MarkerPayloadString;
        
        // Only track UL and LR codes
        if (payload == "UL" || payload == "LR")
        {
            detectedQRCodes[payload] = trackable;
            Debug.Log($"QR: QR Code '{payload}' detected at position: {trackable.transform.position}");
            
            // Create visual marker
            CreateOrUpdateMarker(payload, trackable.transform.position);
            
            // Try to align if we have both codes
            if (autoAlign && detectedQRCodes.ContainsKey("UL") && detectedQRCodes.ContainsKey("LR"))
            {
                CalculateAndApplyAlignment();
                UpdateConnectionLine();
            }
        }
    }
    
    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
        {
            return;
        }
        
        string payload = trackable.MarkerPayloadString;
        
        if (detectedQRCodes.ContainsKey(payload))
        {
            detectedQRCodes.Remove(payload);
            Debug.Log($"QR: QR Code '{payload}' removed");
            
            // Remove marker
            if (payload == "UL" && ulMarker != null)
            {
                Destroy(ulMarker);
                ulMarker = null;
            }
            else if (payload == "LR" && lrMarker != null)
            {
                Destroy(lrMarker);
                lrMarker = null;
            }
            
            // Remove line if either marker is gone
            if (connectionLine != null && (ulMarker == null || lrMarker == null))
            {
                Destroy(connectionLine.gameObject);
                connectionLine = null;
            }
            
            isAligned = false;
        }
    }
    
    public void CalculateAndApplyAlignment()
    {
        // Check if we have track data
        if (TrackStream.Instance == null || !TrackStream.Instance.HasTrackData)
        {
            Debug.LogWarning("QR: Track data not available yet. Cannot align.");
            return;
        }
        
        // Check if we have both QR codes
        if (!detectedQRCodes.ContainsKey("UL") || !detectedQRCodes.ContainsKey("LR"))
        {
            Debug.LogWarning("QR: Both UL and LR QR codes must be detected for alignment.");
            return;
        }
        
        // Get virtual (track) coordinates
        Vector3 virtualUL = TrackStream.Instance.GetUpperLeftCorner();
        Vector3 virtualLR = TrackStream.Instance.GetLowerRightCorner();
        
        // Get physical (QR code) coordinates
        Vector3 physicalUL = detectedQRCodes["UL"].transform.position;
        Vector3 physicalLR = detectedQRCodes["LR"].transform.position;
        
        Debug.Log($"QR: Virtual UL: {virtualUL}, LR: {virtualLR}");
        Debug.Log($"QR: Physical UL: {physicalUL}, LR: {physicalLR}");
        
        // Calculate transformation
        CalculateTransformation(virtualUL, virtualLR, physicalUL, physicalLR);
        
        // Apply transformation
        ApplyTransformation();
        
        isAligned = true;
        Debug.Log("QR: Alignment complete!");
    }
    
    private void CalculateTransformation(Vector3 virtualUL, Vector3 virtualLR, Vector3 physicalUL, Vector3 physicalLR)
    {
        // Scale
        float virtualDistance = Vector3.Distance(virtualUL, virtualLR);
        float physicalDistance = Vector3.Distance(physicalUL, physicalLR);
        scale = physicalDistance / virtualDistance;
        
        Debug.Log($"Virtual distance: {virtualDistance:F3}m, Physical distance: {physicalDistance:F3}m");
        Debug.Log($"Calculated scale: {scale:F3}");
        
        // 2. Rotation
        Vector3 virtualDirection = (virtualLR - virtualUL).normalized;
        Vector3 physicalDirection = (physicalLR - physicalUL).normalized;
        
        // Project to XZ plane
        virtualDirection.y = 0;
        physicalDirection.y = 0;
        virtualDirection.Normalize();
        physicalDirection.Normalize();
        
        float yaw = Vector3.SignedAngle(virtualDirection, physicalDirection, Vector3.up);
        rotation = Quaternion.Euler(0, yaw, 0);
        
        Debug.Log($"QR: Virtual direction: {virtualDirection}, Physical direction: {physicalDirection}");
        Debug.Log($"QR: Calculated rotation: {yaw:F2}°");
        
        // Translation
        Vector3 scaledRotatedUL = rotation * (virtualUL * scale);
        translation = physicalUL - scaledRotatedUL;
        
        Debug.Log($"QR: Scaled/Rotated UL: {scaledRotatedUL}");
        Debug.Log($"QR: Calculated translation: {translation}");
        
        // Store debug values
        debugTranslation = translation;
        debugRotation = yaw;
        debugScale = scale;
    }
    
    private void ApplyTransformation()
    {
        if (sceneRoot == null)
        {
            Debug.LogError("QR: Cannot apply transformation: sceneRoot is not assigned!");
            return;
        }

        Debug.Log($"QR: BEFORE transformation - SceneRoot position: {sceneRoot.position}, rotation: {sceneRoot.rotation.eulerAngles}, scale: {sceneRoot.localScale}");
        
        sceneRoot.localScale = Vector3.one * scale;
        sceneRoot.rotation = rotation;
        sceneRoot.position = translation;

        Debug.Log($"QR: AFTER transformation - SceneRoot position: {sceneRoot.position}, rotation: {sceneRoot.rotation.eulerAngles}, scale: {sceneRoot.localScale}");
        Debug.Log($"QR: Applied transformation to {sceneRoot.name}");
        
        // Validation check
        ValidateAlignment();
    }
    
    private void ValidateAlignment()
    {
        if (TrackStream.Instance == null) return;
        
        Vector3 virtualLR = TrackStream.Instance.GetLowerRightCorner();
        Vector3 physicalLR = detectedQRCodes["LR"].transform.position;
        
        // Transform virtualLR through sceneRoot
        Vector3 predictedPhysicalLR = sceneRoot.TransformPoint(virtualLR);
        
        float error = Vector3.Distance(predictedPhysicalLR, physicalLR);
        Debug.Log($"QR: Alignment validation error: {error:F4}m");
        
        if (error > 0.01f)
        {
            Debug.LogWarning($"QR: Alignment error is high ({error:F3}m). Expected < 0.01m");
        }
        else
        {
            Debug.Log("QR: ✓ Alignment validation passed!");
        }
    }
    
    // Manual alignment trigger (useful for testing)
    [ContextMenu("Trigger Alignment")]
    public void TriggerAlignment()
    {
        CalculateAndApplyAlignment();
    }
    
    // Reset alignment
    [ContextMenu("Reset Alignment")]
    public void ResetAlignment()
    {
        if (sceneRoot != null)
        {
            sceneRoot.localScale = Vector3.one;
            sceneRoot.rotation = Quaternion.identity;
            sceneRoot.position = Vector3.zero;
        }
        
        isAligned = false;
        Debug.Log("QR: Alignment reset");
    }

    private void CreateOrUpdateMarker(string qrCode, Vector3 position)
    {
        if (qrMarkerPrefab == null) return;
        
        GameObject marker = null;
        Color color = qrCode == "UL" ? ulColor : lrColor;
        
        if (qrCode == "UL")
        {
            if (ulMarker == null)
            {
                ulMarker = Instantiate(qrMarkerPrefab, position, Quaternion.identity);
                ulMarker.name = "UL_Marker";
                SetMarkerColor(ulMarker, color);
            }
            else
            {
                ulMarker.transform.position = position;
            }
        }
        else if (qrCode == "LR")
        {
            if (lrMarker == null)
            {
                lrMarker = Instantiate(qrMarkerPrefab, position, Quaternion.identity);
                lrMarker.name = "LR_Marker";
                SetMarkerColor(lrMarker, color);
            }
            else
            {
                lrMarker.transform.position = position;
            }
        }
    }
    private void SetMarkerColor(GameObject marker, Color color)
    {
        Renderer renderer = marker.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private void UpdateConnectionLine()
    {
        if (!detectedQRCodes.ContainsKey("UL") || !detectedQRCodes.ContainsKey("LR"))
            return;
        
        if (connectionLine == null)
        {
            GameObject lineObj = new GameObject("QR_ConnectionLine");
            connectionLine = lineObj.AddComponent<LineRenderer>();
            connectionLine.startWidth = 0.01f;
            connectionLine.endWidth = 0.01f;
            connectionLine.material = new Material(Shader.Find("Sprites/Default"));
            connectionLine.startColor = Color.yellow;
            connectionLine.endColor = Color.yellow;
            connectionLine.positionCount = 2;
        }
        
        connectionLine.SetPosition(0, detectedQRCodes["UL"].transform.position);
        connectionLine.SetPosition(1, detectedQRCodes["LR"].transform.position);
    }
}