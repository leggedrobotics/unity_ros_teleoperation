using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Voxblox;
using System;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(VoxbloxMesh))]
public class VoxbloxMeshEditor: Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VoxbloxMesh myScript = (VoxbloxMesh)target;
        if(GUILayout.Button("Clear"))
        {
            myScript.Clear();
        }
    }
}
#endif

public class VoxbloxMesh : MonoBehaviour
{
    public string topic = "/voxblox_node/mesh";
    public Material material;
    private ROSConnection _ros;
    public bool debug = false;
    public float size = 0.5f;
    private Dictionary<Tuple<long, long, long>, GameObject> blocks;

    public GameObject _parent;

    public bool _enabled = true;


    void Start()
    {

        blocks = new Dictionary<Tuple<long, long, long>, GameObject>();

        _ros = ROSConnection.GetOrCreateInstance();
        _ros.Subscribe<MeshMsg>(topic, UpdateMesh);
    }

    void UpdatePose(string frame)
    {
        _parent = GameObject.Find(frame);
        if(_parent == null) return;

        transform.parent = _parent.transform;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    void UpdateMesh(MeshMsg tmpMesh)
    {
        // Debug.Log("Updating mesh: " + tmpMesh.mesh_blocks.Length + " blocks.");

        MeshBlockMsg[] meshBlocks = tmpMesh.mesh_blocks;
        float block_edge_length = tmpMesh.block_edge_length;

        float point_conv_factor = 2f/System.UInt16.MaxValue;

        if(_parent == null || _parent.name != tmpMesh.header.frame_id)
        {
            UpdatePose(tmpMesh.header.frame_id);
        }


        bool hasColor = false;
        string blocksStr = "";

        Mesh mesh;

        for(int j=0; j<meshBlocks.Length; j++)
        {
            MeshBlockMsg block = meshBlocks[j];
            hasColor = block.r.Length == block.x.Length;
            Color color;
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Color> colors = new List<Color>();
            int vertexCount = 0;

            Tuple<long, long, long> index = new Tuple<long, long, long>(block.index[0], block.index[1], block.index[2]);

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
                MeshCollider collider = blockObj.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = true;

            } else
            {
                mesh = blocks[index].GetComponent<MeshFilter>().mesh;
            }

            mesh.Clear();


            for(int i=0; i<block.x.Length; i++)
            {
                float x = ((float)block.x[i] * point_conv_factor + (float)block.index[0]) * block_edge_length;
                float y = ((float)block.y[i] * point_conv_factor + (float)block.index[1]) * block_edge_length;
                float z = ((float)block.z[i] * point_conv_factor + (float)block.index[2]) * block_edge_length;

                // switch between ros and unity coordinates
                Vector3 pos = new Vector3<FLU>(x, y, z).toUnity;

                vertices.Add(pos);

                if(debug)
                {
                    Debug.DrawRay(pos, Vector3.up*size, Color.red,20f);
                }

                if(hasColor)
                {
                    color = new Color(block.r[i]/255f, block.g[i]/255f, block.b[i]/255f);
                } else
                {
                    color = new Color(.5f, .5f, .5f);
                }
                colors.Add(color);

                triangles.Add(vertexCount);
                vertexCount++;
            }
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.colors = colors.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

    }

    public void ToggleEnabled()
    {
        _enabled = !_enabled;
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(_enabled);
        }
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
