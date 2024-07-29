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
    }
}
#endif

[System.Serializable]
public struct ImageData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public string topicName;
    public int trackingState;
    public bool flip;
    public bool stereo;
}

[System.Serializable]
public class ImageView : MonoBehaviour
{
    public Dropdown dropdown;
    public GameObject topMenu;
    public CameraManager manager;
    public TMPro.TextMeshProUGUI name;
    public Sprite untracked;
    public Sprite tracked;
    public Sprite headTracked;
    public ComputeShader debayer;
    public Material material;

    public string topicName;

    private RenderTexture _texture2D;
    protected Transform _Img;

    protected int _lastSelected = 0;

    protected GameObject _frustrum;
    protected Image _icon;
    protected ROSConnection ros;

    public int _trackingState = 0;
    protected Sprite[] icons;

    public enum DebayerMode
    {
        RGGB,
        BGGR,
        GBRG,
        GRBG,
        None=-1,
    }

    public DebayerMode debayerType = DebayerMode.GRBG;


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

        if(target == null)
        {
            return false;
        }

        List<GameObject> children = new List<GameObject>();

        // check if this is connected to root
        int count = 0;
        while(target.transform.parent != null)
        {
            count++;
            children.Add(target);
            target = target.transform.parent.gameObject;
            if(target.name == "odom")
            {
                children.Clear();
                Debug.Log("Connected to root");
                return true;
            }
            if(count > 100)
            {
                Debug.LogError("Looping too much");
                return false;
            }
        }

