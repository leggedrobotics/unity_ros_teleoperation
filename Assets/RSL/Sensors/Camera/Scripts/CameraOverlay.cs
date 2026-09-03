using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UvgRos;
using RosMessageTypes.Sensor;
using RSL.Core;

namespace RSL.Sensors.Camera
{
    #if UNITY_EDITOR
    using UnityEditor;
    // Empty on purpose -- see GridMapStreamEditor's comment in
    // GridMapStream.cs (SensorStream's editorForChildClasses fallback
    // didn't take effect in practice).
    [CustomEditor(typeof(CameraOverlay))]
    public class CameraOverlayEditor : RSL.Core.SensorStreamEditor
    {
    }
    #endif

    public class CameraOverlay : SensorStream
    {
        public static class TransportHint
        {
            public const string Compressed = "/compressed",  Raw = "";
        }

        public GameObject plane;
        // public Camera depthCam;
        public RawImage overlayImage;
        public RenderTexture renderTexture;
        public string transportHint = TransportHint.Raw;

        private string _topicName;
        private MeshRenderer _meshRenderer;
        private Texture2D _texture2D;
        private byte[] _imageData;
        private UnityEngine.Camera _camera;

        public RawImage uiImage;

        public Shader depthShader;
        public Material depthMaterial;

        // Start is called before the first frame update
        void Start()
        {
            _camera = UnityEngine.Camera.main;

            // depthCam.depthTextureMode = DepthTextureMode.Depth;
            // depthCam.targetTexture = renderTexture;

            depthMaterial = new Material(depthShader);

            _meshRenderer = plane.GetComponent<MeshRenderer>();

            _ros = UvgRosConnection.GetOrCreateInstance();
            _msgType = "sensor_msgs/CompressedImage";

            OnTopicChange(topicName);
        }

        public override void ToggleTrack(int mode)
        {
            // No tracking-mode concept for a fixed passthrough overlay --
            // required by the SensorStream base but not applicable here.
        }

        public override void OnTopicChange(string topic)
        {
            if (!string.IsNullOrEmpty(topicName))
                _ros?.Unsubscribe(topicName);

            if (string.IsNullOrEmpty(topic))
            {
                topicName = null;
                return;
            }

            topicName = topic;
            if (topic.EndsWith("compressed"))
            {
                transportHint = TransportHint.Compressed;
                _topicName = topic;
            }
            else
            {
                _topicName = topic + transportHint;
            }

            // ros.Subscribe<ImageMsg>(_topicName, OnImage);
            try
            {
                _ros.Subscribe<CompressedImageMsg>(_topicName, OnCompressed, mainThread: true);
            }
            catch (System.NotSupportedException e)
            {
                // Hit when the server negotiated an H.264 (encoded_video) route
                // for this topic -- UvgRosConnection.Subscribe<T> only handles
                // the plain chain/msgpack_list framings so far, not that path.
                Debug.LogError("[CameraOverlay] '" + _topicName + "' is not viewable yet: " + e.Message);
            }
        }

        public void OnSelect(int value)
        {
            if (value == _lastSelected) return;
            _lastSelected = value;

            string selectedTopic = topicDropdown.options[value].text;
            if (selectedTopic == "None") selectedTopic = null;

            OnTopicChange(selectedTopic);
        }

        void OnCompressed(CompressedImageMsg msg)
        {
            // decompress the image from jpeg
            if (_texture2D == null)
            {
                _texture2D = new Texture2D(1, 1, TextureFormat.RGB24, false);
                depthMaterial.SetTexture("_RenderTex", _texture2D);
                _meshRenderer.material.mainTexture = _texture2D;
                uiImage.texture = _texture2D;
                uiImage.color = Color.white;
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            ImageConversion.LoadImage(_texture2D, msg.data);

            // demosiac the bayered image


            _texture2D.Apply();
            sw.Stop();
            // See ImageView.OnCompressed's identical instrumentation --
            // LoadImage (JPEG decode) + Apply (GPU upload) both run
            // synchronously on the main thread.
            if (sw.Elapsed.TotalMilliseconds > 4.0)
                Debug.LogWarning("[CameraOverlay] LoadImage+Apply for '" + topicName + "' (" +
                    _texture2D.width + "x" + _texture2D.height + ", " + msg.data.Length +
                    " bytes) took " + sw.Elapsed.TotalMilliseconds.ToString("F1") + "ms on the main thread");

            float aspectRatio = (float)_texture2D.height / (float)_texture2D.width;
            float width = uiImage.rectTransform.rect.width;
            float height = width * aspectRatio;
            uiImage.rectTransform.sizeDelta = new Vector2(uiImage.rectTransform.sizeDelta.x , height);

        }

        void OnImage(ImageMsg msg)
        {
            // Debug.Log("Got image message");
            if (_texture2D == null)
            {
                _texture2D = new Texture2D((int)msg.width, (int)msg.height, TextureFormat.RGB24, false);
                // overlayImage.texture = _texture2D;

                depthMaterial.SetTexture("_RenderTex", _texture2D);
                // overlayImage.material = depthMaterial;

                _meshRenderer.material.mainTexture = _texture2D;

                float aspectRatio = (float)msg.height / (float)msg.width;

                uiImage.texture = _texture2D;
                float width = uiImage.rectTransform.rect.width;
                float height = width * aspectRatio;
                uiImage.rectTransform.sizeDelta = new Vector2(uiImage.rectTransform.sizeDelta.x , height);
                uiImage.color = Color.white;

                // overlayImage.rectTransform.sizeDelta = new Vector2(msg.width, msg.height);

                // renderTexture.width = (int)msg.width;
                // renderTexture.height = (int)msg.height;
            }

            _texture2D.LoadRawTextureData(msg.data);
            _texture2D.Apply();



        }
    }
}
