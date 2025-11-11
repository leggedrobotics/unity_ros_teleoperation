using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine.XR.Interaction.Toolkit;
using RosMessageTypes.Behaviortree;


#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PosePublisher))]
public class PosePublisherEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PosePublisher myScript = (PosePublisher)target;
    }
}
#endif

public class PosePublisher : MonoBehaviour
{
    public string poseTopic;
    public TMPro.TMP_InputField poseTopicInput;
// 
    public string frame_id = "vr_origin";
    public GameObject arrow;
    public bool debug = false;

    private ROSConnection _ros;
    private PoseStampedMsg poseMsg;

    private GameObject arrowInstance;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor interactor;

    private Vector3 start;
    private Vector3 end;

    public bool _enabled = true;
    private bool _sent = false;

    void Start()
    {
        _ros = ROSConnection.GetOrCreateInstance();


        GameObject root = GameObject.FindWithTag("root");
        if (root != null)
            frame_id = root.name;

        poseMsg = new PoseStampedMsg();
        poseMsg.header.frame_id = frame_id;
        poseMsg.pose = new PoseMsg();

        // try to get mission and cancel topic from player prefs
        if (PlayerPrefs.HasKey("poseTopic"))
        {
            poseTopic = PlayerPrefs.GetString("poseTopic");
        }

        poseTopicInput.text = poseTopic;


        _ros.RegisterPublisher<PoseStampedMsg>(poseTopic);
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    void Update()
    {
        if(_enabled && arrowInstance != null )
        {
            // Point arrow at interactor position
            arrowInstance.transform.LookAt(end);
        }
    }

    public void Confirm()
    {
        if(!_enabled || _sent) return;

        // publish pose
        _ros.Publish(poseTopic, poseMsg);

        Debug.Log("[PosePublisher] Published pose");

        _sent = true;
    }

    public void FirstSelected(Vector3 hitPosition)
    {

        if (!_enabled) return;

        start = hitPosition;
        end = hitPosition;

        if (arrowInstance == null)
        {
            arrowInstance = Instantiate(arrow, start, Quaternion.identity, transform.parent);
        }
        else
        {
            arrowInstance.transform.position = start;
        }
    }

    public void FirstSelected(SelectEnterEventArgs args)
    {
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor interactor)
        {
            if (interactor.TryGetHitInfo(out Vector3 hit, out _, out _, out _))
            {
                FirstSelected(hit);
            }
        }
    }

    public void OnPositionUpdate(Vector3 position)
    {
        if (!_enabled) return;

        end = position;

        if (debug)
        {
            Debug.DrawLine(start, end, Color.red, 0.1f);
        }
    }


    public void LastSelected(Vector3 hitPosition)
    {

        end = hitPosition;

        if (debug)
        {
            Debug.DrawLine(start, end, Color.red, 10);
        }

        Vector3 localStart = transform.parent.InverseTransformPoint(start);
        Vector3 localEnd = transform.parent.InverseTransformPoint(end);

        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, localEnd - localStart);

        poseMsg.pose.position = (PointMsg)localStart.To<FLU>();
        poseMsg.pose.orientation = rotation.To<FLU>();

        _sent = false;
        Confirm();
    }

    public void LastSelected(SelectExitEventArgs args)
    {
        Debug.Log("Last Selected (XR)");

        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor interactor)
        {
            if (interactor.TryGetHitInfo(out Vector3 hit, out _, out _, out _))
            {
                LastSelected(hit);
            }
        }
    }

    public void OnPoseTopic(string topic)
    {
        // TODO: unsubscribe from old topic when implemented
        poseTopic = topic;
        _ros.RegisterPublisher<PoseStampedMsg>(topic);
        Debug.Log("Pose topic set to: " + topic);

        //write to player prefs
        PlayerPrefs.SetString("poseTopic", topic);
    }

}
