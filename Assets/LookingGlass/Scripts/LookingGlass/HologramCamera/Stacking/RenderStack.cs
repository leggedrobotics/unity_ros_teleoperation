using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LookingGlass.Toolkit;

namespace LookingGlass {
    /// <summary>
    /// Represents a sequence of rendering commands that will be mixed together, including hologramCamera realtime 3D renders, quilts, and generic render textures.
    /// </summary>
    [Serializable]
    public class RenderStack : IEnumerable<RenderStep> {
        [Tooltip("Sets the filter mode (Point, Bilinear, or Point & Lenticular AA) of the final quilt mix texture.")]
        [SerializeField] internal QuiltFilterMode filterMode = QuiltFilterMode.PointVirtualPixelAA;
        [Min(0)]
        [SerializeField] internal float antiAliasingStrength = 1;

        [Tooltip("Turns off aspect adjustment when set to true.\n\n" +
            "- If aspect adjustments are used, each quilt tile that is copied will be stretched or squashed based on its source render aspect.\n" +
            "- If bypassed, then quilt tile contents are copied with stretch to fit behavior into whatever texture rectangle they are drawn into.\n\n" +
            "Note that aspect adjustment currently result a performance hit, due to Graphics.Blit(...), which is more expensive than when it's turned off (we use Graphics.DrawTexture(...) in that case instead, which is faster).\n\n" +
            "Use aspect adjustment only when needed.")]
        [SerializeField] internal bool bypassAspectAdjustment = true;

        [Tooltip("The list of render commands to perform.")]
        [SerializeField] internal List<RenderStep> steps = new List<RenderStep>();

        private RenderTexture quiltMix;
        private Material alphaBlendMaterial;

        [NonSerialized] private RenderStep defaultStep;

        public event Action onQuiltChanged;
        public event Action onRendered;

        public QuiltFilterMode FilterMode {
            get { return filterMode; }
            set {
                filterMode = value;
                if (quiltMix != null)
                    quiltMix.filterMode = filterMode.GetUnityFilterMode();
            }
        }

        public float AntiAliasingStrength {
            get { return antiAliasingStrength; }
            set { antiAliasingStrength = value; }
        }

        public bool BypassAspectAdjustment {
            get { return bypassAspectAdjustment; }
            set { bypassAspectAdjustment = value; }
        }

        public int Count => steps.Count;
        public RenderStep this[int index] {
            get { return steps[index]; }
        }

        public RenderTexture QuiltMix => quiltMix;

        public void Add(RenderStep step) {
            if (step == null)
                throw new ArgumentNullException(nameof(step));
            steps.Add(step);
        }

        public void Insert(int index, RenderStep step) {
            if (step == null)
                throw new ArgumentNullException(nameof(step));
            steps.Insert(index, step);
        }

        public bool Remove(RenderStep step) => steps.Remove(step);
        public void RemoveAt(int index) => steps.RemoveAt(index);

