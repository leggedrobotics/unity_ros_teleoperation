//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace LookingGlass {
    /// <summary>
    /// <para>A <see cref="MonoBehaviour"/> component that blits quilts and 2D previews to the screen using <see cref="OnRenderImage(RenderTexture, RenderTexture)"/>.</para>
    /// <para>NOTE: This only works in the built-in render pipeline.</para>
    /// <para>See also: <seealso cref="RenderPipelineUtil.GetRenderPipelineType"/></para>
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(900)]
    public class MultiViewRenderer : MonoBehaviour {
        //Using statics here to make the LookingGlass instance available immediately during Awake and OnEnable:
        private static HologramCamera next;
        internal static HologramCamera Next {
            get { return next; }
            set { next = value; }
        }

        private const string FinalCanvasName = "Final Canvas";
        private const string FinalRawImageName = "Final Raw Image";

        [SerializeField] private HologramCamera hologramCamera;

        private Canvas finalCanvas;
        private RawImage finalRawImage;

        public event Action<RenderTexture> onAfterScreenBlit;

        #region Unity Messages
        private void Awake() {
            if (hologramCamera == null) {
                hologramCamera = next;
                next = null;
            }
        }

        private void OnEnable() {
            if (RenderPipelineUtil.IsHDRP || RenderPipelineUtil.IsURP) {
                CreateUI();
                StartCoroutine(EventCallerOnSRP());
                hologramCamera.onTargetDisplayChanged += UpdateCanvasTargetDisplay;
            }
        }

        private void OnDisable() {
            if (hologramCamera != null)
                hologramCamera.onTargetDisplayChanged += UpdateCanvasTargetDisplay;
            if (finalCanvas != null)
                DestroyImmediate(finalCanvas.gameObject);
            if (finalRawImage != null)
                DestroyImmediate(finalRawImage.gameObject);
        }

        private void LateUpdate() {
            if (RenderPipelineUtil.IsHDRP || RenderPipelineUtil.IsURP) {
                RenderTexture quiltMix = hologramCamera.RenderStack.RenderToQuilt(hologramCamera);

                if (finalRawImage != null) {
                    finalRawImage.texture = quiltMix;
                    finalRawImage.material = hologramCamera.LenticularMaterial;
                }
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination) {
            Assert.IsTrue(RenderPipelineUtil.IsBuiltIn, nameof(OnRenderImage) + " is assumed to only be called in the built-in render pipeline!");

            RenderTexture quiltMix = hologramCamera.RenderStack.RenderToQuilt(hologramCamera);
            if (onAfterScreenBlit != null) {
                InvokeScreenBlitEventAndCopy(quiltMix, destination, true);
            } else {
                Graphics.Blit(quiltMix, destination, hologramCamera.LenticularMaterial);
            }
        }
        #endregion

        private void UpdateCanvasTargetDisplay() {
            if (finalCanvas != null)
                finalCanvas.targetDisplay = (int) hologramCamera.TargetDisplay;
        }

        private IEnumerator EventCallerOnSRP() {
            YieldInstruction wait = new WaitForEndOfFrame();

            while (isActiveAndEnabled && (RenderPipelineUtil.IsHDRP || RenderPipelineUtil.IsURP)) {
                yield return wait;

                if (onAfterScreenBlit != null) {
                    RenderTexture quiltMix = finalRawImage.texture as RenderTexture;
                    if (quiltMix != null)
                        InvokeScreenBlitEventAndCopy(quiltMix, null, false);
                }
            }
        }

        private void CreateUI() {
            finalCanvas = new GameObject(FinalCanvasName).AddComponent<Canvas>();
            finalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            UpdateCanvasTargetDisplay();

            //NOTE: Do NOT set the parent, because we don't want pressing F on the keyboard
            //on the LookingGlass object to make the camera go to a huge scale!
            //finalCanvas.transform.SetParent(transform, false);

            finalRawImage = new GameObject(FinalRawImageName).AddComponent<RawImage>();
            finalRawImage.transform.SetParent(finalCanvas.transform);
            finalRawImage.rectTransform.anchorMin = Vector2.zero;
            finalRawImage.rectTransform.anchorMax = Vector2.one;
            finalRawImage.rectTransform.anchoredPosition = Vector2.zero;
            finalRawImage.rectTransform.sizeDelta = Vector2.zero;

            int layerIndex = LayerMask.NameToLayer("TransparentFX");
            finalCanvas.gameObject.layer = layerIndex;
            finalRawImage.gameObject.layer = layerIndex;

            hologramCamera.SetHideFlagsOnObject(finalCanvas);
            hologramCamera.SetHideFlagsOnObject(finalRawImage);
        }

        private void InvokeScreenBlitEventAndCopy(RenderTexture quiltMix, RenderTexture destination, bool blitToDestination) {
            RenderTexture screenTexture = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            try {
                Graphics.Blit(quiltMix, screenTexture, hologramCamera.LenticularMaterial);
                Graphics.Blit(screenTexture, destination);

                onAfterScreenBlit(screenTexture);
            } finally {
                RenderTexture.ReleaseTemporary(screenTexture);
            }
        }
    }
}
