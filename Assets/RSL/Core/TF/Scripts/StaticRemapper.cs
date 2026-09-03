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
        UvgRosConnection ros;

        // Singleton guard: two of these in one scene silently double-
        // subscribed to /tf_static, and UvgRosConnection.Subscribe's
        // duplicate-topic handling (tear down the old native stream, ask the
        // server for a new one) doesn't renegotiate the server's cached
        // route -- the result was a second stream listening on a port the
        // server was never told to send to, with no error anywhere. Refusing
        // the second instance outright, loudly, beats relying on that
        // recovery path working.
        private static StaticRemapper s_instance;

        void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Debug.LogWarning("[StaticRemapper] another instance (" + s_instance.name +
                    ") already exists in this scene -- disabling this one on " + name +
                    ". Only one StaticRemapper should subscribe to /tf_static.");
                enabled = false;
                return;
            }
            s_instance = this;
        }

        void OnDestroy()
        {
            if (s_instance == this) s_instance = null;
        }

        void Start()
        {
            ros = UvgRosConnection.GetOrCreateInstance();
            ros.Subscribe<TFMessageMsg>("/tf_static", StaticTF, mainThread: true);
            ros.Subscribe<TFMessageMsg>("/tf", DynamicTF, mainThread: true);
        }

        // TF2System (see UvgRos.TF2.TF2System's doc comment) no longer
        // subscribes to anything itself -- this is the one place in the
        // project that feeds it live, dynamic /tf messages, mirroring
        // StaticTF below for /tf_static. Both land in the same default
        // "/tf" topic-bucket (GetOrCreateFrame's default tfTopic param), so
        // this doesn't create a second, separate tree.
        void DynamicTF(TFMessageMsg msg)
        {
            if (this == null) return;
            TF2System.GetOrCreateInstance().GetOrCreateTFTopic("/tf").ReceiveTF(msg);
        }
        void StaticTF (TFMessageMsg msg){
            // A buffered message can still drain through Update() a frame or
            // two into Play mode stopping / the app quitting -- by then the
            // frame GameObjects (and possibly this component) are already
            // destroyed, and Unity's fake-null still lets a bare reference
            // through, so this is not optional even though it looks it.
            if (this == null) return;

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
