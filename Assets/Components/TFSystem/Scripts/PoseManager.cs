using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PoseManager))]
public class PoseManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PoseManager myScript = (PoseManager)target;
        if (GUILayout.Button("Center robot"))
        {
            myScript.BaseToLocation(Vector3.zero);
        }
        if (GUILayout.Button("Toggle Fixed Location"))
        {
            myScript.ToggleFixedLocation();
        }
        if (GUILayout.Button("Lock"))
        {
            myScript.SetLocked(true);
        }
        if (GUILayout.Button("Unlock"))
        {
            myScript.SetLocked(false);
        }
    }
}
#endif

public class PoseManager : MonoBehaviour
{
    public static PoseManager Instance { get; private set; }

    public InputActionReference joystickXY;
    public InputActionReference joystickZR;
    public InputActionReference alt;
    public InputActionReference action;
    public InputActionReference bAction;

    public UnityEvent actions;
    public UnityEvent bActions;

    public GameObject sphere;
    public GameObject floor;
    public float speed = 1.0f;
    public float rotationTolerance = 5.0f;
    public bool handSelectable = false;
    public Transform root;
    public Transform _root;
    private Transform _mainCamera;
    private Transform _robot;

    public bool _locked = false;
    private Vector3 _center;
    private Vector3 _forward;

    void Awake()
    {
        // if (Instance != null && Instance != this)
        //     Destroy(this);
        // else
        //     Instance = this;
        Instance = this;
    }

    public static PoseManager GetOrCreateInstance()
    {
        // if (Instance == null)
        // {
        //     GameObject go = new GameObject();
        //     go.name = "PoseManager";
        //     return go.AddComponent<PoseManager>();
        // }
        return Instance;
    }

    void Start()
    {
        if (root == null)
        {
            root = transform;
        }
        // need to ensure this happens after tf init....
        _mainCamera = Camera.main.transform;

        if (actions != null && action == null)
        {
            actions = null;
            Debug.LogWarning("PoseManager: actions set but action not set");
        }

        GameObject robot = GameObject.FindWithTag("robot");

        if (_robot == null)
            Debug.LogWarning("PoseManager: robot not found");        
        else
            _robot = robot.transform;

    }

    void Update()
    {
        if(_robot == null)
        {
            GameObject robot = GameObject.FindWithTag("robot");
            if (robot == null)
            {
                Debug.LogWarning("PoseManager: robot not found");
            }
            _robot = robot.transform;
        }

        while (root.parent != null)
        {
            root = root.parent;
            _root = root;
            Debug.Log("root frame: " + root);

        }

        if(_center != Vector3.zero)
        {
            BaseToLocation(_center);
        }

        if (action?.action.IsPressed() ?? false)
        {
            if (alt?.action.IsPressed() ?? false)
            {
                ResetScale();
            }
            else
            {
                actions?.Invoke();
            }
        }

        if(bAction?.action.IsPressed() ?? false)
        {
            bActions?.Invoke();
        }

        if(_locked)
            return;
        if (joystickXY.action.IsPressed() || joystickZR.action.IsPressed())
        {
            if (alt?.action.IsPressed() ?? false)
                Scale(joystickXY.action.ReadValue<Vector2>());
            else
                Move(joystickXY.action.ReadValue<Vector2>());

            OffsetRotate(joystickZR.action.ReadValue<Vector2>());

            sphere.SetActive(true);
        }
        else
        {
            sphere.SetActive(false);
        }
        if(floor != null)
        {
            floor.transform.position = new Vector3(_root.position.x, 0, _root.position.z);
        }
    }

    public void SetLocked(bool locked)
    {
        Debug.Log(locked);
        _locked = locked;
    }

    void ResetScale()
    {
        _root.localScale = Vector3.one;
    }

    public void ToggleFixedLocation()
    {
        if (_center == Vector3.zero)
        {
            _center = _robot.position;
            _forward = _robot.forward;
        }
        else
        {
            _center = Vector3.zero;
            _forward = Vector3.zero;
        }

    }

    void Move(Vector2 input)
    {
        Vector3 move = new Vector3(input.x, 0, input.y);
        // move = root.TransformDirection(move);

        // get relative to the player's view point
        move = _mainCamera.TransformDirection(move);
        // take into account the gameobject's orientation
        // zero out the vertical component
        move.y = 0;
        // move the gameobject relative to the player regardless of gameobject orientation
        _root.Translate(move * speed * Time.deltaTime, Space.World);
    }

    void Scale(Vector2 input)
    {
        Vector3 scale = new Vector3(input.x, input.x, input.x);
        scale = Vector3.Scale(_root.localScale, scale);
        _root.localScale += scale * speed * Time.deltaTime;
    }

    void OffsetRotate(Vector2 input)
    {
        // offset on the y axis based on forwards/back on second joystick
        Vector3 move = new Vector3(0, input.y, 0);

        // rotate on the x axis based on left/right on second joystick
        _root.Translate(move * speed * Time.deltaTime / 10);
        _root.Rotate(0, input.x * speed * Time.deltaTime * 20, 0);
    }

    public void ClickCb(SelectEnterEventArgs args)
    {
        if (_root == null || !handSelectable) return;

        Vector3 position;
        XRRayInteractor rayInteractor = (XRRayInteractor)args.interactor;
        rayInteractor.TryGetHitInfo(out position, out _, out _, out _);
        _root.position = position;
    }

    public void BaseToLocation(Vector3 position)
    {
        Vector3 offset = position - _robot.position;
        _root.position += offset;

        float angle = Vector3.SignedAngle(_robot.forward, _forward, Vector3.up);
        if (_forward != Vector3.zero && Mathf.Abs(angle) > rotationTolerance)
        {
            _root.Rotate(0, angle, 0);
        }

    }
}
