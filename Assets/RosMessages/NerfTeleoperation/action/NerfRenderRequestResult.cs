//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.NerfTeleoperation
{
    [Serializable]
    public class NerfRenderRequestResult : Message
    {
        public const string k_RosMessageName = "nerf_teleoperation_msgs/NerfRenderRequest";
        public override string RosMessageName => k_RosMessageName;

        //  Result
        public Sensor.CompressedImageMsg rgb_image;
        public Sensor.ImageMsg depth_image;
        public float resolution;
        public float render_time;
        public int client_id;
        public ushort seq;

        public NerfRenderRequestResult()
        {
            this.rgb_image = new Sensor.CompressedImageMsg();
            this.depth_image = new Sensor.ImageMsg();
            this.resolution = 0.0f;
            this.render_time = 0.0f;
            this.client_id = 0;
            this.seq = 0;
        }

        public NerfRenderRequestResult(Sensor.CompressedImageMsg rgb_image, Sensor.ImageMsg depth_image, float resolution, float render_time, int client_id, ushort seq)
        {
            this.rgb_image = rgb_image;
            this.depth_image = depth_image;
            this.resolution = resolution;
            this.render_time = render_time;
            this.client_id = client_id;
            this.seq = seq;
        }

        public static NerfRenderRequestResult Deserialize(MessageDeserializer deserializer) => new NerfRenderRequestResult(deserializer);

        private NerfRenderRequestResult(MessageDeserializer deserializer)
        {
            this.rgb_image = Sensor.CompressedImageMsg.Deserialize(deserializer);
            this.depth_image = Sensor.ImageMsg.Deserialize(deserializer);
            deserializer.Read(out this.resolution);
            deserializer.Read(out this.render_time);
            deserializer.Read(out this.client_id);
            deserializer.Read(out this.seq);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.rgb_image);
            serializer.Write(this.depth_image);
            serializer.Write(this.resolution);
            serializer.Write(this.render_time);
            serializer.Write(this.client_id);
            serializer.Write(this.seq);
        }

        public override string ToString()
        {
            return "NerfRenderRequestResult: " +
            "\nrgb_image: " + rgb_image.ToString() +
            "\ndepth_image: " + depth_image.ToString() +
            "\nresolution: " + resolution.ToString() +
            "\nrender_time: " + render_time.ToString() +
            "\nclient_id: " + client_id.ToString() +
            "\nseq: " + seq.ToString();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize, MessageSubtopic.Result);
        }
    }
}