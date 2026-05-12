using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using LookingGlass;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Run in Editor
[ExecuteInEditMode]
public class DynamicAlcove : MonoBehaviour
{
    [SerializeField] float borderThickness = 0.01f;
    [SerializeField] float backWallDistance = 1.0f;
    [SerializeField] Material boxBGMaterial;
    [SerializeField] int meshSubdivisionsX = 2;
    [SerializeField] int meshSubdivisionsY = 2;
    [SerializeField] bool showInHierarchy = false;

    private List<GameObject> walls = new List<GameObject>();
    private bool isInitialized = false;
    private bool recreateQueued = false;

    private async void Start()
    {
#if UNITY_EDITOR || !UNITY_IOS
        await LKGDisplaySystem.WaitForCalibrations();  // Await calibration loading
#endif
        RecreateBox();
        isInitialized = true;
        if (HologramCamera.Instance)
            HologramCamera.Instance.onCalibrationChanged += OnCalibrationChanged;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        // Queue recreation for next editor update to avoid SendMessage errors
        if (!recreateQueued)
        {
            recreateQueued = true;
            EditorApplication.delayCall += () =>
            {
                if (this != null) // Check if object still exists
                {
                    RecreateBox();
                    recreateQueued = false;
                }
            };
        }
#endif
    }

    private void OnCalibrationChanged()
    {
        RecreateBox();
    }

    private void OnDestroy()
    {
        if(HologramCamera.Instance)
            HologramCamera.Instance.onCalibrationChanged -= OnCalibrationChanged;
        DestroyBox();
    }

    private void RecreateBox()
    {
        var hologramCamera = HologramCamera.Instance;
        if (hologramCamera == null) return;

        var cameraProperties = hologramCamera.CameraProperties;
        var calibration = hologramCamera.Calibration;
        if (cameraProperties == null) return;

        DestroyBox();
        CreateBox(cameraProperties.Size, calibration.ScreenAspect);
    }

    public void CreateBox(float cameraSize, float screenAspect)
    {
#if UNITY_EDITOR
        if (!this.isActiveAndEnabled)
            return;
#endif
        Transform parent = this.transform;

        // Create walls and borders
        GameObject leftBorder = CreateWall("LeftBorder", boxBGMaterial, parent);
        GameObject rightBorder = CreateWall("RightBorder", boxBGMaterial, parent);
        GameObject topBorder = CreateWall("TopBorder", boxBGMaterial, parent);
        GameObject bottomBorder = CreateWall("BottomBorder", boxBGMaterial, parent);
        GameObject backWall = CreateWall("BackWall", boxBGMaterial, parent);
        GameObject leftWall = CreateWall("LeftWall", boxBGMaterial, parent);
        GameObject rightWall = CreateWall("RightWall", boxBGMaterial, parent);
        GameObject floor = CreateWall("Floor", boxBGMaterial, parent);
        GameObject ceiling = CreateWall("Ceiling", boxBGMaterial, parent);

        float width = cameraSize * screenAspect;
        float height = cameraSize;

        // Left Border
        Mesh lbMesh = CreateSubdividedQuad(
            new Vector3(-width, -height + borderThickness, 0),
            new Vector3(-width + borderThickness, -height + borderThickness, 0),
            new Vector3(-width, height - borderThickness, 0),
            new Vector3(-width + borderThickness, height - borderThickness, 0),
            meshSubdivisionsX, meshSubdivisionsY);
        leftBorder.GetComponent<MeshFilter>().mesh = lbMesh;

        // Right Border
        Mesh rbMesh = CreateSubdividedQuad(
            new Vector3(width - borderThickness, -height + borderThickness, 0),
            new Vector3(width, -height + borderThickness, 0),
            new Vector3(width - borderThickness, height - borderThickness, 0),
            new Vector3(width, height - borderThickness, 0),
            meshSubdivisionsX, meshSubdivisionsY);
        rightBorder.GetComponent<MeshFilter>().mesh = rbMesh;

        // Top Border
        Mesh tbMesh = CreateSubdividedQuad(
            new Vector3(-width, height - borderThickness, 0),
            new Vector3(width, height - borderThickness, 0),
            new Vector3(-width, height, 0),
            new Vector3(width, height, 0),
            meshSubdivisionsX, meshSubdivisionsY);
        topBorder.GetComponent<MeshFilter>().mesh = tbMesh;

        // Bottom Border
        Mesh bbMesh = CreateSubdividedQuad(
            new Vector3(-width, -height, 0),
            new Vector3(width, -height, 0),
            new Vector3(-width, -height + borderThickness, 0),
            new Vector3(width, -height + borderThickness, 0),
            meshSubdivisionsX, meshSubdivisionsY);
        bottomBorder.GetComponent<MeshFilter>().mesh = bbMesh;

        // Back Wall
        Mesh bwMesh = CreateSubdividedQuad(
            new Vector3(-width + borderThickness, -height + borderThickness, backWallDistance),
            new Vector3(width - borderThickness, -height + borderThickness, backWallDistance),
            new Vector3(-width + borderThickness, height - borderThickness, backWallDistance),
            new Vector3(width - borderThickness, height - borderThickness, backWallDistance),
            meshSubdivisionsX, meshSubdivisionsY);
        backWall.GetComponent<MeshFilter>().mesh = bwMesh;

        // Left Wall
        Mesh lwMesh = CreateSubdividedQuad(
            new Vector3(-width + borderThickness, -height + borderThickness, 0),
            new Vector3(-width + borderThickness, -height + borderThickness, backWallDistance),
            new Vector3(-width + borderThickness, height - borderThickness, 0),
            new Vector3(-width + borderThickness, height - borderThickness, backWallDistance),
            meshSubdivisionsX, meshSubdivisionsY);
        leftWall.GetComponent<MeshFilter>().mesh = lwMesh;

        // Right Wall
        Mesh rwMesh = CreateSubdividedQuad(
            new Vector3(width - borderThickness, -height + borderThickness, backWallDistance),
            new Vector3(width - borderThickness, -height + borderThickness, 0),
            new Vector3(width - borderThickness, height - borderThickness, backWallDistance),
            new Vector3(width - borderThickness, height - borderThickness, 0),
            meshSubdivisionsX, meshSubdivisionsY);
        rightWall.GetComponent<MeshFilter>().mesh = rwMesh;

        // Floor
        Mesh floorMesh = CreateSubdividedQuad(
            new Vector3(-width + borderThickness, -height + borderThickness, 0),
            new Vector3(width - borderThickness, -height + borderThickness, 0),
            new Vector3(-width + borderThickness, -height + borderThickness, backWallDistance),
            new Vector3(width - borderThickness, -height + borderThickness, backWallDistance),
            meshSubdivisionsX, meshSubdivisionsY);
        floor.GetComponent<MeshFilter>().mesh = floorMesh;

        // Ceiling
        Mesh ceilingMesh = CreateSubdividedQuad(
            new Vector3(-width + borderThickness, height - borderThickness, backWallDistance),
            new Vector3(width - borderThickness, height - borderThickness, backWallDistance),
            new Vector3(-width + borderThickness, height - borderThickness, 0),
            new Vector3(width - borderThickness, height - borderThickness, 0),
            meshSubdivisionsX, meshSubdivisionsY);
        ceiling.GetComponent<MeshFilter>().mesh = ceilingMesh;
    }

