using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UnityEngine.Timeline;

using RSL.Sensors.Lidar;

namespace RSL.Sensors.Markers
{
    public class MarkerPointStream : MonoBehaviour, IMarkerViz
    {

        public Material point_material;
        public Material line_material;

        GraphicsBuffer _meshTriangles;
        GraphicsBuffer _meshVertices;
        GraphicsBuffer _ptData;
        GraphicsBuffer _rotData;


        public float scale = .01f;
        public int maxPts = 100_000;
        public int sides = 3;
        private RenderParams renderParams;
        private Mesh mesh;
        public bool _enabled = true;
        public int _numPts = 0;
        public MarkerType markerType = MarkerType.Points;

        private ColorRGBAMsg[] _colors;
        private PointMsg[] _points;



        // Start is called before the first frame update
        void Awake()
        {
            // mesh = LidarUtils.MakePolygon(sides);
            mesh = LidarUtils.MakeCube();


            _meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.triangles.Length, sizeof(int));
            _meshTriangles.SetData(mesh.triangles);
            _meshVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.vertices.Length, 3 * sizeof(float));
            _meshVertices.SetData(mesh.vertices);
            _ptData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxPts, 16);
            _rotData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxPts, 16);

            renderParams = new RenderParams(point_material);

            renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 100);
            renderParams.matProps = new MaterialPropertyBlock();

            // Sets rotation data to all 0 since we dont rotate the markers
            Quaternion identity = Quaternion.identity;
            Vector4[] rotData = new Vector4[maxPts];
            for (int i = 0; i < maxPts; i++)
            {
                rotData[i] = new Vector4(identity.x, identity.y, identity.z, identity.w);
            }
            _rotData.SetData(rotData);

            renderParams.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(new Vector3(0, 0, 0)));
            renderParams.matProps.SetFloat("_PointSize", scale);
            renderParams.matProps.SetBuffer("_PointData", _ptData);
            renderParams.matProps.SetBuffer("_RotationData", _rotData);

        }


        private void Update()
        {
            if (_enabled && mesh != null && !markerType.IsLines())
            {
                renderParams.matProps.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
                Graphics.RenderPrimitivesIndexed(renderParams, MeshTopology.Triangles, _meshTriangles, _meshTriangles.count, (int)mesh.GetIndexStart(0), _numPts);
            }
        }

        public void OnRenderObject()
        {
            if (!line_material)
            {
                Debug.LogError("MarkerPointStream: line_material is not set");
                return;
            }

            if (_enabled && mesh != null && markerType.IsLines())
            {
                GL.PushMatrix();
                line_material.SetPass(0);
                GL.MultMatrix(transform.localToWorldMatrix);
                GL.Begin(GL.LINES);
                for (int i = 0; i < _numPts; i++)
                {
                    Vector3 pt = _points[i].From<FLU>();
                    ColorRGBAMsg colorMsg = _colors.Length == 1 ? _colors[0] : _colors[i];
                    Color color = new Color(colorMsg.r, colorMsg.g, colorMsg.b, colorMsg.a); // Fixed alpha
                    GL.Color(color);
                    GL.Vertex3(pt.x, pt.y, pt.z);
                }
                GL.End();
                GL.PopMatrix();
            }
        }

        public void SetMesh(Mesh newMesh)
        {
            if (newMesh == null)
            {
                Debug.LogError("MarkerPointStream: SetMesh called with null mesh");
                return;
            }

            _meshTriangles?.Dispose();
            _meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, newMesh.triangles.Length, sizeof(int));
            _meshTriangles.SetData(newMesh.triangles);

            _meshVertices?.Dispose();
            _meshVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, newMesh.vertices.Length, 3 * sizeof(float));
            _meshVertices.SetData(newMesh.vertices);

            renderParams.matProps.SetBuffer("_Positions", _meshVertices);
        }



        private void OnDestroy()
        {
            _meshTriangles?.Dispose();
            _meshTriangles = null;
            _meshVertices?.Dispose();
            _meshVertices = null;
            _ptData?.Dispose();
            _ptData = null;
            _rotData?.Dispose();
            _rotData = null;
        }


        public void SetData(PoseMsg pose, Vector3Msg scale, ColorRGBAMsg[] colors, PointMsg[] points)
        {
            _colors = colors;
            _points = points;
            if (points == null || points.Length == 0)
            {
                _ptData.SetData(new Vector4[0]);
                _numPts = 0;
                return;
            }

            Vector4[] pointData = new Vector4[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 pt = points[i].From<FLU>();
                float color = colors.Length == 1 ? LidarUtils.PackRGBA(colors[0]) : LidarUtils.PackRGBA(colors[i]);
                pointData[i] = new Vector4(pt.x, pt.y, pt.z, color);
            }

            _ptData.SetData(pointData);
            _numPts = points.Length;
        }  


        private void OnValidate()
        {
            if (renderParams.matProps != null)
            {
                renderParams.matProps.SetFloat("_PointSize", scale);
            }
        }
        
        
        public void OnSizeChange(float size)
        {
            scale = size / 10f;
            renderParams.matProps.SetFloat("_PointSize", scale);
        }



    }
}
