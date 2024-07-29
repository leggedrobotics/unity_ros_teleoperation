using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Sensor;

public class LidarUtils 
{
    public static Mesh MakePolygon(int sides)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[sides];
        int[] triangles = new int[(sides - 2) * 3];
        for(int i = 0; i < sides; i++)
        {
            float angle = 2 * Mathf.PI * i / sides;
            vertices[i] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
        }
        for(int i = 0; i < sides - 2; i++)
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

    public static byte[] ExtractXYZI(PointCloud2Msg data, int maxPts, VizType vizType, out int numPts)
    {

        /**
        For different data type the order is 
        Lidar: x, y, z, intensity
        RGBD: x, y, z, rgb0
        RGBD Mesh: ??
        */

        // Just in case...
        if(maxPts < 1) maxPts = 1;
        int decmiator = 1;

        int data_size = (int)vizType;
        if(data_size % 2 == 1) // In case we are rgbdmesh
        {
            data_size -= 1;
        }
        
        // Assumes x, y, z, intensity are the first 4 fields
        numPts = (int)(data.data.Length / data.point_step);

        if(numPts > maxPts)
        {
            decmiator = Mathf.CeilToInt((float)numPts / maxPts);
            numPts = numPts / decmiator;
        }
        byte[] outData = new byte[numPts * (int) vizType];

        for(int i = 0; i < numPts; i++)
        {

            int inIdx = (int)(i * data.point_step * (decmiator));
            int outIdx = i * data_size;
            int outOffset = 0;
            for(int j = 0; j < 4; j++)
            {
                // Special case for rgb(a) unpacking in non Lidar cases
                if(j == 3 && vizType != VizType.Lidar)
                {
                    // convert the reinterpret_cast<float&> to int, then extract the rgb bytes
                    int intensity = System.BitConverter.ToInt32(data.data, inIdx + (int)data.fields[3].offset);
                    ushort r = (ushort)(intensity >> 16 & 0xff);
                    ushort g = (ushort)(intensity >> 8 & 0xff);
                    ushort b = (ushort)(intensity >> 0 & 0xff);
                    ushort a = (ushort)(intensity >> 24 & 0xff);

                    // convert to floats
                    float rf = r / 255.0f;
                    float gf = g / 255.0f;
                    float bf = b / 255.0f;
                    float af = a / 255.0f;


                    // write to outData
                    System.BitConverter.GetBytes(rf).CopyTo(outData, outIdx + outOffset);
                    System.BitConverter.GetBytes(gf).CopyTo(outData, outIdx + outOffset + 4);
                    System.BitConverter.GetBytes(bf).CopyTo(outData, outIdx + outOffset + 8);
                   
                    outOffset+=4*3;

                }
                else
                {
                    // copy over the 4 bytes of the float
                    for(int k = 0; k < 4; k++)
                    {
                        outData[outIdx + j * 4 + k] = data.data[inIdx + outOffset + k];
                    }
                    outOffset += 4;
                }
            }
        }
        return outData;
    }
}
