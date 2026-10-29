using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using UnityEditor;
using UnityEngine;
using UvgRos;

/// <summary>Exercise the installed native video package against a live ROS server in batch mode.</summary>
public static class NetworkReleaseValidation
{
    private static UvgRosConnection connection;
    private static GameObject owner;
    private static MethodInfo update;
    private static int mainThread, sets, singles, phase, before, generic;
    private static bool serviceOk;
    private static UvgRosPublisher<StringMsg> publisher;
    private static double started, phaseAt;
    private static string state;
    private static readonly System.Collections.Generic.HashSet<string> shapes = new();
    private const string Left = "/release/stereo/left", Right = "/release/stereo/right";

    public static void Run()
    {
        try
        {
            state = Environment.GetEnvironmentVariable("RELEASE_STATE");
            var visible = typeof(RSL.Core.SensorStream).GetMethod("IsViewable", BindingFlags.Static | BindingFlags.NonPublic);
            Require((bool)visible.Invoke(null, new object[] { new TopicListEntry { Framing = "encoded_video_synced", MsgType = "sensor_msgs/Image" } }), "UI hides synchronized image topics");
            mainThread = Thread.CurrentThread.ManagedThreadId;
            started = phaseAt = EditorApplication.timeSinceStartup;
            owner = new GameObject("network-release-validation");
            connection = owner.AddComponent<UvgRosConnection>();
            connection.LocalHost = "127.0.0.1";
            connection.Connect("127.0.0.1", int.Parse(Environment.GetEnvironmentVariable("RELEASE_PORT")));
            update = typeof(UvgRosConnection).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            connection.SubscribeSynced(Left, Received, mainThread: true, rosMessageType: "sensor_msgs/Image");
            connection.Subscribe<ImageMsg>(Right, _ => singles++, mainThread: true);
            var routes = (IDictionary)typeof(UvgRosConnection).GetField("_byTopic", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(connection);
            Require(ReferenceEquals(routes[Left], routes[Right]), "members did not share a worker");
            connection.Subscribe<StringMsg>("/release/echo", message =>
            {
                Require(message.data.StartsWith("hello_ros_"), "generic ROS message corrupted");
                generic++;
            }, mainThread: true);
            publisher = connection.Publish<StringMsg>("/release/unity_message");
            connection.CallService<TriggerRequest, TriggerResponse>("/release/trigger", new TriggerRequest(), response =>
            {
                serviceOk = response.success && response.message == "release service works";
            });
            EditorApplication.update += Tick;
        }
        catch (Exception error) { Finish(error); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Received(UvgRosImageSet images)
    {
        try
        {
            Require(Thread.CurrentThread.ManagedThreadId == mainThread, "callback left main thread");
            Require(images.Images.Count == 2, "partial group");
            var a = images.Images[Left]; var b = images.Images[Right];
            Require((long)b.header.stamp.sec * 1000000000 + b.header.stamp.nanosec -
                    ((long)a.header.stamp.sec * 1000000000 + a.header.stamp.nanosec) == 198, "header skew changed");
            Require(a.header.frame_id.Substring(6) == b.header.frame_id.Substring(6), "stereo frames mixed");
            Require(a.data.Length == a.width * a.height * 3 && b.data.Length == b.width * b.height * 3, "bad image dimensions");
            // Unity worker converts source BGR to RGB.
            Require(a.data[2] > a.data[0] + 100 && b.data[0] > b.data[2] + 100, "RGB conversion failed");
            shapes.Add($"{a.width}x{a.height}/{b.width}x{b.height}");
            sets++;
        }
        catch (Exception error) { Finish(error); }
    }

    private static void Tick()
    {
        try
        {
            update.Invoke(connection, null);
            publisher.Send(new StringMsg("unity_release_message"));
            double now = EditorApplication.timeSinceStartup;
            Require(now - started < 90, $"timeout: sets={sets}, singles={singles}, phase={phase}");
            if (phase == 0 && sets >= 100 && shapes.Count >= 2)
            {
                File.WriteAllText(Path.Combine(state, "pause-right"), "");
                phase = 1; phaseAt = now;
            }
            else if (phase == 1 && now - phaseAt > .7) { before = sets; phase = 2; phaseAt = now; }
            else if (phase == 2 && now - phaseAt > 1.5)
            {
                Require(sets == before, "delivered an incomplete group while right source was paused");
                File.Delete(Path.Combine(state, "pause-right"));
                phase = 3; phaseAt = now;
            }
            else if (phase == 3 && sets > before + 10)
            {
                before = sets;
                connection.Connect();
                phase = 4; phaseAt = now;
            }
            else if (phase == 4 && sets > before + 20)
            {
                Require(singles > 20, "ordinary image subscriber received no frames");
                connection.Unsubscribe(Left);
                before = singles;
                phase = 5; phaseAt = now;
            }
            else if (phase == 5 && singles > before + 10)
            {
                connection.Unsubscribe(Right);
                var routes = (IDictionary)typeof(UvgRosConnection).GetField("_byTopic", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(connection);
                connection.Unsubscribe("/release/echo");
                Require(routes.Count == 0, "routes leaked after last unsubscribe");
                Require(generic > 20, "generic ROS subscription received no data");
                Require(serviceOk, "ROS Trigger service did not roundtrip");
                Require(File.ReadAllText(Path.Combine(state, "unity-publish.txt")) == "unity_release_message", "Unity publisher did not reach the ROS subscriber");
                Finish(null);
            }
        }
        catch (Exception error) { Finish(error); }
    }

    private static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        if (state != null) File.Delete(Path.Combine(state, "pause-right"));
        if (connection != null) connection.Disconnect();
        if (owner != null) UnityEngine.Object.DestroyImmediate(owner);
        if (error != null) Debug.LogException(error);
        string result = $"{{\"ok\":{(error == null ? "true" : "false")},\"sets\":{sets},\"single_images\":{singles},\"resolutions\":{shapes.Count},\"generic_messages\":{generic},\"service_ok\":{(serviceOk ? "true" : "false")},\"phase\":{phase}}}";
        if (state != null) File.WriteAllText(Path.Combine(state, "unity-result.json"), result);
        Debug.Log("[network-release-validation] " + result);
        EditorApplication.Exit(error == null ? 0 : 1);
    }
}