    // Generate and configure the basic primitive for the game objects
    private GameObject CreateWall(string name, Material material, Transform parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.GetComponent<MeshRenderer>().material = material;
        wall.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.TwoSided;

        // Prevent saving to scene and optionally hide in hierarchy
        wall.hideFlags = showInHierarchy ? 
            HideFlags.DontSave : 
            HideFlags.HideInHierarchy | HideFlags.DontSave;

        // Copy the layer from the parent
        wall.layer = parent.gameObject.layer;
            
        walls.Add(wall);
        return wall;
    }

    // Create the actual mesh that will be used for the game objects
    // Subdividing the quad makes shadow casting less jagged with spot lights, though some artifacts remain
    private Mesh CreateSubdividedQuad(Vector3 bottomLeft, Vector3 bottomRight, Vector3 topLeft, Vector3 topRight, int subdivisionsX, int subdivisionsY)
    {
        Mesh mesh = new Mesh();

        int vertCountX = subdivisionsX + 1;
        int vertCountY = subdivisionsY + 1;

        Vector3[] vertices = new Vector3[vertCountX * vertCountY];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[subdivisionsX * subdivisionsY * 6];

        // Generate vertices and uvs
        for (int y = 0; y < vertCountY; y++)
        {
            float ty = (float)y / subdivisionsY;
            Vector3 left = Vector3.Lerp(bottomLeft, topLeft, ty);
            Vector3 right = Vector3.Lerp(bottomRight, topRight, ty);

            for (int x = 0; x < vertCountX; x++)
            {
                float tx = (float)x / subdivisionsX;
                vertices[y * vertCountX + x] = Vector3.Lerp(left, right, tx);
                uvs[y * vertCountX + x] = new Vector2(tx, ty);
            }
        }

        // Generate triangles
        int index = 0;
        for (int y = 0; y < subdivisionsY; y++)
        {
            for (int x = 0; x < subdivisionsX; x++)
            {
                int i0 = y * vertCountX + x;
                int i1 = i0 + 1;
                int i2 = i0 + vertCountX;
                int i3 = i2 + 1;

                // First triangle (i0, i2, i1)
                triangles[index++] = i0;
                triangles[index++] = i2;
                triangles[index++] = i1;

                // Second triangle (i1, i2, i3)
                triangles[index++] = i1;
                triangles[index++] = i2;
                triangles[index++] = i3;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public void DestroyBox()
    {
        foreach (var wall in walls)
        {
            if (wall != null)
            {
                DestroyImmediate(wall);
            }
        }
        walls.Clear();
    }
}
