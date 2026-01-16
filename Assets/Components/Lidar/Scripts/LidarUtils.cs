using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using System.Security.Cryptography.X509Certificates;
using Unity.Mathematics;
using GaussianSplatting.Runtime;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using System.Diagnostics;


public class LidarUtils
{
    public static Mesh MakePolygon(int sides)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[sides];
        int[] triangles = new int[(sides - 2) * 3];
        for (int i = 0; i < sides; i++)
        {
            float angle = 2 * Mathf.PI * i / sides;
            vertices[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
        }
        for (int i = 0; i < sides - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);
        return mesh;
    }

    public static Mesh MakePlane(int width, int height)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[width * height];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                vertices[z * width + x] = new Vector3(
                    (float)x / (width - 1) - 0.5f,
                    0,
                    (float)z / (height - 1) - 0.5f
                );
            }
        }
        int triIndex = 0;
        for (int z = 0; z < height - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                triangles[triIndex++] = z * width + x;
                triangles[triIndex++] = (z + 1) * width + x;
                triangles[triIndex++] = z * width + (x + 1);

                triangles[triIndex++] = (z + 1) * width + x;
                triangles[triIndex++] = (z + 1) * width + (x + 1);
                triangles[triIndex++] = z * width + (x + 1);
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);
        return mesh;
    }

    public static Mesh MakeCube()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f)
        };
        mesh.triangles = new int[]
        {
            // Front face
            0, 2, 1,
            0, 3, 2,
            // Top face
            3, 7, 2,
            2, 7, 6,
            // Right face
            1, 2, 6,
            1, 6, 5,
            // Left face
            0, 4, 7,
            0, 7, 3,
            // Back face
            4, 5, 6,
            4, 6, 7,
            // Bottom face
            0, 1, 5,
            0, 5, 4
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
    public static Mesh MakeCylinder(int sides)
    {
        Mesh mesh = new Mesh();

        // Vertices: bottom circle, top circle
        List<Vector3> vertices = new List<Vector3>();
        float radius = 0.5f;
        float height = 1.0f;

        // Bottom and top center points
        vertices.Add(new Vector3(0, -height / 2, 0)); // bottom center
        vertices.Add(new Vector3(0, height / 2, 0));  // top center

        // Circle points
        for (int i = 0; i < sides; i++)
        {
            float angle = 2 * Mathf.PI * i / sides;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices.Add(new Vector3(x, -height / 2, z)); // bottom ring
            vertices.Add(new Vector3(x, height / 2, z));  // top ring
        }

        List<int> triangles = new List<int>();
        // Bottom face (fan) - correct winding order
        for (int i = 0; i < sides; i++)
        {
            int curr = 2 + i * 2;
            int next = 2 + ((i + 1) % sides) * 2;
            triangles.Add(0);      // bottom center
            triangles.Add(curr);   // current bottom
            triangles.Add(next);   // next bottom
        }

        // Top face (fan) - correct winding order
        for (int i = 0; i < sides; i++)
        {
            int curr = 3 + i * 2;
            int next = 3 + ((i + 1) % sides) * 2;
            triangles.Add(1);      // top center
            triangles.Add(next);   // next top
            triangles.Add(curr);   // current top
        }

        // Side faces (quads split into triangles)
        for (int i = 0; i < sides; i++)
        {
            int b0 = 2 + i * 2;
            int t0 = b0 + 1;
            int b1 = 2 + ((i + 1) % sides) * 2;
            int t1 = b1 + 1;

            // First triangle
            triangles.Add(b0);
            triangles.Add(t0);
            triangles.Add(t1);

            // Second triangle
            triangles.Add(b0);
            triangles.Add(t1);
            triangles.Add(b1);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh MakeSphere(int sides)
    {
        Mesh mesh = new Mesh();
        int latitudeBands = sides;
        int longitudeBands = sides;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int latNumber = 0; latNumber <= latitudeBands; latNumber++)
        {
            float theta = latNumber * Mathf.PI / latitudeBands;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int longNumber = 0; longNumber <= longitudeBands; longNumber++)
            {
                float phi = longNumber * 2 * Mathf.PI / longitudeBands;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                Vector3 vertex = new Vector3(cosPhi * sinTheta, cosTheta, sinPhi * sinTheta);
                vertices.Add(vertex);
            }
        }

        for (int latNumber = 0; latNumber < latitudeBands; latNumber++)
        {
            for (int longNumber = 0; longNumber < longitudeBands; longNumber++)
            {
                int first = (latNumber * (longitudeBands + 1)) + longNumber;
                int second = first + longitudeBands + 1;

                triangles.Add(first);
                triangles.Add(second);
                triangles.Add(first + 1);

                triangles.Add(second);
                triangles.Add(second + 1);
                triangles.Add(first + 1);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }


    public static Mesh MakeCone(int sides)
    {
        float radius = 1f;
        Mesh mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Base center
        vertices.Add(new Vector3(0, 0, 0));

        // Base perimeter
        for (int i = 0; i < sides; i++)
        {
            float angle = 2 * Mathf.PI * i / sides;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices.Add(new Vector3(x, 0, z));
        }

        // Apex
        vertices.Add(new Vector3(0, 1, 0));

        // Base triangles (flipped winding order)
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            triangles.Add(0);          // base center
            triangles.Add(i + 1);      // current base perimeter
            triangles.Add(next + 1);   // next base perimeter
        }

        // Side triangles (flipped winding order)
        int apexIndex = vertices.Count - 1;
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            triangles.Add(apexIndex);   // apex
            triangles.Add(next + 1);    // next base perimeter
            triangles.Add(i + 1);       // current base perimeter
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Mesh MakeArrow(float arrowRadius, int sides)
    {
        Mesh mesh = new Mesh();

        CombineInstance[] combine = new CombineInstance[2];

        // Create the arrow shaft
        Mesh shaft = MakeCylinder(sides);
        shaft.name = "ArrowShaft";

        // Rotate so cylinder faces forward (z+), scale shaft so its length is 1 and diameter is arrowRadius
        Matrix4x4 shaftMat = Matrix4x4.TRS(
            new Vector3(0, 0, 0.25f), // move forward so base is at z=0
            Quaternion.Euler(90, 0, 0), // rotate from y+ to z+
            new Vector3(arrowRadius / 2, 0.5f, arrowRadius / 2)
        );
        combine[0] = new CombineInstance { mesh = shaft, transform = shaftMat };

        // Create the arrowhead
        Mesh arrowhead = MakeCone(sides);
        arrowhead.name = "ArrowHead";

        // Rotate so cone faces forward (z+), scale arrowhead so its height is 1 and base diameter is 2 * arrowRadius
        Matrix4x4 arrowheadMat = Matrix4x4.TRS(
            new Vector3(0, 0, 0.5f), // move forward so base is at z=0.5
            Quaternion.Euler(90, 0, 0), // rotate from y+ to z+
            new Vector3(arrowRadius, 0.5f, arrowRadius)
        );
        combine[1] = new CombineInstance { mesh = arrowhead, transform = arrowheadMat };

        mesh.CombineMeshes(combine);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static float PackRGBA(Color color)
    {
        // Pack a Color (r,g,b,a in 0-1 range) into a float
        int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
        int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
        int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
        int a = 255; // Fixed alpha
        int packed = (r << 16) | (g << 8) | (b);
        return System.BitConverter.Int32BitsToSingle(packed);
    }

    public static float PackRGBA(ColorRGBAMsg color)
    {
        // Pack a ColorRGBAMsg (r,g,b,a in 0-1 range) into a float
        int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
        int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
        int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
        int a = 255; // Fixed alpha
        int packed = (r << 16) | (g << 8) | (b);
        return System.BitConverter.Int32BitsToSingle(packed);
    }

    public static Color UnpackRGBA(float packedColor)
    {
        // Unpack a float back to ColorRGBA
        int packed = System.BitConverter.SingleToInt32Bits(packedColor);
        int r = (packed >> 16) & 0xFF;
        int g = (packed >> 8) & 0xFF;
        int b = packed & 0xFF;
        return new Color(r / 255f, g / 255f, b / 255f, 1.0f); // Fixed alpha
    }

    public static byte[] ExtractData(PointCloud2Msg data, int maxPts, VizType vizType, out int numPts)
    {

        /**
        For different data type the order is 
        Lidar: x, y, z, intensity
        RGBD: x, y, z, rgb0
        RGBD Mesh: ??
        Splat: x, y, z, rgba, scalex, scaley, scalez, rot0, rot1, rot2, rot3, nx, ny, nz, fc_dc_0, fc_dc_1, fc_dc_2, opacity
        */

        // Just in case...
        if (maxPts < 1) maxPts = 1;
        int decmiator = 1;

        int data_size = vizType.GetSize();

        numPts = (int)(data.data.Length / data.point_step);

        if (numPts > maxPts)
        {
            decmiator = Mathf.CeilToInt((float)numPts / maxPts);
            numPts = numPts / decmiator;
        }

        byte[] outData = new byte[numPts * data_size];

        // For each point...
        for (int i = 0; i < numPts; i++)
        {
            // Grab the point at the decimated index
            int inIdx = (int)(i * data.point_step * (decmiator));
            int outIdx = i * data_size;

            // For each field in the point...
            for (int j = 0; j < vizType.GetFieldCount(); j++)
            {
                if (j >= data.fields.Length)
                {
                    // Debug.LogWarning($"LidarUtils: Field index {j} out of bounds for fields count {data.fields.Length}. Filling with 0.");

                    // If we are missing the last field (ie xyz only) then fill with 0
                    for (int k = 0; k < 4; k++)
                    {
                        outData[outIdx + j * 4 + k] = 0;
                    }
                    continue;

                }
                // Copy the 4 bytes in the float
                for (int k = 0; k < 4; k++)
                {
                    outData[outIdx + j * 4 + k] = data.data[inIdx + (int)data.fields[j].offset + k];
                }
            }
        }
        return outData;
    }

    public struct SplatData
    {
        public byte[] position;
        public byte[] color;
        public byte[] other;
        public byte[] normal;
        public int numPts;
    }

    static uint EncodeQuatToNorm10(float4 v) // 32 bits: 10.10.10.2
    {
        return (uint) (v.x * 1023.5f) | ((uint) (v.y * 1023.5f) << 10) | ((uint) (v.z * 1023.5f) << 20) | ((uint) (v.w * 3.5f) << 30);
    }

    static int SplatIndexToTextureIndex(uint idx)
    {
        uint2 xy = GaussianUtils.DecodeMorton2D_16x16(idx);
        uint width = GaussianSplatAsset.kTextureWidth / 16;
        idx >>= 8;
        uint x = (idx % width) * 16 + xy.x;
        uint y = (idx / width) * 16 + xy.y;
        return (int)(y * GaussianSplatAsset.kTextureWidth + x);
    }

    static int NextMultipleOf(int size, int multipleOf)
    {
        return (size + multipleOf - 1) / multipleOf * multipleOf;
    }

    public static SplatData ExtractSplat(PointCloud2Msg data, int maxPts, VizType vizType, out int numPts)
    {

        /**
        For different data type the order is 
        Splat: x, y, z, rgb, scalex, scaley, scalez, rot0, rot1, rot2, rot3, nx, ny, nz, fc_dc_0, fc_dc_1, fc_dc_2, opacity
        */

        // Just in case...
        if (maxPts < 1) maxPts = 1;
        int decmiator = 1;

        int data_size = vizType.GetSize();

        numPts = (int)(data.data.Length / data.point_step);

        if (numPts > maxPts)
        {
            decmiator = Mathf.CeilToInt((float)numPts / maxPts);
            numPts = numPts / decmiator;
        }

        SplatData outData = new SplatData();

        outData.numPts = numPts;
        outData.position = new byte[numPts * 12]; // 3 floats for position
        var (width, height) = GaussianSplatAsset.CalcTextureSize(numPts);
        float4[] color = new float4[width * height];
        int mipmapSize = (int) GraphicsFormatUtility.ComputeMipmapSize(width, height, GraphicsFormat.R32G32B32A32_SFloat);
        outData.color = new byte[mipmapSize]; // 4 float for color
        outData.other = new byte[numPts * 16]; // 1 uint for rotation, 3 floats for scale
        outData.normal = new byte[numPts * 12]; // 3 floats for normal

        float3 min_scale = default;
        float3 max_scale = default;
        float4 min_dc = default;
        float4 max_dc = default;
        // For each point...
        for (int i = 0; i < numPts; i++)
        {
            // Grab the point at the decimated index
            int inIdx = (int)(i * data.point_step * (decmiator));
            int vector3Idx = i * 12;
            int vector4Idx = i * 16;

            // Copy the first 12 bytes (three floats) from the incoming point into position data
            int bytesToCopy = Mathf.Min(12, data.data.Length - inIdx);
            if (bytesToCopy > 0)
            {
                System.Buffer.BlockCopy(data.data, inIdx, outData.position, vector3Idx, bytesToCopy);
            }
            // Copy the next 16 bytes (four floats) from the incoming point into color data
            int textureIndex = SplatIndexToTextureIndex((uint) i);
            float3 dc0 = new float3(
                System.BitConverter.ToSingle(data.data, inIdx + 52),
                System.BitConverter.ToSingle(data.data, inIdx + 56),
                System.BitConverter.ToSingle(data.data, inIdx + 60)
            );
            float opacity = System.BitConverter.ToSingle(data.data, inIdx + 64);
            dc0 = GaussianUtils.SH0ToColor(dc0);
            opacity = GaussianUtils.Sigmoid(opacity);
            color[textureIndex] = new float4(dc0.x, dc0.y, dc0.z, opacity);
            // bytesToCopy = Mathf.Min(12, data.data.Length - (inIdx + 56));
            // if (bytesToCopy > 0)
            // {
            //     System.Buffer.BlockCopy(data.data, inIdx + 56, outData.color, SplatIndexToTextureIndex(i), bytesToCopy);
            // }
            // Copy the next 16 bytes (four floats) from the incoming point into rotation data
                        
            float w = System.BitConverter.ToSingle(data.data, inIdx + 24);
            float x = System.BitConverter.ToSingle(data.data, inIdx + 28);
            float y = System.BitConverter.ToSingle(data.data, inIdx + 32);
            float z = System.BitConverter.ToSingle(data.data, inIdx + 36);
            float4 rot = new float4(
                w,
                x, 
                y, 
                z
            );
            var qq = GaussianUtils.NormalizeSwizzleRotation(rot);
            qq = GaussianUtils.PackSmallest3Rotation(qq);
            uint encoded = EncodeQuatToNorm10(qq);
            byte[] otherData = System.BitConverter.GetBytes(encoded); // little-endian Array.Copy(bytes, 0, outData.other, vector4Idx, 4);
            System.Buffer.BlockCopy(otherData, 0, outData.other, vector4Idx, 4);
            // System.Buffer.BlockCopy(data.data, inIdx + 28, outData.rotation, vector4Idx, bytesToCopy);
            // Copy the next 12 bytes (three floats) from the incoming point into scale data
            bytesToCopy = Mathf.Min(12, data.data.Length - (inIdx + 12));
            if (bytesToCopy > 0)
            {
                System.Buffer.BlockCopy(data.data, inIdx + 12, outData.other, vector4Idx + 4, bytesToCopy);
            }
            float3 scale = new float3(
                System.BitConverter.ToSingle(data.data, inIdx + 12),
                System.BitConverter.ToSingle(data.data, inIdx + 16),
                System.BitConverter.ToSingle(data.data, inIdx + 20)
                );
            float3 linearScale = GaussianUtils.LinearScale(scale);
            byte[] sxBytes = System.BitConverter.GetBytes(linearScale.x);
            byte[] syBytes = System.BitConverter.GetBytes(linearScale.y);
            byte[] szBytes = System.BitConverter.GetBytes(linearScale.z);
            System.Buffer.BlockCopy(sxBytes, 0, outData.other, vector4Idx + 4, 4);
            System.Buffer.BlockCopy(syBytes, 0, outData.other, vector4Idx + 8, 4);
            System.Buffer.BlockCopy(szBytes, 0, outData.other, vector4Idx + 12, 4);
            // Copy the next 12 bytes (three floats) from the incoming point into normal data
            bytesToCopy = Mathf.Min(12, data.data.Length - (inIdx + 40));
            if (bytesToCopy > 0)
            {
                System.Buffer.BlockCopy(data.data, inIdx + 40, outData.normal, vector3Idx, bytesToCopy);
            }
        }

        for (int y = 0; y < height; y++){
            int srcIdx = y * width;
            int dstIdx =  y * width * 16;
            for (int x = 0; x < width; x++){
                float4 pix = color[srcIdx + x];

                System.Buffer.BlockCopy(System.BitConverter.GetBytes(pix.x), 0, outData.color, dstIdx + 0, 4);
                System.Buffer.BlockCopy(System.BitConverter.GetBytes(pix.y), 0, outData.color, dstIdx + 4, 4);
                System.Buffer.BlockCopy(System.BitConverter.GetBytes(pix.z), 0, outData.color, dstIdx + 8, 4);
                System.Buffer.BlockCopy(System.BitConverter.GetBytes(pix.w), 0, outData.color, dstIdx + 12, 4);                dstIdx += 16;
            }
        }

        // UnityEngine.Debug.Log($"Splat dc range: min({min_dc.x}, {min_dc.y}, {min_dc.z}, {min_dc.w}) max({max_dc.x}, {max_dc.y}, {max_dc.z}, {max_dc.w})");
        // UnityEngine.Debug.Log($"Splat scale range: min({min_scale.x}, {min_scale.y}, {min_scale.z}) max({max_scale.x}, {max_scale.y}, {max_scale.z})");
        return outData;
    }
    
    public static Texture2D GradientToTexture(Gradient gradient, int resolution = 256) {
        Texture2D tex = new Texture2D(resolution, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int i = 0; i < resolution; i++) {
            float t = i / (float)(resolution - 1);
            Color col = gradient.Evaluate(t);
            tex.SetPixel(i, 0, col);
        }

        tex.Apply();
        return tex;
    }
}
