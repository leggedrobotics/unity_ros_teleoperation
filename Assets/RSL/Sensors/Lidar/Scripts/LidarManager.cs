using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UvgRos;
using UnityEngine.UI;

namespace RSL.Sensors.Lidar
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(LidarManager))]
    public class LidarManagerEditor : RSL.Core.SensorManagerEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            LidarManager myScript = (LidarManager)target;
            // add text boxes for the topics
        }
    }

    #endif

    public class LidarManager : RSL.Core.SensorManager
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

        private UvgRosConnection ros;


        void Start()
        {
            ros = UvgRosConnection.GetOrCreateInstance();

        }


    }
}
