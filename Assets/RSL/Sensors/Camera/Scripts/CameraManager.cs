using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace RSL.Sensors.Camera
{
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(CameraManager))]
    public class CameraManagerEditor : RSL.Core.SensorManagerEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            CameraManager cameraManager = (CameraManager)target;
            if (GUILayout.Button("Increment Tracked State"))
            {
                cameraManager.IncrementTrackedState();
            }
        }
    }

    #endif

    public class CameraManager : RSL.Core.SensorManager
    {

        public Sprite[] trackingSprites;
        public Image trackingImage;
        private int _trackedState = 0;

        public void Start()
        {
            trackingImage.sprite = trackingSprites[_trackedState];
        }

        public void IncrementTrackedState()
        {
            _trackedState++;
            if (_trackedState > trackingSprites.Length - 1)
                _trackedState = 0;

            foreach (var sensor in sensors)
            {   
                Debug.Log("CameraManager -> Headtracking");
                sensor.GetComponent<RSL.Core.SensorStream>().ToggleTrack(_trackedState);
            }

            trackingImage.sprite = trackingSprites[_trackedState];        
        }

    }
}

