//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LookingGlass {
    [HelpURL("https://docs.lookingglassfactory.com/Unity/Scripts/Cursor3D/")]
    public class Cursor3D : MonoBehaviour {
        private static Cursor3D instance;
        public static Cursor3D Instance {
            get {
                if (instance != null) return instance;
                instance = FindObjectOfType<Cursor3D>();
                return instance;
            }
        }

        [Tooltip("Disables the OS cursor at the start")]
        public bool disableSystemCursor = true;

        [Tooltip("Should the cursor scale follow the size of the LookingGlass?")]
        public bool relativeScale = true;

        [NonSerialized] public Texture2D depthNormals;
        [NonSerialized] public Shader depthOnlyShader;
        [NonSerialized] public Shader readDepthPixelShader;
        [NonSerialized] public Material readDepthPixelMat;

        public GameObject cursorGameObject;

        private bool cursorGameObjectExists;
        private bool frameRendered;
        private Camera cursorCam;

        private Vector3 worldPos;
        private Vector3 localPos;
        private Vector3 normal;
        private Quaternion rotation;
        private Quaternion localRotation;
        private bool overObject;

        public RenderTexture debugTexture;

        [SerializeField] private GameObject uiCursor = null;
        public bool uiCursorMode;
        private Canvas parentCanvas;
        private Renderer rend;

        private void Start() {
            if (disableSystemCursor)
                Cursor.visible = false;
            cursorGameObjectExists = cursorGameObject != null;

            //Duncan addition for 2D UI
            if (uiCursor != null) {
                parentCanvas = uiCursor.GetComponentInParent<Canvas>();
                rend = GetComponentInChildren<Renderer>();
                InitializeCursor();
            }
            //end
        }

        private void OnEnable() {
            depthOnlyShader = Shader.Find("LookingGlass/DepthOnly");
            readDepthPixelShader = Shader.Find("LookingGlass/ReadDepthPixel");
            if (readDepthPixelShader != null)
                readDepthPixelMat = new Material(readDepthPixelShader);

            depthNormals = new Texture2D(1, 1, TextureFormat.ARGB32, false, true);
            cursorCam = new GameObject("cursorCam").AddComponent<Camera>();
            cursorCam.transform.SetParent(transform);
            cursorCam.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnDisable() {
            if (cursorCam.gameObject != null)
                DestroyImmediate(cursorCam.gameObject);
        }

        private void Update() {
            HologramCamera hologramCamera = HologramCamera.Instance;
            if (hologramCamera == null) {
                Debug.LogWarning("[LookingGlass] No hologramCamera detected for 3D cursor!");
                enabled = false;
                return;
            }

            if (!hologramCamera.Initialized)
                return;

            if (frameRendered)
                return; // don't update if frame's been rendered already

            cursorCam.CopyFrom(hologramCamera.SingleViewCamera);
            int w = hologramCamera.QuiltSettings.TileWidth;
            int h = hologramCamera.QuiltSettings.TileHeight;

            RenderTexture colorRT = RenderTexture.GetTemporary(
                w, h, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1);
            colorRT.filterMode = FilterMode.Point; // important to avoid some weird edge cases
            colorRT.antiAliasing = 1;
            RenderTexture depthNormalsRT = RenderTexture.GetTemporary(
                1, 1, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

            cursorCam.targetTexture = colorRT;
            cursorCam.allowMSAA = false;
            float halfNormal = 0.5f;
            Color bgColor = new Color(halfNormal, halfNormal, 1f, 1f);
            cursorCam.backgroundColor = QualitySettings.activeColorSpace == ColorSpace.Gamma ?
                bgColor : bgColor.gamma;
            cursorCam.clearFlags = CameraClearFlags.SolidColor;
            cursorCam.cullingMask &= ~Physics.IgnoreRaycastLayer;

            // disable cursor game object before rendering
            bool cursorObjectEnabled = true;
            if (cursorGameObjectExists) {
                cursorObjectEnabled = cursorGameObject.activeSelf;
                if (cursorObjectEnabled) {
                    cursorGameObject.SetActive(false);
                }
            }

            cursorCam.RenderWithShader(depthOnlyShader, "RenderType");
            // turn cursor object back on
            if (cursorGameObjectExists && cursorObjectEnabled) {
                cursorGameObject.SetActive(true);
            }

            Vector2 mousePosition = Vector3.zero;
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = InputSystem.GetDevice<Mouse>();
            if (mouse != null)
                mousePosition = mouse.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
            mousePosition = (Vector2) Input.mousePosition;
#endif

            // copy single pixel and sample it
            // this keeps the ReadPixels from taking forever
            Vector3 mousePos = new Vector3(mousePosition.x, mousePosition.y);
            float monitorW = Screen.width;
            float monitorH = Screen.height;
            int activeDisplays = 0; // check if multiple displays are active
            foreach (var d in Display.displays) {
                if (d.active) activeDisplays++;
            }

            int monitor = 0;

            if (Application.platform == RuntimePlatform.WindowsPlayer && activeDisplays > 1) {
                mousePos = Display.RelativeMouseAt(new Vector3(mousePosition.x, mousePosition.y));
                monitor = Mathf.RoundToInt(mousePos.z);
                if (Display.displays.Length > monitor) {
                    monitorW = Display.displays[monitor].renderingWidth;
                    monitorH = Display.displays[monitor].renderingHeight;
                }
            }
            Vector2 mousePos01 = new Vector2(
                mousePos.x / monitorW,
                mousePos.y / monitorH);
            readDepthPixelMat.SetVector("samplePoint", new Vector4(mousePos01.x, mousePos01.y));
            Graphics.Blit(colorRT, depthNormalsRT, readDepthPixelMat);
            if (debugTexture == null)
                debugTexture = new RenderTexture(colorRT.width, colorRT.height, 0, colorRT.format, RenderTextureReadWrite.Linear);
            Graphics.Blit(colorRT, debugTexture);
            RenderTexture.active = depthNormalsRT;
            depthNormals.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
            depthNormals.Apply();
            Color enc = depthNormals.GetPixel(0, 0);

            // find world pos from depth
            float depth = DecodeFloatRG(enc);
            overObject = depth < 1f;
            if (!overObject) {
                depth = hologramCamera.CameraProperties.NearClipFactor / (hologramCamera.CameraProperties.NearClipFactor + hologramCamera.CameraProperties.FarClipFactor);
            }
            // bool hit = true;
            // depth = hit ? depth : 0.5f; // if nothing hit, default depth
            depth = cursorCam.nearClipPlane + depth * (cursorCam.farClipPlane - cursorCam.nearClipPlane);
            Vector3 screenPoint = new Vector3(mousePos01.x, mousePos01.y, depth);
            worldPos = cursorCam.ViewportToWorldPoint(screenPoint);
            localPos = hologramCamera.transform.InverseTransformPoint(worldPos);
            if (isActiveAndEnabled)
                transform.position = worldPos;


            //Duncan addition for 2D UI
            if (uiCursor != null) {
                //I don't like that I need to return a bool here
                //It's because OrbitControl.cs also handles some 3D cursor rendering logic for multi-touch stuff
                //Would like to find a way to clean this up
                uiCursorMode = Render2DCursorMode(monitor, mousePos);
            }
            //End

            // find world normal based on view normal
            normal = DecodeViewNormalStereo(enc);
            // normals = hit ? normals : Vector3.forward; // if nothing hit, default normal
            normal = cursorCam.cameraToWorldMatrix * normal;
            rotation = Quaternion.LookRotation(-normal);
            localRotation = Quaternion.Inverse(hologramCamera.transform.rotation) * rotation;

            if (isActiveAndEnabled) {
                transform.rotation = rotation;
                // might as well set size here as well
                if (relativeScale)
                    transform.localScale = Vector3.one * hologramCamera.CameraProperties.Size * 0.1f;
            }

            // reset settings
            RenderTexture.ReleaseTemporary(colorRT);
            RenderTexture.ReleaseTemporary(depthNormalsRT);

            // set frame rendered
            frameRendered = true;
        }

        private void LateUpdate() {
            frameRendered = false;
        }

        public Vector3 GetWorldPos() { Update(); return worldPos; }
        public Vector3 GetLocalPos() { Update(); return localPos; }
        public Vector3 GetNormal() { Update(); return normal; }
        public Quaternion GetRotation() { Update(); return rotation; }
        public Quaternion GetLocalRotation() { Update(); return localRotation; }
        public bool GetOverObject() { Update(); return overObject; }

        //Duncan addition for 2D UI
        private void InitializeCursor() {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform, Input.mousePosition,
                parentCanvas.worldCamera,
                out Vector2 pos);
        }

        private void MoveUICursor(Vector3 inputPos, GameObject uiCursor, Canvas parent) {
            Vector2 movePos;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                inputPos, null,
                out movePos);

            uiCursor.transform.position = parentCanvas.transform.TransformPoint(movePos);
        }

        private bool Render2DCursorMode(int monitor, Vector3 mousePos) {
            if (monitor == 1) { //We're on the second monitor
                if (uiCursor.activeSelf)
                    uiCursor.SetActive(false);
                if (!rend.enabled)
                    rend.enabled = true;
                return false;
            } else {
                if (!uiCursor.activeSelf)
                    uiCursor.SetActive(true);
                if (rend.enabled)
                    rend.enabled = false;
                MoveUICursor(mousePos, uiCursor, parentCanvas);
                return true;
            }
        }

        //Duncan addition to handle file opening cursor logic
        public void Refocused() {
            if (disableSystemCursor)
                Cursor.visible = false;
        }

        #region Copied From UnityCG.cginc
        private Vector3 DecodeViewNormalStereo(Color enc4) {
            float kScale = 1.7777f;
            Vector3 enc4xyz = new Vector3(enc4.r, enc4.g, enc4.b);
            Vector3 asdf = Vector3.Scale(enc4xyz, new Vector3(2f * kScale, 2f * kScale, 0f));
            Vector3 nn = asdf + new Vector3(-kScale, -kScale, 1f);
            float g = 2.0f / Vector3.Dot(nn, nn);
            Vector2 nnxy = new Vector3(nn.x, nn.y) * g;
            Vector3 n = new Vector3(nnxy.x, nnxy.y, g - 1f);
            return n;
        }

        private float DecodeFloatRG(Color enc) {
            Vector2 encxy = new Vector2(enc.b, enc.a);
            Vector2 kDecodeDot = new Vector2(1.0f, 1.0f / 255.0f);
            return Vector2.Dot(encxy, kDecodeDot);
        }
        #endregion
    }
}
