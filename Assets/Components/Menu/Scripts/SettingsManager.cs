using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SettingsManager))]
public class SettingsManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SettingsManager myScript = (SettingsManager)target;
        if (GUILayout.Button("Toggle Stream"))
        {
            myScript.ToggleStream();
        }
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
    public PoseManager poseManager;
    public NvbloxMesh nvbloxMesh;
    public Image axisIcon;
    public Sprite unlockedIcon;
    public Sprite lockedIcon;
    
    public Sprite streamOnIcon;
    public Sprite streamOffIcon;
    public Image streamIcon;

    public bool startStreaming = false;

    private bool _lockedPose = true;

    private PosePublisher _posePublisher;
    private JoystickManager _joystickManager;
    private Streamer _streamer;
    void Start()
    {
        poseManager = PoseManager.Instance;
        poseManager?.SetLocked(_lockedPose);
        axisIcon.sprite = _lockedPose ? lockedIcon : unlockedIcon;

        _joystickManager = GetComponent<JoystickManager>();
        _joystickManager.SetEnabled(true);

        _posePublisher = GetComponent<PosePublisher>();
        _posePublisher.SetEnabled(false);

        _streamer = FindObjectOfType<Streamer>();
        if (_streamer == null)
        {
            Debug.LogWarning("No Streamer found in scene");
        } else {
            if (startStreaming)
            {
                _streamer.enabled = true;
                streamIcon.sprite = streamOnIcon;
            }
            else
            {
                _streamer.enabled = false;
                streamIcon.sprite = streamOffIcon;
            }
            Debug.Log($"Streaming to topic {_streamer.topic}");
        }
    }

    public void ToggleStream()
    {
        if(_streamer != null){
            _streamer.enabled = !_streamer.enabled;
            streamIcon.sprite = _streamer.enabled ? streamOnIcon : streamOffIcon;
        }
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
        poseManager.ToggleFixedLocation();
    }

    public void TogglePoseLock()
    {
        _lockedPose = !_lockedPose;
        poseManager?.SetLocked(_lockedPose);
        axisIcon.sprite = _lockedPose ? lockedIcon : unlockedIcon;

        _joystickManager?.SetEnabled(_lockedPose);
    }

}
