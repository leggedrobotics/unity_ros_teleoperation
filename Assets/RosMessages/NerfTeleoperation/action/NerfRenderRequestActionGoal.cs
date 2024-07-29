using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using RosMessageTypes.Std;
using RosMessageTypes.Actionlib;

namespace RosMessageTypes.NerfTeleoperation
{
    public class NerfRenderRequestActionGoal : ActionGoal<NerfRenderRequestGoal>
    {
        public const string k_RosMessageName = "nerf_teleoperation_msgs/NerfRenderRequestActionGoal";
        public override string RosMessageName => k_RosMessageName;


        public NerfRenderRequestActionGoal() : base()
        {
            this.goal = new NerfRenderRequestGoal();
        }

        public NerfRenderRequestActionGoal(HeaderMsg header, GoalIDMsg goal_id, NerfRenderRequestGoal goal) : base(header, goal_id)
        {
            this.goal = goal;
        }
        public static NerfRenderRequestActionGoal Deserialize(MessageDeserializer deserializer) => new NerfRenderRequestActionGoal(deserializer);

        NerfRenderRequestActionGoal(MessageDeserializer deserializer) : base(deserializer)
        {
            this.goal = NerfRenderRequestGoal.Deserialize(deserializer);
        }
        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.header);
            serializer.Write(this.goal_id);
            serializer.Write(this.goal);
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
