using UnityEngine;
using UnityEngine.UI;

namespace LookingGlass.UI {
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public class RawImageUpdater : MonoBehaviour {
        [SerializeField] private HologramCamera hologramCamera;
        private RawImage rawImage;

        private HologramCamera HologramCamera {
            get { return hologramCamera; }
            set {
                if (hologramCamera != null)
                    UnregisterEvents();

                hologramCamera = value;
                UpdateQuilt();
                UpdateTargetDisplay();

                if (hologramCamera != null) {
                    UnregisterEvents();
                    RegisterEvents();
                }
            }
        }

        #region Unity Messages
        private void Reset() {
            HologramCamera = GetComponentInParent<HologramCamera>();
        }

        private void Awake() {
            rawImage = GetComponent<RawImage>();
        }

        private void OnEnable() {
            HologramCamera = hologramCamera;
        }
        #endregion

        private void RegisterEvents() {
            hologramCamera.onTargetDisplayChanged += UpdateTargetDisplay;
            hologramCamera.onQuiltChanged += UpdateQuilt;
        }

        private void UnregisterEvents() {
            hologramCamera.onTargetDisplayChanged -= UpdateTargetDisplay;
            hologramCamera.onQuiltChanged -= UpdateQuilt;
        }

        private void UpdateQuilt() {
            rawImage.texture = hologramCamera.QuiltTexture;
            rawImage.material = hologramCamera.LenticularMaterial;
        }

        private void UpdateTargetDisplay() {
            Canvas c = rawImage.GetComponentInParent<Canvas>();
            c.targetDisplay = (int) hologramCamera.TargetDisplay;
        }
    }
}