using UnityEngine;
using UnityEditor;

// Ensure this entire script is only compiled in the Editor
#if UNITY_EDITOR
[CustomEditor(typeof(QRCodeAlignment))]
public class AlignmentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. Draw the default fields (like debugUpperLeft, sceneRoot, etc.)
        DrawDefaultInspector();

        // 2. Get a reference to the target script
        QRCodeAlignment myScript = (QRCodeAlignment)target;

        // 3. Draw the button and call the debug function on the target script
        if (GUILayout.Button("Perform Debug Alignment"))
        {
            myScript.DebugAlignment();
        }
    }
}
#endif