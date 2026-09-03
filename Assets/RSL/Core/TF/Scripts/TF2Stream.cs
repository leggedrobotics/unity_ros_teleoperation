// Forked from com.unity.robotics.ros-tcp-connector's Runtime/TcpConnector/TFStream.cs
// (see TF2System.cs in this same folder for why this project keeps its own copy
// instead of depending on the shared package's version). Renamed with a "2"
// suffix (TF2Stream/TF2Frame) since the original, un-forked TFStream/TFFrame
// are still present via the still-installed ros-tcp-connector package.
using RosMessageTypes.BuiltinInterfaces;
using System.Collections.Generic;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;
using UnityEngine;

namespace UvgRos.TF2
{
    // Represents a transform - position and rotation.
    //(Like the Unity Transform class, but without the GameObject baggage that comes with it.)
    public struct TF2Frame
    {
        public Vector3 translation;
        public Quaternion rotation;
        public static TF2Frame identity = new TF2Frame(Vector3.zero, Quaternion.identity);

        public TF2Frame(Vector3 translation, Quaternion rotation)
        {
            this.translation = translation;
            this.rotation = rotation;
        }

        public Vector3 TransformPoint(Vector3 point)
        {
            return translation + rotation * point;
        }

        public Vector3 InverseTransformPoint(Vector3 point)
        {
            return Quaternion.Inverse(rotation) * (point - translation);
        }

        public TF2Frame Compose(TF2Frame child)
        {
            return new TF2Frame(TransformPoint(child.translation), rotation * child.rotation);
        }

        public static TF2Frame Lerp(TF2Frame a, TF2Frame b, float lerp)
        {
            return new TF2Frame
            {
                translation = Vector3.Lerp(a.translation, b.translation, lerp),
                rotation = Quaternion.Lerp(a.rotation, b.rotation, lerp)
            };
        }
    }

    // Represents a transform frame changing over time.
    public class TF2Stream
    {
        public string Name { get; private set; }
        public string TFTopic { get; private set; }
        public TF2Stream Parent { get; private set; }
        public IEnumerable<TF2Stream> Children => m_Children;

        public static bool UseSimTime { get; set; } = false;

        // oldest first
        List<long> m_Timestamps = new List<long>();
        // same order as m_Timestamps
        List<TF2Frame> m_Frames = new List<TF2Frame>();
        List<TF2Stream> m_Children = new List<TF2Stream>();

        // a gameobject at the last known position of this tfstream
        GameObject m_GameObject;
        public GameObject GameObject => m_GameObject;

        public TF2Stream(TF2Stream parent, string name, string tfTopic)
        {
            Name = name;
            TFTopic = tfTopic;
            m_GameObject = new GameObject(name);
            m_GameObject.tag = "tf";
            SetParent(parent);
        }

        public void SetParent(TF2Stream newParent)
        {
            if (Parent == newParent)
                return;

            if (Parent != null)
            {
                Parent.m_Children.Remove(this);
            }

            if (newParent != null)
            {
                m_GameObject.transform.parent = newParent.m_GameObject.transform;
                newParent.m_Children.Add(this);
            }
            else
            {
                m_GameObject.transform.parent = null;
            }
            Parent = newParent;
        }

        public void Add(long timestamp, Vector3 translation, Quaternion rotation)
        {
            TF2Frame newEntry = new TF2Frame(translation, rotation);
            // most likely case: we're just adding a newer transform to the end of the list
            if (!UseSimTime || m_Timestamps.Count == 0 || m_Timestamps[m_Timestamps.Count - 1] < timestamp)
            {
                m_Timestamps.Add(timestamp);
                m_Frames.Add(newEntry);
                m_GameObject.transform.localPosition = translation;
                m_GameObject.transform.localRotation = rotation;
            }
            else
            {
                int index = m_Timestamps.BinarySearch(timestamp);
                if (index < 0)
                {
                    // no preexisting entry, but ~index gives us the position to insert the new entry
                    m_Timestamps.Insert(~index, timestamp);
                    m_Frames.Insert(~index, newEntry);
                }
                else
                {
                    // we found an existing entry at the same timestamp!? Just replace the old one, I guess.
                    m_Frames[index] = newEntry;
                }
            }

            // for now, just a lazy way to keep the buffer from growing infinitely: every 50 updates, discard the oldest 50
            if (m_Timestamps.Count > 100)
            {
                m_Timestamps.RemoveRange(0, 50);
                m_Frames.RemoveRange(0, 50);
            }
        }

        public TF2Frame GetLocalTF(long time = 0)
        {
            // this stream has no data at all, so just report identity.
            if (m_Frames.Count == 0)
                return TF2Frame.identity;

            // if time is 0, just get the newest position
            if (time == 0)
                return m_Frames[m_Frames.Count - 1];

            int index = m_Timestamps.BinarySearch(time);
            if (index >= 0)
            {
                // no problem, we have an entry at this time
                return m_Frames[index];
            }

            index = ~index;
            if (index == 0)
            {
                // older than our first entry: just use the first one
                return m_Frames[0];
            }
            else if (index == m_Frames.Count)
            {
                // newer than our last entry: just use the last one
                return m_Frames[m_Frames.Count - 1];
            }
            else
            {
                // between two entries: interpolate
                float lerpValue = (time - m_Timestamps[index - 1]) / (float)(m_Timestamps[index] - m_Timestamps[index - 1]);
                return TF2Frame.Lerp(m_Frames[index - 1], m_Frames[index], lerpValue);
            }
        }

        public TF2Frame GetLocalTF(TimeMsg time, int rosVersion)
        {
            return GetLocalTF(time.ToLongTime(rosVersion));
        }

        public TF2Frame GetWorldTF(long time = 0)
        {
            TF2Frame parent;
            if (Parent != null)
                parent = Parent.GetWorldTF(time);
            else
                parent = TF2Frame.identity;

            return parent.Compose(GetLocalTF(time));
        }

        public TF2Frame GetWorldTF(TimeMsg time, int rosVersion)
        {
            return GetWorldTF(time.ToLongTime(rosVersion));
        }

        // Can we safely stop polling for updates to a transform at this time?
        public bool IsTimeStable(long time)
        {
            if (time == 0) // time 0 ("use the newest data") is never stable
                return false;

            if (m_Timestamps.Count == 0 || m_Timestamps[0] > time || m_Timestamps[m_Timestamps.Count - 1] < time)
                return false;

            if (Parent != null && !Parent.IsTimeStable(time))
                return false;

            return true;
        }
    }
}
