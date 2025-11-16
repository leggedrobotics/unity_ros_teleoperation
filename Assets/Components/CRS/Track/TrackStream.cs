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
    public Material lineMaterial;
    public Color trackOutline = Color.red;
    public float outlineWidth = 0.01f;
    public Color trackCenterLine = Color.green;
    public float centerLineWidth = 0.01f;
    private List<GameObject> lineObjects = new List<GameObject>();

    void Awake()
    {
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
            }
        }
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
}
