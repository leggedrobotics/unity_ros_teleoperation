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
    public InputAction joystickXY;
    public InputAction joystickXYClick;
    public InputAction joystickZR;
    public InputAction joystickZRClick;
    public InputAction controllerX;
    public InputAction controllerY;
    public InputAction controllerA;
    public InputAction controllerB;
    public InputAction controllerTriggerL;
    public InputAction controllerTriggerR;
    public InputAction controllerGripL;
    public InputAction controllerGripR;
    
private ROSConnection _ros;

    private JoyMsg _joyMsg;
    private bool _enabled = false;
    private int leftHandState = 0; // 0 = not tracked, 1 = tracked, 2 = hand tracked
    private int rightHandState = 0; // 0 = not tracked, 1 = tracked, 2 = hand tracked



    void Start()
    {
        // Using System wide Input Actions
        if(InputSystem.actions)
        {
            joystickXY = InputSystem.actions.FindAction("XRI RightHand Interaction/Joystick");
            joystickZR = InputSystem.actions.FindAction("XRI LeftHand Interaction/Joystick");
            joystickXYClick = InputSystem.actions.FindAction("XRI RightHand/JoystickClicked");
            joystickZRClick = InputSystem.actions.FindAction("XRI LeftHand/JoystickClicked");
            controllerX = InputSystem.actions.FindAction("XRI LeftHand/Xbutton");
            controllerY = InputSystem.actions.FindAction("XRI LeftHand/Ybutton");
            controllerA = InputSystem.actions.FindAction("XRI RightHand/Abutton");
            controllerB = InputSystem.actions.FindAction("XRI RightHand/Bbutton");
            controllerTriggerL = InputSystem.actions.FindAction("XRI LeftHand/Trigger");
            controllerTriggerR = InputSystem.actions.FindAction("XRI RightHand/Trigger");
            controllerGripL = InputSystem.actions.FindAction("XRI LeftHand/Grip");
            controllerGripR = InputSystem.actions.FindAction("XRI RightHand/Grip");
        }


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
            Vector2 xy = joystickXY.ReadValue<Vector2>();
            Vector2 zr = joystickZR.ReadValue<Vector2>();     

            _joyMsg.axes = new float[] {xy.x, xy.y, zr.x, zr.y, controllerTriggerL.ReadValue<float>(), controllerTriggerR.ReadValue<float>(), controllerGripL.ReadValue<float>(), controllerGripR.ReadValue<float>()};
            _joyMsg.buttons = new int[] {
                controllerX.ReadValue<float>() > 0.5f ? 1 : 0,
                controllerA.ReadValue<float>() > 0.5f ? 1 : 0,
                controllerB.ReadValue<float>() > 0.5f ? 1 : 0,
                controllerY.ReadValue<float>() > 0.5f ? 1 : 0,
                0,
                0,
                0,
                0,
                controllerTriggerL.ReadValue<float>() > 0.5f ? 1 : 0,
                controllerTriggerR.ReadValue<float>() > 0.5f ? 1 : 0,
                controllerGripL.ReadValue<float>() > 0.5f ? 1 : 0,
                controllerGripR.ReadValue<float>() > 0.5f ? 1 : 0,
                joystickXYClick.ReadValue<float>() > 0.5f ? 1 : 0,
                joystickZRClick.ReadValue<float>() > 0.5f ? 1 : 0,
                0,
                leftHandState,
                rightHandState
            };
            _ros.Publish(joyTopic, _joyMsg);
        }
    }

}
