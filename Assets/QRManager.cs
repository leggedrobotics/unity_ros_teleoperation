using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

public class QRManager : MonoBehaviour
{
    [Header("QR Code Visualization")]
    [SerializeField] private GameObject qrVisualizationPrefab;
    
    private int activeCount = 0;

    public void OnTrackableAdded(MRUKTrackable trackable)
    {
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
        {
            return;
        }

        Debug.Log("========================================");
        Debug.Log($"QR Code tracked! UUID: {trackable.Anchor.Uuid}");
        Debug.Log($"Position: {trackable.transform.position}");
        Debug.Log($"Payload: {trackable.MarkerPayloadString}");
        
        // Instantiate the prefab as a child of the trackable
        if (qrVisualizationPrefab != null)
        {
            GameObject instance = Instantiate(qrVisualizationPrefab, trackable.transform);
            instance.transform.localPosition = new Vector3(0, 0.1f, 0);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            
            activeCount++;
            
            Debug.Log($"Visualization instantiated! Active count: {activeCount}");
            Debug.Log($"Instance position: {instance.transform.position}");
        }
        else
        {
            Debug.LogError("qrVisualizationPrefab is NULL! Please assign it in the Inspector!");
        }
        
        Debug.Log("========================================");
    }

    public void OnTrackableRemoved(MRUKTrackable trackable)
    {
        if (trackable.TrackableType != OVRAnchor.TrackableType.QRCode)
        {
            return;
        }

        Debug.Log($"QR Code removed: {trackable.Anchor.Uuid}");
        activeCount--;
        Debug.Log($"Active count: {activeCount}");
    }
}