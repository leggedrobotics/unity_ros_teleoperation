using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Rendering;
using Unity.VisualScripting;



#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(LidarStream))]
public class LidarStreamEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();


        LidarStream myScript = (LidarStream)target;
        if (GUILayout.Button("Toggle Enabled"))
        {
            myScript.ToggleEnabled();
        }
        if (GUILayout.Button("Refresh Topics"))
        {
            myScript.RefreshTopics();
        }
        if (GUILayout.Button("Select 0"))
        {
            myScript.OnTopicSelect(0);
        }
        if (GUILayout.Button("Select 1"))
        {
            myScript.OnTopicSelect(1);
        }
        if (GUILayout.Button("Set color to RGB"))
        {
            myScript.OnColorSelect(0);
        }
        if (GUILayout.Button("Set color to Intensity"))
        {
            myScript.OnColorSelect(1);
        }
        if (GUILayout.Button("Set color to Z"))
        {
            myScript.OnColorSelect(2);
        }
        if (GUILayout.Button("Clear"))
        {
            myScript.Clear();
        }
        GUILayout.Label("Number of Points: " + myScript._numPts);
    }
}
#endif


public enum ColorMode
{
    RGB,
    Intensity,
    Z
}


public enum VizType
{
    Lidar = 0,
    RGBDMesh = 1,
    RGBD = 2,
    Splat = 4,
}

public static class VizTypeExtensions
{
    public static int GetFieldCount(this VizType vizType)
    {
        switch (vizType)
        {
            case VizType.Lidar:
            case VizType.RGBD:
                return 4;
            case VizType.RGBDMesh:
                return 5;
            case VizType.Splat:
                return 18;
            default:
                return 4;
        }
    }

    public static int GetSize(this VizType vizType)
    {
        return vizType.GetFieldCount() * 4;
    }
}


public class LidarStream : SensorStream
{
    public Material point_material;

    GraphicsBuffer _meshTriangles;
    GraphicsBuffer _meshVertices;
    GraphicsBuffer _ptData;

    public bool useTF = true;
    public float scale = 1.0f;
    public int maxPts = 30_000_000;
    public int displayPts = 10;
    public int sides = 3;
    private RenderParams renderParams;
    public VizType vizType = VizType.Lidar;

    public ColorMode colorMode = ColorMode.Intensity;
    public Color intensityMin = Color.black;
    public Color intensityMax = Color.white;

    public Slider densitySlider;
    public Slider sizeSlider;
    public Dropdown colorModeDropdown;

    public TextMeshProUGUI debugText;
    public TextMeshProUGUI topicText;


    private Mesh mesh;
    private LidarSpawner _lidarSpawner;
    public bool _enabled = false;
    public GameObject _parent;
    private bool _missingParent = false;
    private bool rgbd = false;
    public int _numPts = 0;
    private int _trackingMode = 0; // 0: None, 1: Camera

    private LocalKeyword _rgbdKeyword;
    private LocalKeyword _intensityKeyword;
    private LocalKeyword _zKeyword;

    public GameObject p;


