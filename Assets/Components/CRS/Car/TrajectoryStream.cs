using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.Visualization;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using UnityEngine.UI;
using UnityEngine.Timeline;
using System;
using Unity.VectorGraphics;

public class TrajectoryStream : SensorStream
{
    // This class is used to manage the visualization of markers in Unity.
    private static int nextId = 0;
    private int _id;
    public float pointSize = 0.1f; // Default point size for point markers

    public TMPro.TextMeshProUGUI topicText;


    public Mesh arrowMesh;
    public GameObject pointsPrefab;

    private Dictionary<string, GameObject> _namespaces = new Dictionary<string, GameObject>();

    private delegate void UpdatePointSize(float size);
    private UpdatePointSize _updatePointSize;
    private bool _enabled = true;

    public enum MarkerAction
    {
        Add_modify,     // 0
        Deprecated,    // 1
        Delete,        // 2
        Delete_all,   // 3
    }

    void Awake()
    {
        _id = nextId++;
        // Initialize ROS connection
        _ros = ROSConnection.GetOrCreateInstance();

        GameObject sceneRootObj = GameObject.Find("SceneRoot");
        if (sceneRootObj != null)
        {
            transform.SetParent(sceneRootObj.transform);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        _msgType = "visualization_msgs/MarkerArray";
        _namespaces = new Dictionary<string, GameObject>();

        OnTopicChange("/controller_visualization");
    }


    bool Validate(MarkerMsg msg)
    {
        if (msg.id == (int)MarkerType.Text_view_facing || msg.id == (int)MarkerType.Mesh_resource || msg.id == (int)MarkerType.Triangle_list)
        {
            // Possibly log unsupported
            return false;
        }

        return true;
    }

    void OnMarker(MarkerMsg msg)
    {
        if (!_enabled)
        {
            // If the marker stream is not enabled, do not process the message
            return;
        }

        // Handle the received marker message
        // Get the marker type name from the enum
        string markerTypeName = Enum.GetName(typeof(MarkerType), msg.type);
        // Debug.Log($"Received marker with ID: {msg.id}, Type: {markerTypeName}");

        if (!Validate(msg))
        {
            return;
        }

        GameObject markerObject = null;
        if (msg.action == (int)MarkerAction.Delete || msg.action == (int)MarkerAction.Delete_all)
        {
            // If the action is delete, remove the marker
            if (_namespaces.TryGetValue(msg.ns, out markerObject))
            {
                Destroy(markerObject);
                _namespaces.Remove(msg.ns);
            }
            return; // Skip further processing for delete actions
        }

        string markerKey = $"{msg.ns}_{msg.id}";

        PointMsg[] points = msg.points;
        if (points == null || points.Length == 0)
        {
            points = new PointMsg[1];
            points[0] = new PointMsg(0, 0, 0); // Default point if none are provided
        }
        // else we want to add or modify the marker
        if (!_namespaces.TryGetValue(markerKey, out markerObject) || markerObject.GetComponent<MarkerPointStream>().markerType != (MarkerType)msg.type)
        {

            if (msg.type == (int)MarkerType.Text_view_facing || msg.type == (int)MarkerType.Mesh_resource || msg.type == (int)MarkerType.Triangle_list)
            {
                Debug.LogWarning($"Unsupported marker type: {markerTypeName}");
                return; // Skip unsupported types
            }



            if (markerObject == null)
            {
                Debug.Log($"Creating marker {markerKey} of type {markerTypeName} {markerObject}");

                markerObject = Instantiate(pointsPrefab);
                _updatePointSize += markerObject.GetComponent<MarkerPointStream>().OnSizeChange;
            }



            switch (msg.type)
            {
                case (int)MarkerType.Arrow:
                    markerObject.name = markerKey + "_Arrow";
                    markerObject.GetComponent<MarkerPointStream>().SetMesh(LidarUtils.MakeArrow(0.5f, 10));
                    markerObject.GetComponent<MarkerPointStream>().markerType = MarkerType.Arrow;
                    break;
                case (int)MarkerType.Cube:
                case (int)MarkerType.Cube_list:
                    // markerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    markerObject.name = markerKey + "_Cube";
                    markerObject.GetComponent<MarkerPointStream>().SetMesh(LidarUtils.MakeCube());
                    markerObject.GetComponent<MarkerPointStream>().markerType = MarkerType.Cube;
                    break;
                case (int)MarkerType.Sphere:
                case (int)MarkerType.Sphere_list:
                    // markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    markerObject.name = markerKey + "_Sphere";
                    markerObject.GetComponent<MarkerPointStream>().SetMesh(LidarUtils.MakeSphere(10));
                    markerObject.GetComponent<MarkerPointStream>().markerType = MarkerType.Sphere;
                    break;
                case (int)MarkerType.Cylinder:
                    // markerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    markerObject.name = markerKey + "_Cylinder";
                    markerObject.GetComponent<MarkerPointStream>().SetMesh(LidarUtils.MakeCylinder(10));
                    markerObject.GetComponent<MarkerPointStream>().markerType = MarkerType.Cylinder;
                    break;
                case (int)MarkerType.Line_strip:
                    markerObject.name = markerKey + "_LineStrip";
                    markerObject.GetComponent<MarkerPointStream>().markerType = MarkerType.Line_strip;
                    break;
                case (int)MarkerType.Line_list:
                    markerObject.name = markerKey + "_LineList";
                    markerObject.GetComponent<MarkerPointStream>().markerType = MarkerType.Line_list;
                    break;
                case (int)MarkerType.Points:
                    markerObject.name = markerKey + "_Points";
                    markerObject.GetComponent<MarkerPointStream>().SetMesh(LidarUtils.MakeSphere(4));
                    markerObject.GetComponent<MarkerPointStream>().markerType = MarkerType.Points;

                    break;
                default:
                    Debug.LogWarning($"Unsupported marker type: {markerTypeName}");
                    return; // Skip unsupported types
            }
            // markerObject.name = msg.ns;
            // markerObject.transform.SetParent(msg.header.frame_id != "" ? GameObject.Find(msg.header.frame_id).transform : transform);

            if (msg.lifetime.sec > 0)
            {
                Debug.Log($"Marker {msg.ns} will be destroyed after {msg.lifetime} seconds");
            }

            _namespaces[markerKey] = markerObject;

        }

        if (msg.colors.Length == 0)
        {
            // If no colors are provided, use a default color
            msg.colors = new ColorRGBAMsg[] { msg.color };
        }

        markerObject.GetComponent<IMarkerViz>().SetData(
            msg.pose,
            msg.scale,
            msg.colors,
            points
        );

        if (markerObject.transform.parent == null || markerObject.transform.parent.name != msg.header.frame_id)
        {
            markerObject.transform.SetParent(this.transform);
            markerObject.transform.localPosition = msg.pose.position.From<FLU>();
            markerObject.transform.localRotation = msg.pose.orientation.From<FLU>();
        }

    }

    void OnMarkerArray(MarkerArrayMsg msg)
    {
        if (!_enabled)
        {
            return;
        }

        // Process each marker in the array
        foreach (var marker in msg.markers)
        {
            OnMarker(marker);
        }
    }

    public void OnTopicSelected(int value)
    {

        if (value < 0 || value >= topicDropdown.options.Count)
        {
            Debug.LogWarning("Invalid topic selected: " + value);
            return;
        }

        string selectedTopic = topicDropdown.options[value].text;
        if (selectedTopic == "None")
        {
            OnTopicChange(null);
        }
        else
        {
            OnTopicChange(selectedTopic);
        }
    }


    public override void OnTopicChange(string topic)
    {
        Debug.Log($"Changing to topic: {topic}");
        if (topicName != null)
        {
            _ros.Unsubscribe(topicName);
            topicName = null;
            foreach (var ns in _namespaces.Values)
            {
                Destroy(ns);
            }
            _namespaces.Clear();
        }
        if (topic == null)
        {
            _enabled = false;
            topicText?.SetText("None");
            return;
        }
        _enabled = true;
        topicName = topic;
        topicText?.SetText(topic);
        _ros.Subscribe<MarkerArrayMsg>(topic, OnMarkerArray);
        Debug.Log("Subscribed to " + topic);
    }

    void OnDestroy()
    {
        if (topicName != null)
            _ros.Unsubscribe(topicName);

        foreach (var ns in _namespaces.Values)
        {
            Destroy(ns);
        }
        _namespaces.Clear();        
    }


    public override void ToggleTrack(int trackId)
    {
        throw new NotImplementedException("ToggleTrack method is not implemented.");
    }

    public void OnSizeChange(float size)
    {
        // Update the point size for all markers that support it
        if (_updatePointSize != null)
        {
            _updatePointSize(size);
        }
    }

    public void OnValidate()
    {
        if (pointSize < 0)
        {
            pointSize = 0f; // Reset to default value
        }
        if (_updatePointSize != null)
        {
            // Notify all point markers to update their size
            _updatePointSize(pointSize);
        }
    }
}
