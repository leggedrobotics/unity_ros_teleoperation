// Renders a small, head-locked "skeleton" HUD of the current TF tree in
// front of the user: a sphere per known frame, a line to each frame's
// parent, scaled down and expressed relative to a chosen pivot frame so the
// pivot always sits upright at the HUD's local origin -- same idea as
// PoseManager.SetFixedLocation's camera-reparent trick (see PoseManager.cs),
// just applied to this component's own transform instead of Camera.main.
//
// Attach this to any scene GameObject (e.g. an empty child of the XR rig);
// it builds its own child markers at runtime. Assign lineMaterial in the
// Inspector, or leave it unset to fall back to a plain unlit default.
using System.Collections.Generic;
using UnityEngine;
using RSL.Core.Robots;
using UvgRos.TF2;


namespace RSL.Core.TF
{
    public class MiniSkeletonView : MonoBehaviour
    {
        [Header("TF")]
        public string tfTopic = "/tf";
        [Tooltip("Frame treated as fixed/upright in the HUD. Empty = follow ModelManager's current root frame.")]
        public string pivotFrameId = "";

        [Header("HUD placement (relative to Camera.main)")]
        public float scale = 0.1f;
        public Vector3 hudOffset = new Vector3(0f, -0.15f, 0.5f);

        [Header("Appearance")]
        public float markerRadius = 0.01f;
        public Material lineMaterial;
        public Color lineColor = Color.cyan;

        private class MarkerEntry
        {
            public Transform marker;
            public LineRenderer line;
        }

        private readonly Dictionary<TF2Stream, MarkerEntry> _markers = new Dictionary<TF2Stream, MarkerEntry>();

        void Start()
        {
            TF2System.GetOrCreateInstance().AddListener(OnFrameChanged, notifyAllStreamsNow: true, tfTopic: tfTopic);
        }

        void OnFrameChanged(TF2Stream stream)
        {
            if (stream == null || _markers.ContainsKey(stream)) return;

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.SetParent(transform, false);
            marker.transform.localScale = Vector3.one * markerRadius;
            Collider col = marker.GetComponent<Collider>();
            if (col != null) Destroy(col);
            marker.name = "tf_" + stream.Name;

            LineRenderer line = null;
            if (stream.Parent != null)
            {
                line = marker.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.startWidth = line.endWidth = markerRadius * 0.5f;
                line.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
                line.startColor = line.endColor = lineColor;
                line.useWorldSpace = false;
            }

            _markers[stream] = new MarkerEntry { marker = marker.transform, line = line };
        }

        void LateUpdate()
        {
            if (Camera.main == null) return;

            Transform cam = Camera.main.transform;
            transform.SetPositionAndRotation(cam.position + cam.rotation * hudOffset, cam.rotation);

            string frameId = pivotFrameId;
            if (string.IsNullOrEmpty(frameId) && ModelManager.instance != null && ModelManager.instance.rootFrame != null)
                frameId = ModelManager.instance.rootFrame.text;
            if (string.IsNullOrEmpty(frameId)) return;

            TF2Stream pivotStream = TF2System.GetOrCreateInstance().GetTransformStream(frameId, tfTopic);
            TF2Frame pivotWorld = pivotStream != null ? pivotStream.GetWorldTF() : TF2Frame.identity;
            Quaternion pivotRotInverse = Quaternion.Inverse(pivotWorld.rotation);

            foreach (var kvp in _markers)
            {
                TF2Stream stream = kvp.Key;
                MarkerEntry entry = kvp.Value;
                TF2Frame world = stream.GetWorldTF();

                entry.marker.localPosition = pivotWorld.InverseTransformPoint(world.translation) * scale;
                entry.marker.localRotation = pivotRotInverse * world.rotation;

                if (entry.line != null && stream.Parent != null && _markers.TryGetValue(stream.Parent, out MarkerEntry parentEntry))
                {
                    entry.line.SetPosition(0, Vector3.zero);
                    entry.line.SetPosition(1, entry.marker.InverseTransformPoint(parentEntry.marker.position));
                }
            }
        }
    }
}
