//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.RetargetingRos
{
    [Serializable]
    public class HandGestureMsg : Message
    {
        public const string k_RosMessageName = "retargeting_ros/HandGesture";
        public override string RosMessageName => k_RosMessageName;

        public Std.HeaderMsg header;
        public sbyte gesture_id;
        public string name;

        public HandGestureMsg()
        {
            this.header = new Std.HeaderMsg();
            this.gesture_id = 0;
            this.name = "";
        }

        public HandGestureMsg(Std.HeaderMsg header, sbyte gesture_id, string name)
        {
            this.header = header;
            this.gesture_id = gesture_id;
            this.name = name;
        }

        public static HandGestureMsg Deserialize(MessageDeserializer deserializer) => new HandGestureMsg(deserializer);

        private HandGestureMsg(MessageDeserializer deserializer)
        {
            this.header = Std.HeaderMsg.Deserialize(deserializer);
            deserializer.Read(out this.gesture_id);
            deserializer.Read(out this.name);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.gesture_id);
            serializer.Write(this.name);
        }

        public override string ToString()
        {
            return "HandGestureMsg: " +
            "\nheader: " + header.ToString() +
            "\ngesture_id: " + gesture_id.ToString() +
            "\nname: " + name.ToString();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}
