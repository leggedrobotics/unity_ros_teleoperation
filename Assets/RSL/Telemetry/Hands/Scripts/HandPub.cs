using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UvgRos;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;
using RosMessageTypes.Sensor;
using RosMessageTypes.VrHaptic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RSL.Telemetry.Hands
{
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

        private InputAction activeController;
        private InputAction twistController;
        private InputAction poseController;

        public Sprite enableIcon;
        public Sprite disableIcon;
        public Button enableButton;
        private Image _img;
        public TextMeshProUGUI infoText;

        UvgRosConnection _ros;
        UvgRosPublisher<ManoLandmarksMsg> _landmarksPublisher;
        UvgRosPublisher<PointCloudMsg> _pointCloudPublisher;
        UvgRosPublisher<ManoLandmarksMsg> _leftLandmarksPublisher;
        UvgRosPublisher<ManoLandmarksMsg> _rightLandmarksPublisher;
        UvgRosPublisher<PointCloudMsg> _leftPointCloudPublisher;
        UvgRosPublisher<PointCloudMsg> _rightPointCloudPublisher;
        UvgRosPublisher<HandGestureMsg> _gesturePublisher;
        XRHandSubsystem m_handSubsystem;
        private readonly List<XRHandSubsystem> _handSubsystems = new List<XRHandSubsystem>();
        private float _nextSubsystemCheck;
        private float _nextPublishAttempt;

        private bool _publishing = false;
        public void OnPublishManoState(int state) => SetPublishing(state == 0);

        public void SetPublishing(bool enabled)
        {
            _publishing = enabled;
            if (_img != null)
                _img.sprite = _publishing ? enableIcon : disableIcon;
        }

        private bool _highConfidence = false;
        private const string _landmarksTopic = "/quest/hand_pose";
        private const string _pointCloudTopic = "/quest/hand_points";
        private const string _gestureTopic = "/quest/hand_gesture";

        public string worldFrame = "vr_origin";

        void Start()
        {
            // New input System Actions fromr system wide actions
            if(InputSystem.actions)
            {
                activeController = InputSystem.actions.FindAction("XRI LeftHand/Gripped");
                twistController = InputSystem.actions.FindAction("XRI LeftHand/XButton");
                poseController = InputSystem.actions.FindAction("XRI LeftHand/YButton");
            }

            if (enableButton != null)
                _img = enableButton.transform.Find("Image/Image").GetComponent<Image>();
            SetPublishing(_publishing);

            _ros = UvgRosConnection.GetOrCreateInstance();
            _gesturePublisher = _ros.LazyPublish<HandGestureMsg>(_gestureTopic);
            TryBindHandSubsystem();

            // setup action map listeners
            if (twistController != null)
                twistController.performed += OnTwistController;
            if (poseController != null)
                poseController.performed += OnPoseController;
        }

        void Update()
        {
            if ((m_handSubsystem == null || !m_handSubsystem.running) && Time.unscaledTime >= _nextSubsystemCheck)
            {
                _nextSubsystemCheck = Time.unscaledTime + 1f;
                TryBindHandSubsystem();
            }
            if(activeController != null && activeController.ReadValue<float>() > 0.5f)
            {
                PubActiveController();
            } else {
                if(infoText != null)
                    infoText.color = Color.red;
            }
        }

        void TryBindHandSubsystem()
        {
            UnbindHandSubsystem();
            _handSubsystems.Clear();
            SubsystemManager.GetSubsystems(_handSubsystems);
            foreach (var subsystem in _handSubsystems)
            {
                if (!subsystem.running) continue;
                m_handSubsystem = subsystem;
                m_handSubsystem.updatedHands += OnHandUpdate;
                foreach (var jointID in jointMap.Values)
                    if (!subsystem.jointsInLayout[jointID.ToIndex()])
                        Debug.LogWarning("Hand publishing: joint " + jointID + " is not supported");
                break;
            }
        }

        void UnbindHandSubsystem()
        {
            if (m_handSubsystem != null)
                m_handSubsystem.updatedHands -= OnHandUpdate;
            m_handSubsystem = null;
        }

        void OnDisable() => UnbindHandSubsystem();

        void OnTwistController(InputAction.CallbackContext context) => PubTwistController();
        void OnPoseController(InputAction.CallbackContext context) => PubPoseController();

        void OnDestroy()
        {
            UnbindHandSubsystem();
            if (twistController != null)
                twistController.performed -= OnTwistController;
            if (poseController != null)
                poseController.performed -= OnPoseController;
        }

        public void PubActiveController()
        {
            HandGestureMsg msg = new HandGestureMsg();
            msg.name = "Closed_Fist";
            _gesturePublisher.Send(msg);
            if(infoText != null)
                infoText.color = Color.green;

        }

        public void PubTwistController()
        {
            HandGestureMsg msg = new HandGestureMsg();
            msg.name = "Thumb_Up";
            _gesturePublisher.Send(msg);
            
            infoText?.SetText("Twist activated");
        }

        public void PubPoseController()
        {
            HandGestureMsg msg = new HandGestureMsg();
            msg.name = "Thump_Down";
            _gesturePublisher.Send(msg);
            infoText?.SetText("Pose activated");
        }

        public void ToggleConfidence()
        {
            _highConfidence = !_highConfidence;
            infoText?.SetText(_highConfidence ? "High Confidence" : "Low Confidence");
        }

        public void TogglePublishing()
        {
            SetPublishing(!_publishing);
        }

        void OnHandUpdate(XRHandSubsystem subsystem, 
            XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
            XRHandSubsystem.UpdateType updateType)
        {
            if(!_publishing) return;
            if (updateSuccessFlags == 0 && _highConfidence) return;
            

            // bypass render update to slightly throttle
            if(updateType != XRHandSubsystem.UpdateType.Dynamic) return;
            
            if (!EnsureHandPublishers()) return;
            PublishHand(subsystem.leftHand, _leftLandmarksPublisher, _leftPointCloudPublisher, false);
            PublishHand(subsystem.rightHand, _rightLandmarksPublisher, _rightPointCloudPublisher, true);
        }

        bool EnsureHandPublishers()
        {
            if (_rightPointCloudPublisher != null) return true;
            if (_ros == null || _ros.HasConnectionError || Time.unscaledTime < _nextPublishAttempt) return false;
            _nextPublishAttempt = Time.unscaledTime + 3f;
            try
            {
                // Explicitly enabling MANO opens these routes even without a ROS subscriber.
                if (_landmarksPublisher == null)
                    _landmarksPublisher = _ros.Publish<ManoLandmarksMsg>(_landmarksTopic);
                if (_pointCloudPublisher == null)
                    _pointCloudPublisher = _ros.Publish<PointCloudMsg>(_pointCloudTopic);
                if (_leftLandmarksPublisher == null)
                    _leftLandmarksPublisher = _ros.Publish<ManoLandmarksMsg>(_landmarksTopic + "/left");
                if (_rightLandmarksPublisher == null)
                    _rightLandmarksPublisher = _ros.Publish<ManoLandmarksMsg>(_landmarksTopic + "/right");
                if (_leftPointCloudPublisher == null)
                    _leftPointCloudPublisher = _ros.Publish<PointCloudMsg>(_pointCloudTopic + "/left");
                _rightPointCloudPublisher = _ros.Publish<PointCloudMsg>(_pointCloudTopic + "/right");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Could not open hand publishing routes; retrying: " + e.Message);
                return false;
            }
        }

        void PublishHand(XRHand hand, UvgRosPublisher<ManoLandmarksMsg> landmarksPublisher,
            UvgRosPublisher<PointCloudMsg> pointsPublisher, bool legacyRightHand)
        {
            if (!hand.isTracked) return;
            var points = new Point32Msg[jointMap.Count];
            var confidence = new float[jointMap.Count];
            int count = 0;
            foreach (var entry in jointMap)
            {
                int i = entry.Key;
                points[i] = new Point32Msg();
                if (!hand.GetJoint(entry.Value).TryGetPose(out UnityEngine.Pose pose)) continue;
                // XR joint poses and vr_origin are both in tracking-origin coordinates.
                points[i].x = pose.position.z;
                points[i].y = -pose.position.x;
                points[i].z = pose.position.y;
                confidence[i] = 1;
                count++;
            }
            if (count == 0 || (_highConfidence && count != jointMap.Count)) return;
            long ticks = System.DateTime.UtcNow.Ticks - 621355968000000000L;
            var header = new HeaderMsg();
            header.frame_id = worldFrame;
            header.stamp.sec = (int)(ticks / System.TimeSpan.TicksPerSecond);
            header.stamp.nanosec = (uint)((ticks % System.TimeSpan.TicksPerSecond) * 100);
            var landmarks = new ManoLandmarksMsg { header = header, landmarks = CastPoints(points) };
            var cloud = new PointCloudMsg
            {
                header = header,
                points = points,
                channels = new[] { new ChannelFloat32Msg { name = "intensity", values = confidence } }
            };
            landmarksPublisher.Send(landmarks);
            pointsPublisher.Send(cloud);
            if (legacyRightHand)
            {
                _landmarksPublisher.Send(landmarks);
                _pointCloudPublisher.Send(cloud);
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
}