        public IEnumerator<RenderStep> GetEnumerator() => steps.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<RenderStep>) this).GetEnumerator();

        public void Clear() => steps.Clear();

        public void ResetToDefault() {
            steps.Clear();
            steps.Add(new RenderStep(LookingGlass.RenderStep.Type.CurrentHologramCamera));
        }

        private bool SetupQuiltIfNeeded(HologramCamera hologramCamera) {
            QuiltSettings quiltSettings = hologramCamera.QuiltSettings;
            RenderTexture quilt = hologramCamera.QuiltTexture;
            FilterMode unityFilterMode = filterMode.GetUnityFilterMode();
            if (quiltMix == null || quiltMix.graphicsFormat != quilt.graphicsFormat || quiltMix.width != quiltSettings.quiltWidth || quiltMix.height != quiltSettings.quiltHeight) {
                if (quiltMix != null)
                    quiltMix.Release();
                quiltMix = new RenderTexture(quiltSettings.quiltWidth, quiltSettings.quiltHeight, 0, quilt.graphicsFormat);
                quiltMix.name = "Quilt Mix (" + quiltSettings.quiltWidth + "x" + quiltSettings.quiltHeight + ")";
                quiltMix.filterMode = unityFilterMode;
                QuiltMix.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm;
                try {
                    onQuiltChanged?.Invoke();
                } catch (Exception e) {
                    Debug.LogException(e);
                }
                return true;
            }
            if (quiltMix != null && quiltMix.filterMode != unityFilterMode)
                quiltMix.filterMode = unityFilterMode;
            return false;
        }

        public RenderTexture RenderToQuilt(HologramCamera hologramCamera) {
            try {
                SetupQuiltIfNeeded(hologramCamera);
                MultiViewRendering.Clear(quiltMix, CameraClearFlags.SolidColor, new Color(0, 0, 0, 1));

                if (steps.Count <= 0) {
                    if (defaultStep == null)
                        defaultStep = new RenderStep(LookingGlass.RenderStep.Type.CurrentHologramCamera);
                    RenderStep(defaultStep, hologramCamera, quiltMix);
                } else {
                    if (defaultStep != null)
                        defaultStep = null;

                    foreach (RenderStep step in steps)
                        if (step.IsEnabled)
                            RenderStep(step, hologramCamera, quiltMix);
                }
                try {
                    onRendered?.Invoke();
                } catch (Exception e) {
                    Debug.LogException(e);
                }
                return quiltMix;
            } finally {
                RenderTexture.active = null;
            }
        }

        private void RenderStep(RenderStep step, HologramCamera hologramCamera, RenderTexture mix) {
            QuiltSettings hologramCameraRenderSettings = hologramCamera.QuiltSettings;
            Camera postProcessCamera = step.PostProcessCamera;
            switch (step.RenderType) {
                case LookingGlass.RenderStep.Type.CurrentHologramCamera:
                    if (alphaBlendMaterial == null)
                        alphaBlendMaterial = new Material(Util.FindShader("LookingGlass/Alpha Blend"));

                    if (hologramCamera.Preview2D) {
                        hologramCamera.RenderPreview2D(false, true);
                        MultiViewRendering.CopyViewToAllQuiltTiles(hologramCameraRenderSettings, hologramCamera.Preview2DRT, mix, true);
                    } else {
                        hologramCamera.RenderQuiltLayer(false, false);
                        Graphics.Blit(hologramCamera.QuiltTexture, mix, alphaBlendMaterial);
                    }
                    break;
                case LookingGlass.RenderStep.Type.Quilt:
                    Texture quilt = step.QuiltTexture;
                    if (quilt != null) {
                        RenderTexture temp = RenderTexture.GetTemporary(quilt.width, quilt.height);
                        Graphics.Blit(quilt, temp);

                        int minViews = Mathf.Min(hologramCameraRenderSettings.tileCount, step.QuiltSettings.tileCount);

                        //TODO: Account for the quilt's renderAspect.
                        //Currently, setting it does nothing.
                        for (int v = 0; v < minViews; v++)
                            MultiViewRendering.CopyViewBetweenQuilts(step.QuiltSettings, v, temp, hologramCameraRenderSettings, v, mix, bypassAspectAdjustment);

                        if (postProcessCamera != null && postProcessCamera.gameObject.activeInHierarchy) {
                            RenderTexture tempDepthTex = CreateTemporaryBlankDepthTexture();
                            MultiViewRendering.RunPostProcess(hologramCamera, mix, tempDepthTex, postProcessCamera);
                            RenderTexture.ReleaseTemporary(tempDepthTex);
                        }
                        RenderTexture.ReleaseTemporary(temp);
                    }
                    break;
                case LookingGlass.RenderStep.Type.GenericTexture:
                    Texture texture = step.Texture;
                    if (texture != null) {
                        bool usedTemporary = false;
                        if (!(texture is RenderTexture renderTex)) {
                            usedTemporary = true;
                            renderTex = RenderTexture.GetTemporary(texture.width, texture.height);
                            Graphics.Blit(texture, renderTex);
                        }
                        MultiViewRendering.CopyViewToAllQuiltTiles(hologramCameraRenderSettings, renderTex, mix, true);

                        //TODO: Copy depth texture from camera's RenderTexture targetTexture, and use it for post-processing accurately instead of below with the blank depth texture:
                        //if (postProcessCamera != null && postProcessCamera.gameObject.activeInHierarchy) {
                        //    RenderTexture tempDepthTex = CreateTemporaryBlankDepthTexture();
                        //    MultiViewRendering.RunPostProcess(hologramCamera, mix, tempDepthTex, postProcessCamera);
                        //    RenderTexture.ReleaseTemporary(tempDepthTex);
                        //}
                        if (usedTemporary)
                            RenderTexture.ReleaseTemporary(renderTex);
                    }
                    break;
            }
        }

        private RenderTexture CreateTemporaryBlankDepthTexture() {
            RenderTexture depth = RenderTexture.GetTemporary(32, 32, 0, RenderTextureFormat.RFloat);
            MultiViewRendering.Clear(depth, CameraClearFlags.SolidColor, Color.clear);
            return depth;
        }
    }
}
