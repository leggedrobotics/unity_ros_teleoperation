using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

namespace RSL.Core.CameraOverlay
{
    public class CameraOverlay : MonoBehaviour
    {
        public static class TransportHint
        {
            public const string Compressed = "/compressed",  Raw = "";
        }

        public GameObject plane;
        // public Camera depthCam;
        public RawImage overlayImage;
        public RenderTexture renderTexture;
        ROSConnection ros;
        public string topicName = "/img";
        public string transportHint = TransportHint.Raw;

        private string _topicName;
        private MeshRenderer _meshRenderer;
        private Texture2D _texture2D;
        private byte[] _imageData;
        private Camera _camera;

        public RawImage uiImage;

        public Shader depthShader;
        public Material depthMaterial;

        // Start is called before the first frame update
        void Start()
        {
            _camera = Camera.main;

            // depthCam.depthTextureMode = DepthTextureMode.Depth;
            // depthCam.targetTexture = renderTexture;

            depthMaterial = new Material(depthShader);





            if (topicName.EndsWith("compressed"))
            {
                transportHint = TransportHint.Compressed;
                _topicName = topicName;
            }
            else
            {
                _topicName = topicName + transportHint;
            }

            _meshRenderer = plane.GetComponent<MeshRenderer>();



            ros = ROSConnection.GetOrCreateInstance();
            // ros.Subscribe<ImageMsg>(_topicName, OnImage);
            ros.Subscribe<CompressedImageMsg>(_topicName, OnCompressed);
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
            ImageConversion.LoadImage(_texture2D, msg.data);

            // demosiac the bayered image


            _texture2D.Apply();

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
