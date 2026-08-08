using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Std;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.Experimental.Rendering;



#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ImageView))]
public class ImageViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ImageView imageView = (ImageView)target;
        if (GUILayout.Button("Click"))
        {
            imageView.OnClick();
        }
        if (GUILayout.Button("Select First Item"))
        {
            imageView.OnSelect(1);
        }

        if (GUILayout.Button("Select Second Item"))
        {
            imageView.OnSelect(2);
        }
        if (GUILayout.Button("Clear"))
        {
            imageView.OnSelect(0);
        }
        if (GUILayout.Button("Render"))
        {
            imageView.Render();
        }
        if (GUILayout.Button("Increment Track"))
        {
            imageView.ToggleTrack();
        }
    }
}
#endif

[System.Serializable]
public class ImageData : ISensorData
{
    public bool flip;
    public bool stereo;
}

[System.Serializable]
public class ImageView : SensorStream
{
    public GameObject topMenu;
    public TMPro.TextMeshProUGUI nameText;
    public Sprite untracked;
    public Sprite tracked;
    public Sprite headTracked;
    public ComputeShader debayer;
    public Material material;

    private RenderTexture _texture2D;
    protected Transform _Img;

    protected GameObject _frustrum;
    protected Image _icon;
    protected GameObject _root;
    protected Sprite[] icons;

    public enum DebayerMode
    {
        RGGB,
        BGGR,
        GBRG,
        GRBG,
        None = -1,
    }

    public DebayerMode debayerType = DebayerMode.GRBG;

    private Texture2D _tempTextureBuffer;
    private int _tempTextureWidth = -1;
    private int _tempTextureHeight = -1;

    [SerializeField] private bool _useTextureCompression = true;
    [SerializeField] private bool _downscaleLarge4K = false;

    void Awake()
    {
        _msgType = "sensor_msgs/Image";

        _ros = ROSConnection.GetOrCreateInstance();
        nameText.text = "None";

        icons = new Sprite[] { untracked, headTracked, tracked };
        _icon = topMenu.transform.Find("Track/Image/Image").GetComponent<Image>();
        _Img = transform.Find("Img");
        material = _Img.GetComponent<MeshRenderer>().material;

        _frustrum = transform.Find("Frustrum")?.gameObject;
        _frustrum?.SetActive(false);

        _root = GameObject.FindWithTag("root");
    }

    protected virtual void Start()
    {
        topicDropdown.onValueChanged.AddListener(OnSelect);
        topicDropdown.gameObject.SetActive(false);
        topMenu.SetActive(false);

        RefreshTopics();
    }

    public void OnValidate()
    {
        if (debayer != null && debayerType != DebayerMode.None)
        {
            debayer.SetInt("mode", (int)debayerType);
        }
    }

    public bool CleanTF(string name)
    {
        GameObject target = GameObject.Find(name);

        if (target == null)
        {
            return false;
        }

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
            if (count > 100)
            {
                Debug.LogError("Looping too much");
                return false;
            }
        }

