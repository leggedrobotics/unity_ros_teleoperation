// Forked from com.unity.robotics.ros-tcp-connector's Runtime/TcpConnector/TFSystem.cs
// (itself already a leggedrobotics/ROS-TCP-Connector fork, not upstream Unity's).
// Renamed with a "2" suffix (TF2System/TF2Stream/TF2Frame/TF2Attachment)
// since the original classes are still present via the still-installed
// ros-tcp-connector package -- same name, different type, in Unity terms.
// Two other Unity projects on this machine (unity_ros_teleoperation_dagger,
// unity2) still depend on the shared package's version and its behavior --
// notably its internal auto-subscribe to ROSConnection for live /tf -- so
// this project keeps its own isolated copy in UvgRos.TF2 instead of patching
// the shared one, with two deliberate differences from the original:
//
//   1. TFTopicState no longer auto-subscribes to anything. The original
//      constructor called ROSConnection.GetOrCreateInstance().Subscribe<
//      TFMessageMsg>(tfTopic, ReceiveTF) directly, which is exactly the kind
//      of live ROSConnection networking this project is migrating off of.
//      ReceiveTF is still public -- callers push messages in explicitly
//      instead (see StaticRemapper.cs's UvgRosConnection.Subscribe -> ReceiveTF
//      wiring for both /tf_static and /tf).
//
//   2. `instance` resets on every Play session via RuntimeInitializeOnLoadMethod.
//      Under "Enter Play Mode Options (no domain reload)" a plain static field
//      survives across Play sessions holding GameObjects from the *previous*
//      session, which are gone by the time something touches them next time --
//      MissingReferenceException. This is Unity's own documented fix pattern
//      for statics that must not survive a no-domain-reload session.
//
// Also: the 3 reads of `(int) ROSConnection.GetOrCreateInstance().rosVersion`
// (used only to decide which of TimeMsg's two time fields to read) are
// replaced with a local constant -- this project's own UvgRosMessageCodec
// only ever populates TimeMsg.sec (ROS2-shaped), never u_sec, so this was
// never really "which ROS version," just a fact about our own decode path.
//
// Everything else -- frame-name parsing/parenting in GetOrCreateFrame, the
// flat-namespace-keyed-by-leaf-name m_TransformTable, GetTransform,
// AddListener/NotifyChanged, TF2Frame's math -- is unchanged from the
// original. The leaf-name keying matches tf2's own global-frame_id-
// uniqueness invariant (not a bug), and the surface with no consumer in this
// project today (GetTransform*, GetTransformStream, AddListener) costs
// nothing to keep for future use -- MiniSkeletonView.cs is in fact the
// first real consumer of AddListener/NotifyChanged.
using System;
using System.Collections.Generic;
using RosMessageTypes.BuiltinInterfaces;
using RosMessageTypes.Std;
using RosMessageTypes.Tf2;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine;

namespace UvgRos.TF2
{
    public class TF2System
    {
        public static TF2System instance { get; private set; }
        Dictionary<string, TFTopicState> m_TFTopics = new Dictionary<string, TFTopicState>();

        // This project's UvgRosMessageCodec only ever populates TimeMsg.sec
        // (ROS2-shaped) -- see the file-level comment above.
        private const int kRosVersion = 2;

        public class TFTopicState
        {
            string m_TFTopic;
            Dictionary<string, TF2Stream> m_TransformTable = new Dictionary<string, TF2Stream>();
            List<Action<TF2Stream>> m_Listeners = new List<Action<TF2Stream>>();

            public TFTopicState(string tfTopic = "/tf")
            {
                m_TFTopic = tfTopic;
            }

