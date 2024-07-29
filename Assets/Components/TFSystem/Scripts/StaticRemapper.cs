using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Tf2;
using RosMessageTypes.Geometry;


public class StaticRemapper : MonoBehaviour
{
    ROSConnection ros;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<TFMessageMsg>("/tf_static", StaticTF);
        
    }
    void StaticTF (TFMessageMsg msg){
        // get the tf system
        var tfSystem = TFSystem.GetOrCreateInstance();

        // add the static tfs to the tf tree
        foreach (var tf in msg.transforms)
        {
            var child = tfSystem.GetOrCreateFrame(tf.child_frame_id);
            var parent = tfSystem.GetOrCreateFrame(tf.header.frame_id);
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
