using System.Collections;
using System.Collections.Generic;
using RosMessageTypes.Crs;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

public class CarStream : SensorStream
{
    public GameObject carPrefab;
    private GameObject carInstance;
    public float carScale = 0.4f;
    public Material carMaterial;
    
    public bool showCar = true;

    // Trail settings
    public bool showTrail = true;
    private GameObject trailObject;
    private TrailRenderer trail;
    public float trailTime = 3f;
    public Color trailColor = Color.white;

    // Input settings
    [Header("Input Settings")]
    private KeyCode toggleCarKey = KeyCode.J;
    private KeyCode toggleTrailKey = KeyCode.H;
    
    // Quest controller button mappings (Right Hand)
    private InputFeatureUsage<bool> questToggleCarButton = CommonUsages.menuButton;
    private InputFeatureUsage<bool> questToggleTrailButton = CommonUsages.gripButton;
    
    private bool wasCarButtonPressed = false;
    private bool wasTrailButtonPressed = false;

    void Awake()
    {
        _ros = ROSConnection.GetOrCreateInstance();

        GameObject sceneRootObj = GameObject.Find("SceneRoot");
        if (sceneRootObj != null)
        {
            transform.SetParent(sceneRootObj.transform);
        }
    }

    void Start()
    {
        _msgType = "crs_msgs/car_state_cart";
        _ros.Subscribe<Car_state_cartMsg>(topicName, OnCarState);

        if (carMaterial == null)
        {
            carMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            carMaterial.SetColor("_BaseColor", Color.white);
        }

        if (carPrefab != null)
        {
            carInstance = Instantiate(carPrefab, Vector3.zero, Quaternion.identity);
            carInstance.transform.SetParent(transform);
            carInstance.name = "Car";
            carInstance.transform.localScale = Vector3.one * carScale;

            Renderer[] renderers = carInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material = carMaterial;
            }
            
            carInstance.SetActive(showCar);
        }

        // Create separate trail object
        trailObject = new GameObject("CarTrail");
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
            showCar = !showCar;
        }
        
        if (Input.GetKeyDown(toggleTrailKey))
        {
            showTrail = !showTrail;
        }

        // Handle Quest controller input
        HandleQuestInput();

        // Update visibility
        if (carInstance != null)
        {
            carInstance.SetActive(showCar);
        }
        
        if (trailObject != null)
        {
            trailObject.SetActive(showTrail);
        }
    }

    private void HandleQuestInput()
    {
        // Get the right controller
        InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        
        if (rightController.isValid)
        {
            // Toggle car visibility
            bool carButtonPressed;
            if (rightController.TryGetFeatureValue(questToggleCarButton, out carButtonPressed))
            {
                if (carButtonPressed && !wasCarButtonPressed)
                {
                    showCar = !showCar;
                }
                wasCarButtonPressed = carButtonPressed;
            }
            
            // Toggle trail visibility
            bool trailButtonPressed;
            if (rightController.TryGetFeatureValue(questToggleTrailButton, out trailButtonPressed))
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
    }

    public override void ToggleTrack(int mode)
    {
        _trackingState = mode;
    }

    private void OnCarState(Car_state_cartMsg msg)
    {
        if (carInstance == null)
            return;


        // Position
        PointMsg rosPosition = new(msg.x, msg.y, msg.z);
        Vector3 unityPosition = rosPosition.From<FLU>();
        // carInstance.transform.position = unityPosition;
        carInstance.transform.localPosition = unityPosition;

        // Update trail position
        if (trailObject != null)
        {
            trailObject.transform.localPosition = unityPosition;
            // trailObject.transform.position = unityPosition;
        }

        // Rotation
        float yawDegrees = (float)msg.yaw * Mathf.Rad2Deg;
        // carInstance.transform.rotation = Quaternion.Euler(0, -yawDegrees, 0);
        carInstance.transform.localRotation = Quaternion.Euler(0, -yawDegrees, 0);
    }

    void OnDestroy()
    {
        // Clean up
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
