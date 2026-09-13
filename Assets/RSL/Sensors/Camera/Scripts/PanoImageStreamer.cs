using UnityEngine;

namespace RSL.Sensors.Camera
{
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(PanoImageStreamer))]
    public class PanoImageStreamerEditor : ImageViewEditor
    { 
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            PanoImageStreamer streamer = (PanoImageStreamer)target;
            if (GUILayout.Button("Flip"))
            {
                streamer.Flip();
            }
        }
    }
    #endif

    public class PanoImageStreamer : ImageView
    {
        // Flip() is no longer overridden here -- ImageView's own Flip() does
        // the exact same _Img.localScale.x negation now, and since Flip() is
        // virtual, inheriting it dispatches correctly (this used to have to
        // be its own non-virtual copy specifically to avoid a base-typed
        // call resolving to ImageView's old no-op -- see ImageView.Flip's
        // own comment).

        override protected void Resize()
        {
            // Not needed for pano images, as they are not rendered on a plane but on a sphere, so they will always be displayed correctly regardless of the aspect ratio of the image
        }
    }
}
