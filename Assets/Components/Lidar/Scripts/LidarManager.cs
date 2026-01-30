using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.UI;

namespace RSL.Sensors.Lidar
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(LidarManager))]
    public class LidarManagerEditor : SensorManagerEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            LidarManager myScript = (LidarManager)target;
            // add text boxes for the topics
        }
    }

    #endif

    public class LidarManager : SensorManager
    {
        // public LidarStream lidarStreamer;
        // public LidarStream rgbdStreamer;

        // public TMPro.TextMeshProUGUI lidarTopic;
        // public TMPro.TextMeshProUGUI rgbdTopic;

        // public Dropdown topicDropdown;

        // private string _lidarTopic;
        // private string _rgbdTopic;

        // private bool _lidarClicked;


        // public GameObject menu;

        private ROSConnection ros;


        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();

        }


    }
}
