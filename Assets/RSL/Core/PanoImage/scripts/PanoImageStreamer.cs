using UnityEngine;

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
    public void Flip()
    {
        _Img.localScale = new Vector3(-_Img.localScale.x, _Img.localScale.y, _Img.localScale.z);
    }

    override protected void Resize()
    {
        // Not needed for pano images, as they are not rendered on a plane but on a sphere, so they will always be displayed correctly regardless of the aspect ratio of the image
    }
}
