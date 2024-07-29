using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;


public class LidarSpawner : MonoBehaviour
{
    /// <summary>
    /// Tester class to generate a bunch of fake Lidar pts in the ROS point2 format
    /// </summary>
    /// 

    public int numPts = 1000;
    public bool randomize = false;
    public int decimator = 1;

    public delegate void OnPointCloudGenerated(PointCloud2Msg pointCloud);
    public OnPointCloudGenerated PointCloudGenerated;

    private int _counter = 0;
    public float timeScale = 1.0f;
    public float scale = 1.0f;
    private PointCloud2Msg _pointCloud;
    public bool debug = false;
    
    void Start()
    {
        _pointCloud = new PointCloud2Msg();
        _pointCloud.header = new HeaderMsg();
        _pointCloud.header.frame_id = "base_link";
        _pointCloud.fields = new PointFieldMsg[6];
        _pointCloud.fields[0] = new PointFieldMsg();
        _pointCloud.fields[0].name = "x";
        _pointCloud.fields[0].offset = 0;
        _pointCloud.fields[0].datatype = 7;
        _pointCloud.fields[0].count = 1;
        _pointCloud.fields[1] = new PointFieldMsg();
        _pointCloud.fields[1].name = "y";
        _pointCloud.fields[1].offset = 4;
        _pointCloud.fields[1].datatype = 7;
        _pointCloud.fields[1].count = 1;
        _pointCloud.fields[2] = new PointFieldMsg();
        _pointCloud.fields[2].name = "z";
        _pointCloud.fields[2].offset = 8;
        _pointCloud.fields[2].datatype = 7;
        _pointCloud.fields[2].count = 1;
        _pointCloud.fields[3] = new PointFieldMsg();
        _pointCloud.fields[3].name = "intensity";
        _pointCloud.fields[3].offset = 12;
        _pointCloud.fields[3].datatype = 7;
        _pointCloud.fields[3].count = 1;
        _pointCloud.fields[4] = new PointFieldMsg();
        _pointCloud.fields[4].name = "ring";
        _pointCloud.fields[4].offset = 16;
        _pointCloud.fields[4].datatype = 4;
        _pointCloud.fields[4].count = 1;
        _pointCloud.fields[5] = new PointFieldMsg();
        _pointCloud.fields[5].name = "time";
        _pointCloud.fields[5].offset = 18;
        _pointCloud.fields[5].datatype = 7;
        _pointCloud.fields[5].count = 1;
        _pointCloud.is_bigendian = false;
        _pointCloud.point_step = 22;
        _pointCloud.row_step = (uint)(_pointCloud.point_step * numPts);
        _pointCloud.data = new byte[_pointCloud.row_step * numPts];
        _pointCloud.is_dense = false;

    }

    private void OnValidate() 
    {
        if(numPts < 1)
        {
            numPts = 1;
        }
        if(_pointCloud != null)
        {
            _pointCloud.row_step = (uint)(_pointCloud.point_step * numPts);
            _pointCloud.data = new byte[_pointCloud.row_step * numPts];
        }
    }


    public void Wave()
    {
        int row = Mathf.FloorToInt(Mathf.Sqrt(numPts));
        // Sets the points to be an evenly spaced grid on the x/z and the y to be a sin wave based on time
        // with the intensity being the sin wave of the x/z position
        for(int i = 0; i < numPts; i++)
        {
            // for(int j = 0; j < row; j++)
            // {
                float x = (i % row) - row/2;
                float z = Mathf.Floor(i/row) - row/2;
                float y = Mathf.Sin((Time.time * timeScale)*scale+x+z);
                float intensity = x/row;
                DataToArray(x, y, z, intensity,i, _pointCloud);
            // }

            if(debug)
            {
                Vector3 start = new Vector3(x, y, z);
                Vector3 dir = Vector3.up*1f;
                Debug.DrawRay(start, dir, Color.red, 1f);
            }
        }
    }

    public void Randomize()
    {
        for(int i = 0; i < numPts; i++)
        {
            float x = Random.Range(-10f, 10f);
            float y = Random.Range(-10f, 10f);
            float z = Random.Range(-10f, 10f);
            float intensity = Random.Range(0f, 1f);
            DataToArray(x, y, z, intensity, i,_pointCloud);
        }
    }

    private static void DataToArray(float x, float y, float z, float intensity, int i, PointCloud2Msg dst)
    {
        byte[] xBytes = System.BitConverter.GetBytes(x);
        byte[] yBytes = System.BitConverter.GetBytes(y);
        byte[] zBytes = System.BitConverter.GetBytes(z);
        byte[] intensityBytes = System.BitConverter.GetBytes(intensity);
        byte[] ringBytes = System.BitConverter.GetBytes((ushort)0);
        byte[] timeBytes = System.BitConverter.GetBytes((float)Time.time);
        System.Array.Copy(xBytes, 0, dst.data, i * dst.point_step + dst.fields[0].offset, 4);
        System.Array.Copy(yBytes, 0, dst.data, i * dst.point_step + dst.fields[1].offset, 4);
        System.Array.Copy(zBytes, 0, dst.data, i * dst.point_step + dst.fields[2].offset, 4);
        System.Array.Copy(intensityBytes, 0, dst.data, i * dst.point_step + dst.fields[3].offset, 4);
        System.Array.Copy(ringBytes, 0, dst.data, i * dst.point_step + dst.fields[4].offset, 2);
        System.Array.Copy(timeBytes, 0, dst.data, i * dst.point_step + dst.fields[5].offset, 4);
    }

    // Update is called once per frame
    void Update()
    {
        _counter++;
        if(_counter >= decimator)
        {
            if(randomize)
                Randomize();
            else
                Wave();
            PointCloudGenerated?.Invoke(_pointCloud);
            _counter = 0;
        }
    }
}
