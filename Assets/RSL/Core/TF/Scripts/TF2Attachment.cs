// Forked from com.unity.robotics.ros-tcp-connector's Runtime/TcpConnector/TFAttachment.cs
// (see TF2System.cs in this same folder for why -- renamed with a "2" suffix
// since the original TFAttachment is still present via the still-installed
// ros-tcp-connector package). Two differences from the original:
//   1. FrameID's setter re-runs the reparent immediately (the original only
//      reparented once, in Start() -- changing FrameID afterwards, e.g. from
//      ModelManager's root-frame dropdown, silently had no visible effect
//      until the next scene reload).
//   2. Start() guards against a null/missing transform instead of an
//      unguarded access -- cheap insurance directly relevant to the
//      MissingReferenceException this whole fork exists to fix.
using UnityEngine;

namespace UvgRos.TF2
{
    public class TF2Attachment : MonoBehaviour
    {
        [SerializeField]
        string m_FrameID;
        public string FrameID
        {
            get => m_FrameID;
            set
            {
                m_FrameID = value;
                // Guard against Application.isPlaying: URDFConverter (an
                // editor-time tool) sets FrameID while baking a robot prefab,
                // long before anything should try to resolve it against a
                // live TF tree.
                if (Application.isPlaying && isActiveAndEnabled) Reparent();
            }
        }
        [SerializeField]
        string m_TFTopic = "/tf";
        public string TFTopic { get => m_TFTopic; set => m_TFTopic = value; }

        void Start()
        {
            Reparent();
        }

        void Reparent()
        {
            if (string.IsNullOrEmpty(m_FrameID)) return;

            GameObject frameObject = TF2System.GetOrCreateInstance().GetTransformObject(m_FrameID, m_TFTopic);
            if (frameObject == null)
            {
                Debug.LogWarning("[TF2Attachment] '" + name + "' could not resolve frame '" + m_FrameID + "' on topic '" + m_TFTopic + "'");
                return;
            }

            transform.parent = frameObject.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
