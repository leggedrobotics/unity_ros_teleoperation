using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using LookingGlass.Toolkit;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LookingGlass {
    [Serializable]
    public class HologramEmulation : IDisposable {
        [ExecuteAlways]
        private class UpdateBehaviour : MonoBehaviour {
            public Action onUpdate;
            private void Update() {
                if (onUpdate == null) {
                    DestroyImmediate(gameObject);
                    return;
                }
                onUpdate();
            }
        }

        private const float TwoPi = 2 * Mathf.PI;
        private static HologramEmulation lastClicked;

        /// <summary>
        /// <para>A mathematical function of a sine wave that goes through the following points:</para>
        /// <list type="bullet">
        /// <item>(0.00, 0.00)</item>
        /// <item>(0.25, 1.00)</item>
        /// <item>(0.50, 0.00)</item>
        /// <item>(0.75, -1.00)</item>
        /// <item>(1.00, 0.00)</item>
        /// </list>
        /// </summary>
        /// <param name="x">The input variable into the math function, which may be any real number.</param>
        /// <returns>A float in the range [0, 1] along the curve.</returns>
        private static float SineWave(float x) {
            return Mathf.Sin(TwoPi * x);
        }

        /// <summary>
        /// <para>A mathematical function of a sine save that goes through the following points:</para>
        /// <list type="bullet">
        /// <item>(0.00, 0.00)</item>
        /// <item>(<paramref name="max"/>, <paramref name="max"/>)</item>
        /// <item>(2 * <paramref name="max"/>, 0.00)</item>
        /// <item>(3 * <paramref name="max"/>, <paramref name="max"/>)</item>
        /// <item>(4 * <paramref name="max"/>, 0.00)</item>
        /// </list>
        /// </summary>
        /// <param name="x">The input variable into the math function, which may be any real number.</param>
        /// <param name="max">The max output value. This scales the whole sine wave above the x-axis.</param>
        /// <returns>A float in the range [0, <paramref name="max"/>] along the curve.</returns>
        private static float SineWave(float x, float max) {
            return max * ((SineWave(1 / (2 * max) * (x - max / 2)) + 1) / 2);
        }

        [Tooltip("The quilt to read from.")]
        [SerializeField] private RenderTexture sourceQuilt;

        [FormerlySerializedAs("renderSettings")]
        [Tooltip("The quilt settings of the source quilt, so it can be interpreted correctly.")]
        [SerializeField] private QuiltSettings quiltSettings;

        [Tooltip("The hologram texture.")]
        [SerializeField] private RenderTexture singleViewTexture;
        [ReadOnlyField] private int viewIndex;
        [SerializeField] private bool autoRotate = false;
        [SerializeField] private float autoRotateSpeed = 30;

        private float rawViewIndex;
        private bool openingSwivel;
        private float timeOpeningSwivelStarted;
        private int lastFrameRendered = -1;
#if UNITY_EDITOR
        private int lastPreviewGUIFrame = 0;
#endif
        private Vector2 lastMouseDownPos;

        //NOTE: This is optional and may be null:
        private HologramCamera hologramCamera;

        private Transform previewParent;
        private MeshRenderer previewPlane;
        private Camera previewCamera;
        private RenderTexture previewTexture;
        private Material previewMaterial;
        private UpdateBehaviour updater;

        private bool isDisposed = false;

        public RenderTexture SourceQuilt {
            get { return sourceQuilt; }
        }

        public QuiltSettings QuiltSettings {
            get { return quiltSettings; }
        }

        public RenderTexture SingleViewTexture => singleViewTexture;
        public int ViewIndex => viewIndex;
        public int ViewCount => quiltSettings.tileCount;

        public bool AutoRotate {
            get { return autoRotate; }
            set {
                autoRotate = value;
                rawViewIndex = viewIndex;
                openingSwivel = false;
            }
        }

        /// <summary>
        /// Gets <see cref="Time.deltaTime"/>, but clamps it in the range [0, 0.12] to limit jerky movement from larger values of dt.
        /// </summary>
        private float GetSmallDeltaTime() => Mathf.Clamp(Time.deltaTime, 0, 0.12f);

        public HologramEmulation(HologramCamera source) {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            hologramCamera = source;
            Initialize(source.RenderStack.QuiltMix, source.QuiltSettings);
            source.RenderStack.onQuiltChanged += UpdateQuilt;
            source.RenderStack.onRendered += UpdateAfterRenderStack;
            source.onQuiltSettingsChanged += UpdateQuiltSettings;
        }

        public HologramEmulation(RenderTexture sourceQuilt, QuiltSettings quiltSettings) {
            Initialize(sourceQuilt, quiltSettings);
        }

        private void Initialize(RenderTexture sourceQuilt, QuiltSettings quiltSettings) {
            this.sourceQuilt = sourceQuilt;
            this.quiltSettings = quiltSettings;
            CreateHologramTexture();
            SetupPreviewRenderObjects();

            SetView(ViewCount / 2);

            AutoRotate = autoRotate;
            StartOpeningSwivel();
        }

        public void Dispose() {
            isDisposed = true;

            if (lastClicked == this)
                lastClicked = null;

            if (hologramCamera != null) {
                hologramCamera.RenderStack.onQuiltChanged -= UpdateQuilt;
                hologramCamera.RenderStack.onRendered -= UpdateAfterRenderStack;
                hologramCamera.onQuiltSettingsChanged -= UpdateQuiltSettings;
            }

            if (previewParent != null)
                GameObject.DestroyImmediate(previewParent.gameObject);
            if (singleViewTexture != null)
                singleViewTexture.Release();
            if (previewTexture != null)
                previewTexture.Release();
        }

        private void UpdateQuilt() {
            sourceQuilt = hologramCamera.RenderStack.QuiltMix;
            UpdateQuiltSettings();
            if (!autoRotate)
                StartOpeningSwivel();
        }

        private void UpdateAfterRenderStack() => Render();

        private void UpdateQuiltSettings() {
            quiltSettings = hologramCamera.QuiltSettings;
            UpdateObjectOnAnyQuiltChanges();
            CreateHologramTexture();
            Render(true);
            if (!autoRotate)
                StartOpeningSwivel();
        }

        private void CreateHologramTexture() {
            if (singleViewTexture != null)
                singleViewTexture.Release();

            singleViewTexture = new RenderTexture(quiltSettings.TileWidth, quiltSettings.TileHeight, 0, RenderTextureFormat.Default) {
                filterMode = FilterMode.Point,
            };

            singleViewTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm;

            singleViewTexture.name = "Hologram";
            singleViewTexture.enableRandomWrite = true;
            singleViewTexture.Create();
        }

        private void StartOpeningSwivel() {
            timeOpeningSwivelStarted = Time.time;
            openingSwivel = true;
            rawViewIndex = viewIndex;
        }

        private void OnUpdate() {
            float dt = GetSmallDeltaTime();
            if (openingSwivel) {
                if (Time.time - timeOpeningSwivelStarted > 0.5f)
                    OpeningSwivelUpdate(dt);
            } else {
                if (!isDisposed && autoRotate)
                    AutoRotateUpdate(dt);
            }
        }

        private void OpeningSwivelUpdate(float dt) {
            if (ViewCount <= 1)
                return;
            int prevIndex = viewIndex;
            int maxView = ViewCount - 1;

            float speedRamp = (float) -1 / (1.5f * maxView) * rawViewIndex + 4;
            rawViewIndex += dt * autoRotateSpeed * speedRamp;

            float endLerp = 1 * maxView;
            float end = 3 * maxView;

            if (rawViewIndex >= end) {
                rawViewIndex = viewIndex = maxView / 2;
                openingSwivel = false;
            } else {
                float linearDamper = (float) -1 / (3 * maxView) * rawViewIndex + 1;
                float value = linearDamper * SineWave(rawViewIndex, maxView);
                if (rawViewIndex >= endLerp)
                    value = Mathf.Lerp(value, maxView / 2, (rawViewIndex - endLerp) / (end - endLerp));
                viewIndex = (int) (value);
            }

            if (viewIndex != prevIndex)
                SetView(viewIndex);
        }

        private void AutoRotateUpdate(float dt) {
            if (ViewCount <= 1)
                return;

            int prevIndex = viewIndex;
            rawViewIndex += dt * autoRotateSpeed;
            viewIndex = (int) SineWave(rawViewIndex, ViewCount - 1);

            if (viewIndex != prevIndex)
                SetView(viewIndex);
        }

        private void SetupPreviewRenderObjects() {
            Mesh mesh = Resources.Load<Mesh>("Hologram Plane");
            previewMaterial = new Material(Resources.Load<Shader>("Unlit Texture"));

            previewPlane = new GameObject("Plane").AddComponent<MeshRenderer>();
            previewPlane.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            previewPlane.sharedMaterial = previewMaterial;
            previewPlane.enabled = false;

            previewPlane.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
            previewCamera = new GameObject("Camera").AddComponent<Camera>();
            previewCamera.enabled = false;
            previewCamera.backgroundColor = Color.clear;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.cullingMask = 1 << previewPlane.gameObject.layer;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 10;

            previewParent = new GameObject("Hologram Emulation").transform;
            previewParent.gameObject.hideFlags = HideFlags.HideAndDontSave;

            previewCamera.transform.SetParent(previewParent);

            previewPlane.transform.SetParent(previewParent);
            previewPlane.transform.localPosition = new Vector3(0, 0.07f, 0); //NOTE: This is a small offset to orient the plane a bit higher in the camera's viewing volume
            previewPlane.transform.localEulerAngles = new Vector3(0, 180, 0);

            UpdateObjectOnAnyQuiltChanges();

            updater = previewParent.gameObject.AddComponent<UpdateBehaviour>();
            updater.onUpdate = OnUpdate;

            previewParent.position = new Vector3(100, 0, 0);
        }

        public void SetView(int viewIndex) {
            if (viewIndex < 0 || viewIndex >= quiltSettings.tileCount)
                throw new ArgumentOutOfRangeException(nameof(viewIndex), viewIndex, nameof(viewIndex) + " must be in range [0, " + quiltSettings.tileCount + ").");
            this.viewIndex = viewIndex;
            RenderOnceReady();
        }

        private async void RenderOnceReady() {
            if (hologramCamera != null && !hologramCamera.Initialized)
                await hologramCamera.WaitForInitialization();
            if (hologramCamera == null || isDisposed)
                return;
            Render();
        }

        private bool Render(bool force = false) {
            int t = Time.frameCount;
            if (!force && t <= lastFrameRendered)
                return false;

            lastFrameRendered = t;
            if (hologramCamera != null && hologramCamera.isActiveAndEnabled && hologramCamera.Initialized) {
                hologramCamera.RenderQuilt();
                MultiViewRendering.CopyViewFromQuilt(quiltSettings, viewIndex, sourceQuilt, singleViewTexture, true);
            } else {
                //NOTE: This is just a visual UX improvement to the user.
                //I couldn't blit from Texture2D.blackTexture because that texture has 0 in the alpha channel.
                //  Thus, Texture2D.blackTexture would make the hologram preview invisible!

                Graphics.Blit(Util.OpaqueBlackTexture, singleViewTexture);
            }

            previewMaterial.mainTexture = singleViewTexture;
            int viewCount = ViewCount;
            float yRotOffset = 0;
            if (viewCount > 0)
                yRotOffset = 15 * (float) viewIndex / viewCount - 7.5f;

            previewPlane.transform.localEulerAngles = new Vector3(0, 180 + yRotOffset, 0);
            previewPlane.enabled = true;
            previewCamera.Render();
            previewPlane.enabled = false;
            return true;
        }

        private void UpdateObjectOnAnyQuiltChanges() {
            if (previewTexture != null)
                previewTexture.Release();

            float aspect = quiltSettings.renderAspect;
            previewTexture = new RenderTexture((int) (quiltSettings.TileHeight * aspect), quiltSettings.TileHeight, 0);
            previewTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm;
            previewTexture.name = "Hologram Preview";
            previewCamera.targetTexture = previewTexture;

            previewPlane.transform.localScale = new Vector3(aspect, 1, 1);

            //NOTE: Assumes the Hologram Plane mesh is 1 unit in width and height
            Vector3 size = previewPlane.transform.lossyScale;

            //Ohhhhhhhhhhh I think I sorta understand why I was confused.. it's cause the Camera's viewing volume already expands in width when I update its targetTexture's width..
            //So I was double-compensating for the width expansion by mistake
            float paddingPercent = 1.3f;
            float idealDistance = paddingPercent * size.y / (2 * Mathf.Tan(0.5f * previewCamera.fieldOfView * Mathf.Deg2Rad));

            previewCamera.transform.localPosition = new Vector3(0, 0, -idealDistance);
        }

        public string GetQuiltDescription() =>
            quiltSettings.quiltWidth + "×" + quiltSettings.quiltHeight + ", " +
            quiltSettings.columns + "×" + quiltSettings.rows + ", " +
            quiltSettings.renderAspect + " aspect";

#if UNITY_EDITOR
        public bool RequiresConstantRepaint() => HasPreviewGUI();
        public bool HasPreviewGUI() => sourceQuilt != null && singleViewTexture != null;

        public string GetInfoString() => GetQuiltDescription();

        public void OnSceneGUI() {
            Rect sceneViewPos = SceneView.currentDrawingSceneView.position;
            Rect r = new Rect(0, 0, previewTexture.width, previewTexture.height);
            while (r.width > 512 || r.height > 512)
                r.size /= 2;

            r.position = new Vector2(sceneViewPos.width - r.width, sceneViewPos.height - r.height);

            Handles.BeginGUI();
            MouseDragGUI(r);

            //NOTE: This invisible button allows us to prevent the SceneView from consuming our mouse dragging events!
            //Without this, our GUI events are already used for mouse drags because the SceneView consumes it first.
            Color prevColor = GUI.color;
            try {
                GUI.color = Color.clear;
                GUI.Button(r, "");
            } finally {
                GUI.color = prevColor;
            }
            GUI.DrawTexture(r, previewTexture, ScaleMode.ScaleToFit, true, 0);
            EditorGUIUtility.AddCursorRect(r, MouseCursor.Pan);
            Handles.EndGUI();
        }

        public void OnPreviewGUI(Rect r, GUIStyle background) {
            if (Event.current.type == EventType.Repaint) {
                //WARNING: Debug.Log calls here automatically queues player loop updates!
                int t = Time.frameCount;
                
                //NOTE: This helps keep the inspector preview GUI updating even when 0 SceneViews are actively rendering:
                if (lastPreviewGUIFrame == t)
                    EditorApplication.QueuePlayerLoopUpdate();

                lastPreviewGUIFrame = t;
            }

            MouseDragGUI(r);
            GUI.DrawTexture(r, previewTexture, ScaleMode.ScaleToFit, true, 0);
            EditorGUIUtility.AddCursorRect(r, MouseCursor.Pan);
        }

        private void MouseDragGUI(Rect r) {
            Event e = Event.current;

            switch (e.type) {
                case EventType.MouseDown:
                    Vector2 mousePos = e.mousePosition;
                    if (r.Contains(mousePos)) {
                        lastClicked = this; //NOTE: This fixes the MouseDrag always affecting the first preview (when multiple objects are selected)
                        lastMouseDownPos = mousePos;

                        //NOTE: We don't want to use the MouseDown event, because for some reason,
                        //that prevents us from receiving MouseDrag events from Unity in the SceneView after the initial MouseDown event.
                        //e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (!r.Contains(lastMouseDownPos) || lastClicked != this)
                        return;

                    if (e.button == 0 && !e.alt) {
                        openingSwivel = false;
                        int prevIndex = viewIndex;
                        float dt = GetSmallDeltaTime();

                        int maxView = ViewCount - 1;
                        if (rawViewIndex < 0 || rawViewIndex >= ViewCount)
                            rawViewIndex = SineWave(rawViewIndex, maxView);

                        rawViewIndex = Mathf.Clamp(rawViewIndex - dt * 8 * e.delta.x, 0, maxView);
                        viewIndex = (int) SineWave(rawViewIndex, maxView);

                        if (viewIndex != prevIndex)
                            SetView(viewIndex);

                        if (autoRotate)
                            AutoRotate = false;
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    lastClicked = null;
                    break;
            }
        }
#endif
    }
}
