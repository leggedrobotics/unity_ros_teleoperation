// Test-only bootstrap: connects to TWO real robot servers at once (e.g. two
// eval/scripts/bag_replay_server.py instances in uvgRTP_ros, each serving a
// real bag -- a B2W and an ALMA recording are the intended pair) to verify
// multi-robot connectivity, per-robot TF isolation, and origin re-alignment
// end-to-end in Unity.
//
// Both robots now get a REAL, live TF tree -- this used to be impossible for
// a second robot (StaticRemapper was a hard singleton feeding only the
// default connection's raw, unprefixed frame names into TF2System), which is
// exactly why the previous version of this test needed robot B's frames
// pre-remapped outside Unity entirely and gave up on TF for it altogether.
// Now: one StaticRemapper per robot (RobotId-keyed, not a singleton), each
// robot's frames namespaced automatically via UvgRosConnection.RemapFrame
// (empty prefix for the default robot, "{RobotId}/" otherwise) before they
// reach the ONE shared TF2System tree -- so two robots that each genuinely
// publish colliding frame names (both "base_link", both "map", ...) resolve
// as e.g. "base_link" and "robotB/base_link" instead of colliding. One
// shared tree (not one per robot) is deliberate: it's what lets the origin
// re-alignment below attach one robot's frame directly to another's, not
// just to a fixed world point.
//
// Also demonstrates moving a robot's origin from an "external command" --
// UvgRosConnection.WorldAnchor is exactly the write target a real external
// source (an RTK-like alignment service, a network message, anything other
// than a human dragging a gizmo) would use; RealignRobotB() here stands in
// for that external source and is wired to a keypress purely so this is
// checkable interactively without one.
using UnityEngine;

namespace UvgRos.Test
{
    public class MultiRobotConnectTest : MonoBehaviour
    {
        [Header("Robot A -- default/unnamed connection")]
        public string RobotAIp = "127.0.0.1";
        public int RobotAPort = 24001;
        [Tooltip("M4's real TF root, per its own bag/prefab (frame chains all start " +
                 "\"/BASE/CABIN/...\") -- NOT \"base_link\", which this bag doesn't " +
                 "publish at all.")]
        public string RobotARootFrame = "BASE";
        [Tooltip("Left null: falls back to the real M4 excavator prefab shipped in " +
                 "com.leggedrobotics.ros.robots.heavy.m4, loaded by path (editor-only).")]
        public GameObject RobotAModelPrefab;

        [Header("Robot B -- named connection")]
        public string RobotBId = "robotB";
        public string RobotBIp = "127.0.0.1";
        public int RobotBPort = 24002;
        [Tooltip("ALMA's real TF root, per its own prefab's authored FrameID chains " +
                 "(they all start \"/base/...\") -- NOT \"base_link\", which this bag " +
                 "doesn't publish at all.")]
        public string RobotBRootFrame = "base";
        [Tooltip("Left null: falls back to the real ALMA prefab shipped in " +
                 "com.leggedrobotics.ros.robots.anymal.alma, loaded by path (editor-only).")]
        public GameObject RobotBModelPrefab;

        [Tooltip("Where RealignRobotB() moves robot B's origin to -- a plain field " +
                 "(rather than hardcoded in the method) so a different target is easy " +
                 "to verify against whatever WorldAnchor value happens to already be " +
                 "persisted from an earlier run.")]
        public Vector3 RealignTarget = new Vector3(7f, 1f, -2f);

        [Header("Lidar -- optional, per robot")]
        public string RobotALidarTopic = "/mole/livox_lidar_publisher/lidar_front_left";
        public string RobotBLidarTopic = "/lidar/point_cloud";

        UvgRosConnection _robotA;
        UvgRosConnection _robotB;
        GameObject _robotARoot;
        GameObject _robotBRoot;

