using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using RosMessageTypes.RetargetingRos;
using TMPro;
using UnityEngine.InputSystem;


public class HandPub : MonoBehaviour
{
    // maps openxr joint indices to mano joint indices  {mano index, openxr enum}
    private static Dictionary<int, XRHandJointID> jointMap = new Dictionary<int, XRHandJointID> {
        {0, XRHandJointID.Wrist},
        {1, XRHandJointID.ThumbMetacarpal},
        {2, XRHandJointID.ThumbProximal},
        {3, XRHandJointID.ThumbDistal},
        {4, XRHandJointID.ThumbTip},
        {5, XRHandJointID.IndexProximal},
        {6, XRHandJointID.IndexIntermediate},
        {7, XRHandJointID.IndexDistal},
        {8, XRHandJointID.IndexTip},
        {9, XRHandJointID.MiddleProximal},
        {10, XRHandJointID.MiddleIntermediate},
        {11, XRHandJointID.MiddleDistal},
        {12, XRHandJointID.MiddleTip},
        {13, XRHandJointID.RingProximal},
        {14, XRHandJointID.RingIntermediate},
        {15, XRHandJointID.RingDistal},
        {16, XRHandJointID.RingTip},
        {17, XRHandJointID.LittleProximal},
        {18, XRHandJointID.LittleIntermediate},
        {19, XRHandJointID.LittleDistal},
        {20, XRHandJointID.LittleTip}
    };

    public InputActionReference activeController;
    public InputActionReference twistController;
    public InputActionReference poseController;


    public TextMeshProUGUI infoText;
    private PoseManager poseManager;

    ROSConnection ros;
    XRHandSubsystem m_handSubsystem;

    private bool _highConfidence = false;
    private const string _landmarksTopic = "/hand_pose";
    private const string _pointCloudTopic = "/hand_points";
    private const string _gestureTopic = "/hand_gesture";

    public string worldFrame = "quest_origin";

    void Start()
    {
        poseManager = PoseManager.Instance;

        if(poseManager == null)
        {
            Debug.LogError("PoseManager not found!");
        }

        var _handSubsystem = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(_handSubsystem);
        Debug.Log("Found " + _handSubsystem.Count + " hand subsystems");
        foreach (var hand in _handSubsystem)
        {
            if(hand.running)
            {
                m_handSubsystem = hand;
                break;
            }
        }

        if(m_handSubsystem == null)
        {
            Debug.LogError("No running hand subsystem found");
        } else {
            Debug.Log("Found running hand subsystem");
            m_handSubsystem.updatedHands += OnHandUpdate;

            // check if the joints in this layout contain the joints we need
            var joints = m_handSubsystem.jointsInLayout;
            for (int i=0; i<27; i++)
            {
                if(!joints[i])
                {
                    string jointNeeded = jointMap.ContainsKey(i) ? "" : "not ";
                    Debug.LogError("Joint " + i + " not found in joint layout, " + jointNeeded + "needed");
                }
            }
            
        }
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ManoLandmarksMsg>(_landmarksTopic);
        ros.RegisterPublisher<PointCloudMsg>(_pointCloudTopic);
        ros.RegisterPublisher<HandGestureMsg>(_gestureTopic);

        // setup action map listeners
        twistController.action.performed += _ => PubTwistController();
        poseController.action.performed += _ => PubPoseController();
    }

    void Update()
    {
        if(activeController.action.ReadValue<float>() > 0.5f)
        {
            PubActiveController();
        } else {
            if(infoText != null)
                infoText.color = Color.red;
        }
    }

    public void PubActiveController()
    {
        HandGestureMsg msg = new HandGestureMsg();
        msg.name = "Closed_Fist";
        ros.Publish(_gestureTopic, msg);
        if(infoText != null)
            infoText.color = Color.green;

    }

    public void PubTwistController()
    {
        HandGestureMsg msg = new HandGestureMsg();
        msg.name = "Thumb_Up";
        ros.Publish(_gestureTopic, msg);
        
        infoText?.SetText("Twist activated");
    }

    public void PubPoseController()
    {
        HandGestureMsg msg = new HandGestureMsg();
        msg.name = "Thump_Down";
        ros.Publish(_gestureTopic, msg);
        infoText?.SetText("Pose activated");
    }

    public void ToggleConfidence()
    {
        _highConfidence = !_highConfidence;
        infoText?.SetText(_highConfidence ? "High Confidence" : "Low Confidence");
    }

    void OnHandUpdate(XRHandSubsystem subsystem, 
        XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
        XRHandSubsystem.UpdateType updateType)
    {
        if(updateSuccessFlags == 0 && _highConfidence) return;
        

        // bypass render update to slightly throttle
        if(updateType != XRHandSubsystem.UpdateType.Dynamic) return;
        
        ManoLandmarksMsg msg = new ManoLandmarksMsg();
        PointCloudMsg pointCloudMsg = new PointCloudMsg();
        HeaderMsg header = new HeaderMsg();
        header.frame_id = worldFrame;
        msg.header = header;
        pointCloudMsg.header = header;
        ChannelFloat32Msg[] channels = new ChannelFloat32Msg[1];
        channels[0] = new ChannelFloat32Msg();
        channels[0].name = "intensity";
        channels[0].values = new float[jointMap.Count];
        Point32Msg[] points = new Point32Msg[jointMap.Count];

        XRHand hand = subsystem.rightHand;
        if(hand.isTracked)
        {
            string mode = "";
            if(subsystem.leftHand.isTracked)
            {
                mode = "Both";
            } else {
                mode = "Right";
            }
            // if(modeText != null)
            // infoText?.SetText(mode + " " + (updateSuccessFlags == 0 ? "Low" : "High"));
            
            int count =0;
            foreach (int i in jointMap.Keys)
            {
                var jointID = jointMap[i];
                if(jointID == XRHandJointID.EndMarker)
                    Debug.LogWarning("end joint....?" + i + " " + jointID);
    
                var trackingData = hand.GetJoint(jointID);
                if(trackingData == null)
                {
                    Debug.LogError("Null tracking data!" + jointID);
                    continue;
                }
                if(trackingData.TryGetPose(out Pose pose))
                {
                    if(points[i] == null)
                        points[i] = new Point32Msg();

                    // Transform poses relative to the pose manager's root
                    if(poseManager.root == null)
                        return;
                    pose.position = poseManager.transform.InverseTransformPoint(pose.position);


                    points[i].x = pose.position.x;
                    points[i].y = pose.position.z;
                    points[i].z = pose.position.y;
                    channels[0].values[i] = 1;

                    count++;
                } else {
                    Debug.LogError("Failed to get pose for joint " + jointID + " " + i);
                    channels[0].values[i] = 0;
                }
            }

            // if(jointsText != null)
            //     jointsText.SetText("Joints: "+count);

            pointCloudMsg.points = points;
            msg.landmarks = CastPoints(points);
            ros.Publish(_landmarksTopic, msg);
            ros.Publish(_pointCloudTopic, pointCloudMsg);
        } 
    }

    public static PointMsg[] CastPoints(Point32Msg[] points)
    {
        PointMsg[] castedPoints = new PointMsg[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            castedPoints[i] = new PointMsg();
            if(points[i] == null) continue;
            castedPoints[i].x = points[i].x;
            castedPoints[i].y = points[i].y;
            castedPoints[i].z = points[i].z;
        }
        return castedPoints;
    }
}
