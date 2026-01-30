using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

using RosMessageTypes.Std;
using RosMessageTypes.GridMap;
using RosMessageTypes.Geometry;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

using RSL.Sensors.Lidar;

public class GridMapStream : SensorStream
{

    public Dropdown colorDropdown;
    public Dropdown heightDropdown;


    public TextMeshProUGUI topicText;
    public float opacity = 1.0f;
    public Gradient gradient;
    public Material gridMapMaterial;

    public GameObject _parent;

    private Mesh _mesh;
    private string[] _layers;

    private bool _enabled = false;
    private GraphicsBuffer _gridData;
    private GraphicsBuffer _intensityData;
    private Material _material;
    private int _colorLayer = 0;
    private int _heightLayer = 0;
    private float[][] _data;
    public GameObject empty;
    private Matrix4x4 _transform = Matrix4x4.identity;

    void Awake()
    {
        _msgType = "grid_map_msgs/GridMap";
        _ros = ROSConnection.GetOrCreateInstance();
    }

    void Start()
    {

        empty = new GameObject("Empty");

        topicText?.SetText(topicName);
        topicDropdown?.ClearOptions();
        colorDropdown?.ClearOptions();
        heightDropdown?.ClearOptions();

        RefreshTopics();
        _enabled = true;


        GetComponent<MeshRenderer>().material = gridMapMaterial;
        _material = GetComponent<MeshRenderer>().material;

        // UpdateMesh(10, 10);

        colorDropdown.AddOptions(new List<string>(new string[] { "--" }));
        colorDropdown.onValueChanged.AddListener(OnColorChange);

        heightDropdown.AddOptions(new List<string>(new string[] { "--" }));
        heightDropdown.onValueChanged.AddListener(OnHeightChange);

        topicDropdown.onValueChanged.AddListener(OnTopicSelect);

        _material.SetTexture("_GradientTex", LidarUtils.GradientToTexture(gradient));
        _material.SetFloat("_Opacity", opacity);

    }


    private void UpdateMesh(int width, int height)
    {
        _mesh = LidarUtils.MakePlane(width, height);

        _gridData?.Dispose();
        _gridData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _mesh.vertices.Length, sizeof(float));

        _intensityData?.Dispose();
        _intensityData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _mesh.vertices.Length, sizeof(float));


        _material.SetBuffer("_GridData", _gridData);
        _material.SetBuffer("_IntensityData", _intensityData);

        _mesh.bounds = new Bounds(Vector3.zero, new Vector3(10000, 1000, 10000));

        GetComponent<MeshFilter>().mesh = _mesh;
    }



    void OnGridMapMessage(GridMapMsg message)
    {
        if (_data == null || message.layers.Length != _data.Length)
            _data = new float[message.layers.Length][];

        for (int i = 0; i < message.layers.Length; i++)
            _data[i] = message.data[i].data;

        if (_parent == null || _parent.name != message.info.header.frame_id)
            _parent = GameObject.Find(message.info.header.frame_id);


        // check if we have changed sizes
        if (_mesh == null || message.data[0].data.Length != _mesh.vertexCount)
        {
            Debug.Log("GridMap: Changing mesh size to " + message.data[0].layout.dim[1].size + "x" + message.data[0].layout.dim[0].size);
            UpdateMesh((int)message.data[0].layout.dim[1].size, (int)message.data[0].layout.dim[0].size);
        }

        _transform = transform.worldToLocalMatrix * _parent.transform.localToWorldMatrix * Matrix4x4.TRS(
            message.info.pose.position.From<FLU>(),
            message.info.pose.orientation.From<FLU>() * Quaternion.Euler(0, 90, 0),
            new Vector3((float)message.info.length_x, 1, (float)message.info.length_y)
        );

        Matrix4x4 center = _parent.transform.localToWorldMatrix * Matrix4x4.TRS(
            message.info.pose.position.From<FLU>(),
            message.info.pose.orientation.From<FLU>() * Quaternion.Euler(0, 90, 0),
            new Vector3((float)message.info.length_x, 1, (float)message.info.length_y)
        );



        _layers = message.layers;
        if (colorDropdown.options.Count != message.layers.Length)
        {
            colorDropdown.ClearOptions();
            colorDropdown.AddOptions(new List<string>(message.layers));
            colorDropdown.value = Mathf.Max(_colorLayer, 0);
        }
        if (heightDropdown.options.Count != message.layers.Length)
        {
            heightDropdown.ClearOptions();
            heightDropdown.AddOptions(new List<string>(message.layers));
            heightDropdown.value = Mathf.Max(_heightLayer, 0);
        }

        float min, max;
        min = float.MaxValue;
        max = float.MinValue;
        foreach (float v in _data[_colorLayer])
        {
            if (v < min) min = v;
            if (v > max) max = v;
        }

        _material.SetFloat("_IntensityMin", min);
        _material.SetFloat("_IntensityMax", max);

        _gridData.SetData(_data[_heightLayer]);
        _intensityData.SetData(_data[_colorLayer]);
        _material.SetMatrix("_ObjectToWorld", _transform);

        
        // Vector3 transformedPosition = center.MultiplyPoint3x4(Vector3.zero);
        // GetComponent<MeshFilter>().mesh.bounds = new Bounds(transformedPosition, new Vector3(100, 100, 100));

    }


    public void OnDestroy()
    {
        if (topicName != null)
            _ros.Unsubscribe(topicName);
        _gridData?.Dispose();
        _intensityData?.Dispose();
        _gridData = null;
        _intensityData = null;
    }

    public override void OnTopicChange(string topic)
    {
        if (topicName != null)
        {
            _ros.Unsubscribe(topicName);
            topicName = null;
        }
        if (topic == null)
        {
            Debug.Log("Disabling GridMap display");
            _enabled = false;
            topicText?.SetText("None");
            return;
        }
        _enabled = true;
        topicName = topic;
        topicText?.SetText(topic);
        _ros.Subscribe<GridMapMsg>(topic, OnGridMapMessage);
        Debug.Log("Subscribed to " + topic);
    }

    public void OnTopicSelect(int value)
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

    public void OnColorChange(int value)
    {
        if (_data == null || value < 0 || value >= _layers.Length)
        {
            Debug.LogWarning("Invalid layer selected: " + value);
            return;
        }

        string selectedLayer = _layers[value];
        _colorLayer = value;
        Debug.Log("Selected color layer: " + selectedLayer);
    }

    public void OnHeightChange(int value)
    {
        if (_data == null || value < 0 || value >= _layers.Length)
        {
            Debug.LogWarning("Invalid layer selected: " + value);
            return;
        }

        string selectedLayer = _layers[value];
        _heightLayer = value;
        Debug.Log("Selected height layer: " + selectedLayer);
    }

    public void OnOpacityChange(float opacity)
    {

        this.opacity = opacity;
        _material.SetFloat("_Opacity", opacity);
    }

    public void OnValidate()
    {
        if (Application.isPlaying && _material != null)
        {
            _material.SetTexture("_GradientTex", LidarUtils.GradientToTexture(gradient));
            _material.SetFloat("_Opacity", opacity);
        }
    }

    public override void ToggleTrack(int mode)
    {
        throw new System.NotImplementedException();
    }
}
