using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Std;
using RosMessageTypes.Sensor;
using Unity.Robotics.ROSTCPConnector;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.UI;


#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(StereoStreamer))]
public class StereoStreamerEditor : ImageViewEditor
{}
#endif


public class StereoStreamer : ImageView
{

    private Texture2D _leftTexture2D;
    private Texture2D _rightTexture2D;

    /// <summary>
    /// Updates the list of the available topics in the dropdown menu
    /// </summary>
    /// <param name="topics">Topics from the ROS TCP function</param>
    protected override void UpdateTopics(Dictionary<string, string> topics)
    {
        List<string> options = new List<string>();
        options.Add("None");
        foreach (var topic in topics)
        {
            if (topic.Value == "sensor_msgs/Image" || topic.Value == "sensor_msgs/CompressedImage")
            {
                // issue with depth images at the moment
                if (topic.Key.Contains("left")) 
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

    /// <summary>
    /// Flips the image horizontally, NOTE: Not yet implemented for stereo images.... (might need to swap left and right as well...)
    /// </summary>
    public void Flip()
    {
        Debug.Log("Flip not yet implemented");    
    }

    public override void OnSelect(int value)
    {
        if (value == _lastSelected) return;
        _lastSelected = value;

        if (topicName != null)
        {
            ros.Unsubscribe(topicName);
            ros.Unsubscribe(topicName.Replace("left", "right"));
        }

        name.text = dropdown.options[value].text.Split(' ')[0];

        if (value == 0)
        {
            topicName = null;
            // set texture to grey
            _leftTexture2D = new Texture2D(3, 2, TextureFormat.RGBA32, false);
            material.SetTexture("_LeftTex", _leftTexture2D);

            _rightTexture2D = new Texture2D(3, 2, TextureFormat.RGBA32, false);
            material.SetTexture("_RightTex", _rightTexture2D);
            
            dropdown.gameObject.SetActive(false);
            topMenu.SetActive(false);
            return;
        }

        topicName = dropdown.options[value].text;

        if (topicName.EndsWith("compressed"))
        {
            ros.Subscribe<CompressedImageMsg>(topicName, OnCompressedLeft);
            ros.Subscribe<CompressedImageMsg>(topicName.Replace("left", "right"), OnCompressedRight);
        }
        else
        {
            Debug.LogError("Only compressed images are supported at the moment");
            // ros.Subscribe<ImageMsg>(topicName, OnImage);
        }
        dropdown.gameObject.SetActive(false);
        topMenu.SetActive(false);

    }

    /// <summary>
    /// Sets up the texture for the image or reallocates if the size has changed
    /// </summary>
    /// <param name="width">Target width for the new image</param>
    /// <param name="height">Target height for the new image</param>
    /// <param name="left">Toggle for whether to operate on the left or right texture</param>
    private void SetupTex(int width = 2, int height = 2, bool left = true)
    {
        if (left)
        {
            if (_leftTexture2D == null || _leftTexture2D.width != width || _leftTexture2D.height != height)
            {
                if (_leftTexture2D != null)
                    Destroy(_leftTexture2D);
                _leftTexture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
                _leftTexture2D.wrapMode = TextureWrapMode.Clamp;
                _leftTexture2D.filterMode = FilterMode.Bilinear;
                material.SetTexture("_LeftTex", _leftTexture2D);
            }
        }
        else
        {
            if (_rightTexture2D == null || _rightTexture2D.width != width || _rightTexture2D.height != height)
            {
                if (_rightTexture2D != null)
                    Destroy(_rightTexture2D);
                _rightTexture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
                _rightTexture2D.wrapMode = TextureWrapMode.Clamp;
                _rightTexture2D.filterMode = FilterMode.Bilinear;
                material.SetTexture("_RightTex", _rightTexture2D);
            }
        }
    }

    /// <summary>
    /// Resize the image object to match the aspect ratio of the image
    /// </summary>
    private void Resize()
    {
        if (_leftTexture2D == null) return;
        float aspectRatio = (float)_leftTexture2D.width/(float)_leftTexture2D.height;

        float width = _Img.transform.localScale.x;
        float height = width / aspectRatio;
        
        _Img.localScale = new Vector3(width, 1, height);
    }

    /// <summary>
    /// Callback for for left compressed image
    /// </summary>
    /// <param name="msg"></param>
    void OnCompressedLeft(CompressedImageMsg msg)
    {
        SetupTex(2,2,true);
        ParseHeader(msg.header);

        try
        {
            ImageConversion.LoadImage(_leftTexture2D , msg.data);
            _leftTexture2D.Apply();
            Resize();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    /// <summary>
    /// Callback for for right compressed image
    /// </summary>
    /// <param name="msg"></param>
    void OnCompressedRight(CompressedImageMsg msg)
    {
        SetupTex(2,2,false);
        ParseHeader(msg.header);

        try
        {
            ImageConversion.LoadImage(_rightTexture2D , msg.data);
            _rightTexture2D.Apply();
            Resize();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    void OnDestroy()
    {
        if (topicName != null)
        {
            ros.Unsubscribe(topicName);
            ros.Unsubscribe(topicName.Replace("left", "right"));
        }
    }

    public override void Deserialize(string data)
    {
        try{
            ImageData imgData = JsonUtility.FromJson<ImageData>(data);

            transform.position = imgData.position;
            transform.rotation = imgData.rotation;
            transform.localScale = imgData.scale;
            _trackingState = imgData.trackingState;

            topicName = imgData.topicName;

            if(topicName == null)
            {
                return;
            }

            name.text = topicName;
            ros.Subscribe<CompressedImageMsg>(topicName, OnCompressedLeft);
            ros.Subscribe<CompressedImageMsg>(topicName.Replace("left", "right"), OnCompressedRight);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            Debug.LogError("Error deserializing image data! Most likely old data format, clearing prefs");
            PlayerPrefs.DeleteKey("layout");
            PlayerPrefs.Save();
        }
        
    }
    public override string Serialize()
    {
        ImageData imgData = JsonUtility.FromJson<ImageData>(base.Serialize());
        imgData.stereo = true;

        return JsonUtility.ToJson(imgData);
    }
}

