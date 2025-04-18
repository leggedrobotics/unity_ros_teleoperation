//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.RetargetingRos
{
    [Serializable]
    public class ManoLandmarksMsg : Message
    {
        public const string k_RosMessageName = "retargeting_ros/ManoLandmarks";
        public override string RosMessageName => k_RosMessageName;

        public Std.HeaderMsg header;
        public Geometry.PointMsg[] landmarks;

        public ManoLandmarksMsg()
        {
            this.header = new Std.HeaderMsg();
            this.landmarks = new Geometry.PointMsg[0];
        }

        public ManoLandmarksMsg(Std.HeaderMsg header, Geometry.PointMsg[] landmarks)
        {
            this.header = header;
            this.landmarks = landmarks;
        }

        public static ManoLandmarksMsg Deserialize(MessageDeserializer deserializer) => new ManoLandmarksMsg(deserializer);

        private ManoLandmarksMsg(MessageDeserializer deserializer)
        {
            this.header = Std.HeaderMsg.Deserialize(deserializer);
            deserializer.Read(out this.landmarks, Geometry.PointMsg.Deserialize, deserializer.ReadLength());
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.WriteLength(this.landmarks);
            serializer.Write(this.landmarks);
        }

        public override string ToString()
        {
            return "ManoLandmarksMsg: " +
            "\nheader: " + header.ToString() +
            "\nlandmarks: " + System.String.Join(", ", landmarks.ToList());
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
