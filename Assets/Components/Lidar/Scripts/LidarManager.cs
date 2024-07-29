using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(LidarManager))]
public class LidarManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LidarManager myScript = (LidarManager)target;
        // add text boxes for the topics
        if (GUILayout.Button("Click Lidar"))
        {
            myScript.OnLidarClick();
        }
        if (GUILayout.Button("Click RGBD"))
        {
            myScript.OnRGBDClick();
        }
        if (GUILayout.Button("Toggle Menu"))
        {
            myScript.ToggleMenu();
        }

        if (GUILayout.Button("Select 0"))
        {
            myScript.OnTopicSelect(0);
        }
        if (GUILayout.Button("Select 1"))
        {
            myScript.OnTopicSelect(1);
        }
    }
}

#endif

public class LidarManager : MonoBehaviour
{
    public LidarDrawer lidarDrawer;
    public LidarDrawer rgbdDrawer;

    public TMPro.TextMeshProUGUI lidarTopic;
    public TMPro.TextMeshProUGUI rgbdTopic;

    public Dropdown topicDropdown;

    private string _lidarTopic;
    private string _rgbdTopic;

    private bool _lidarClicked;

    public GameObject menu;

    private ROSConnection ros;


    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        _lidarTopic = lidarTopic.text;
        _rgbdTopic = rgbdTopic.text;

        if(PlayerPrefs.HasKey("lidarTopic"))
        {
            _lidarTopic = PlayerPrefs.GetString("lidarTopic");
        }
        if(PlayerPrefs.HasKey("rgbdTopic"))
        {
            _rgbdTopic = PlayerPrefs.GetString("rgbdTopic");
        }

        lidarTopic.text = _lidarTopic;
        rgbdTopic.text = _rgbdTopic;

        lidarDrawer.topic = _lidarTopic;
        rgbdDrawer.topic = _rgbdTopic;

        lidarTopic.transform.parent.GetComponent<Button>().onClick.AddListener(OnLidarClick);
        rgbdTopic.transform.parent.GetComponent<Button>().onClick.AddListener(OnRGBDClick);

        topicDropdown.onValueChanged.AddListener(OnTopicSelect);
        topicDropdown.gameObject.SetActive(false);

        menu.SetActive(false);
    }

    public void OnTopicSelect(int value){
        if(value == -1)
        {
            return;
        }
        string topic = topicDropdown.options[value].text;
        if(value == 0)
        {
            topic = null;
        }
        if(_lidarClicked)
        {
            OnLidarTopic(topic);
        }
        else
        {
            OnRGBDTopic(topic);
        }
        topicDropdown.gameObject.SetActive(false);
    }

    public void PopulateTopics(Dictionary<string, string> topics)
    {
        List<string> topicList = new List<string>();
        topicList.Add("None");
        foreach (KeyValuePair<string, string> topic in topics)
        {
            if (topic.Value == "sensor_msgs/PointCloud2")
                topicList.Add(topic.Key);
        }
        
        if(topicList.Count == 1)
        {
            Debug.LogWarning("No PointCloud2 topics found!");
            return;
        } else
        {
            Debug.Log("Found " + (topicList.Count - 1) + " PointCloud2 topics: " + string.Join(", ", topicList.GetRange(1, topicList.Count - 1).ToArray()));
        }

        topicDropdown.ClearOptions();
        topicDropdown.AddOptions(topicList);

        // if(_lidarClicked)
        // {
        //     topicDropdown.value = topicList.IndexOf(_lidarTopic);
        // }
        // else
        // {
        //     topicDropdown.value = topicList.IndexOf(_rgbdTopic);
        // }
    }

    public void OnLidarClick()
    {
        // Close the dropdown if it's already open and we clicked the button again
        if(_lidarClicked && topicDropdown.gameObject.activeSelf)
        {
            topicDropdown.gameObject.SetActive(false);
            return;
        }

        // Store that lidar was the last one clicked and get new topics
        _lidarClicked = true;
        topicDropdown.gameObject.SetActive(true);
        ros.GetTopicAndTypeList(PopulateTopics);
    }

    public void OnRGBDClick()
    {
        // Close the dropdown if it's already open and we clicked the button again
        if(!_lidarClicked && topicDropdown.gameObject.activeSelf)
        {
            topicDropdown.gameObject.SetActive(false);
            return;
        }

        // Store that RGBD was the last one clicked and get new topics
        _lidarClicked = false;
        topicDropdown.gameObject.SetActive(true);
        ros.GetTopicAndTypeList(PopulateTopics);
    }

    public void OnLidarTopic(string topic)
    {
        _lidarTopic = topic;
        lidarTopic.text = _lidarTopic;
        lidarDrawer._enabled = true;
        lidarDrawer.OnTopicChange(_lidarTopic);
        PlayerPrefs.SetString("lidarTopic", _lidarTopic);
        PlayerPrefs.Save();
    }

    public void OnRGBDTopic(string topic)
    {
        _rgbdTopic = topic;
        rgbdTopic.text = _rgbdTopic;
        rgbdDrawer._enabled = true;
        rgbdDrawer.OnTopicChange(_rgbdTopic);
        PlayerPrefs.SetString("rgbdTopic", _rgbdTopic);
        PlayerPrefs.Save();
    }

    public void ToggleMenu()
    {
        menu.SetActive(!menu.activeSelf);
        
        // If we are closing the menu, close the dropdown
        if(!menu.activeSelf)
        {
            topicDropdown.gameObject.SetActive(false);
        }
    }

    public void Clear()
    {
        lidarDrawer.enabled = false;
        rgbdDrawer.enabled = false;
    }

    public void Lidar()
    {
        lidarDrawer.ToggleEnabled();
    }

    public void RGBD()
    {
        rgbdDrawer.ToggleEnabled();
    }

}
