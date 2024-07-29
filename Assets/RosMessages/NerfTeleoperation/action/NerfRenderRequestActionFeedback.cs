using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.NerfTeleoperation
{
    public class NerfRenderRequestActionFeedback : ActionFeedback<NerfRenderRequestFeedback>
    {
        public const string k_RosMessageName = "nerf_teleoperation_msgs/NerfRenderRequestActionFeedback";
        public override string RosMessageName => k_RosMessageName;


        public NerfRenderRequestActionFeedback() : base()
        {
            this.feedback = new NerfRenderRequestFeedback();
        }

        public NerfRenderRequestActionFeedback(HeaderMsg header, GoalStatusMsg status, NerfRenderRequestFeedback feedback) : base(header, status)
        {
            this.feedback = feedback;
        }
        public static NerfRenderRequestActionFeedback Deserialize(MessageDeserializer deserializer) => new NerfRenderRequestActionFeedback(deserializer);

        NerfRenderRequestActionFeedback(MessageDeserializer deserializer) : base(deserializer)
        {
            this.feedback = NerfRenderRequestFeedback.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.status);
            serializer.Write(this.feedback);
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
