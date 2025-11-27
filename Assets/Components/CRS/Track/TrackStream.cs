using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;
using RosMessageTypes.Visualization;
using Unity.Mathematics;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

public class TrackStream : SensorStream
{

    [Header("Debug Corners")]
    [SerializeField] private GameObject debugMarkerPrefab;
    private GameObject ulDebugMarker;
    private GameObject lrDebugMarker;

    public static TrackStream Instance { get; private set; }
    
    public Material lineMaterial;
    public Color trackOutline = Color.red;
    public float outlineWidth = 0.01f;
    public Color trackCenterLine = Color.green;
    public float centerLineWidth = 0.01f;
    
    private List<GameObject> lineObjects = new List<GameObject>();
    
    // Bounding box properties
    private Bounds trackBounds;
    public Bounds TrackBounds => trackBounds;
    public bool HasTrackData { get; private set; } = false;
    
    private const float PLATE_SIZE = 0.3f;
    
void Awake()
{
    if (Instance == null)
    {
        Instance = this;
    }
    else
    {
        Debug.LogWarning("Multiple TrackStream instances detected. Destroying duplicate.");
        Destroy(gameObject);
        return;
    }
    
    // Auto-parent to SceneRoot if it exists
    GameObject sceneRootObj = GameObject.Find("SceneRoot");
    if (sceneRootObj != null)
    {
        transform.SetParent(sceneRootObj.transform);
        Debug.Log($"Track: TrackStream auto-parented to SceneRoot");
        Debug.Log($"Track: TrackStream parent is now: {transform.parent.name}");
    }
    else
    {
        Debug.LogWarning("Track: SceneRoot not found! TrackStream will not be aligned.");
    }
    
    _ros = ROSConnection.GetOrCreateInstance();
}
    
    void Start()
    {
        _msgType = "visualization_msgs/MarkerArray";
        _ros.Subscribe<MarkerArrayMsg>(topicName, OnTrackMessage);
        Debug.Log("Subscribed to /track");
    }
    
    public override void OnTopicChange(string newTopic)
    {
        if (!string.IsNullOrEmpty(topicName))
        {
            _ros.Unsubscribe(topicName);
        }
        topicName = newTopic;
        if (!string.IsNullOrEmpty(topicName) && topicName != "None")
        {
            _ros.Subscribe<MarkerArrayMsg>(topicName, OnTrackMessage);
        }
    }
    
    private void OnTrackMessage(MarkerArrayMsg track)
    {
        foreach (GameObject lineObj in lineObjects)
        {
            Destroy(lineObj);
        }
        lineObjects.Clear();
        
        List<Vector3> allTrackPoints = new List<Vector3>();
        
        foreach (var marker in track.markers)
        {
            if (marker.ns != "track_boundary")
            {
                // Center line
                CreateLine(marker.points, 0, marker.points.Length, trackCenterLine, centerLineWidth);
                continue;
            }
            
            if (marker.points != null && marker.points.Length > 0)
            {
                // Track outlines
                int halfSize = marker.points.Length / 2;
                CreateLine(marker.points, 0, halfSize, trackOutline, outlineWidth);
                CreateLine(marker.points, halfSize, marker.points.Length, trackOutline, outlineWidth);
                
                // Add outline points to bounding box calculation
                for (int i = 0; i < marker.points.Length; i++)
                {
                    allTrackPoints.Add(marker.points[i].From<FLU>());
                }
            }
        }
        
        // Calculate padded bounding box
        if (allTrackPoints.Count > 0)
        {
            CalculatePaddedBounds(allTrackPoints.ToArray());
        }
    }
    
    private void CalculatePaddedBounds(Vector3[] trackPoints)
    {
        // Find raw min/max
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        
        foreach (var point in trackPoints)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }
        
        // Extend to plate bounds
        min.x = Mathf.Floor(min.x / PLATE_SIZE) * PLATE_SIZE;
        min.z = Mathf.Floor(min.z / PLATE_SIZE) * PLATE_SIZE;
        
        max.x = Mathf.Ceil(max.x / PLATE_SIZE) * PLATE_SIZE;
        max.z = Mathf.Ceil(max.z / PLATE_SIZE) * PLATE_SIZE;
        
        min.y = 0;
        max.y = 0;
        
        trackBounds = new Bounds();
        trackBounds.SetMinMax(min, max);
        
        HasTrackData = true;
        
        Debug.Log($"Track bounds calculated: Min={min}, Max={max}");
        Debug.Log($"Track dimensions: {max.x - min.x}m x {max.z - min.z}m");
        Debug.Log($"Number of plates: X={Mathf.RoundToInt((max.x - min.x) / PLATE_SIZE)}, Z={Mathf.RoundToInt((max.z - min.z) / PLATE_SIZE)}");

        UpdateDebugMarkers();
    }
    
    private void CreateLine(PointMsg[] points, int startIdx, int endIdx, Color color, float width)
    {
        GameObject lineObj = new GameObject("TrackBoundaryLine");
        lineObj.transform.SetParent(transform);
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.positionCount = endIdx - startIdx;

        lineRenderer.useWorldSpace = false;
        
        for (int i = startIdx; i < endIdx; i++)
        {
            Vector3 position = points[i].From<FLU>();
            lineRenderer.SetPosition(i - startIdx, position);
        }
        
        lineObjects.Add(lineObj);
    }
    
    public override void ToggleTrack(int mode)
    {
        _trackingState = mode;
    }
    
    // Helper methods for QRCodeAlignment to get corners
    public Vector3 GetUpperLeftCorner()
    {
        return new Vector3(trackBounds.max.x, 0, trackBounds.max.z);
    }
    
    public Vector3 GetLowerRightCorner()
    {
        return new Vector3(trackBounds.min.x, 0, trackBounds.min.z);
    }

    private void UpdateDebugMarkers()
    {
        if (debugMarkerPrefab == null)
        {
            Debug.LogWarning("Debug Marker Prefab not assigned. Cannot show corners.");
            return;
        }

        Vector3 ulPos = GetUpperLeftCorner();
        Vector3 lrPos = GetLowerRightCorner();
        
        Transform trackTransform = transform; 
        
        if (ulDebugMarker == null)
        {
            ulDebugMarker = Instantiate(debugMarkerPrefab, trackTransform);
            ulDebugMarker.name = "DEBUG_UL_Corner (Red)";
            // Set the color
            Renderer ulRenderer = ulDebugMarker.GetComponent<Renderer>();
            if (ulRenderer != null) ulRenderer.material.color = Color.red;
        }
        ulDebugMarker.transform.localPosition = ulPos;

        if (lrDebugMarker == null)
        {
            lrDebugMarker = Instantiate(debugMarkerPrefab, trackTransform);
            lrDebugMarker.name = "DEBUG_LR_Corner (Blue)";
            // Set the color
            Renderer lrRenderer = lrDebugMarker.GetComponent<Renderer>();
            if (lrRenderer != null) lrRenderer.material.color = Color.blue;
        }
        lrDebugMarker.transform.localPosition = lrPos;
        
        Debug.Log($"Debug Markers updated. UL: {ulPos} (Red), LR: {lrPos} (Blue)");
    }
}