        void Start()
        {
            _robotA = UvgRosConnection.GetOrCreateInstance();
            _robotA.RosIPAddress = RobotAIp;
            _robotA.RosPort = RobotAPort;
            _robotA.Connect();
            _robotARoot = MakeRobotRoot("RobotA_Root", _robotA, RobotARootFrame, RobotAModelPrefab);
            MakeLidar("RobotA_Lidar", _robotA, RobotALidarTopic, _robotARoot.transform);
            Debug.Log("[MultiRobotConnectTest] robot A (default connection) -> " + RobotAIp + ":" + RobotAPort +
                      ", root frame '" + RobotARootFrame + "' (unprefixed -- default robot)");

            _robotB = UvgRosConnection.GetOrCreateInstance(RobotBId);
            _robotB.RosIPAddress = RobotBIp;
            _robotB.RosPort = RobotBPort;
            _robotB.Connect();
            _robotBRoot = MakeRobotRoot("RobotB_Root", _robotB, RobotBRootFrame, RobotBModelPrefab);
            MakeLidar("RobotB_Lidar", _robotB, RobotBLidarTopic, _robotBRoot.transform);
            Debug.Log("[MultiRobotConnectTest] robot B (\"" + RobotBId + "\" connection) -> " +
                      RobotBIp + ":" + RobotBPort + ", root frame '" +
                      _robotB.RemapFrame(RobotBRootFrame) + "' (prefixed -- proves isolation from robot A " +
                      "even if both bags genuinely publish the same frame names)");

            Debug.Log("[MultiRobotConnectTest] press M to re-home robot B's origin via " +
                      "UvgRosConnection.WorldAnchor, simulating an external alignment command");
        }

        // No Update()/keypress binding: this project's Player Settings use
        // the new Input System exclusively, where the legacy
        // UnityEngine.Input class throws InvalidOperationException on every
        // call rather than just no-opping (confirmed: it did, every frame).
        // RealignRobotB() is public specifically so it can be triggered
        // directly (inspector button, bridge INVOKE, a real caller) instead
        // of through a keypress this project can't read.

        /// <summary>Stands in for an external command moving a robot's
        /// origin -- an RTK-like alignment service, a network message, a
        /// UI action -- all of which would call exactly this setter. Sets
        /// RobotB_Root's transform directly -- safe now because RobotB_Root
        /// carries no TF2Attachment of its own (see MakeRobotRoot's header
        /// comment for why): nothing else writes this transform, so nothing
        /// clobbers it on the next frame the way it would if RobotB_Root
        /// were itself TF-driven.</summary>
        public void RealignRobotB()
        {
            var pose = new Pose(RealignTarget, Quaternion.Euler(0f, 45f, 0f));
            _robotB.WorldAnchor = pose;
            if (_robotBRoot != null)
            {
                _robotBRoot.transform.localPosition = pose.position;
                _robotBRoot.transform.localRotation = pose.rotation;
            }
            Debug.Log("[MultiRobotConnectTest] robot B re-homed to " + pose.position +
                      " / " + pose.rotation.eulerAngles + " via WorldAnchor (persisted for robot id '" +
                      RobotBId + "')");
        }

        // Extracts just the trailing segment of a slash-separated frame id,
        // e.g. "/base/LH_HAA_drive/LH_HIP/.../LH_FOOT" -> "LH_FOOT". Some
        // robot prefabs (Alma.prefab, M4.prefab) author each bone's own
        // FrameID as its FULL ancestor chain back to the root, not just its
        // own name -- a different convention from live /tf data, where
        // ReceiveTF resolves header.frame_id/child_frame_id as two SEPARATE
        // atomic names (see TF2System.cs). Remapping (prefixing with
        // robot_id) the WHOLE chain as one string, as this code used to,
        // produces a key like "robotB/base/.../LH_FOOT" that TF2System's
        // (now full-path-keyed, post multi-robot-collision fix) table never
        // populates -- live data only ever creates the short atomic key
        // "robotB/LH_FOOT". The bone's own TF2Attachment silently never
        // resolved, so the mesh sat frozen at its prefab bind pose even
        // though the connection and frame isolation were both working
        // (confirmed live: DUMP/TF_LIST showed real, separate, moving frames
        // the whole time -- only the model's OWN bone lookups were wrong).
        // Extracting the leaf first and remapping THAT restores the alias
        // onto the same atomic key live data actually populates.
        static string LeafFrameName(string frameId)
        {
            string trimmed = frameId.TrimEnd('/');
            int lastSlash = trimmed.LastIndexOf('/');
            return lastSlash >= 0 ? trimmed.Substring(lastSlash + 1) : trimmed;
        }

        // Layers 8/9 are unused in this project's TagManager (blank names) --
        // used here purely as a culling mask so CAM_SHOT_SPLIT can render
        // one robot at a time even when the two are spatially close enough
        // for their bounding boxes to overlap (confirmed necessary once M4's
        // real boom was long enough to reach into ALMA's own framing). Not a
        // scene-hierarchy concept -- a model's bones get reparented all over
        // TF2System's tree by TF2Attachment, so layer is the one property
        // that survives that regardless of where each bone ends up.
        const int LayerDefaultRobot = 8;
        const int LayerNamedRobot = 9;

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }

