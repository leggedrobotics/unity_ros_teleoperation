//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.NerfTeleoperation
{
    [Serializable]
    public class SplatRequestResponse : Message
    {
        public const string k_RosMessageName = "nerf_teleoperation_msgs/SplatRequest";
        public override string RosMessageName => k_RosMessageName;

        //  Output the 
        public string base64_ply;

        public SplatRequestResponse()
        {
            this.base64_ply = "";
        }

        public SplatRequestResponse(string base64_ply)
        {
            this.base64_ply = base64_ply;
        }

        public static SplatRequestResponse Deserialize(MessageDeserializer deserializer) => new SplatRequestResponse(deserializer);

        private SplatRequestResponse(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.base64_ply);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.base64_ply);
        }

        public override string ToString()
        {
            return "SplatRequestResponse: " +
            "\nbase64_ply: " + base64_ply.ToString();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize, MessageSubtopic.Response);
        }
    }
}