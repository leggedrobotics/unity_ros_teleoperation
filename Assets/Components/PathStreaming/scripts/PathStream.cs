using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Nav;

using RSL.Sensors.Lidar;

public class PathStream : SensorStream
{
    public TMPro.TextMeshProUGUI topicText;
    public TMPro.TextMeshProUGUI countText;

    public Material arrowMaterial;
    public Material lineMaterial;
    public Gradient lineGradient;

    private GraphicsBuffer _meshTriangles;
    private GraphicsBuffer _meshVertices;
    private GraphicsBuffer _ptData;
    private GraphicsBuffer _rotData;
    private RenderParams _renderParams;
    private Mesh _mesh;
    public Transform _parent;
    private bool _enabled = true;
    private Vector4[] _ptArray;
    private Vector4[] _rotArray;
    private LineRenderer _lineRenderer;
    private int _numPoints = 0;


    public int maxPoints = 1000;
    public float scale = 0.5f;
    public float arrowRadius = 0.25f;
    public int arrowSides = 10;
    public float lineWidth = 0.1f;

    public void Awake()
    {
        _msgType = "nav_msgs/Path";
        _ros = ROSConnection.GetOrCreateInstance();
        _mesh = LidarUtils.MakeArrow(arrowRadius, arrowSides);

        // Create empty child line renderer
        GameObject lineRendererObject = new GameObject("PathLineRenderer");
        lineRendererObject.transform.SetParent(transform);
        _lineRenderer = lineRendererObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 0;
        _lineRenderer.material = lineMaterial;
        _lineRenderer.colorGradient = lineGradient;
        _lineRenderer.widthMultiplier = lineWidth * scale;

        _meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _mesh.triangles.Length, 4);
        _meshTriangles.SetData(_mesh.triangles);
        _meshVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _mesh.vertices.Length, 12);
        _meshVertices.SetData(_mesh.vertices);
        _ptData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxPoints, 16);
        _rotData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxPoints, 16);

        _renderParams = new RenderParams(arrowMaterial);
        _renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 100);
        _renderParams.matProps = new MaterialPropertyBlock();

        _renderParams.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(new Vector3(0, 0, 0)));
        _renderParams.matProps.SetFloat("_PointSize", scale);
        _renderParams.matProps.SetBuffer("_PointData", _ptData);
        _renderParams.matProps.SetBuffer("_Positions", _meshVertices);
        _renderParams.matProps.SetBuffer("_RotationData", _rotData);

        topicDropdown.ClearOptions();
        topicDropdown.onValueChanged.AddListener((value) => { OnTopicSelected(value); });

        countText?.SetText("0");

        RefreshTopics();
    }

    private void Update()
    {
        if (_enabled && _mesh != null)
        {
            Transform t = _parent == null ? transform : _parent;
            _renderParams.matProps.SetMatrix("_ObjectToWorld", t.localToWorldMatrix);
            Graphics.RenderPrimitivesIndexed(_renderParams, MeshTopology.Triangles, _meshTriangles, _meshTriangles.count, (int)_mesh.GetIndexStart(0), _numPoints);
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

    private void OnValidate()
    {
        if (Application.isPlaying && _renderParams.matProps != null)
        {
            _renderParams.matProps.SetFloat("_PointSize", scale);
            _lineRenderer.widthMultiplier = lineWidth * scale;
        }

    }

    public override void OnTopicChange(string newTopic)
    {
        if (topicName != null)
        {
            _ros.Unsubscribe(topicName);
            topicName = null;
        }
        if (newTopic == null)
        {
            Debug.Log("Disabling Path display");
            _enabled = false;
            topicText?.SetText("None");
            return;
        }
        _enabled = true;
        topicName = newTopic;
        topicText?.SetText(newTopic);
        _ros.Subscribe<PathMsg>(newTopic, OnPath);
        Debug.Log("[PathStream] Subscribed to " + newTopic);
    }

    private void OnPath(PathMsg msg)
    {
        if (!_enabled)
            return;

        if (_parent == null || _parent.name != msg.header.frame_id)
        {
            Transform parent = GameObject.Find(msg.header.frame_id)?.transform;
            if (parent != null)
            {
                _parent = parent;
            }
            else
            {
                _parent = GameObject.FindWithTag("root")?.transform;
            }
            _lineRenderer.transform.SetParent(_parent);
        }

        _numPoints = Mathf.Min(msg.poses.Length, maxPoints);
        countText?.SetText(_numPoints.ToString());

        _lineRenderer.positionCount = _numPoints;
        _ptArray = new Vector4[Mathf.Max(1, _numPoints)];
        _rotArray = new Vector4[Mathf.Max(1, _numPoints)];
        for (int i = 0; i < _numPoints; i++)
        {
            Vector3 position = msg.poses[i].pose.position.From<FLU>();
            Quaternion rotation = msg.poses[i].pose.orientation.From<FLU>();

            _lineRenderer.SetPosition(i, position);
            _ptArray[i] = new Vector4(position.x, position.y, position.z, LidarUtils.PackRGBA(lineGradient.Evaluate((float)i / (float)(_numPoints - 1))));
            _rotArray[i] = new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);
        }

        _ptData.SetData(_ptArray);
        _rotData.SetData(_rotArray);

    }

    public void OnSizeUpdate(float size)
    {
        scale = size;
        _renderParams.matProps.SetFloat("_PointSize", size);
    }

    public void OnWidthUpdate(float width)
    {
        lineWidth = width;
        _lineRenderer.widthMultiplier = lineWidth * scale;
    }


    private void OnDestroy()
    {
        if (topicName != null)
            _ros.Unsubscribe(topicName);

        Destroy(_lineRenderer.gameObject);
        _meshTriangles?.Dispose();
        _meshTriangles = null;
        _meshVertices?.Dispose();
        _meshVertices = null;
        _ptData?.Dispose();
        _ptData = null;
        _rotData?.Dispose();
        _rotData = null;
    }

    public override void ToggleTrack(int mode)
    {
        throw new System.NotImplementedException();
    }

}