        /// <summary>Re-tags every GameObject under each robot's root with
        /// that robot's culling layer. Not a one-time setup step: TF2System
        /// creates a fresh GameObject (always on layer 0, Unity's default)
        /// for every newly-seen frame name as /tf data streams in over time,
        /// so new joints keep showing up on the wrong layer well after
        /// Start() -- call this again (bridge INVOKE, before each
        /// CAM_SHOT_SPLIT) rather than expecting one initial pass to have
        /// caught everything.</summary>
        public void ApplyLayers()
        {
            if (_robotARoot != null) SetLayerRecursively(_robotARoot, LayerDefaultRobot);
            if (_robotBRoot != null) SetLayerRecursively(_robotBRoot, LayerNamedRobot);
        }

        GameObject MakeRobotRoot(string name, UvgRosConnection ros, string rootFrame, GameObject modelPrefab)
        {
            // RobotX_Root deliberately carries NO TF2Attachment of its own.
            // TF2Attachment.FrameID's setter reparents its OWN GameObject
            // onto the resolved TF2System frame the instant it is set -- so
            // if root itself had one, root would immediately detach from
            // wherever it started and re-home onto that frame, and the
            // model (a child of root) would just come along for the ride,
            // with WorldAnchor's offset applied at the WRONG level (root's
            // position relative to the live frame, not a stable anchor the
            // live frame hangs off of). Instead: root stays a plain,
            // WorldAnchor-positioned GameObject, and the robot's actual
            // resolved root TF frame is reparented to be root's CHILD below.
            // Every other bone attaches to ITS OWN resolved frame as normal
            // (TF2Attachment's ordinary behavior, unchanged) -- since those
            // frames descend from the root frame via the real /tf parent
            // chain, and the root frame now hangs off `root`, WorldAnchor's
            // offset propagates down through ordinary Unity parenting to
            // every joint, root frame included, without needing every bone
            // to know about `root` at all.
            var root = new GameObject(name);
            var anchor = ros.WorldAnchor;
            root.transform.localPosition = anchor.position;
            root.transform.localRotation = anchor.rotation;

            var remapper = root.AddComponent<RSL.Core.TF.StaticRemapper>();
            remapper.RobotId = ros.RobotId;

            string remappedRootFrame = ros.RemapFrame(LeafFrameName(rootFrame));
            GameObject rootFrameObject = UvgRos.TF2.TF2System.GetOrCreateInstance().GetTransformObject(remappedRootFrame, "/tf");
            rootFrameObject.transform.SetParent(root.transform, worldPositionStays: false);

#if UNITY_EDITOR
            if (modelPrefab == null && ros.RobotId == RobotBId)
                modelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Packages/com.leggedrobotics.ros.robots.anymal.alma/Runtime/Alma.prefab");
            else if (modelPrefab == null && ros.RobotId == UvgRosConnection.DefaultRobotId)
                modelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Packages/com.leggedrobotics.ros.robots.heavy.m4/M4.prefab");
#endif
            if (modelPrefab != null)
            {
                var model = Instantiate(modelPrefab);
                model.transform.SetParent(root.transform, worldPositionStays: false);
                // These robot packages (Alma.prefab, M4.prefab) are authored
                // generically against com.unity.robotics.ros-tcp-connector's
                // OWN TFAttachment/TFSystem -- not this project's UvgRos.TF2
                // fork (confirmed: GetComponentsInChildren<TF2Attachment>
                // found zero on either model; the prefab's real component,
                // traced via its serialized script GUID, is
                // Unity.Robotics.ROSTCPConnector.TFAttachment). Left alone,
                // each one would resolve against the STOCK TFSystem, which
                // nothing in this project ever feeds live data into (see
                // TF2System.cs's own file header for why this project forked
                // away from exactly that auto-subscribing behavior) -- the
                // joints would sit at their prefab bind pose forever, never
                // driven by the real, live, robot_id-prefixed tree. Replace
                // each one with an equivalent TF2Attachment carrying the same
                // robot_id-prefixed LEAF frame name (see LeafFrameName) and
                // TFTopic, and remove the original so it doesn't sit there
                // doing nothing.
                int replaced = 0;
                foreach (var stockAttachment in model.GetComponentsInChildren<Unity.Robotics.ROSTCPConnector.TFAttachment>(true))
                {
                    var go = stockAttachment.gameObject;
                    string remappedFrameId = ros.RemapFrame(LeafFrameName(stockAttachment.FrameID));
                    string tfTopic = stockAttachment.TFTopic;
                    DestroyImmediate(stockAttachment);
                    var replacement = go.AddComponent<UvgRos.TF2.TF2Attachment>();
                    replacement.TFTopic = tfTopic;
                    replacement.FrameID = remappedFrameId;
                    replaced++;
                }
                // SkinnedMeshRenderer culls itself off-camera using bounds
                // computed once around its bind pose, not the mesh's actual
                // live extent -- fine normally (bounds barely move), but
                // this model's bones now genuinely swing far from bind pose
                // once correctly TF-driven (the fix above), and a stale
                // bind-pose bounding box can stop overlapping the camera
                // frustum entirely while the mesh itself is completely
                // correct. Confirmed live: ALMA's body/legs vanished from
                // every render the moment its legs started actually moving.
                // updateWhenOffscreen recomputes real bounds from the live
                // skinned mesh every frame instead of trusting the cached
                // ones -- more expensive, fine for this test rig.
                foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skinned.updateWhenOffscreen = true;
                    Debug.Log("[MultiRobotConnectTest] " + name + " SkinnedMeshRenderer '" + skinned.name +
                              "' enabled=" + skinned.enabled + " activeInHierarchy=" + skinned.gameObject.activeInHierarchy +
                              " sharedMesh=" + (skinned.sharedMesh != null ? skinned.sharedMesh.name : "NULL") +
                              " bones=" + skinned.bones.Length + " rootBone=" + (skinned.rootBone != null ? skinned.rootBone.name : "NULL") +
                              " bounds=" + skinned.bounds + " localBounds=" + skinned.localBounds);
                }
                foreach (var mesh in model.GetComponentsInChildren<MeshRenderer>(true))
                {
                    Debug.Log("[MultiRobotConnectTest] " + name + " MeshRenderer '" + mesh.name +
                              "' enabled=" + mesh.enabled + " activeInHierarchy=" + mesh.gameObject.activeInHierarchy +
                              " bounds=" + mesh.bounds);
                }

                Debug.Log("[MultiRobotConnectTest] " + name + " instantiated '" + model.name +
                          "', renderers found: " + model.GetComponentsInChildren<Renderer>(true).Length +
                          ", TFAttachment -> TF2Attachment replacements: " + replaced);
            }
            else
            {
                var skeleton = root.AddComponent<TfSkeletonView>();
                skeleton.RobotId = ros.RobotId;
            }
            return root;
        }

        // Instantiates the real LidarViewer prefab (not a bare AddComponent)
        // specifically to inherit its already-wired point_material/
        // splatRendererPrefab references -- LidarStream.Awake() builds a
        // RenderParams straight off point_material with no null guard, so a
        // raw AddComponent<LidarStream>() with that field left unset would
        // throw immediately instead of quietly rendering nothing.
        void MakeLidar(string name, UvgRosConnection ros, string topic, Transform parent)
        {
            if (string.IsNullOrEmpty(topic)) return;
#if UNITY_EDITOR
            GameObject viewerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/RSL/Sensors/Lidar/LidarViewer.prefab");
            if (viewerPrefab == null)
            {
                Debug.LogWarning("[MultiRobotConnectTest] LidarViewer.prefab not found -- skipping lidar for " + name);
                return;
            }
            var viewer = Instantiate(viewerPrefab, parent, worldPositionStays: false);
            viewer.name = name;
            var lidar = viewer.GetComponentInChildren<RSL.Sensors.Lidar.LidarStream>(true);
            if (lidar == null)
            {
                Debug.LogWarning("[MultiRobotConnectTest] LidarViewer.prefab has no LidarStream -- skipping lidar for " + name);
                return;
            }
            // Set BEFORE this component's own Start() runs (LidarStream binds
            // _ros there, not in Awake() -- same AddComponent-vs-field-
            // assignment ordering issue as StaticRemapper/TF2Attachment
            // earlier in this file; already fixed at the source in
            // LidarStream.cs, this just has to actually set RobotId in time,
            // which it does since Start() only runs after every object's
            // fields are done being assigned this frame).
            lidar.RobotId = ros.RobotId;
            lidar.topicName = topic;
            lidar._enabled = true;
            // The viewer prefab's own world-space UI (topic dropdown, point-
            // count readout, sliders) has real mesh Renderers behind it --
            // with nothing to interact with it here (no human, no EventSystem
            // click target), it just sits there contributing its own bounds
            // to CAM_SHOT_SPLIT's auto-fit framing, comparable in size to
            // the robot itself and skewing the camera away from the actual
            // point cloud/mesh. The point cloud renders via LidarStream's own
            // direct Graphics.RenderPrimitivesIndexed call, independent of
            // this UI, so disabling it loses nothing but clutter.
            foreach (var canvas in viewer.GetComponentsInChildren<Canvas>(true))
                canvas.gameObject.SetActive(false);
#endif
        }
    }
}
