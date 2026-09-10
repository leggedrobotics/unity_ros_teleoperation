// Forked from com.unity.robotics.ros-tcp-connector's Runtime/TcpConnector/TFAttachment.cs
// (see TF2System.cs in this same folder for why -- renamed with a "2" suffix
// since the original TFAttachment is still present via the still-installed
// ros-tcp-connector package). Three differences from the original:
//   1. FrameID's setter re-runs the reparent immediately (the original only
//      reparented once, in Start() -- changing FrameID afterwards, e.g. from
//      ModelManager's root-frame dropdown, silently had no visible effect
//      until the next scene reload).
//   2. Start() guards against a null/missing transform instead of an
//      unguarded access -- cheap insurance directly relevant to the
//      MissingReferenceException this whole fork exists to fix.
//   3. A manual position/rotation offset (m_PositionOffset/m_RotationOffsetEuler),
//      applied on top of the resolved frame every frame in LateUpdate rather
//      than once at Reparent() time -- multi-robot: two robots' TF trees
//      both anchored near "odom"/"world" render on top of each other, and
//      this is the knob that pulls them apart. LateUpdate (after the TF
//      stream's own Update-time frame pose writes) rather than a one-shot
//      apply is what lets the offset fields be dragged live in the Inspector
//      during Play mode -- a plain [SerializeField] edited in the Inspector
//      writes the backing field directly, bypassing the FrameID-style
//      property setter, so nothing would re-apply it otherwise. This is safe
//      to do every frame because nothing else ever writes THIS transform's
//      local pose -- only the PARENT frame object's pose is externally
//      driven by TF2Stream, so there is nothing here to fight.
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

        [Tooltip("Manual offset applied on top of the resolved TF frame -- pulls " +
                 "this robot's whole tree away from another robot's, when both are " +
                 "otherwise anchored near the same place. Edit live in Play mode.")]
        [SerializeField]
        Vector3 m_PositionOffset = Vector3.zero;
        public Vector3 PositionOffset { get => m_PositionOffset; set => m_PositionOffset = value; }

        [SerializeField]
        Vector3 m_RotationOffsetEuler = Vector3.zero;
        public Vector3 RotationOffsetEuler { get => m_RotationOffsetEuler; set => m_RotationOffsetEuler = value; }

        void Start()
        {
            Reparent();
        }

        void LateUpdate()
        {
            if (transform.parent != null) ApplyOffset();
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
            ApplyOffset();
        }

        void ApplyOffset()
        {
            transform.localPosition = m_PositionOffset;
            transform.localRotation = Quaternion.Euler(m_RotationOffsetEuler);
        }
    }
}
