using System;
using UnityEngine;
using LookingGlass.Toolkit;

namespace LookingGlass {
    /// <summary>
    /// Contains several options, useful in the inspector, for debugging a <see cref="HologramCamera"/> component.
    /// </summary>
    [Serializable]
    public class HologramCameraDebugging : PropertyGroup {
        [NonSerialized] private bool wasShowingObjects = false;
        [NonSerialized] private float prevOnlyShowView;

        internal event Action onShowAllObjectsChanged;

        public bool ShowAllObjects {
            get { return hologramCamera.showAllObjects; }
            set {
                wasShowingObjects = hologramCamera.showAllObjects = value;
                onShowAllObjectsChanged?.Invoke();
            }
        }

        public int OnlyShowView {
            get { return hologramCamera.onlyShowView; }
            set {
                int nextValue = Mathf.Clamp(value, -1, hologramCamera.QuiltSettings.tileCount - 1);
                prevOnlyShowView = nextValue;
                hologramCamera.onlyShowView = nextValue;
            }
        }

        public bool OnlyRenderOneView {
            get { return hologramCamera.onlyRenderOneView; }
            set { hologramCamera.onlyRenderOneView = value; }
        }

        public bool FallbackCameraTargetTexture {
            get { return hologramCamera.fallbackCameraTargetTexture; }
            set { hologramCamera.fallbackCameraTargetTexture = value; }
        }

        public ManualCalibrationMode ManualCalibrationMode {
            get { return hologramCamera.manualCalibrationMode; }
            set {
                hologramCamera.manualCalibrationMode = value;
                hologramCamera.UpdateCalibration();
            }
        }
        public TextAsset CalibrationTextAsset {
            get { return hologramCamera.calibrationTextAsset; }
            set {
                hologramCamera.calibrationTextAsset = value;
                if (ManualCalibrationMode == ManualCalibrationMode.UseCalibrationTextAsset)
                    hologramCamera.UpdateCalibration();
            }
        }
        public Calibration ManualCalibration {
            get { return hologramCamera.manualCalibration; }
            set {
                hologramCamera.manualCalibration = value;
                if (ManualCalibrationMode == ManualCalibrationMode.UseManualSettings)
                    hologramCamera.UpdateCalibration();
            }
        }

        public Shader OverrideLenticularShader {
            get { return hologramCamera.overrideLenticularShader; }
            set {
                hologramCamera.overrideLenticularShader = value;
                MultiViewRendering.CreateLenticularMaterial(ref hologramCamera.lenticularMaterial, value);
            }
        }

        protected override void OnInitialize() {
            hologramCamera.onQuiltSettingsChanged += () => {
                OnlyShowView = OnlyShowView;
            };
        }

        protected internal override void OnValidate() {
            if (ShowAllObjects != wasShowingObjects)
                ShowAllObjects = ShowAllObjects;

            if (OnlyShowView != prevOnlyShowView)
                OnlyShowView = OnlyShowView;
        }

        public void UseManualCalibration(Calibration manualCalibration) {
            hologramCamera.manualCalibration = manualCalibration;
            hologramCamera.manualCalibrationMode = ManualCalibrationMode.UseManualSettings;
            hologramCamera.UpdateCalibration();
        }
    }
}
