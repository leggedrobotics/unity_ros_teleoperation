using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using UnityEngine.InputSystem;
using RosMessageTypes.Sensor;

public class JoystickManager : MonoBehaviour
{
    public string joyTopic = "/quest/joystick";
    public InputActionReference joystickXY;
    public InputActionReference joystickXYClick;
    public InputActionReference joystickZR;
    public InputActionReference joystickZRClick;
    public InputActionReference controllerX;
    public InputActionReference controllerY;
    public InputActionReference controllerA;
    public InputActionReference controllerB;
    public InputActionReference controllerTriggerL;
    public InputActionReference controllerTriggerR;
    public InputActionReference controllerGripL;
    public InputActionReference controllerGripR;

    private ROSConnection _ros;

    private JoyMsg _joyMsg;
    private bool _enabled = false;
    private int leftHandState = 0; // 0 = not tracked, 1 = tracked, 2 = hand tracked
    private int rightHandState = 0; // 0 = not tracked, 1 = tracked, 2 = hand tracked



    void Start()
    {
        _ros = ROSConnection.GetOrCreateInstance();

        _joyMsg = new JoyMsg();
        _joyMsg.header.frame_id = "vr_origin";

        _ros.RegisterPublisher<JoyMsg>(joyTopic);
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    void Update()
    {
        if(_enabled)
        {
            Vector2 xy = joystickXY.action.ReadValue<Vector2>();
            Vector2 zr = joystickZR.action.ReadValue<Vector2>();     

            _joyMsg.axes = new float[] {xy.x, xy.y, zr.x, zr.y, controllerTriggerL.action.ReadValue<float>(), controllerTriggerR.action.ReadValue<float>(), controllerGripL.action.ReadValue<float>(), controllerGripR.action.ReadValue<float>()};
            _joyMsg.buttons = new int[] {
                controllerX.action.ReadValue<float>() > 0.5f ? 1 : 0,
                controllerA.action.ReadValue<float>() > 0.5f ? 1 : 0,
                controllerB.action.ReadValue<float>() > 0.5f ? 1 : 0,
                controllerY.action.ReadValue<float>() > 0.5f ? 1 : 0,
                0,
                0,
                0,
                0,
                controllerTriggerL.action.ReadValue<float>() > 0.5f ? 1 : 0,
                controllerTriggerR.action.ReadValue<float>() > 0.5f ? 1 : 0,
                controllerGripL.action.ReadValue<float>() > 0.5f ? 1 : 0,
                controllerGripR.action.ReadValue<float>() > 0.5f ? 1 : 0,
                joystickXYClick.action.ReadValue<float>() > 0.5f ? 1 : 0,
                joystickZRClick.action.ReadValue<float>() > 0.5f ? 1 : 0,
                0,
                leftHandState,
                rightHandState
            };
            _ros.Send(joyTopic, _joyMsg);
        }
    }

}
