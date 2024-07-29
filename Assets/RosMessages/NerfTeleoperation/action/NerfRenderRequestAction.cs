using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;


namespace RosMessageTypes.NerfTeleoperation
{
    public class NerfRenderRequestAction : Action<NerfRenderRequestActionGoal, NerfRenderRequestActionResult, NerfRenderRequestActionFeedback, NerfRenderRequestGoal, NerfRenderRequestResult, NerfRenderRequestFeedback>
    {
        public const string k_RosMessageName = "nerf_teleoperation_msgs/NerfRenderRequestAction";
        public override string RosMessageName => k_RosMessageName;


        public NerfRenderRequestAction() : base()
        {
            this.action_goal = new NerfRenderRequestActionGoal();
            this.action_result = new NerfRenderRequestActionResult();
            this.action_feedback = new NerfRenderRequestActionFeedback();
        }

        public static NerfRenderRequestAction Deserialize(MessageDeserializer deserializer) => new NerfRenderRequestAction(deserializer);

        NerfRenderRequestAction(MessageDeserializer deserializer)
        {
            this.action_goal = NerfRenderRequestActionGoal.Deserialize(deserializer);
            this.action_result = NerfRenderRequestActionResult.Deserialize(deserializer);
            this.action_feedback = NerfRenderRequestActionFeedback.Deserialize(deserializer);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.action_goal);
            serializer.Write(this.action_result);
            serializer.Write(this.action_feedback);
        }

    }
}
