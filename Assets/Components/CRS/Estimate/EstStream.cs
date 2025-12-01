using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.Crs;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using UnityEngine.XR;

public class EstimatorCarStream : SensorStream
{
    public GameObject carPrefab;
    private GameObject carInstance;
    public float carScale = 0.4f;
    public Color estimatorColor = Color.blue;
    private Material carMaterial;
    
    public bool showEstimator = true;

    [Header("Estimator Trail Settings")]
    public bool showTrail = true;
    private GameObject trailObject;
    private TrailRenderer trail;
    public float trailTime = 3f;
    public Color trailColor = Color.blue;

    private KeyCode toggleCarKey = KeyCode.F;
    private KeyCode toggleTrailKey = KeyCode.G;
    
    // Quest controller button mappings (Left Hand)
    private InputFeatureUsage<bool> questToggleCarButton = CommonUsages.menuButton;
    private InputFeatureUsage<bool> questToggleTrailButton = CommonUsages.gripButton;
    
    private bool wasCarButtonPressed = false;
    private bool wasTrailButtonPressed = false;

    void Awake()
    {
        _ros = ROSConnection.GetOrCreateInstance();
        
    }

    void Start()
    {
        _msgType = "crs_msgs/car_state_cart";
        _ros.Subscribe<Car_state_cartMsg>(topicName, OnEstimatorState);

        carMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        carMaterial.SetColor("_BaseColor", estimatorColor);

        if (carPrefab != null)
        {
            carInstance = Instantiate(carPrefab, Vector3.zero, Quaternion.identity);
            carInstance.transform.SetParent(transform);
            carInstance.name = "EstimatorCar";
            carInstance.transform.localScale = Vector3.one * carScale;

            Renderer[] renderers = carInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material = carMaterial;
            }
            
            carInstance.SetActive(showEstimator);
        }
        
        // Create separate trail object
        trailObject = new GameObject("EstimatorTrail");
        trailObject.transform.SetParent(transform);
        trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.startWidth = 0.005f;
        trail.endWidth = 0.0025f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = trailColor;
        trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0);
        
        trailObject.SetActive(showTrail);
    }

    void Update()
    {
        // Handle keyboard input (Desktop)
        if (Input.GetKeyDown(toggleCarKey))
        {
            ToggleCarVisibility();
        }
        
        if (Input.GetKeyDown(toggleTrailKey))
        {
            ToggleTrailVisibility();
        }

        // Handle Quest controller input
        HandleQuestInput();

        // Update visibility
        if (carInstance != null)
        {
            carInstance.SetActive(showEstimator);
        }
        
        if (trailObject != null)
        {
            trailObject.SetActive(showTrail);
        }
    }

    public void ToggleCarVisibility()
    {
        showEstimator = !showEstimator;
    }
    public void ToggleTrailVisibility()
    {
        showTrail = !showTrail;
    }

    private void HandleQuestInput()
    {
        // Get the left controller
        InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        
        if (leftController.isValid)
        {
            // Toggle car visibility
            bool carButtonPressed;
            if (leftController.TryGetFeatureValue(questToggleCarButton, out carButtonPressed))
            {
                if (carButtonPressed && !wasCarButtonPressed)
                {
                    showEstimator = !showEstimator;
                }
                wasCarButtonPressed = carButtonPressed;
            }
            
            // Toggle trail visibility
            bool trailButtonPressed;
            if (leftController.TryGetFeatureValue(questToggleTrailButton, out trailButtonPressed))
            {
                if (trailButtonPressed && !wasTrailButtonPressed)
                {
                    showTrail = !showTrail;
                }
                wasTrailButtonPressed = trailButtonPressed;
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
        
        if (!string.IsNullOrEmpty(topicName))
        {
            _ros.Subscribe<Car_state_cartMsg>(topicName, OnEstimatorState);
        }
    }

    public override void ToggleTrack(int mode)
    {
        _trackingState = mode;
    }

    private void OnEstimatorState(Car_state_cartMsg msg)
    {
        if (carInstance == null) return;

        // Position
        PointMsg rosPosition = new(msg.x, msg.y, msg.z);
        Vector3 unityPosition = rosPosition.From<FLU>();
        
        carInstance.transform.position = unityPosition;
        
        if (trailObject != null)
        {
            trailObject.transform.position = unityPosition;
        }

        // Rotation
        float yawDegrees = (float)msg.yaw * Mathf.Rad2Deg;
        carInstance.transform.rotation = Quaternion.Euler(0, -yawDegrees, 0);
    }

    void OnDestroy()
    {
        if (carInstance != null)
        {
            Destroy(carInstance);
        }
        
        if (trailObject != null)
        {
            Destroy(trailObject);
        }

        if (!string.IsNullOrEmpty(topicName))
        {
            _ros.Unsubscribe(topicName);
        }
    }
}
