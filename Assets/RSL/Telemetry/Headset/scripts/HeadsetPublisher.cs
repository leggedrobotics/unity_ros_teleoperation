using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.InputSystem;
using RosMessageTypes.Geometry;
using RosMessageTypes.Tf2;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using TMPro;

namespace RSL.Telemetry.Headset
{
    public class HeadsetPublisher : MonoBehaviour
    {
        public string unityFrame = "vr_origin";
        public string headsetFrame = "headset";
        public string handFrameLeft = "hand_left";
        public string poseTopic = "/quest/pose";
        public TextMeshProUGUI decimatorText;

        public InputActionReference headsetPose;
        public InputActionReference headsetRotation;
        public InputActionReference handPoseLeft;
        public InputActionReference handRotationLeft;
        public InputActionReference handPoseRight;
        public InputActionReference handRotationRight;

        private Transform root;
        private string handFrameRight = "hand_right";
        private ROSConnection ros;
        private TFMessageMsg tfMsg;
        private PoseStampedMsg headsetPoseMsg;
        private PoseStampedMsg leftHandMsg;
        private PoseStampedMsg rightHandMsg;

        private HeaderMsg headsetHeader;
        private HeaderMsg odomHeader;

        private string rootFrame;
        private int _decimator = 1;
        private int _frameCounter = 0;

        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();

            handFrameRight = handFrameLeft.Replace("left", "right");

            root = GameObject.FindWithTag("root").transform;
            if (root == null)
            {
                Debug.LogError("Root not found");
                rootFrame = "odom"; // default to odom if no root is found
            }
            else 
                rootFrame = root.GetComponent<TFAttachment>().FrameID;

            ros.RegisterPublisher<PoseStampedMsg>(poseTopic+"/headset");

            ros.RegisterPublisher<TFMessageMsg>("/tf");

            headsetPoseMsg = new PoseStampedMsg();
            leftHandMsg = new PoseStampedMsg();
            rightHandMsg = new PoseStampedMsg();

            headsetHeader = new HeaderMsg();
            headsetHeader.frame_id = unityFrame;

            odomHeader = new HeaderMsg();
            odomHeader.frame_id = rootFrame;

            tfMsg = new TFMessageMsg(); 

            if (decimatorText != null)
                decimatorText.text = "TF Decimator: " + _decimator;

        }

        void Update()
        {
            // For better performance we can choose to only publish every nth frame
            _frameCounter++;
            if (_frameCounter % _decimator != 0)
                return;

            _frameCounter = 0;


            // TF publishing (preferred means of poses, but sometimes weird with timing)
            tfMsg.transforms = new TransformStampedMsg[4];
            tfMsg.transforms[0] = new TransformStampedMsg();
            HeaderMsg rootHeader = new HeaderMsg();
            rootHeader.frame_id = rootFrame;
            // Publish the pose from odom to unity center
            tfMsg.transforms[0].header = rootHeader;
            tfMsg.transforms[0].child_frame_id = unityFrame;
            tfMsg.transforms[0].transform = new TransformMsg();
            tfMsg.transforms[0].transform.translation = new Vector3Msg();
            tfMsg.transforms[0].transform.rotation = new QuaternionMsg();
            tfMsg.transforms[0].transform.translation = root.InverseTransformPoint(Vector3.zero).To<FLU>();
            tfMsg.transforms[0].transform.rotation = Quaternion.Inverse(root.rotation).To<FLU>();

            // Publish the headset to unity center
            tfMsg.transforms[1] = new TransformStampedMsg();
            tfMsg.transforms[1].header = headsetHeader;
            tfMsg.transforms[1].child_frame_id = headsetFrame;
            tfMsg.transforms[1].transform = new TransformMsg();
            tfMsg.transforms[1].transform.translation = new Vector3Msg();
            tfMsg.transforms[1].transform.rotation = new QuaternionMsg();
            tfMsg.transforms[1].transform.rotation.w = 1;

            // Publish the left hand to unity center
            tfMsg.transforms[2] = new TransformStampedMsg();
            tfMsg.transforms[2].header = headsetHeader;
            tfMsg.transforms[2].child_frame_id = handFrameLeft;
            tfMsg.transforms[2].transform = new TransformMsg();

            // Publish the right hand to unity center
            tfMsg.transforms[3] = new TransformStampedMsg();
            tfMsg.transforms[3].header = headsetHeader;
            tfMsg.transforms[3].child_frame_id = handFrameRight;
            tfMsg.transforms[3].transform = new TransformMsg();



            QuaternionMsg quaternion = headsetRotation.action.ReadValue<Quaternion>().To<FLU>();

            if(quaternion.From<FLU>().Equals(default))
                quaternion.w = 1;


            tfMsg.transforms[1].transform.translation = headsetPose.action.ReadValue<Vector3>().To<FLU>();
            tfMsg.transforms[1].transform.rotation = quaternion;

            tfMsg.transforms[2].transform.translation = handPoseLeft.action.ReadValue<Vector3>().To<FLU>();
            tfMsg.transforms[2].transform.rotation = handRotationLeft.action.ReadValue<Quaternion>().To<FLU>();

            tfMsg.transforms[3].transform.translation = handPoseRight.action.ReadValue<Vector3>().To<FLU>();
            tfMsg.transforms[3].transform.rotation = handRotationRight.action.ReadValue<Quaternion>().To<FLU>();

            // Unity defaults to a quaternion with all 0s if the headset/hands arent detected, if this happens we set the identity quaternion
            for(int i = 0; i < tfMsg.transforms.Length; i++)
                if (tfMsg.transforms[i].transform.rotation.From<FLU>().Equals(default))
                    tfMsg.transforms[i].transform.rotation.w = 1;

            headsetPoseMsg.pose.position = headsetPose.action.ReadValue<Vector3>().To<FLU>();
            headsetPoseMsg.pose.orientation = quaternion;
            ros.Publish(poseTopic+"/headset", headsetPoseMsg);

            ros.Publish("/tf", tfMsg);
        }

        // Read-only view of the decimator for UI that reflects it rather than
        // owning it (the UI Toolkit settings panel initialises its slider from
        // this instead of assuming the field's default).
        public int Decimator => _decimator;

        public void OnDecimatorChange(float value)
        {
            _decimator = Mathf.Max(1, (int)value);
            // Null when the driving UI is UI Toolkit rather than the uGUI
            // palmmenu -- the TMP label only exists in the latter.
            if (decimatorText != null)
                decimatorText.text = "TF Decimator: " + _decimator;
        }
    }
}
