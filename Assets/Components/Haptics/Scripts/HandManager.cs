using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR.Input;
using UnityEngine.InputSystem.XR;

using Bhaptics.SDK2;


public class HandManager : MonoBehaviour
{

    public static HandManager Instance { get; private set;}

    public InputActionReference leftHaptic;
    public InputActionReference rightHaptic;

    public float duration = 0.1f;
    public Image leftHandImg;
    public Image rightHandImg;

    private bool started = false;
    private long lastTime = 0;

    private bool _hasLeft;
    private bool _hasRight;

    private int[] leftHand;
    private int[] rightHand;

    private XRController leftController;
    private XRController rightController;

    private void Awake()
    {
        if(Instance != null && Instance != this)
            Destroy(this);
        else
            Instance = this;

        leftHand = new int[6];
        rightHand = new int[6];
    }

    public void CheckDevices()
    {
        List<HapticDevice> devices = BhapticsLibrary.GetDevices();

        leftHandImg.color = Color.red;
        rightHandImg.color = Color.red;
        _hasLeft = false;
        _hasRight = false;

        foreach (HapticDevice device in devices)
        {
            if (device.DeviceName.Contains("(L)") && device.IsConnected)
            {
                _hasLeft = true;
                leftHandImg.color = Color.green;
            }
            if (device.DeviceName.Contains("(R)") && device.IsConnected)
            {
                _hasRight = true;
                rightHandImg.color = Color.green;
            }
            if(!started)
            {
                Debug.Log(device.DeviceName + " is connected: " + device.IsConnected);
            }
        }

        started = true;
    }
    // Start is called before the first frame update
    void Start()
    {
        CheckDevices();
    }

    // Update is called once per frame
    void Update()
    {
        if(Time.time - lastTime > duration*2)
            CheckDevices();
        if(Time.time - lastTime > duration)
        {
            lastTime = (long)Time.time;

            float maxRight = Mathf.Max(rightHand)/100f;
            float maxLeft = Mathf.Max(leftHand)/100f;

            if(DebugLogger.active){
                leftHandImg.color = DebugLogger.debugGradient.Evaluate(maxLeft);
                rightHandImg.color = DebugLogger.debugGradient.Evaluate(maxRight);               
            }

            if(maxRight > 0)
            {
                OpenXRInput.SendHapticImpulse(rightHaptic, maxRight, duration, UnityEngine.InputSystem.XR.XRController.rightHand);
            }
            if(maxLeft > 0)
            {
                OpenXRInput.SendHapticImpulse(leftHaptic, maxLeft, duration, UnityEngine.InputSystem.XR.XRController.leftHand);
            }



            if(_hasRight)
            {
                BhapticsLibrary.PlayMotors(
                    (int)Bhaptics.SDK2.PositionType.GloveR,
                    rightHand,
                    (int)(1000*duration)
                );
            }
            if(_hasLeft)
            {
                BhapticsLibrary.PlayMotors(
                    (int)Bhaptics.SDK2.PositionType.GloveL,
                    leftHand,
                    (int)(1000*duration)
                );
            }
        }
        
        
    }

    public void UpdateValue(bool isRight, int index, int value)
    {
        // Just in case...
        value = Mathf.Clamp(value, 0, 100);

        if(isRight)
        {
            rightHand[index] = value;
        }
        else
        {
            leftHand[index] = value;
        }
    }

    public void UpdateValues(bool isRight, int[] values)
    {
        if(isRight)
        {
            rightHand = values;
        }
        else
        {
            leftHand = values;
        }
    }

    public void UpdateValue1D(bool isRight, int index, float value)
    {
        // This mode doesnt support only sending force to the palm
        if(index == 5) return;
        
        // Value here represents the position between this finger and the palm, so convert to forces for each
        int fingerForce = (int)(100 * value);
        int palmForce = 100 - fingerForce;

        if(isRight)
        {
            rightHand[index] = fingerForce;
            rightHand[5] = (palmForce + rightHand[5]) / 2;
        }
        else
        {
            leftHand[index] = fingerForce;
            leftHand[5] = (palmForce + leftHand[5]) / 2;
        }

    }
}
