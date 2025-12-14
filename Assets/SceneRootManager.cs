using UnityEngine;
using UnityEngine.InputSystem;

public class SceneRootManualAlignment : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference leftJoystick;
    public InputActionReference rightJoystick;
    public InputActionReference backButton; // Menu/Select button

    [Header("Movement Settings")]
    public float translationSpeed = 1.0f;
    public float rotationSpeed = 30.0f;
    public float scaleSpeed = 0.1f;

    [Header("Alignment Mode")]
    public bool alignmentModeActive = false;

    [Header("VR Camera Reference")]
    public Transform vrCamera;

    private Transform sceneRoot;

    void Start()
    {
        sceneRoot = transform;

        if (vrCamera == null)
        {
            vrCamera = Camera.main?.transform;
        }
    }

    void Update()
    {
        if (alignmentModeActive)
        {
            HandleAlignment();
        }
    }

    public void ToggleAlignmentMode()
    {
        alignmentModeActive = !alignmentModeActive;
    }

    public void SetAlignmentMode(bool active)
    {
        alignmentModeActive = active;
        Debug.Log($"Manual Alignment Mode: {(alignmentModeActive ? "ACTIVE" : "INACTIVE")}");
    }

    private void HandleAlignment()
    {
        Vector2 leftStick = leftJoystick.action.ReadValue<Vector2>();
        Vector2 rightStick = rightJoystick.action.ReadValue<Vector2>();
        bool backPressed = backButton.action.ReadValue<float>() > 0.5f;

        if (backPressed)
        {
            float scaleChange = leftStick.y * scaleSpeed * Time.deltaTime;
            Vector3 newScale = sceneRoot.localScale + Vector3.one * scaleChange;
            
            if (newScale.x > 0.1f && newScale.y > 0.1f && newScale.z > 0.1f)
            {
                sceneRoot.localScale = newScale;
            }
        }
        else
        {
            Vector3 cameraForward = vrCamera.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();
            
            Vector3 cameraRight = vrCamera.right;
            cameraRight.y = 0;
            cameraRight.Normalize();
            
            // Transform joystick input to be relative to camera orientation
            Vector3 movement = (cameraRight * leftStick.x + cameraForward * leftStick.y) 
                               * translationSpeed * Time.deltaTime;
            
            sceneRoot.Translate(movement, Space.World);

            // Right stick: Rotation (Y-axis yaw)
            float rotation = rightStick.x * rotationSpeed * Time.deltaTime;
            sceneRoot.Rotate(Vector3.up, rotation, Space.World);
        }
    }

    private void OnEnable()
    {
        leftJoystick?.action.Enable();
        rightJoystick?.action.Enable();
        backButton?.action.Enable();
    }

    private void OnDisable()
    {
        leftJoystick?.action.Disable();
        rightJoystick?.action.Disable();
        backButton?.action.Disable();
    }
}