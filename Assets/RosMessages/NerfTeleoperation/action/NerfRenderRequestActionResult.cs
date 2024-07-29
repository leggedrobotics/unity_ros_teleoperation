using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.NerfTeleoperation
{
    public class NerfRenderRequestActionResult : ActionResult<NerfRenderRequestResult>
    {
        public const string k_RosMessageName = "nerf_teleoperation_msgs/NerfRenderRequestActionResult";
        public override string RosMessageName => k_RosMessageName;


        public NerfRenderRequestActionResult() : base()
        {
            this.result = new NerfRenderRequestResult();
        }

        public NerfRenderRequestActionResult(HeaderMsg header, GoalStatusMsg status, NerfRenderRequestResult result) : base(header, status)
        {
            this.result = result;
        }
        public static NerfRenderRequestActionResult Deserialize(MessageDeserializer deserializer) => new NerfRenderRequestActionResult(deserializer);

        NerfRenderRequestActionResult(MessageDeserializer deserializer) : base(deserializer)
        {
            this.result = NerfRenderRequestResult.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.status);
            serializer.Write(this.result);
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
