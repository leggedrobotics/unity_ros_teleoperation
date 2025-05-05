using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(URDFConverter))]
[CanEditMultipleObjects]
public class URDFConverterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        URDFConverter converter = (URDFConverter)target;
        string res = converter.count.ToString();
        string meshData = converter.meshData;
        GUILayout.Label("Removal Iterations: " + res);
        GUILayout.Label(meshData);

        if(GUILayout.Button("Convert"))
        {
            Debug.Log("Converting...");
            converter.Convert();
        }
        if(GUILayout.Button("Count"))
        {
            Debug.Log("Counting...");
            converter.Count();
        }

    }
}


public class URDFConverter : MonoBehaviour
{
    [HideInInspector]
    public int count = 0;
    [HideInInspector]
    public string meshData = "Run Count() to get mesh data";

    public void Convert(){
        // Run convert until there is nothing lest to remove or 100 times
        int removed = 1;
        int i = 0;
        for(i=0; i < 100 && removed > 0; i++){
            removed = Convert("", transform);
            Debug.Log("Removed " + removed + " extra objects");
        }
        count = i-1;
    }

    public void Count(){
        // counts the tris and verts in this object and all children
        int tris = 0;
        int verts = 0;
        int maxTris = 0;
        string path = "";
        foreach(MeshFilter mf in GetComponentsInChildren<MeshFilter>()){
            tris += mf.sharedMesh.triangles.Length / 3;
            verts += mf.sharedMesh.vertices.Length;
            if(mf.sharedMesh.triangles.Length / 3 > maxTris){
                maxTris = mf.sharedMesh.triangles.Length / 3;
                path = mf.transform.name;
            }
        }
        string res = "Verts: " + verts.ToString("N0") + "       Tris: " + tris.ToString("N0") + "       Max Tris: " + maxTris.ToString("N0") + "       Path: " + path;
        Debug.Log(res);
        
        meshData = res;
    }


    int Convert(string path, Transform o)
    {
        count = 0;
        if(o.name == "Collisions" || o.name == "Plugins"){
            DestroyImmediate(o.gameObject);
            return 1;
        }
        else if(!(o.name == "Visuals" || o.name == "unnamed"))
        {
            path += "/" + o.name;
        }

        // if a Visuals placeholder, remove it and move children up
        if(o.name == "Visuals"){
            foreach(Transform t in o.transform){
                t.SetParent(o.parent);
            }
            DestroyImmediate(o.gameObject);
            return 1;
        }

        // if mesh in name return
        if(o.name.Contains("mesh")){
            // remove tf attachment in all children
            foreach(Component c in o.GetComponentsInChildren<Component>()){
                if(c.GetType().ToString().Contains("TFAttachment"))
                {
                    DestroyImmediate(c);
                }
            }
            return 1;
        }


        // if not a prefab and doesnt have a mesh renderer, add tf attachment
        if(o.GetComponent<MeshRenderer>() == null && !PrefabUtility.IsPartOfAnyPrefab(o.gameObject) && o.GetComponent<TFAttachment>() == null ){
            o.gameObject.AddComponent<TFAttachment>();
        }

        foreach(Component c in o.GetComponents<Component>()){
            if(c.GetType().ToString().Contains("TFAttachment"))
            {
                TFAttachment tf = (TFAttachment)c;
                tf.FrameID = path;
            } else if(c.GetType().ToString().Contains("Articulation") || c.GetType().ToString().Contains("Urdf") )
            {
                DestroyImmediate(c);
            }
        }
        foreach(Transform t in o.transform){
            count += Convert(path, t);
        }
        return count;
    }
}
#endif  