        foreach(GameObject child in children)
        {
            Destroy(child);
        }
        return false;
    }

    void UpdatePose(string frame)
    {
        if(!CleanTF(frame))
        {
            return;
        }
        GameObject _parent = GameObject.Find(frame);
        if(_parent == null) return;

        transform.parent = _parent.transform;
        transform.localPosition = new Vector3(0.1f, 0.2f, 0);
        transform.localRotation = Quaternion.Euler(-90, 90, 180);
        // transform.localScale = new Vector3(-1, 1, 1);
    }

    void Awake()
    {
        ros = ROSConnection.GetOrCreateInstance();
        name.text = "None";

        icons = new Sprite[] {untracked, tracked, headTracked};
        _icon = topMenu.transform.Find("Track/Image/Image").GetComponent<Image>();

    }

    void Start()
    {
        _Img = transform.Find("Img");
        material = _Img.GetComponent<MeshRenderer>().material;

        dropdown.onValueChanged.AddListener(OnSelect);

        dropdown.gameObject.SetActive(false);
        topMenu.SetActive(false);

        ros.GetTopicAndTypeList(UpdateTopics);

        _frustrum = transform.Find("Frustrum").gameObject;
    }

    private void OnDestroy() {
        ros.Unsubscribe(topicName);
    }

    protected virtual void UpdateTopics(Dictionary<string, string> topics)
    {
        List<string> options = new List<string>();
        options.Add("None");
        foreach (var topic in topics)
        {
            if (topic.Value == "sensor_msgs/Image" || topic.Value == "sensor_msgs/CompressedImage")
            {
                // issue with depth images at the moment
                if (topic.Key.Contains("depth")) continue;
                options.Add(topic.Key);
            }
        }

        if(options.Count == 1)
        {
            Debug.LogWarning("No image topics found!");
            return;
        }
        dropdown.ClearOptions();

        dropdown.AddOptions(options);

        dropdown.value = Mathf.Min(_lastSelected, options.Count - 1);
    }

    public void Clear()
    {
        manager.Remove(gameObject);
    }

    public void ToggleTrack(int newState)
    {
        _trackingState = newState;

        _trackingState = _trackingState % 3;

        _icon.sprite = icons[_trackingState];
    }

    public void ToggleTrack()
    {
        ToggleTrack(_trackingState + 1);
        dropdown.gameObject.SetActive(false);
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

    public void OnClick()
    {
        ros.GetTopicAndTypeList(UpdateTopics);
        dropdown.gameObject.SetActive(!dropdown.gameObject.activeSelf);
        topMenu.gameObject.SetActive(dropdown.gameObject.activeSelf);
    }

    public virtual void OnSelect(int value)
    {
        if (value == _lastSelected) return;

        _lastSelected = value;

        if (topicName != null)
            ros.Unsubscribe(topicName);

        name.text = dropdown.options[value].text;

        if (value == 0)
        {
            topicName = null;
            // set texture to grey
            material.SetTexture("_BaseMap", null);
            
            dropdown.gameObject.SetActive(false);
            topMenu.SetActive(false);
            return;
        }

        topicName = dropdown.options[value].text;

        if (topicName.EndsWith("compressed"))
        {
            ros.Subscribe<CompressedImageMsg>(topicName, OnCompressed);
        }
        else
        {
            ros.Subscribe<ImageMsg>(topicName, OnImage);
        }
        dropdown.gameObject.SetActive(false);
        topMenu.SetActive(false);
    }

    protected virtual void SetupTex(int width = 2, int height = 2)
    {
        if (_texture2D == null)
        {
            _texture2D = new RenderTexture(width, height, 0, GraphicsFormat.R8G8B8A8_UNorm);
            _texture2D.enableRandomWrite = true;
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
        
        string filename = name.text.Replace("/", "_");
        System.IO.File.WriteAllBytes(Application.dataPath + "/../" + filename + ".png", bytes);
    }

    protected virtual void Resize()
    {
        if (_texture2D == null) return;
        float aspectRatio = (float)_texture2D.width/(float)_texture2D.height;

        float width = _Img.transform.localScale.x;
        float height = width / aspectRatio;
        
        _Img.localScale = new Vector3(width, 1, height);
    }

    protected void ParseHeader(HeaderMsg header)
    {
        if (_trackingState == 1)
        {
            // If we are tracking to the TF, update the parent
            if(header.frame_id != null && (transform.parent == null || header.frame_id != transform.parent.name))
            {
                _frustrum.SetActive(true);
                // If the parent is not the same as the frame_id, update the parent
                UpdatePose(header.frame_id);
            }

        } else if (_trackingState == 0 && transform.parent != null && transform.parent.name != "odom")
        {
            _frustrum.SetActive(false);
            // Otherwise, set the parent to the odom frame but keep the current position
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;
            UpdatePose("odom");
            transform.position = pos;
            transform.rotation = rot;
        } else  if (_trackingState == 2 && transform.parent != Camera.main.transform)
        {
            // in head tracking mode so we want the parent to be the camera
            transform.parent = Camera.main.transform;
            _frustrum.SetActive(false);
        }
    }

    void OnCompressed(CompressedImageMsg msg)
    {
        // SetupTex();
        ParseHeader(msg.header);

        try
        {
            Texture2D _input = new Texture2D(2, 2);
            ImageConversion.LoadImage(_input, msg.data);
            _input.Apply();
            SetupTex(_input.width, _input.height);

            if(debayerType == DebayerMode.None)
            {
                RenderTexture.active = _texture2D;
                Graphics.Blit(_input, _texture2D);
                RenderTexture.active = null;
                return;
            }

            // debayer the image using compute shader
            debayer.SetInt("mode", (int)debayerType);
            debayer.SetTexture(0, "Input", _input);
            debayer.SetTexture(0, "Result", _texture2D);
            debayer.Dispatch(0, _input.width / 2, _input.height / 2, 1);

            Destroy(_input);

            Resize();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    void OnImage(ImageMsg msg)
    {
        SetupTex((int)msg.width, (int)msg.height);
        ParseHeader(msg.header);

        try
        {
            // _texture2D.LoadRawTextureData(msg.data);
            // _texture2D.Apply();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
        Resize();
    }

    public virtual void Deserialize(string data)
    {
        try{
            ImageData imgData = JsonUtility.FromJson<ImageData>(data);
            
            transform.position = imgData.position;
            transform.rotation = imgData.rotation;
            transform.localScale = imgData.scale;
            topicName = imgData.topicName;
            _trackingState = imgData.trackingState;

            if (topicName.EndsWith("compressed"))
            {
                ros.Subscribe<CompressedImageMsg>(topicName, OnCompressed);
                name.text = topicName;
            }
            else if (topicName != null)
            {
                ros.Subscribe<ImageMsg>(topicName, OnImage);
                name.text = topicName;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            Debug.LogError("Error deserializing image data! Most likely old data format, clearing prefs");
            PlayerPrefs.DeleteKey("layout");
            PlayerPrefs.Save();
        }
    }

    public virtual string Serialize()
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
