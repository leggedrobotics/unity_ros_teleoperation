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
        FollowCar,      // Original arrows following the car
        ScreenHUD       // UI-based steering wheel + F1 style bar
    }
    
    [Header("Visualization Mode")]
    public VisualizationMode currentMode = VisualizationMode.FollowCar;
    
    [Header("Follow Car Mode Settings")]
    public float arrowLength = 0.3f;
    public float arrowHeight = 0.25f;
    public float barHeight = 0.5f;
    public float barOffset = 0.25f;
    
    [Header("Steering Wheel UI")]
    public GameObject steeringWheelPanel;
    public Image steeringWheelBase;
    public TextMeshProUGUI steeringText;
    
    [Header("F1-Style Torque Bar UI")]
    public GameObject torqueBarPanel;
    public Image torqueBarBackground;
    public Image torqueBarFillPositive;  // Green bar for throttle (fills up)
    public Image torqueBarFillNegative;  // Red bar for brake (fills down)
    public Image torqueCenterLine;

    public TextMeshProUGUI torqueLabelTop;
    public TextMeshProUGUI torqueLabelBottom;
    public TextMeshProUGUI torqueValueText;
    
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
    topicName = "/car_1/control_input";
    
    _ros.Subscribe<Car_inputMsg>(topicName, OnControlInput);
    Debug.Log($"Subscribed to {topicName}");
    
    // Create follow car visualization
    CreateFollowCarVisualization();
    
    // Setup UI elements
    SetupUIElements();

    // Start coroutine to find car
    StartCoroutine(FindCarCoroutine());

        currentMode = VisualizationMode.FollowCar;
    
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
        // Steering Wheel Setup
        if (steeringWheelBase != null)
        {
            // Wheel will be rotated as a whole
        }
        
        // F1-Style Torque Bar Setup
        if (torqueBarBackground != null)
        {
            torqueBarBackground.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        }
        
        if (torqueBarFillPositive != null)
        {
            torqueBarFillPositive.type = Image.Type.Filled;
            torqueBarFillPositive.fillMethod = Image.FillMethod.Vertical;
            torqueBarFillPositive.fillOrigin = (int)Image.OriginVertical.Bottom;
            torqueBarFillPositive.fillAmount = 0f;
            torqueBarFillPositive.color = Color.green;
        }
        
        if (torqueBarFillNegative != null)
        {
            torqueBarFillNegative.type = Image.Type.Filled;
            torqueBarFillNegative.fillMethod = Image.FillMethod.Vertical;
            torqueBarFillNegative.fillOrigin = (int)Image.OriginVertical.Top;
            torqueBarFillNegative.fillAmount = 0f;
            torqueBarFillNegative.color = Color.red;
        }
        
        if (torqueCenterLine != null)
        {
            torqueCenterLine.color = new Color(1f, 1f, 1f, 0.8f);
        }
        
        if (torqueLabelTop != null)
        {
            torqueLabelTop.text = "ACCEL";
            torqueLabelTop.color = Color.green;
            torqueLabelTop.fontSize = 7;
            torqueLabelTop.fontStyle = FontStyles.Bold;
        }
        
        if (torqueLabelBottom != null)
        {
            torqueLabelBottom.text = "BRAKE";
            torqueLabelBottom.color = Color.red;
            torqueLabelBottom.fontSize = 7;
            torqueLabelBottom.fontStyle = FontStyles.Bold;
        }
        
        if (torqueValueText != null)
        {
            torqueValueText.fontSize = 8;
            torqueValueText.fontStyle = FontStyles.Bold;
            torqueValueText.color = Color.white;
        }
    }

    // ========== FOLLOW CAR MODE ==========
private void CreateFollowCarVisualization()
{
    // Steering arrow
    steeringArrowObj = new GameObject("SteeringArrow");
    steeringArrowObj.transform.SetParent(transform); // Good - parents to this script's GameObject
    
    steeringArrow = steeringArrowObj.AddComponent<LineRenderer>();
    steeringArrow.material = new Material(Shader.Find("Sprites/Default"));
    steeringArrow.startColor = Color.red;
    steeringArrow.endColor = Color.red;
    steeringArrow.startWidth = 0.05f;
    steeringArrow.endWidth = 0.025f;
    steeringArrow.positionCount = 2;
    steeringArrow.useWorldSpace = true; // ← ADD THIS LINE
    
    // Torque bar
    torqueBarObj = new GameObject("TorqueBar");
    torqueBarObj.transform.SetParent(transform); // Good - parents to this script's GameObject
    
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
    else
    {
        UpdateUIVisualization();
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
            Vector3 steerDirection = carRotation * Quaternion.Euler(0, currentSteering * Mathf.Rad2Deg, 0) * Vector3.forward;
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
        if (steeringWheelBase != null)
        {
            // Amplify rotation for better visibility
            float rotationAngle = currentSteering * Mathf.Rad2Deg * 2.0f;
            steeringWheelBase.rectTransform.localRotation = Quaternion.Euler(0, 0, -rotationAngle);
        }
        
        // Update text
        if (steeringText != null)
        {
            steeringText.text = $"STEER\n{(currentSteering * Mathf.Rad2Deg):F1}°";
            
            // Color based on steering amount
            float steerPercent = Mathf.Abs(currentSteering / maxSteeringAngle);
            steeringText.color = Color.Lerp(Color.white, Color.red, steerPercent);
        }
    }

    private void UpdateF1TorqueBarUI()
    {
        float clampedTorque = Mathf.Clamp(currentTorque, -1f, 1f);
        
        // Update positive bar (throttle - fills upward)
        if (torqueBarFillPositive != null)
        {
            if (clampedTorque > 0)
            {
                torqueBarFillPositive.fillAmount = clampedTorque;
                // Gradient from light green to bright green
                torqueBarFillPositive.color = Color.Lerp(new Color(0.4f, 1f, 0.4f), Color.green, clampedTorque);
            }
            else
            {
                torqueBarFillPositive.fillAmount = 0f;
            }
        }
        
        // Update negative bar (brake - fills downward)
        if (torqueBarFillNegative != null)
        {
            if (clampedTorque < 0)
            {
                torqueBarFillNegative.fillAmount = -clampedTorque;
                // Gradient from light red to bright red
                torqueBarFillNegative.color = Color.Lerp(new Color(1f, 0.4f, 0.4f), Color.red, -clampedTorque);
            }
            else
            {
                torqueBarFillNegative.fillAmount = 0f;
            }
        }
        
        // Update value text
        if (torqueValueText != null)
        {
            torqueValueText.text = $"{(currentTorque * 100f):F0}%";
            
            // Color text based on current action
            if (currentTorque > 0.1f)
                torqueValueText.color = Color.green;
            else if (currentTorque < -0.1f)
                torqueValueText.color = Color.red;
            else
                torqueValueText.color = Color.white;
        }
        
        // Highlight active label
        if (torqueLabelTop != null && torqueLabelBottom != null)
        {
            if (currentTorque > 0.1f)
            {
                torqueLabelTop.color = Color.green;
                torqueLabelBottom.color = new Color(0.3f, 0.3f, 0.3f);
            }
            else if (currentTorque < -0.1f)
            {
                torqueLabelTop.color = new Color(0.3f, 0.3f, 0.3f);
                torqueLabelBottom.color = Color.red;
            }
            else
            {
                torqueLabelTop.color = new Color(0.3f, 0.6f, 0.3f);
                torqueLabelBottom.color = new Color(0.6f, 0.3f, 0.3f);
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