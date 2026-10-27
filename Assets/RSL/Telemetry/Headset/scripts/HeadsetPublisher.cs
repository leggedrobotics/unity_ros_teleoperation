using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using RosMessageTypes.Geometry;
using RosMessageTypes.Tf2;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using TMPro;
using UvgRos;
using UvgRos.TF2;

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

        // State 0 is checked; TF is the default representation.
        private bool _publishTf = true;
        private bool _publishPoseStamped = false;
        public void SetPublishTf(bool enabled) => _publishTf = enabled;
        public void SetPublishPoseStamped(bool enabled) => _publishPoseStamped = enabled;

        // StateButton callbacks: state 0 = on (check icon), 1 = off.
        public void OnPublishTfState(int state) => SetPublishTf(state == 0);
        public void OnPublishPoseStampedState(int state) => SetPublishPoseStamped(state == 0);

        private Transform root;
        private string handFrameRight = "hand_right";
        private UvgRosConnection ros;
        private UvgRosPublisher<PoseStampedMsg> _posePublisher;
        private UvgRosPublisher<PoseStampedMsg> _leftPosePublisher;
        private UvgRosPublisher<PoseStampedMsg> _rightPosePublisher;
        private UvgRosPublisher<TFMessageMsg> _tfPublisher;
        private TFMessageMsg tfMsg;
        private PoseStampedMsg headsetPoseMsg;
        private PoseStampedMsg leftHandMsg;
        private PoseStampedMsg rightHandMsg;

        private HeaderMsg headsetHeader;
        private float _nextPosePublishAttempt;

        private string rootFrame;
        private int _decimator = 1;
        private int _frameCounter = 0;

        void Start()
        {
            ros = UvgRosConnection.GetOrCreateInstance();

            handFrameRight = handFrameLeft.Replace("left", "right");

            var rootObject = GameObject.FindWithTag("root");
            root = rootObject != null ? rootObject.transform : null;
            if (root == null)
            {
                Debug.LogError("Root not found");
                rootFrame = "odom"; // default to odom if no root is found
            }
            else
            {
                var attachment = root.GetComponent<TF2Attachment>();
                rootFrame = attachment != null ? attachment.FrameID : "odom";
            }
            // /tf: eager Publish, NOT LazyPublish -- /tf always has other
            // publishers, which get_subscriptions' demand check excludes,
            // so a lazy /tf would never open. It is a deliberate fan-in
            // topic; just always publish.
            _tfPublisher = ros.Publish<TFMessageMsg>("/tf");

            if (decimatorText != null)
                decimatorText.text = "TF Decimator: " + _decimator;

        }

        void Update()
        {
            if (!_publishTf && !_publishPoseStamped) return;
            // For better performance we can choose to only publish every nth frame
            _frameCounter++;
            if (_frameCounter % _decimator != 0)
                return;

            _frameCounter = 0;
            long ticks = System.DateTime.UtcNow.Ticks - 621355968000000000L;
            headsetHeader = new HeaderMsg();
            headsetHeader.frame_id = unityFrame;
            headsetHeader.stamp.sec = (int)(ticks / System.TimeSpan.TicksPerSecond);
            headsetHeader.stamp.nanosec = (uint)((ticks % System.TimeSpan.TicksPerSecond) * 100);

            // TF publishing (preferred means of poses, but sometimes weird with timing)
            tfMsg = new TFMessageMsg();
            tfMsg.transforms = new TransformStampedMsg[4];
            tfMsg.transforms[0] = new TransformStampedMsg();
            HeaderMsg rootHeader = new HeaderMsg();
            rootHeader.frame_id = rootFrame;
            rootHeader.stamp = headsetHeader.stamp;
            // Publish the pose from odom to unity center
            tfMsg.transforms[0].header = rootHeader;
            tfMsg.transforms[0].child_frame_id = unityFrame;
            tfMsg.transforms[0].transform = new TransformMsg();
            tfMsg.transforms[0].transform.translation = new Vector3Msg();
            tfMsg.transforms[0].transform.rotation = new QuaternionMsg();
            tfMsg.transforms[0].transform.translation = (root != null ? root.InverseTransformPoint(Vector3.zero) : Vector3.zero).To<FLU>();
            tfMsg.transforms[0].transform.rotation = (root != null ? Quaternion.Inverse(root.rotation) : Quaternion.identity).To<FLU>();

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

            if (_publishPoseStamped && EnsurePosePublishers())
            {
                headsetPoseMsg = new PoseStampedMsg { header = headsetHeader };
                leftHandMsg = new PoseStampedMsg { header = headsetHeader };
                rightHandMsg = new PoseStampedMsg { header = headsetHeader };
                headsetPoseMsg.pose.position = headsetPose.action.ReadValue<Vector3>().To<FLU>();
                headsetPoseMsg.pose.orientation = quaternion;
                _posePublisher.Send(headsetPoseMsg);
                leftHandMsg.pose.position = handPoseLeft.action.ReadValue<Vector3>().To<FLU>();
                leftHandMsg.pose.orientation = tfMsg.transforms[2].transform.rotation;
                rightHandMsg.pose.position = handPoseRight.action.ReadValue<Vector3>().To<FLU>();
                rightHandMsg.pose.orientation = tfMsg.transforms[3].transform.rotation;
                _leftPosePublisher.Send(leftHandMsg);
                _rightPosePublisher.Send(rightHandMsg);
            }

            if (_publishTf)
                _tfPublisher.Send(tfMsg);
        }

        bool EnsurePosePublishers()
        {
            if (_rightPosePublisher != null) return true;
            if (ros == null || ros.HasConnectionError || Time.unscaledTime < _nextPosePublishAttempt) return false;
            _nextPosePublishAttempt = Time.unscaledTime + 3f;
            try
            {
                if (_posePublisher == null)
                    _posePublisher = ros.Publish<PoseStampedMsg>(poseTopic + "/headset");
                if (_leftPosePublisher == null)
                    _leftPosePublisher = ros.Publish<PoseStampedMsg>(poseTopic + "/hand_left");
                _rightPosePublisher = ros.Publish<PoseStampedMsg>(poseTopic + "/hand_right");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Could not open pose publishing routes; retrying: " + e.Message);
                return false;
            }
        }

        public void OnDecimatorChange(float value)
        {
            _decimator = Mathf.Max(1, (int)value);
            if (decimatorText != null)
                decimatorText.text = "TF Decimator: " + _decimator;
        }
    }
}