            // Keyed by the FULL frame_id (after trailing-slash stripping), not
            // just its trailing segment. The original upstream version keyed
            // on the trailing segment alone, matching real ROS's global
            // frame_id-uniqueness invariant for a SINGLE robot's /tf graph --
            // correct there, but it silently aliases two different robots'
            // same-named leaf frames (both "base_link", both "odom", ...)
            // onto the identical TF2Stream once robot_id prefixing is layered
            // on top (see UvgRosConnection.RemapFrame): a "robotB/BOOM" probe
            // and a plain "BOOM" probe resolved to the exact same GameObject,
            // live-tracking robot A's real boom motion under robot B's name,
            // confirmed empirically before this fix. Full-path keying makes
            // that collision structurally impossible: two different full
            // strings simply cannot land in the same slot.
            //
            // The recursive slash-splitting below (peel the trailing segment,
            // recurse on the rest to find/create a parent) is UNCHANGED --
            // still useful for a single external caller passing one already-
            // hierarchical name (e.g. a genuinely namespaced ROS frame_id) --
            // it just no longer collapses the key down to that trailing
            // segment once the node exists.
            // No parent is ever inferred from slashes in `frame_id` -- a
            // second regression from the SAME multi-robot collision fix that
            // introduced full-path keying just above. The old (leaf-keyed)
            // code recursively treated every "/"-separated segment as a real
            // ancestor frame, walking e.g. "a/b/c" into a->b->c. That was
            // already semantically wrong for a robot_id prefix (robot_id is a
            // NAMESPACE, not a real parent frame in anyone's actual tf tree),
            // but it was harmless there because a leaf-keyed table couldn't
            // tell the difference anyway. Once keys became full paths, this
            // recursion started actively DOING something: the first time any
            // caller resolved e.g. "robotB/LF_THIGH" BEFORE its real parent
            // edge ("LF_HFE_output" -> "LF_THIGH", carried on live /tf) had
            // arrived -- which a model's own TF2Attachment.Reparent() does,
            // synchronously, before any network data exists at all -- this
            // recursion silently parented it onto a synthetic "robotB" stub
            // instead of leaving it unparented. ReceiveTF's `if (tf.Parent ==
            // null)` guard (added by the same earlier fix, to stop a static
            // message from clobbering an already-linked dynamic frame) then
            // refused to ever correct it: Parent was no longer null, just
            // wrong. Confirmed live: LF_HIP/LF_THIGH/LF_HFE_drive/LF_SHANK
            // and their whole downstream chain sat frozen at world (0,0,0)
            // (ALMA's legs not moving) while each still received real,
            // changing LOCAL rotation data the whole time -- the rotation
            // math was fine, only the parent link was wrong.
            //
            // Parent linkage now comes ONLY from an explicit SetParent call
            // (ReceiveTF/StaticTF, both already using two separate atomic
            // names + a real edge from an actual tf message) -- never
            // inferred here. `frame_id` is treated as one opaque key
            // regardless of how many slashes it contains.
            public TF2Stream GetOrCreateFrame(string frame_id)
            {
                TF2Stream tf;
                while (frame_id.EndsWith("/"))
                    frame_id = frame_id.Substring(0, frame_id.Length - 1);

                var slash = frame_id.LastIndexOf('/');
                var singleName = slash == -1 ? frame_id : frame_id.Substring(slash + 1);
                if (!m_TransformTable.TryGetValue(frame_id, out tf) || tf == null)
                {
                    tf = new TF2Stream(null, singleName, m_TFTopic);
                    m_TransformTable[frame_id] = tf;
                    NotifyChanged(tf);
                }

                return tf;
            }

            // Looks up/creates parent and child as two SEPARATE, already-
            // complete frame names (exactly what StaticRemapper.StaticTF
            // already does for /tf_static, a few lines below in that file) --
            // NOT by concatenating them into one "parent/child" string and
            // re-splitting, which the multi-robot case breaks: a robot-id-
            // prefixed pair like "robotB/CABIN" + "/" + "robotB/BOOM" is a
            // 4-segment string, and peeling one trailing segment at a time
            // off THAT produces a spurious extra "robotB" node wedged between
            // them instead of the intended direct CABIN -> BOOM edge. Each of
            // header.frame_id/child_frame_id is already the caller's complete,
            // correct name (robot_id prefix included where relevant) with no
            // implied internal hierarchy of its own.
            public void ReceiveTF(TFMessageMsg message)
            {
                foreach (var tf_message in message.transforms)
                {
                    var parent = GetOrCreateFrame(tf_message.header.frame_id);
                    var tf = GetOrCreateFrame(tf_message.child_frame_id);
                    if (tf.Parent == null) tf.SetParent(parent);

                    tf.Add(
                        tf_message.header.stamp.ToLongTime(kRosVersion),
                        tf_message.transform.translation.From<FLU>(),
                        tf_message.transform.rotation.From<FLU>()
                    );
                    NotifyChanged(tf);
                }
            }

