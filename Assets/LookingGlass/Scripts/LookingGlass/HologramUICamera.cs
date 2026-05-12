using UnityEngine;
using UnityEngine.UI;
using LookingGlass.Toolkit;

namespace LookingGlass {
    /// <summary>
    /// Allows you to render UI directly into the <see cref="HologramCamera"/>'s final quilt mix, so that it may be anti-aliased for better viewing on Looking Glass displays.
    /// </summary>
    [ExecuteAlways]
    public class HologramUICamera : MonoBehaviour {
        [Tooltip("The " + nameof(HologramUICamera) + " to render UI into.\n\n" +
            "If set to null, the default instance will be found and set automatically via script.")]
        [SerializeField] private HologramCamera hologramCamera;

        [Tooltip("The canvas to render into the quilt hologram.")]
        [SerializeField] private Canvas canvas;

        [Tooltip("The camera that renders UI into a target " + nameof(RenderTexture) + ". The target texture will be created automatically by this script at runtime.")]
        [SerializeField] private Camera renderCamera;

        private CanvasScaler canvasScaler;
        private RenderTexture renderTexture;
        private RenderStep alteredStep;

        public HologramCamera HologramCamera {
            get { return hologramCamera; }
            set { hologramCamera = value; }
        }

        public Canvas Canvas => canvas;
        public Camera RenderCamera => renderCamera;

        public bool ValidateNonNullHologramCamera() {
            //NOTE: We assume that the HologramCamera's scripting execution order causes its Awake/OnEnable to happen first:
            if (hologramCamera == null)
                hologramCamera = HologramCamera.Instance;

            if (hologramCamera == null) {
                Debug.LogWarning(
                    "Looking Glass UI Camera couldn't find a Hologram Camera in the scene!"
                );
                return false;
            }
            return true;
        }

        private void OnEnable() {
            canvasScaler = (canvas != null) ? canvas.GetComponent<CanvasScaler>() : null;
        }

        private void OnDisable() {
            if (hologramCamera != null) {
                alteredStep.Texture = null;
                alteredStep = null;
            }
            if (renderTexture != null) {
                renderTexture.Release();
                renderTexture = null;
            }
        }

        private void Update() {
            if (!ValidateNonNullHologramCamera())
                return;

            CheckToUpdateRenderTexture();
            CheckToUpdateRenderStack();

            //NOTE: We render the camera into a RenderTexture the size of one quilt tile,
            //  but MUST ALSO set the aspect to match the renderAspect to look right for our display!
            renderCamera.targetTexture = renderTexture;
            renderCamera.aspect = hologramCamera.QuiltSettings.renderAspect;

            if (canvasScaler != null && canvasScaler.isActiveAndEnabled) {
                renderCamera.orthographicSize = Mathf.Lerp(
                    canvasScaler.referenceResolution.x / renderCamera.aspect,
                    canvasScaler.referenceResolution.y,
                    canvasScaler.matchWidthOrHeight
                ) / 2;
            } else {
                renderCamera.orthographicSize = canvas.pixelRect.height / 2;
            }
        }

        private void CheckToUpdateRenderTexture() {
            bool shouldExist = ValidateNonNullHologramCamera();
            QuiltSettings quiltSettings = hologramCamera.QuiltSettings;

            int newWidth = quiltSettings.TileWidth;
            int newHeight = quiltSettings.TileHeight;
            if (renderTexture == null || renderTexture.width != newWidth || renderTexture.height != newHeight) {
                if (renderTexture != null)
                    renderTexture.Release();

                renderTexture = new RenderTexture(newWidth, newHeight, 32);
                renderTexture.name = "Hologram UI";
            }
        }

        private void CheckToUpdateRenderStack() {
            RenderStack renderStack = hologramCamera.RenderStack;
            int count = renderStack.Count;
            int last = count - 1;

            if (count <= 0 || renderStack[last].RenderType != RenderStep.Type.GenericTexture) {
                Debug.Log("Please add a render step at the end of the " + nameof(HologramCamera) +
                    "'s render stack, and set it to " + nameof(RenderStep.Type.GenericTexture) + " to use the UI.");
                return;
            }

            alteredStep = renderStack[last];
            alteredStep.Texture = renderTexture;
        }
    }
}
