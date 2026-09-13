using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.UIElements;
// Both namespaces define Button/Image; the UI Toolkit ones are aliased and
// the uGUI Image field stays written out in full.
using Button = UnityEngine.UIElements.Button;

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
        public UnityEngine.UI.Image trackingImage;
        private int _trackedState = 0;

        /// <summary>Which tracking mode is active (indexes trackingSprites).</summary>
        // Read-only view so a UXML row can show the state without owning it.
        public int TrackedState => _trackedState;

        /// <summary>How many tracking modes this manager cycles through.</summary>
        public int TrackedStateCount => trackingSprites != null ? trackingSprites.Length : 0;

        public void Start()
        {
            ApplyTrackingSprite();
        }

        // trackingImage is the uGUI Image on the old manager panel; it is null
        // whenever the driving UI is a UXML row instead, and this used to be
        // dereferenced unguarded from both Start and IncrementTrackedState.
        private void ApplyTrackingSprite()
        {
            if (trackingImage == null || trackingSprites == null || trackingSprites.Length == 0) return;
            trackingImage.sprite = trackingSprites[Mathf.Clamp(_trackedState, 0, trackingSprites.Length - 1)];
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

            ApplyTrackingSprite();
            RefreshUi();
        }

        /// <summary>
        /// Adds the TF tracking button to this manager's row. The base row has no
        /// such control -- only camera feeds can be head/world/frame locked -- which
        /// is exactly why managers own their own markup.
        /// </summary>
        public override void BindUi(VisualElement row)
        {
            base.BindUi(row);
            if (row == null) return;

            Button track = row.Q<Button>("Track");
            if (track != null) track.clicked += IncrementTrackedState;
        }

        public override void RefreshUi()
        {
            base.RefreshUi();
            if (Row == null) return;

            Button track = Row.Q<Button>("Track");
            if (track == null) return;

            // One class per state; USS carries the icon. The three sprites on the
            // prefab are axis-arrow, head-snowflake-outline and axis-arrow-lock,
            // and the classes mirror them in that order.
            for (int i = 0; i < 3; i++) track.EnableInClassList($"is-track-{i}", i == _trackedState);
        }

    }
}

