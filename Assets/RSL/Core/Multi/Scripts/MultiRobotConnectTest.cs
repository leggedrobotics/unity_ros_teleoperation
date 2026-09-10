// Test-only bootstrap: connects to TWO fake robot servers at once (see
// eval/scripts/bag_replay_server.py in uvgRTP_ros) to verify multi-robot
// connectivity end-to-end in Unity, and gives each a way to be manually
// pulled apart in the scene.
//
// Robot A (a real ROS1 bag of a dynaarm-equipped robot, incl. /tf) uses the
// DEFAULT UvgRosConnection -- the same one ModelManager/StaticRemapper/every
// existing SensorStream already use -- just pointed at the fake server, so
// its skeleton renders through the EXISTING, unmodified TF pipeline. Its
// manual offset is TF2Attachment's new PositionOffset/RotationOffsetEuler
// fields on ModelManager's root TF2Attachment (see that class).
//
// Robot B (a ROS2 mcap of Gaussian-splat point cloud data, no TF at all --
// bag_replay_server.py remaps its frame_id to "ikea_map" specifically so it
// cannot collide with robot A's real "map" frame) gets its OWN named
// UvgRosConnection ("ikea"), so its data never touches robot A's shared
// TF2System at all. Rendered via a LidarStream cloned from the project's
// existing LidarViewer prefab (so it inherits real material/splat-renderer
// asset references that can't be constructed from pure code), in Splat viz
// mode, with useTF off since "ikea_map" is never actually published. Its
// manual offset is just its own plain Transform -- nothing drives it, so a
// normal Inspector edit already sticks.
using UnityEngine;
using RSL.Sensors.Lidar;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UvgRos.Test
{
    public class MultiRobotConnectTest : MonoBehaviour
    {
        public string RobotAIp = "127.0.0.1";
        public int RobotAPort = 24001;
        public string RobotBIp = "127.0.0.1";
        public int RobotBPort = 24002;

        [Tooltip("Cloned to create robot B's point-cloud viewer, so it inherits " +
                 "real material/splat-renderer references -- see LidarViewer.prefab. " +
                 "Auto-located in the Editor if left unassigned.")]
        public GameObject LidarViewerPrefab;

        void Start()
        {
            var robotA = UvgRosConnection.GetOrCreateInstance();
            robotA.RosIPAddress = RobotAIp;
            robotA.RosPort = RobotAPort;
            robotA.Connect();
            Debug.Log("[MultiRobotConnectTest] robot A (default connection) -> " + RobotAIp + ":" + RobotAPort);

            var robotB = UvgRosConnection.GetOrCreateInstance("ikea");
            robotB.RosIPAddress = RobotBIp;
            robotB.RosPort = RobotBPort;
            robotB.Connect();
            Debug.Log("[MultiRobotConnectTest] robot B (\"ikea\" connection) -> " + RobotBIp + ":" + RobotBPort);

            GameObject prefab = LidarViewerPrefab;
#if UNITY_EDITOR
            if (prefab == null)
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RSL/Sensors/Lidar/LidarViewer.prefab");
#endif
            if (prefab == null)
            {
                Debug.LogError("[MultiRobotConnectTest] no LidarViewerPrefab assigned and could not " +
                                "auto-locate LidarViewer.prefab -- robot B's viewer was not created.");
                return;
            }

            GameObject viewerGo = Instantiate(prefab, new Vector3(3f, 0f, 0f), Quaternion.identity);
            viewerGo.name = "RobotB_SplatViewer";
            LidarStream stream = viewerGo.GetComponentInChildren<LidarStream>(true);
            if (stream == null)
            {
                Debug.LogError("[MultiRobotConnectTest] LidarViewerPrefab has no LidarStream component.");
                return;
            }
            stream.RobotId = "ikea";
            stream.topicName = "/splat";
            stream.vizType = VizType.Splat;
            stream.useTF = false;  // "ikea_map" is never actually published -- nothing to track
            stream.OnVizTypeSelect((int)VizType.Splat);  // activates the splat renderer object
            stream.Subscribe();
            Debug.Log("[MultiRobotConnectTest] robot B viewer subscribed to /splat (Splat viz)");
        }
    }
}
