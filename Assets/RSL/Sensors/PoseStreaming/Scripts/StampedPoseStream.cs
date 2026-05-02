using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Std;
using RosMessageTypes.Geometry;

using RSL.Sensors.Lidar;

namespace RSL.Sensors.PoseStreaming
{
    public class StampedPoseStream : RSL.Core.SensorStream
    {
        public TMPro.TextMeshProUGUI topicText;
        public Material arrowMaterial;
        
        private GraphicsBuffer _meshTriangles;
        private GraphicsBuffer _meshVertices;
        private GraphicsBuffer _ptData;
        private GraphicsBuffer _rotData;

        private RenderParams _renderParams;
        public Slider sizeSlider;
        private Mesh _mesh;
        public Transform _parent;
        private Quaternion _rotation = Quaternion.Euler(90, 0, 0);
        private bool _enabled = true;
        private Vector4[] _ptArray;
        private Vector4[] _rotArray;



        public float scale = 1.0f;
        public float arrowRadius = 0.25f;
        public int arrowSides = 10;

        public void Awake()
        {
            _msgType = "geometry_msgs/PoseStamped";
            _ros = ROSConnection.GetOrCreateInstance();
            _mesh = LidarUtils.MakeArrow(arrowRadius, arrowSides);

            _meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _mesh.triangles.Length, 4);
            _meshTriangles.SetData(_mesh.triangles);
            _meshVertices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _mesh.vertices.Length, 12);
            _meshVertices.SetData(_mesh.vertices);
            _ptData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 16);
            _rotData = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, 16);

            _renderParams = new RenderParams(arrowMaterial);
            _renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * 100);
            _renderParams.matProps = new MaterialPropertyBlock();

            _renderParams.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(new Vector3(0, 0, 0)));
            _renderParams.matProps.SetFloat("_PointSize", scale);
            _renderParams.matProps.SetBuffer("_PointData", _ptData);
            _renderParams.matProps.SetBuffer("_Positions", _meshVertices);
            _renderParams.matProps.SetBuffer("_RotationData", _rotData);

            _ptArray = new Vector4[1];
            _ptArray[0] = new Vector4(0, 0, 0, LidarUtils.PackRGBA(new Color(1, 0, 0, 1)));
            _ptData.SetData(_ptArray);

            _rotArray = new Vector4[1];
            _rotData.SetData(_rotArray);

            

            topicDropdown.ClearOptions();
            topicDropdown.onValueChanged.AddListener((value) => { OnTopicSelected(value); });

            RefreshTopics();
        }

        private void Update()
        {
            if (_enabled && _mesh != null)
            {
                Transform t = _parent == null ? this.transform : _parent;
                _renderParams.matProps.SetMatrix("_ObjectToWorld", t.localToWorldMatrix);
                Graphics.RenderPrimitivesIndexed(_renderParams, MeshTopology.Triangles, _meshTriangles, _meshTriangles.count, (int)_mesh.GetIndexStart(0), 1);
            }
        }

        public void OnTopicSelected(int value)
        {
            if (value < 0 || value >= topicDropdown.options.Count)
            {
                Debug.LogWarning("Invalid topic selected: " + value);
                return;
                }

            string selectedTopic = topicDropdown.options[value].text;
            if (selectedTopic == "None")
            {
                    OnTopicChange(null);
                }
                else
                {
                    OnTopicChange(selectedTopic);
                }
        }

        private void OnValidate()
        {
            if(Application.isPlaying && _renderParams.matProps != null)
            {
                _renderParams.matProps.SetFloat("_PointSize", scale);
            }
            
        }

        public override void OnTopicChange(string newTopic)
        {
            if (topicName != null)
            {
                _ros.Unsubscribe(topicName);
                topicName = null;
            }
            if (newTopic == null)
            {
                Debug.Log("Disabling Pose display");
                _enabled = false;
                topicText?.SetText("None");
                return;
            }
            _enabled = true;
            topicName = newTopic;
            topicText?.SetText(newTopic);
            _ros.Subscribe<PoseStampedMsg>(newTopic, OnPose);
            Debug.Log("[PoseStream] Subscribed to " + newTopic);
        }

        private void OnPose(PoseStampedMsg msg)
        {
            if (!_enabled)
                return;

            if (_parent == null || _parent.name != msg.header.frame_id)
            {
                Transform parent = GameObject.Find(msg.header.frame_id)?.transform;
                if (parent != null)
                {
                    _parent = parent;
                }
                else
                {
                    _parent = GameObject.FindWithTag("root")?.transform;
                }
            }

            Vector3 t = msg.pose.position.From<FLU>();
            _rotation = msg.pose.orientation.From<FLU>();
            _ptArray[0].x = t.x;
            _ptArray[0].y = t.y;
            _ptArray[0].z = t.z;
            _ptData.SetData(_ptArray);

            _rotArray[0] = new Vector4(_rotation.x, _rotation.y, _rotation.z, _rotation.w);
            _rotData.SetData(_rotArray);

        }

        public void OnSizeUpdate(float size)
        {
            scale = size;
            _renderParams.matProps.SetFloat("_PointSize", size);
        }


        private void OnDestroy()
        {
            if (topicName != null)
                _ros.Unsubscribe(topicName);
            _meshTriangles?.Dispose();
            _meshTriangles = null;
            _meshVertices?.Dispose();
            _meshVertices = null;
            _ptData?.Dispose();
            _ptData = null;
        }

        public override void ToggleTrack(int mode)
        {
            throw new System.NotImplementedException();
        }
    }
}
