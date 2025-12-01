using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.Crs;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR;


public class ControlInputStream : SensorStream
{
    [Header("Visualization Settings")]
    public GameObject carObject;

    public enum VisualizationMode
    {
        Hide,           // No visualization 
        FollowCar,      // Original arrows following the car
        ScreenHUD       // UI-based steering wheel + F1 style bar
    }

    [Header("Visualization Mode")]
    public VisualizationMode currentMode = VisualizationMode.Hide;

    [Header("Follow Car Mode Settings")]
    public float arrowLength = 0.3f;
    public float arrowHeight = 0.25f;
    public float barHeight = 0.5f;
    public float barOffset = 0.25f;

    [Header("Steering Wheel UI")]
    public GameObject steeringWheelPanel;
    private Image _steeringWheelBase;
    private TextMeshProUGUI _steeringText;

    [Header("F1-Style Torque Bar UI")]
    public GameObject torqueBarPanel;
    private Image _torqueBarBackground;
    private Image _torqueBarFillPositive;  // Green bar for throttle (fills up)
    private Image _torqueBarFillNegative;  // Red bar for brake (fills down)
    private Image _torqueCenterLine;

    private TextMeshProUGUI _torqueLabelTop;
    private TextMeshProUGUI _torqueLabelBottom;
    private TextMeshProUGUI _torqueValueText;

    // Follow Car Mode objects
    private LineRenderer steeringArrow;
    private LineRenderer torqueBar;
    private GameObject steeringArrowObj;
    private GameObject torqueBarObj;

    private float currentSteering = 0f;
    private float currentTorque = 0f;
    private float maxSteeringAngle = 0.5236f; // 30 degrees

    private bool wasButtonPressed = false;
    void Awake()
    {
        _ros = ROSConnection.GetOrCreateInstance();
    }

    void Start()
    {
        Debug.Log("ControlInputStream Start() called");

        _msgType = "crs_msgs/car_input";

        _ros.Subscribe<Car_inputMsg>(topicName, OnControlInput);
        Debug.Log($"Subscribed to {topicName}");

        // Create follow car visualization
        CreateFollowCarVisualization();

        // Setup UI elements
        SetupUIElements();

        // Start coroutine to find car
        StartCoroutine(FindCarCoroutine());

        // Delay the initial visibility update to next frame
        StartCoroutine(InitializeVisualizationMode());
    }

    private IEnumerator InitializeVisualizationMode()
    {
        // Wait one frame for UI to be fully initialized
        yield return null;
        UpdateVisualizationMode();
    }

    private IEnumerator FindCarCoroutine()
    {
        int attempts = 0;
        while (carObject == null && attempts < 100)
        {
            TryFindCar();
            if (carObject != null)
            {
                Debug.Log($"ControlInputStream: Auto-found car after {attempts} attempts!");
                yield break;
            }
            attempts++;
            yield return new WaitForSeconds(0.05f);
        }

        if (carObject == null)
        {
            Debug.LogWarning("ControlInputStream: Could not auto-find car. Please assign manually in Inspector.");
        }
    }

