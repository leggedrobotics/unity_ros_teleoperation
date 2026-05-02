using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RSL.Core.PosePublisher;

namespace RSL.Core.Menu
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(SettingsManager))]
    public class SettingsManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SettingsManager myScript = (SettingsManager)target;
            if (GUILayout.Button("Toggle Nvblox"))
            {
                myScript.ToggleNvblox();
            }
            if (GUILayout.Button("Recenter"))
            {
                myScript.Recenter();
            }
            if (GUILayout.Button("Toggle Center Lock"))
            {
                myScript.ToggleCenterLock();
            }
            if (GUILayout.Button("Toggle Pose Lock"))
            {
                myScript.TogglePoseLock();
            }

        }
    }
    #endif

    public class SettingsManager : MonoBehaviour
    {
        public RSL.Core.TF.PoseManager poseManager;
        public RSL.Core.VoxBlox.NvbloxMesh nvbloxMesh;

        [Header("Robot Lock")]
        public Image robotIcon;
        public Sprite unlockedRobotIcon;
        public Sprite lockedRobotIcon;

        [Header("Axis Lock")]
        public Image axisIcon;
        public Sprite unlockedIcon;
        public Sprite lockedIcon;

        [Header("Streamer")]
        public Image streamIcon;
        public Sprite streamOnIcon;
        public Sprite streamOffIcon;

        public bool startStreaming = false;

        private bool _lockedPose = true;
        private bool _fixedPosition = false;

        private PosePublisher.PosePublisher _posePublisher;
        private JoystickManager _joystickManager;
        // private Streamer _streamer;
        void Start()
        {
            poseManager = RSL.Core.TF.PoseManager.Instance;
            poseManager?.SetLocked(_lockedPose);
            axisIcon.sprite = _lockedPose ? lockedIcon : unlockedIcon;



            _joystickManager = GetComponent<JoystickManager>();
            _joystickManager.SetEnabled(true);

            _posePublisher = GetComponent<PosePublisher.PosePublisher>();
            _posePublisher.SetEnabled(false);

        }


        public void ChangeMode(int modes)
        {
            switch (modes)
            {
                case 0: // Everything disabled
                    _joystickManager.SetEnabled(false);
                    _posePublisher.SetEnabled(false);
                    break;
                case 1: // Pose Publisher enabled
                    _joystickManager.SetEnabled(false);
                    _posePublisher.SetEnabled(true);
                    break;
                case 2: // Joystick Manager enabled
                    _joystickManager.SetEnabled(true);
                    _posePublisher.SetEnabled(false);
                    break;
            }
        }

        public void ToggleNvblox()
        {
            nvbloxMesh?.ToggleEnabled();
        }

        public void Recenter()
        {
            Vector3 position = Camera.main.transform.position;
            position += Camera.main.transform.forward*2;
            position.y = 0.5f;

            poseManager.BaseToLocation(position);
        }

        public void ToggleCenterLock()
        {
            _fixedPosition = !_fixedPosition;
            poseManager?.SetFixedLocation(_fixedPosition);
            robotIcon.sprite = _fixedPosition ? lockedRobotIcon : unlockedRobotIcon;
        }

        public void TogglePoseLock()
        {
            _lockedPose = !_lockedPose;
            poseManager?.SetLocked(_lockedPose);
            axisIcon.sprite = _lockedPose ? lockedIcon : unlockedIcon;

            _joystickManager?.SetEnabled(_lockedPose);
        }
    }
}