        foreach (GameObject child in children)
        {
            Destroy(child);
        }
        return false;
    }

    protected void UpdatePose(string frame)
    {
        CleanTF(frame);

        GameObject _parent = GameObject.Find(frame);
        if (_parent == null) return;

        transform.parent = _parent.transform;
        transform.localPosition = new Vector3(0.1f, 0.2f, 0);
        transform.localRotation = Quaternion.Euler(-90, 90, 180);
        // transform.localScale = new Vector3(-1, 1, 1);
    }

    private void OnDestroy()
    {
        if (topicName != null)
            _ros.Unsubscribe(topicName);

        if (_tempTextureBuffer != null)
            Destroy(_tempTextureBuffer);

        if (_texture2D != null)
            _texture2D.Release();
    }

    protected override void UpdateTopics(Dictionary<string, string> topics)
    {
        List<string> options = new List<string>();
        options.Add("None");
        foreach (var topic in topics)
        {
            if (topic.Value == "sensor_msgs/Image" || topic.Value == "sensor_msgs/CompressedImage")
            {
                // issue with depth images at the moment
                if (topic.Key.Contains("depth")) continue;

                if (topic.Key.Contains("small"))
                {
                    options.Insert(1, topic.Key);
                }
                else
                {
                    options.Add(topic.Key);
                }
            }
        }

        if (options.Count == 1)
        {
            Debug.LogWarning("No image topics found!");
            return;
        }
        topicDropdown.ClearOptions();

        topicDropdown.AddOptions(options);

        topicDropdown.value = Mathf.Min(_lastSelected, options.Count - 1);
    }

    public override void ToggleTrack(int newState)
    {
        _trackingState = newState;

        _trackingState = _trackingState % 3;

        _icon.sprite = icons[_trackingState];

        if (_trackingState == 0 && transform.parent != null && transform.parent.name != "odom")
        {
            _frustrum?.SetActive(false);
            // Otherwise, set the parent to the odom frame but keep the current position
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;
            UpdatePose("odom");
            transform.position = pos;
            transform.rotation = rot;
        }
        else if (_trackingState == 1 && transform.parent != Camera.main.transform)
        {
            // in head tracking mode so we want the parent to be the camera

            transform.parent = Camera.main.transform;
            _frustrum?.SetActive(false);
        }

    }

    public void ToggleTrack()
    {
        ToggleTrack(_trackingState + 1);
        topicDropdown.gameObject.SetActive(false);
        topMenu.SetActive(false);
    }

    public void Flip()
    {
    }

    public void ScaleUp()
    {
        transform.localScale *= 1.1f;
    }

    public void ScaleDown()
    {
        transform.localScale *= 0.9f;
    }

    public virtual void OnClick()
    {
        RefreshTopics();
        topicDropdown.gameObject.SetActive(!topicDropdown.gameObject.activeSelf);
        topMenu.gameObject.SetActive(topicDropdown.gameObject.activeSelf);
    }

    public override void OnTopicChange(string topic)
    {
        nameText.text = topic;

        if (string.IsNullOrEmpty(topic))
        {
            if (topicName != null)
                _ros.Unsubscribe(topicName);

            topicName = null;
            // set texture to grey
            material.SetTexture("_BaseMap", null);

            topicDropdown.gameObject.SetActive(false);
            topMenu.SetActive(false);
            return;
        }

        topicName = topic;

        if (topicName.EndsWith("compressed"))
        {
            _ros.Subscribe<CompressedImageMsg>(topicName, OnCompressed);
        }
        else
        {
            _ros.Subscribe<ImageMsg>(topicName, OnImage);
        }
        topicDropdown.gameObject.SetActive(false);
        topMenu.SetActive(false);
    }

    public virtual void OnSelect(int value)
    {
        if (value == _lastSelected) return;

        _lastSelected = value;

        if (topicName != null)
            _ros.Unsubscribe(topicName);

        string selectedTopic = topicDropdown.options[value].text;

        if (selectedTopic == "None")
            selectedTopic = null;

        OnTopicChange(selectedTopic);
    }

    protected virtual void SetupTex(int width = 2, int height = 2)
    {
        if (_texture2D == null || _texture2D.width != width || _texture2D.height != height)
        {
            if (_texture2D != null)
                _texture2D.Release();

            GraphicsFormat format = _useTextureCompression ?
                GraphicsFormat.B8G8R8A8_SRGB :
                GraphicsFormat.R8G8B8A8_UNorm;

            _texture2D = new RenderTexture(width, height, 0, format);
            _texture2D.enableRandomWrite = true;
            _texture2D.autoGenerateMips = false;
            _texture2D.Create();
            material.SetTexture("_BaseMap", _texture2D);
        }
    }

    /// <summary>
    /// For debugging, render the current image to a file
    /// </summary>
    public void Render()
    {
        // Save the _uiImage rendertexture to a file
        RenderTexture.active = _texture2D;
        Texture2D tex = new Texture2D(_texture2D.width, _texture2D.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, _texture2D.width, _texture2D.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        byte[] bytes = tex.EncodeToPNG();

        string filename = nameText.text.Replace("/", "_");
        System.IO.File.WriteAllBytes(Application.dataPath + "/../" + filename + ".png", bytes);
    }

    protected virtual void Resize()
    {
        if (_texture2D == null) return;
        float aspectRatio = (float)_texture2D.width / (float)_texture2D.height;

        float width = _Img.transform.localScale.x;
        float height = width / aspectRatio;

        _Img.localScale = new Vector3(width, 1, height);
    }

    protected virtual void ParseHeader(HeaderMsg header)
    {
        if (_trackingState == 2)
        {
            // If we are tracking to the TF, update the parent
            if (header.frame_id != null && (transform.parent == null || header.frame_id != transform.parent.name))
            {
                _frustrum?.SetActive(true);
                // If the parent is not the same as the frame_id, update the parent
                UpdatePose(header.frame_id);
            }
        }
    }

    void OnCompressed(CompressedImageMsg msg)
    {
        ParseHeader(msg.header);

        try
        {
            if (_tempTextureBuffer == null)
                _tempTextureBuffer = new Texture2D(2, 2);

            ImageConversion.LoadImage(_tempTextureBuffer, msg.data);
            _tempTextureBuffer.Apply();

            int imgWidth = _tempTextureBuffer.width;
            int imgHeight = _tempTextureBuffer.height;

            if (_downscaleLarge4K && imgWidth > 2048)
            {
                _tempTextureBuffer = DownsampleTexture(_tempTextureBuffer, 2048);
            }

            SetupTex(_tempTextureBuffer.width, _tempTextureBuffer.height);

            if (debayerType == DebayerMode.None)
            {
                Graphics.Blit(_tempTextureBuffer, _texture2D);
                Resize();
                return;
            }

            debayer.SetInt("mode", (int)debayerType);
            debayer.SetTexture(0, "Input", _tempTextureBuffer);
            debayer.SetTexture(0, "Result", _texture2D);
            debayer.Dispatch(0, _tempTextureBuffer.width / 2, _tempTextureBuffer.height / 2, 1);

            Resize();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    void OnImage(ImageMsg msg)
    {
        ParseHeader(msg.header);

        try
        {
            int imgWidth = (int)msg.width;
            int imgHeight = (int)msg.height;

            if (_tempTextureBuffer == null || _tempTextureWidth != imgWidth || _tempTextureHeight != imgHeight)
            {
                if (_tempTextureBuffer != null)
                    Destroy(_tempTextureBuffer);
                _tempTextureBuffer = new Texture2D(imgWidth, imgHeight, GetTextureFormat(msg.encoding), false);
                _tempTextureWidth = imgWidth;
                _tempTextureHeight = imgHeight;
            }

            _tempTextureBuffer.LoadRawTextureData(msg.data);
            _tempTextureBuffer.Apply();

            if (_downscaleLarge4K && imgWidth > 2048)
            {
                _tempTextureBuffer = DownsampleTexture(_tempTextureBuffer, 2048);
            }

            SetupTex(_tempTextureBuffer.width, _tempTextureBuffer.height);
            Graphics.Blit(_tempTextureBuffer, _texture2D);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
        Resize();
    }

    private Texture2D DownsampleTexture(Texture2D source, int maxDimension = 2048)
    {
        if (source.width <= maxDimension && source.height <= maxDimension)
            return source;

        float scale = (float)maxDimension / Mathf.Max(source.width, source.height);
        int newWidth = Mathf.RoundToInt(source.width * scale);
        int newHeight = Mathf.RoundToInt(source.height * scale);

        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.Default);
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        Destroy(source);
        return result;
    }

    public static TextureFormat GetTextureFormat(string rosEncoding)
    {
        switch (rosEncoding.ToLowerInvariant())
        {
            // 8-bit color
            case "rgb8":
                return TextureFormat.RGB24;
            case "bgr8":
                // channel order can be swapped later if needed
                return TextureFormat.RGB24;
            case "rgba8":
                return TextureFormat.RGBA32;
            case "bgra8":
                return TextureFormat.RGBA32;

            // 16-bit color (map to 16-bit-per-channel RGBA when available)
            case "rgb16":
            case "bgr16":
            case "rgba16":
            case "bgra16":
                // Use RGBA64 (16 bits per channel) when available; preserves bit depth.
                return TextureFormat.RGBA64;

            // Monochrome
            case "mono8":
            case "8uc1":
                return TextureFormat.R8;
            case "mono16":
            case "16uc1":
                return TextureFormat.R16;

            // OpenCV-like 8-bit types
            case "8uc2":
                // two channels -> use RG16 (two 8-bit channels packed) as best fit
                return TextureFormat.RG16;
            case "8uc3":
                return TextureFormat.RGB24;
            case "8uc4":
                return TextureFormat.RGBA32;

            // Signed/other 8-bit types - map to nearest Unity format
            case "8sc1":
                return TextureFormat.R8;
            case "8sc2":
                return TextureFormat.RG16;
            case "8sc3":
                return TextureFormat.RGB24;
            case "8sc4":
                return TextureFormat.RGBA32;

            // 16-bit OpenCV types
            case "16uc2":
            case "16sc2":
                return TextureFormat.RG16;
            case "16uc3":
            case "16sc3":
                return TextureFormat.RGBA64; // map 3-channel 16-bit to 4-channel 16-bit container
            case "16uc4":
            case "16sc4":
                return TextureFormat.RGBA64;

            // 32-bit integer/float types -> use float formats
            case "32sc1":
            case "32sc2":
            case "32sc3":
            case "32sc4":
                return TextureFormat.RGBAFloat;
            case "32fc1":
            case "32fc2":
            case "32fc3":
            case "32fc4":
                return TextureFormat.RGBAFloat;

            // 64-bit float -> map to float texture (precision loss but usable)
            case "64fc1":
            case "64fc2":
            case "64fc3":
            case "64fc4":
                return TextureFormat.RGBAFloat;

            // Bayer patterns
            case "bayer_rggb8":
            case "bayer_bggr8":
            case "bayer_gbrg8":
            case "bayer_grbg8":
                // raw 8-bit sensor data
                return TextureFormat.R8;
            case "bayer_rggb16":
            case "bayer_bggr16":
            case "bayer_gbrg16":
            case "bayer_grbg16":
                // raw 16-bit sensor data
                return TextureFormat.R16;

            default:
                // Fallback to a safe, widely supported format
                return TextureFormat.RGBA32;
        }
    }

    public override void Deserialize(string data)
    {
        try
        {
            ImageData imgData = JsonUtility.FromJson<ImageData>(data);

            transform.position = imgData.position;
            transform.rotation = imgData.rotation;
            transform.localScale = imgData.scale;
            topicName = imgData.topicName;
            _trackingState = imgData.trackingState;

            OnTopicChange(topicName);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            Debug.LogError("Error deserializing image data! Most likely old data format, clearing prefs");
        }
    }

    public override string Serialize()
    {
        ImageData data = new ImageData();
        data.position = transform.position;
        data.rotation = transform.rotation;
        data.scale = transform.localScale;
        data.topicName = topicName;
        data.trackingState = _trackingState;
        data.flip = false;
        data.stereo = false;

        return JsonUtility.ToJson(data);
    }
}