            public IEnumerable<string> GetTransformNames()
            {
                return m_TransformTable.Keys;
            }

            public IEnumerable<TF2Stream> GetTransforms()
            {
                return m_TransformTable.Values;
            }

            public TF2Stream GetTransformStream(string frame_id)
            {
                TF2Stream result = null;
                m_TransformTable.TryGetValue(frame_id, out result);
                return result;
            }

            public void AddListener(Action<TF2Stream> callback)
            {
                m_Listeners.Add(callback);
            }

            public void NotifyChanged(TF2Stream stream)
            {
                foreach (Action<TF2Stream> callback in m_Listeners)
                {
                    callback(stream);
                }
            }

            public void NotifyAllChanged()
            {
                foreach (var stream in m_TransformTable.Values)
                    NotifyChanged(stream);
            }
        }

        private TF2System()
        {

        }

        public static TF2System GetOrCreateInstance()
        {
            if (instance != null)
                return instance;

            instance = new TF2System();
            return instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            // Guarantees a fresh instance (and therefore fresh GameObjects for
            // every TF2Stream) at the start of every Play session, even under
            // "Enter Play Mode Options (no domain reload)" where a plain
            // static field would otherwise keep pointing at last session's
            // now-destroyed GameObjects.
            instance = null;
        }

        public IEnumerable<string> GetTransformNames(string tfTopic = "/tf")
        {
            return GetOrCreateTFTopic(tfTopic).GetTransformNames();
        }

        public IEnumerable<TF2Stream> GetTransforms(string tfTopic = "/tf")
        {
            return GetOrCreateTFTopic(tfTopic).GetTransforms();
        }

        public void AddListener(Action<TF2Stream> callback, bool notifyAllStreamsNow = true, string tfTopic = "/tf")
        {
            TFTopicState state = GetOrCreateTFTopic(tfTopic);
            state.AddListener(callback);
            if (notifyAllStreamsNow)
                state.NotifyAllChanged();
        }

        public void NotifyAllChanged(TF2Stream stream)
        {
            GetOrCreateTFTopic(stream.TFTopic).NotifyAllChanged();
        }

        public TF2Frame GetTransform(HeaderMsg header, string tfTopic = "/tf")
        {
            return GetTransform(header.frame_id, header.stamp.ToLongTime(kRosVersion), tfTopic);
        }

        public TF2Frame GetTransform(string frame_id, long time, string tfTopic = "/tf")
        {
            var stream = GetTransformStream(frame_id, tfTopic);
            if (stream != null)
                return stream.GetWorldTF(time);
            return TF2Frame.identity;
        }

        public TF2Frame GetTransform(string frame_id, TimeMsg time, string tfTopic = "/tf")
        {
            return GetTransform(frame_id, time.ToLongTime(kRosVersion), tfTopic);
        }

        public TF2Stream GetTransformStream(string frame_id, string tfTopic = "/tf")
        {
            return GetOrCreateTFTopic(tfTopic).GetTransformStream(frame_id);
        }

        public GameObject GetTransformObject(string frame_id, string tfTopic = "/tf")
        {
            TF2Stream stream = GetOrCreateTFTopic(tfTopic).GetOrCreateFrame(frame_id);
            return stream.GameObject;
        }

        public TFTopicState GetOrCreateTFTopic(string tfTopic = "/tf")
        {
            TFTopicState tfTopicState;
            if (!m_TFTopics.TryGetValue(tfTopic, out tfTopicState))
            {
                tfTopicState = new TFTopicState(tfTopic);
                m_TFTopics[tfTopic] = tfTopicState;
            }
            return tfTopicState;
        }

        public TF2Stream GetOrCreateFrame(string frame_id, string tfTopic = "/tf")
        {
            TFTopicState topicState = GetOrCreateTFTopic(tfTopic);
            return topicState.GetOrCreateFrame(frame_id);
        }
    }
}
