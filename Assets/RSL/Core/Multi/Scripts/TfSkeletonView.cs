// Bare-bones TF skeleton visualizer: draws one line per parent-child edge
// for whichever robot's frames this instance owns, so a robot with no mesh
// prefab (e.g. the M4 excavator here -- no Unity model exists for it in
// this project at all) still shows up as something visible in a screenshot/
// video rather than empty space. Uses real LineRenderer components (not
// Gizmos), specifically because Gizmos only render inside the Editor's own
// Scene/Game view overlay pass -- they never appear in a plain
// Camera.Render() capture, which is what a screenshot/video needs to work
// in -batchmode with no interactive Scene view open at all.
using System.Collections.Generic;
using UnityEngine;
using UvgRos.TF2;

namespace UvgRos.Test
{
    public class TfSkeletonView : MonoBehaviour
    {
        [Tooltip("Which robot's frames to draw -- UvgRosConnection.DefaultRobotId " +
                 "for the unprefixed default robot, or another robot's RobotId " +
                 "(e.g. \"robotB\") to draw only that robot's (prefixed) subtree.")]
        public string RobotId = UvgRosConnection.DefaultRobotId;

        public Color LineColor = Color.yellow;
        public float LineWidth = 0.02f;
        public string TfTopic = "/tf";

        readonly Dictionary<string, LineRenderer> _edges = new Dictionary<string, LineRenderer>();

        bool OwnsFrame(string frameName) =>
            RobotId == UvgRosConnection.DefaultRobotId
                ? !frameName.Contains("/")
                : frameName.StartsWith(RobotId + "/");

        void LateUpdate()
        {
            TF2System system = TF2System.GetOrCreateInstance();
            foreach (string name in system.GetTransformNames(TfTopic))
            {
                if (!OwnsFrame(name)) continue;
                TF2Stream stream = system.GetTransformStream(name, TfTopic);
                if (stream?.Parent == null || stream.GameObject == null || stream.Parent.GameObject == null)
                    continue; // a root frame (or the parent link hasn't been learned yet) -- nothing to draw

                if (!_edges.TryGetValue(name, out LineRenderer line) || line == null)
                    line = _edges[name] = MakeLine(name);

                line.SetPosition(0, stream.Parent.GameObject.transform.position);
                line.SetPosition(1, stream.GameObject.transform.position);
            }
        }

        // This project renders with URP (per its manifest.json) -- the
        // built-in "Sprites/Default" shader used everywhere else for a quick
        // debug line renders as flat magenta there, not a color at all. Try
        // URP's own unlit shader first and only fall back to the built-in
        // one on a project that turns out not to have it.
        static Shader s_LineShader;
        static Shader LineShader => s_LineShader ??=
            Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");

        LineRenderer MakeLine(string edgeName)
        {
            var go = new GameObject("edge_" + edgeName);
            go.transform.SetParent(transform, worldPositionStays: true);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = line.endWidth = LineWidth;
            line.material = new Material(LineShader) { color = LineColor };
            line.startColor = line.endColor = LineColor;
            line.useWorldSpace = true;
            return line;
        }
    }
}
