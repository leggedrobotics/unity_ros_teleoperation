using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine.XR.Interaction.Toolkit;


#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(PosePublisher))]
public class PosePublisherEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PosePublisher myScript = (PosePublisher)target;
        if (GUILayout.Button("Cancel"))
        {
            // myScript.Cancel();
        }
        if (GUILayout.Button("Publish"))
        {
            myScript.LastSelected(new SelectExitEventArgs());
        }
    }
}
#endif

public class PosePublisher : MonoBehaviour
{
    public string poseTopic;
    public string missionTopic;
    public string frame_id = "odom";
    public GameObject arrow;
    public bool debug = false;

    private ROSConnection ros;
    private PoseStampedMsg poseMsg;
    private Vector3 start;
    private GameObject arrowInstance;

    private XRRayInteractor interactor;

    private bool _enabled = true;
    private bool _sent = false;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();

        poseMsg = new PoseStampedMsg();
        poseMsg.header.frame_id = frame_id;
        poseMsg.pose = new PoseMsg();

     

        ros.RegisterPublisher<PoseStampedMsg>(poseTopic);
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    void Update()
    {
        if(_enabled && arrowInstance != null && interactor != null)
        {
            //point arrow at interactor position
            Vector3 end;
            interactor.TryGetHitInfo(out end, out _, out _, out _);
            arrowInstance.transform.LookAt(end);
        }
    }

    public void Confirm()
    {
        if(!_enabled || _sent) return;

        // publish pose
        ros.Send(poseTopic, poseMsg);

        Debug.Log("published pose");

        start = Vector3.zero;
        interactor = null;
        _sent = true;
    }


    public void FirstSelected(SelectEnterEventArgs args)
    {
        if(!_enabled) return;

        Vector3 tmp;
        ((XRRayInteractor)args.interactor).TryGetHitInfo(out tmp, out _, out _, out _);

        start = tmp;//transform.parent.InverseTransformPoint(tmp);
    
        interactor = (XRRayInteractor)args.interactor;

        if (arrowInstance == null)
        {
            arrowInstance = Instantiate(arrow, start, Quaternion.identity, transform.parent);

        }else
        {
            arrowInstance.transform.position = start;
        }
    }

    public void LastSelected(SelectExitEventArgs args)
    {
        Vector3 end;
        ((XRRayInteractor)args.interactor).TryGetHitInfo(out end, out _, out _, out _);
        if (debug)
        {
            Debug.DrawLine(start, end, Color.red, 10);
        }

        start = transform.parent.InverseTransformPoint(start);
        end = transform.parent.InverseTransformPoint(end);

        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, end - start);
        poseMsg.pose.position = (PointMsg)start.To<FLU>();
        poseMsg.pose.orientation = rotation.To<FLU>();

        _sent = false;

    }

}
