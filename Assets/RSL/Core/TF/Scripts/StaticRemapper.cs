using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UvgRos;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Tf2;
using RosMessageTypes.Geometry;
using UvgRos.TF2;

namespace RSL.Core.TF
{
    public class StaticRemapper : MonoBehaviour
    {
        /// <summary>Which robot's /tf and /tf_static this instance feeds into
        /// the shared TF2System tree -- defaults to the unnamed single-robot
        /// case, so a scene with exactly one of these behaves exactly as
        /// before. Multiple robots each get their own StaticRemapper with a
        /// distinct RobotId; frames are namespaced via
        /// UvgRosConnection.RemapFrame (empty prefix for the default robot,
        /// "{RobotId}/" otherwise) so e.g. two robots both publishing "map"
        /// land as "map" and "robotB/map" in the one shared tree instead of
        /// colliding.</summary>
        public string RobotId = UvgRosConnection.DefaultRobotId;

        UvgRosConnection ros;

        // Guard keyed by RobotId, not a bare singleton: two instances for the
        // SAME robot silently double-subscribe to /tf_static, and
        // UvgRosConnection.Subscribe's duplicate-topic handling (tear down
        // the old native stream, ask the server for a new one) doesn't
        // renegotiate the server's cached route -- the result was a second
        // stream listening on a port the server was never told to send to,
        // with no error anywhere. Two DIFFERENT robots' remappers coexisting
        // is exactly the point of this class now, so only refuse a second
        // instance for the same id.
        private static readonly Dictionary<string, StaticRemapper> s_instances =
            new Dictionary<string, StaticRemapper>();

        void OnDestroy()
        {
            if (s_instances.TryGetValue(RobotId, out var existing) && existing == this)
                s_instances.Remove(RobotId);
        }

        // The duplicate guard lives here, not in Awake(). AddComponent<T>()
        // calls Awake() synchronously, before the caller gets a chance to
        // assign RobotId on the returned instance (that's the whole reason a
        // caller sets it at all -- the field's own default is
        // DefaultRobotId) -- so a guard in Awake() always sees "default"
        // regardless of what RobotId is set to a line later, and every
        // dynamically-added second robot disabled itself against the FIRST
        // one under the same wrong key. Start() runs after every Awake() in
        // the frame (and after MakeRobotRoot's assignment, which happens
        // between AddComponent and the first frame), so RobotId is correct
        // by the time this checks it. Confirmed live: RobotB's remapper
        // disabled itself against RobotA's under the shared key "default"
        // until this moved.
        void Start()
        {
            if (s_instances.TryGetValue(RobotId, out var existing) && existing != null && existing != this)
            {
                Debug.LogWarning("[StaticRemapper] another instance (" + existing.name +
                    ") already feeds robot '" + RobotId + "' -- disabling this one on " + name +
                    ". Only one StaticRemapper should subscribe to a given robot's /tf_static.");
                enabled = false;
                return;
            }
            s_instances[RobotId] = this;

            ros = UvgRosConnection.GetOrCreateInstance(RobotId);
            ros.Subscribe<TFMessageMsg>("/tf_static", StaticTF, mainThread: true);
            ros.Subscribe<TFMessageMsg>("/tf", DynamicTF, mainThread: true);
        }

        // TF2System (see UvgRos.TF2.TF2System's doc comment) no longer
        // subscribes to anything itself -- this is the one place in the
        // project that feeds it live, dynamic /tf messages, mirroring
        // StaticTF below for /tf_static. Both land in the same default
        // "/tf" topic-bucket (GetOrCreateFrame's default tfTopic param) --
        // ONE shared tree across every robot, not a separate tree per robot,
        // is what lets a future alignment link attach one robot's frame
        // directly to another's (see UvgRosConnection.WorldAnchorTargetFrame).
        // Frame NAMES are namespaced instead, via RemapFrame just below.
        void DynamicTF(TFMessageMsg msg)
        {
            if (this == null) return;
            RemapFrames(msg);
            TF2System.GetOrCreateInstance().GetOrCreateTFTopic("/tf").ReceiveTF(msg);
        }

        // Rewrites every transform's frame_id/child_frame_id in place through
        // this robot's UvgRosConnection.RemapFrame -- a no-op for the default
        // robot (empty TfFramePrefix), so its frame names reach TF2System
        // exactly as they do today.
        void RemapFrames(TFMessageMsg msg)
        {
            foreach (var tf in msg.transforms)
            {
                tf.header.frame_id = ros.RemapFrame(tf.header.frame_id);
                tf.child_frame_id = ros.RemapFrame(tf.child_frame_id);
            }
        }

        void StaticTF (TFMessageMsg msg){
            // A buffered message can still drain through Update() a frame or
            // two into Play mode stopping / the app quitting -- by then the
            // frame GameObjects (and possibly this component) are already
            // destroyed, and Unity's fake-null still lets a bare reference
            // through, so this is not optional even though it looks it.
            if (this == null) return;

            RemapFrames(msg);

            // get the tf system
            var tfSystem = TF2System.GetOrCreateInstance();

            // add the static tfs to the tf tree
            foreach (var tf in msg.transforms)
            {
                var child = tfSystem.GetOrCreateFrame(tf.child_frame_id);
                var parent = tfSystem.GetOrCreateFrame(tf.header.frame_id);
                if (child?.GameObject == null || parent?.GameObject == null) continue;
                child.SetParent(parent);
                child.GameObject.transform.localPosition = ToVector3(tf.transform.translation);
                child.GameObject.transform.localRotation = ToQuaternion(tf.transform.rotation);

                // child.SetTransform(tf.transform.translation.ToVector3(), tf.transform.rotation.ToQuaternion());
            }
        }
            public static Vector3 ToVector3(Vector3Msg msg)
        {
            // convert
            Vector3 v = new Vector3(-(float)msg.y, (float)msg.z, (float)msg.x);
            return v;
        }

        public static Quaternion ToQuaternion(QuaternionMsg msg)
        {
            // ROS uses FLU, Unity uses FRD
            // convert
            Quaternion q = new Quaternion(-(float)msg.y, (float)msg.z, (float)msg.x, -(float)msg.w);

            return q;

        }
    }
}