    void Awake()
    {
        _msgType = "sensor_msgs/PointCloud2";
        _ros = ROSConnection.GetOrCreateInstance();

        mesh = LidarUtils.MakePolygon(sides);

        _meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.triangles.Length, 4);
        _meshTriangles.SetData(mesh.triangles);
        _meshVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.vertices.Length, 12);
        _meshVertices.SetData(mesh.vertices);
        _ptData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxPts, vizType.GetSize());


        renderParams = new RenderParams(point_material);

        renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 100);
        renderParams.matProps = new MaterialPropertyBlock();

        renderParams.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(new Vector3(0, 0, 0)));
        renderParams.matProps.SetFloat("_PointSize", scale);
        renderParams.matProps.SetBuffer("_PointData", _ptData);
        renderParams.matProps.SetInt("_BaseVertexIndex", (int)mesh.GetBaseVertex(0));
        renderParams.matProps.SetBuffer("_Positions", _meshVertices);

        _rgbdKeyword = new LocalKeyword(renderParams.material.shader, "COLOR_RGB");
        _intensityKeyword = new LocalKeyword(renderParams.material.shader, "COLOR_INTENSITY");
        _zKeyword = new LocalKeyword(renderParams.material.shader, "COLOR_Z");

        SetColorMode(renderParams.material, _intensityKeyword);

        if (colorModeDropdown != null)
        {
            colorModeDropdown.ClearOptions();
            List<string> colorOptions = new List<string>
            {
                "RGB",
                "Intensity",
                "Z"
            };
            colorModeDropdown.AddOptions(colorOptions);
            colorModeDropdown.onValueChanged.AddListener(OnColorSelect);
        }


        debugText?.SetText("--");

        RefreshTopics();
        if (topicDropdown != null && densitySlider != null && sizeSlider != null)
        {
            topicDropdown.onValueChanged.AddListener(OnTopicSelect);
            densitySlider.onValueChanged.AddListener(OnDensityChange);
            sizeSlider.onValueChanged.AddListener(OnSizeChange);
            densitySlider.value = (float)displayPts / maxPts;
        }

        if ((_lidarSpawner = GetComponent<LidarSpawner>()) != null)
        {
            _lidarSpawner.PointCloudGenerated += OnPointcloud;
        }

        if (_enabled && !string.IsNullOrEmpty(topicName))
        {
            _ros.Subscribe<PointCloud2Msg>(topicName, OnPointcloud);
        }
    }

    public bool CleanTF(string name)
    {
        GameObject target = GameObject.Find(name);

        if (target == null)
        {
            return false;
        }


        Debug.Log("Cleaning " + name + " " + target);

        List<GameObject> children = new List<GameObject>();

        // check if this is connected to root
        int count = 0;
        while (target.transform.parent != null)
        {
            count++;
            children.Add(target);
            target = target.transform.parent.gameObject;
            if (target.name == "odom")
            {
                children.Clear();
                Debug.Log("Connected to root");
                return true;
            }
            if (count > 1000)
            {
                Debug.LogWarning("Too many iterations");
                return false;
            }
        }

        foreach (GameObject child in children)
        {
            Destroy(child);
        }
        return false;
    }

    void UpdatePose(string frame)
    {
        // if(!CleanTF(frame))
        // {
        //     // return;
        // }
        p = GameObject.Find(frame);
        _parent = GameObject.Find(frame);
        if (_parent == null)
        {
            // The parent object doesn't exist yet, so we place this object at the origin
            _parent = GameObject.FindWithTag("root");
            _missingParent = true;
        }

        // transform.parent = _parent.transform;
        // transform.localPosition = Vector3.zero;
        // transform.localRotation = Quaternion.Euler(-90, 90, 0);
        // transform.localScale = new Vector3(-1, 1, 1);
    }

    private void OnValidate()
    {
        if (renderParams.matProps != null)
        {
            renderParams.matProps.SetFloat("_PointSize", scale);
            if (colorMode == ColorMode.RGB)
            {
                SetColorMode(renderParams.material, _rgbdKeyword);
            }
            else if (colorMode == ColorMode.Intensity)
            {
                SetColorMode(renderParams.material, _intensityKeyword);
            }
            else if (colorMode == ColorMode.Z)
            {
                SetColorMode(renderParams.material, _zKeyword);
            }
            renderParams.matProps.SetColor("_ColorMin", intensityMin);
            renderParams.matProps.SetColor("_ColorMax", intensityMax);

        }
        if (displayPts > maxPts)
        {
            displayPts = maxPts;
        }
        if (displayPts < 0)
        {
            displayPts = 0;
        }
    }

    private void OnDestroy()
    {
        if (topicName != null)
            _ros.Unsubscribe(topicName);
        _meshTriangles?.Dispose();
        _meshTriangles = null;
        _meshVertices?.Dispose();
        _meshVertices = null;
        _ptData?.Dispose();
        _ptData = null;
    }

    private void Update()
    {
        if (_enabled)
        {
            Transform parentTransform = _parent != null && useTF ? _parent.transform : transform;
            Matrix4x4 localToWorldMatrix = parentTransform.localToWorldMatrix;
            if (!useTF)
            {
                localToWorldMatrix = Matrix4x4.Translate(new Vector3(0, -1, 0)) * localToWorldMatrix;
            }
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(-90, 90, 0));
            Matrix4x4 inversionMatrix = Matrix4x4.Scale(new Vector3(-1, 1, 1));
            Matrix4x4 transformationMatrix = localToWorldMatrix * rotationMatrix * inversionMatrix;
            renderParams.matProps.SetMatrix("_ObjectToWorld", transformationMatrix);
            Graphics.RenderPrimitivesIndexed(renderParams, MeshTopology.Triangles, _meshTriangles, _meshTriangles.count, (int)mesh.GetIndexStart(0), _numPts);
        }
    }

    public void OnPointcloud(PointCloud2Msg pointCloud)
    {
        if (_parent == null || _parent.name != pointCloud.header.frame_id)
        {
            UpdatePose(pointCloud.header.frame_id);
        }
        if (pointCloud.data.Length == 0) return;

        int fields = pointCloud.fields.Length;
        uint point_step = pointCloud.point_step;
        // Debug.Log("Fields: " + fields + " Point Step: " + point_step);

        _ptData.SetData(LidarUtils.ExtractData(pointCloud, displayPts, vizType, out _numPts));

        string txt;
        if (_numPts < 1000)
        {
            txt = _numPts + "";
        }
        else if (_numPts < 1_000_000)
        {
            txt = (_numPts / 1000f).ToString("F2") + "K";
        }
        else
        {
            txt = (_numPts / 1_000_000f).ToString("F2") + "M";
        }
        debugText?.SetText(txt);
    }

    public override void OnTopicChange(string topic)
    {
        if (string.IsNullOrEmpty(topic))
            topic = null;

        if (topicName != null)
        {
            _ros.Unsubscribe(topicName);
            topicName = null;
        }
        if (topic == null)
        {
            Debug.Log("Disabling pointcloud display");
            _enabled = false;
            topicText?.SetText("None");
            return;
        }
        _enabled = true;
        topicName = topic;
        topicText?.SetText(topic);
        _ros.Subscribe<PointCloud2Msg>(topic, OnPointcloud);
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

    public void OnDensityChange(float density)
    {
        displayPts = (int)(density * maxPts);
    }

    public void OnSizeChange(float size)
    {
        scale = size / 10f;
        renderParams.matProps.SetFloat("_PointSize", scale);
    }

    public void OnColorSelect(int value)
    {
        if (value < 0 || value >= colorModeDropdown.options.Count)
        {
            Debug.LogWarning("Invalid color mode selected: " + value);
            return;
        }

        colorMode = (ColorMode)value;
        SetColorMode(renderParams.material, colorMode switch
        {
            ColorMode.RGB => _rgbdKeyword,
            ColorMode.Intensity => _intensityKeyword,
            ColorMode.Z => _zKeyword,
            _ => _intensityKeyword // Default to intensity if something goes wrong
        });
    }

    public override void ToggleTrack(int mode)
    {
        // 0 is normal TF tracking
        // 1 spawns the lidar at the floating origin
        useTF = mode == 0;
    }

    public void ToggleEnabled()
    {
        _enabled = !_enabled;
        if (!_enabled)
        {
            _ros.Unsubscribe(topicName);
            _parent = null;
        }
        else
        {
            Debug.Log("Subscribing to " + topicName);
            _ros.Subscribe<PointCloud2Msg>(topicName, OnPointcloud);
        }
    }

    private void SetColorMode(Material mat, LocalKeyword keyword)
    {
        mat.SetKeyword(_rgbdKeyword, _rgbdKeyword == keyword);
        mat.SetKeyword(_intensityKeyword, _intensityKeyword == keyword);
        mat.SetKeyword(_zKeyword, _zKeyword == keyword);
        Debug.Log("Set color mode to " + keyword.name);
    }

    // public override void ToggleTrack(int mode)
    // {

    //     if (mode == 0)
    //     {
    //         Transform rootTransform = GameObject.FindWithTag("root")?.transform;
    //         if (rootTransform != null)
    //         {
    //             transform.SetParent(rootTransform, true);
    //         }
    //         else
    //         {
    //             Debug.LogWarning("Root object with tag 'root' not found.");
    //             transform.SetParent(null, true);
    //         }
    //     }
    //     else if (mode == 1)
    //     {
    //         transform.SetParent(Camera.main.transform);
    //     }
    //     else
    //     {
    //         Debug.LogWarning("Invalid tracking mode: " + mode);
    //         return;
    //     }
    //     _trackingMode = mode;
    // }
    public void IncrementTrack()
    {
        _trackingMode++;
        if (_trackingMode > 1)
        {
            _trackingMode = 0;
        }
        ToggleTrack(_trackingMode);
    }

}