    private void TryFindCar()
    {
        if (carObject != null) return;

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "Car" && obj.scene.isLoaded && obj.hideFlags == HideFlags.None)
            {
                carObject = obj;
                return;
            }
        }

        CarStream[] carStreams = FindObjectsOfType<CarStream>();
        foreach (CarStream carStream in carStreams)
        {
            foreach (Transform child in carStream.transform)
            {
                if (child.name == "Car")
                {
                    carObject = child.gameObject;
                    return;
                }
            }
        }
    }

    private void SetupUIElements()
    {
        _torqueBarFillPositive = torqueBarPanel.transform.Find("TorqueBarFillPositive").GetComponent<Image>();
        _torqueBarFillNegative = torqueBarPanel.transform.Find("TorqueBarFillNegative").GetComponent<Image>();
        _torqueValueText = torqueBarPanel.transform.Find("TorqueValueText").GetComponent<TextMeshProUGUI>();
        _torqueLabelTop = torqueBarPanel.transform.Find("TorqueLabelTop").GetComponent<TextMeshProUGUI>();
        _torqueLabelBottom = torqueBarPanel.transform.Find("TorqueLabelBottom").GetComponent<TextMeshProUGUI>();
        _torqueBarBackground = torqueBarPanel.transform.Find("TorqueBarBackground").GetComponent<Image>();
        _torqueCenterLine = torqueBarPanel.transform.Find("TorqueCenterLine").GetComponent<Image>();
        _steeringWheelBase = steeringWheelPanel.transform.Find("SteeringWheelBase").GetComponent<Image>();
        _steeringText = steeringWheelPanel.transform.Find("SteeringText").GetComponent<TextMeshProUGUI>();

        // Steering Wheel Setup
        if (_steeringWheelBase != null)
        {
            // Wheel will be rotated as a whole
        }

        // F1-Style Torque Bar Setup
        if (_torqueBarBackground != null)
        {
            _torqueBarBackground.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        }

        if (_torqueBarFillPositive != null)
        {
            _torqueBarFillPositive.type = Image.Type.Filled;
            _torqueBarFillPositive.fillMethod = Image.FillMethod.Vertical;
            _torqueBarFillPositive.fillOrigin = (int)Image.OriginVertical.Bottom;
            _torqueBarFillPositive.fillAmount = 0f;
            _torqueBarFillPositive.color = Color.green;
        }

        if (_torqueBarFillNegative != null)
        {
            _torqueBarFillNegative.type = Image.Type.Filled;
            _torqueBarFillNegative.fillMethod = Image.FillMethod.Vertical;
            _torqueBarFillNegative.fillOrigin = (int)Image.OriginVertical.Top;
            _torqueBarFillNegative.fillAmount = 0f;
            _torqueBarFillNegative.color = Color.red;
        }

        if (_torqueCenterLine != null)
        {
            _torqueCenterLine.color = new Color(1f, 1f, 1f, 0.8f);
        }

        if (_torqueLabelTop != null)
        {
            _torqueLabelTop.text = "ACCEL";
            _torqueLabelTop.color = Color.green;
            _torqueLabelTop.fontSize = 7;
            _torqueLabelTop.fontStyle = FontStyles.Bold;
        }

        if (_torqueLabelBottom != null)
        {
            _torqueLabelBottom.text = "BRAKE";
            _torqueLabelBottom.color = Color.red;
            _torqueLabelBottom.fontSize = 7;
            _torqueLabelBottom.fontStyle = FontStyles.Bold;
        }

        if (_torqueValueText != null)
        {
            _torqueValueText.fontSize = 8;
            _torqueValueText.fontStyle = FontStyles.Bold;
            _torqueValueText.color = Color.white;
        }
    }

    // ========== FOLLOW CAR MODE ==========
    private void CreateFollowCarVisualization()
    {
        // Steering arrow
        steeringArrowObj = new GameObject("SteeringArrow");
        steeringArrowObj.transform.SetParent(transform);

        steeringArrow = steeringArrowObj.AddComponent<LineRenderer>();
        steeringArrow.material = new Material(Shader.Find("Sprites/Default"));
        steeringArrow.startColor = Color.red;
        steeringArrow.endColor = Color.red;
        steeringArrow.startWidth = 0.05f;
        steeringArrow.endWidth = 0.025f;
        steeringArrow.positionCount = 2;
        steeringArrow.useWorldSpace = true;

        // Torque bar
        torqueBarObj = new GameObject("TorqueBar");
        torqueBarObj.transform.SetParent(transform);

        torqueBar = torqueBarObj.AddComponent<LineRenderer>();
        torqueBar.material = new Material(Shader.Find("Sprites/Default"));
        torqueBar.startColor = Color.green;
        torqueBar.endColor = Color.green;
        torqueBar.startWidth = 0.1f;
        torqueBar.endWidth = 0.05f;
        torqueBar.positionCount = 2;
        torqueBar.useWorldSpace = true; // ← ADD THIS LINE

        Debug.Log("Created Follow Car visualization");
    }

    public void SetVisualizationMode(int mode)
    {
        currentMode = (VisualizationMode)mode;
        UpdateVisualizationMode();
    }

    private void UpdateVisualizationMode()
    {
        bool showFollowCar = (currentMode == VisualizationMode.FollowCar);
        bool showHUD = (currentMode == VisualizationMode.ScreenHUD);

        // Toggle 3D visualizations
        if (steeringArrowObj != null) steeringArrowObj.SetActive(showFollowCar);
        if (torqueBarObj != null) torqueBarObj.SetActive(showFollowCar);

        // Toggle UI panels
        if (steeringWheelPanel != null) steeringWheelPanel.SetActive(showHUD);
        if (torqueBarPanel != null) torqueBarPanel.SetActive(showHUD);
        if (showHUD && steeringWheelPanel != null && torqueBarPanel != null) SetupUIElements();
    }

    private void OnControlInput(Car_inputMsg msg)
    {
        currentSteering = (float)msg.steer;
        currentTorque = (float)msg.torque;
    }

    void Update()
    {
        // Toggle with V key (for desktop)
        bool togglePressed = Input.GetKeyDown(KeyCode.V);

        // Toggle with Quest controller A button (right hand)
        InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightController.isValid)
        {
            bool buttonValue;
            if (rightController.TryGetFeatureValue(CommonUsages.primaryButton, out buttonValue))
            {
                if (buttonValue && !wasButtonPressed) // Simple debounce
                {
                    togglePressed = true;
                }
                wasButtonPressed = buttonValue;
            }
        }

        if (togglePressed)
        {
            currentMode = (currentMode == VisualizationMode.FollowCar)
                ? VisualizationMode.ScreenHUD
                : VisualizationMode.FollowCar;
            UpdateVisualizationMode();
            Debug.Log($"Switched to {currentMode} mode");
        }

        if (currentMode == VisualizationMode.FollowCar)
        {
            UpdateFollowCarVisualization();
        }
        else if (currentMode == VisualizationMode.ScreenHUD)
        {
            UpdateUIVisualization();
        }
        else
        {
            // Hide mode - do nothing
        }
    }

    // ========== FOLLOW CAR UPDATE ==========
    private void UpdateFollowCarVisualization()
    {
        if (carObject == null) return;

        Vector3 carPosition = carObject.transform.position;
        Quaternion carRotation = carObject.transform.rotation;

        // Update steering arrow
        if (steeringArrow != null)
        {
            Vector3 start = carPosition + Vector3.up * arrowHeight;
            Vector3 steerDirection = carRotation * Quaternion.Euler(0, -currentSteering * Mathf.Rad2Deg, 0) * Vector3.forward;
            Vector3 end = start + steerDirection * arrowLength;

            steeringArrow.SetPosition(0, start);
            steeringArrow.SetPosition(1, end);
        }

        // Update torque bar
        if (torqueBar != null)
        {
            Vector3 basePos = carPosition + carRotation * (Vector3.right * barOffset);
            Vector3 start = basePos;
            Vector3 end = basePos + Vector3.up * (currentTorque * barHeight);

            torqueBar.SetPosition(0, start);
            torqueBar.SetPosition(1, end);

            Color torqueColor = currentTorque >= 0 ? Color.green : Color.red;
            torqueBar.startColor = torqueColor;
            torqueBar.endColor = torqueColor;
        }
    }

    // ========== UI HUD UPDATE ==========

    private void UpdateUIVisualization()
    {
        UpdateSteeringWheelUI();
        UpdateF1TorqueBarUI();
    }

    private void UpdateSteeringWheelUI()
    {
        // Rotate the entire steering wheel based on steering angle
        if (_steeringWheelBase != null)
        {
            // Amplify rotation for better visibility
            float rotationAngle = -currentSteering * Mathf.Rad2Deg * 2.0f;
            _steeringWheelBase.rectTransform.localRotation = Quaternion.Euler(0, 0, -rotationAngle);
        }

        // Update text
        if (_steeringText != null)
        {
            _steeringText.text = $"STEER\n{(currentSteering * Mathf.Rad2Deg):F1}°";

            // Color based on steering amount
            float steerPercent = Mathf.Abs(currentSteering / maxSteeringAngle);
            _steeringText.color = Color.Lerp(Color.white, Color.red, steerPercent);
        }
    }

    private void UpdateF1TorqueBarUI()
    {
        float clampedTorque = Mathf.Clamp(currentTorque, -1f, 1f);

        // Update positive bar (throttle - fills upward)
        if (_torqueBarFillPositive != null)
        {
            if (clampedTorque > 0)
            {
                _torqueBarFillPositive.fillAmount = clampedTorque;
                // Gradient from light green to bright green
                _torqueBarFillPositive.color = Color.Lerp(new Color(0.4f, 1f, 0.4f), Color.green, clampedTorque);
            }
            else
            {
                _torqueBarFillPositive.fillAmount = 0f;
            }
        }

        // Update negative bar (brake - fills downward)
        if (_torqueBarFillNegative != null)
        {
            if (clampedTorque < 0)
            {
                _torqueBarFillNegative.fillAmount = -clampedTorque;
                // Gradient from light red to bright red
                _torqueBarFillNegative.color = Color.Lerp(new Color(1f, 0.4f, 0.4f), Color.red, -clampedTorque);
            }
            else
            {
                _torqueBarFillNegative.fillAmount = 0f;
            }
        }

        // Update value text
        if (_torqueValueText != null)
        {
            _torqueValueText.text = $"{(currentTorque * 100f):F0}%";

            // Color text based on current action
            if (currentTorque > 0.1f)
                _torqueValueText.color = Color.green;
            else if (currentTorque < -0.1f)
                _torqueValueText.color = Color.red;
            else
                _torqueValueText.color = Color.white;
        }

        // Highlight active label
        if (_torqueLabelTop != null && _torqueLabelBottom != null)
        {
            if (currentTorque > 0.1f)
            {
                _torqueLabelTop.color = Color.green;
                _torqueLabelBottom.color = new Color(0.3f, 0.3f, 0.3f);
            }
            else if (currentTorque < -0.1f)
            {
                _torqueLabelTop.color = new Color(0.3f, 0.3f, 0.3f);
                _torqueLabelBottom.color = Color.red;
            }
            else
            {
                _torqueLabelTop.color = new Color(0.3f, 0.6f, 0.3f);
                _torqueLabelBottom.color = new Color(0.6f, 0.3f, 0.3f);
            }
        }
    }

    public override void OnTopicChange(string newTopic)
    {
        if (!string.IsNullOrEmpty(topicName))
        {
            _ros.Unsubscribe(topicName);
        }

        topicName = newTopic;

        if (!string.IsNullOrEmpty(topicName) && topicName != "None")
        {
            _ros.Subscribe<Car_inputMsg>(topicName, OnControlInput);
        }
    }

    public override void ToggleTrack(int mode)
    {
        _trackingState = mode;
    }

    void OnDestroy()
    {
        if (!string.IsNullOrEmpty(topicName))
        {
            _ros.Unsubscribe(topicName);
        }
    }
}