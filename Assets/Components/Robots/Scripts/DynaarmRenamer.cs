using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(DynaarmRenamer))]
public class DynaarmRenamerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DynaarmRenamer myScript = (DynaarmRenamer)target;
        if (GUILayout.Button("Rename"))
        {
            myScript.Rename();
        }
        if (GUILayout.Button("Undo"))
        {
            myScript.Undo();
        }
    }
}

#endif

public class DynaarmRenamer : MonoBehaviour
{
    public string prefix = "RIGHT_";

    public void Rename()
    {
       Rename(gameObject);
    }

    private void Rename(GameObject go)
    {
        go.name = prefix + go.name;
        foreach (Transform child in go.transform)
        {
            if (!child.name.Equals("unnamed"))
                Rename(child.gameObject);
        }
    }

    public void Undo()
    {
        Undo(gameObject);
    }

    public void Undo(GameObject go)
    {
        go.name = go.name.Replace(prefix, "");
        foreach (Transform child in go.transform)
        {
            Undo(child.gameObject);
        }
    }



}
