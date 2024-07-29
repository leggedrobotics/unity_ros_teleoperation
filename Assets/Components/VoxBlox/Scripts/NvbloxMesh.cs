using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nvblox;
using System;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;



#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(NvbloxMesh))]
public class NvbloxMeshEditor: Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NvbloxMesh myScript = (NvbloxMesh)target;
        if(GUILayout.Button("Clear"))
        {
            myScript.Clear();
        }
    }
}
#endif


namespace RosMessageTypes.Nvblox
{
    public static class Index3DMsgExtensions
    {
        public static Tuple<int, int, int> ToTuple(this Index3DMsg index)
        {
            return new Tuple<int, int, int>(index.x, index.y, index.z);
        }
    }
}

public class NvbloxMesh : MonoBehaviour
{
    public string topic = "/nvblox_node/mesh";
    public Material material;

    [Range(0, 1)]
    public float opacity = 0.5f;
    private ROSConnection _ros;
    private Dictionary<Tuple<int, int, int>, GameObject> blocks;

    public GameObject _parent;
    public bool collision = false;

    public bool _enabled = false;


    void Start()
    {

        blocks = new Dictionary<Tuple<int, int, int>, GameObject>();
        material.SetFloat("_Opacity", opacity);

        _ros = ROSConnection.GetOrCreateInstance();
        if(_enabled)
            _ros.Subscribe<MeshMsg>(topic, UpdateMesh);
    }

    void UpdatePose(string frame)
    {
        _parent = GameObject.Find(frame);
        if(_parent == null) return;

        transform.parent = _parent.transform;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    void UpdateMesh(MeshMsg tmpMesh)
    {

        MeshBlockMsg[] meshBlocks = tmpMesh.blocks;
        float block_size = tmpMesh.block_size;
        bool clear = tmpMesh.clear;

        if(clear)
        {
            Debug.Log("Clearing mesh");
            Clear();
        }

        if(_parent == null || _parent.name != tmpMesh.header.frame_id)
        {
            UpdatePose(tmpMesh.header.frame_id);
        }


        string blocksStr = "";

        Mesh mesh;
        int numVertices = 0, count = 0;

        for(int j=0; j<meshBlocks.Length; j++)
        {
            MeshBlockMsg block = meshBlocks[j];
            if(block.vertices.Length == 0) continue;

            Color color;
            numVertices += block.vertices.Length;
            count++;
            Vector3[] vertices = new Vector3[block.vertices.Length];
            Vector3[] normals = new Vector3[block.normals.Length];
            Color[] colors = new Color[block.colors.Length];

            Tuple<int, int, int> index = tmpMesh.block_indices[j].ToTuple();

            if(!blocks.ContainsKey(index))
            {
                GameObject blockObj = new GameObject("Block " + index.Item1 + " " + index.Item2 + " " + index.Item3);
                blockObj.transform.parent = transform;
                blockObj.transform.localPosition = Vector3.zero;
                blockObj.transform.localRotation = Quaternion.identity;
                blockObj.transform.localScale = Vector3.one;
                blocks.Add(index, blockObj);

                mesh = new Mesh();
                blockObj.AddComponent<MeshFilter>().mesh = mesh;
                blockObj.AddComponent<MeshRenderer>().material = material;
                if(collision)
                {
                    MeshCollider collider = blockObj.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh;
                    collider.convex = true;
                }

            } else
            {
                mesh = blocks[index].GetComponent<MeshFilter>().mesh;
            }

            mesh.Clear();


            for(int i=0; i<block.vertices.Length; i++)
            {
                float x = block.vertices[i].x;
                float y = block.vertices[i].y;
                float z = block.vertices[i].z;

                // switch between ros and unity coordinates
                vertices[i] = new Vector3<FLU>(x, y, z).toUnity;

                if(block.normals.Length > 0)
                {
                    x = block.normals[i].x;
                    y = block.normals[i].y;
                    z = block.normals[i].z;
                    normals[i] = new Vector3<FLU>(x, y, z).toUnity;
                } 

                if(block.colors.Length > 0)
                {
                    color = new Color(block.colors[i].r, block.colors[i].g, block.colors[i].b);
                } else
                {
                    color = Color.white;
                }
                colors[i] = color;
            }



            // if(block.triangles.Length > 0)
            // {
            //     triangles.AddRange(block.triangles);
            // }

                
            mesh.vertices = vertices;
            mesh.triangles = block.triangles;
            if(block.normals.Length > 0)
            {
                mesh.normals = normals;
            } else {
                mesh.RecalculateNormals();
            }
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }

        Debug.LogWarning("Average vertices: " + (float)numVertices / count);

    }

    public void ChangeOpactity(float opacity)
    {
        material.SetFloat("_Opacity", opacity);
    }

    private void OnValidate() {
        if(material != null)
        {
            material.SetFloat("_Opacity", opacity);
        }
    }




    public void ToggleEnabled()
    {
        _enabled = !_enabled;
        if(_enabled)
        {
            _ros.Subscribe<MeshMsg>(topic, UpdateMesh);
        } else
        {
            _ros.Unsubscribe(topic);
        }

        // foreach (Transform child in transform)
        // {
        //     child.gameObject.SetActive(_enabled);
        // }
    }

    public void Clear()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        blocks.Clear();
    }

}
