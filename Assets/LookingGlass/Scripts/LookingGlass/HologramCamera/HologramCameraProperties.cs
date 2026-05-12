using System;
using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// A set of fields that correspond to fields on a Unity <see cref="HologramCamera"/>, with some extra hologramCamera fields.
    /// </summary>
    [Serializable]
    public class HologramCameraProperties : PropertyGroup {
        public TransformMode TransformMode {
            get { return hologramCamera.transformMode; }
            set {
                if (TransformMode == value)
                    return;

                hologramCamera.transformMode = value;
                switch (value) {
                    case TransformMode.Volume:
                        hologramCamera.transform.position += hologramCamera.transform.forward * FocalPlane;
                        CalculateCameraClipFactors(Size, NearClipPlane, FarClipPlane, FocalPlane, 1, out float nearClipFactor, out float farClipFactor);
                        NearClipFactor = nearClipFactor;
                        FarClipFactor = farClipFactor;
                        break;
                    case TransformMode.Camera:
                        hologramCamera.transform.position -= hologramCamera.transform.forward * FocalPlane;
                        CalculateCameraClipPlanes(Size, NearClipFactor, FarClipFactor, FocalPlane, 1, out float nearClipPlane, out float farClipPlane);
                        NearClipPlane = nearClipPlane;
                        FarClipPlane = farClipPlane;
                        break;
                }
            }
        }

        public float NearClipFactor {
            get { return hologramCamera.nearClipFactor; }
            set { hologramCamera.nearClipFactor = Mathf.Clamp(value, 0.01f, 5); }
        }

        public float FarClipFactor {
            get { return hologramCamera.farClipFactor; }
            set { hologramCamera.farClipFactor = Mathf.Clamp(value, 0.01f, 40); }
        }

        public float NearClipPlane {
            get { return hologramCamera.nearClipPlane; }
            set {
                hologramCamera.nearClipPlane = Mathf.Max(value, 0.01f);
                hologramCamera.farClipPlane = Mathf.Max(hologramCamera.farClipPlane, hologramCamera.nearClipPlane + 0.01f);
            }
        }

        public float FarClipPlane {
            get { return hologramCamera.farClipPlane; }
            set { hologramCamera.farClipPlane = Mathf.Max(value, NearClipPlane + 0.01f); }
        }

        public float FocalPlane {
            get { return hologramCamera.focalPlane; }
            set {
                switch (TransformMode) {
                    case TransformMode.Volume:
                        throw new InvalidOperationException("The focal plane is auto-calculated in " + nameof(TransformMode.Volume) + " mode, and cannot be set!");
                    case TransformMode.Camera:
                        hologramCamera.focalPlane = Mathf.Clamp(value, NearClipPlane, FarClipPlane);
                        break;
                    default:
                        throw new NotSupportedException("Unsupported " + nameof(LookingGlass.TransformMode) + ": " + TransformMode + "!");
                }
            }
        }

        public float Size {
            get { return hologramCamera.size; }
            set { hologramCamera.size = Mathf.Max(0.01f, value); }
        }

        public SizeMode SizeMode {
            get { return hologramCamera.sizeMode; }
            set { hologramCamera.sizeMode = value; }
        }

        public CameraClearFlags ClearFlags {
            get { return hologramCamera.clearFlags; }
            set {
                hologramCamera.clearFlags = value;
                hologramCamera.clearDirtyFlag = true;
            }
        }

        public Color BackgroundColor {
            get { return hologramCamera.background; }
            set { hologramCamera.background = value; }
        }

        public LayerMask CullingMask {
            get { return hologramCamera.cullingMask; }
            set { hologramCamera.cullingMask = value; }
        }

        public float FieldOfView {
            get { return hologramCamera.fov; }
            set { hologramCamera.fov = Mathf.Clamp(value, 5, 90); }
        }

        public float Depth {
            get { return hologramCamera.depth; }
            set { hologramCamera.depth = Mathf.Clamp(value, -100, 100); }
        }

        public RenderingPath RenderingPath {
            get { return hologramCamera.renderingPath; }
            set { hologramCamera.renderingPath = value; }
        }

        public bool UseOcclusionCulling {
            get { return hologramCamera.useOcclusionCulling; }
            set { hologramCamera.useOcclusionCulling = value; }
        }

        public bool AllowHDR {
            get { return hologramCamera.allowHDR; }
            set { hologramCamera.allowHDR = value; }
        }

        public bool AllowMSAA {
            get { return hologramCamera.allowMSAA; }
            set { hologramCamera.allowMSAA = value; }
        }

        public bool AllowDynamicResolution {
            get { return hologramCamera.allowDynamicResolution; }
            set { hologramCamera.allowDynamicResolution = value; }
        }

        public bool UseFrustumTarget {
            get { return hologramCamera.useFrustumTarget; }
            set { hologramCamera.useFrustumTarget = value; }
        }

        public Transform FrustumTarget {
            get { return hologramCamera.frustumTarget; }
            set { hologramCamera.frustumTarget = value; }
        }

        public float CenterOffset {
            get { return hologramCamera.centerOffset; }
            set { hologramCamera.centerOffset = Mathf.Clamp01(value); }
        }

        public float HorizontalFrustumOffset {
            get { return hologramCamera.horizontalFrustumOffset; }
            set { hologramCamera.horizontalFrustumOffset = Mathf.Clamp(value, -90, 90); }
        }

        public float VerticalFrustumOffset {
            get { return hologramCamera.verticalFrustumOffset; }
            set { hologramCamera.verticalFrustumOffset = Mathf.Clamp(value, -90, 90); }
        }

        public float Depthiness {
            get { return hologramCamera.depthiness; }
            set { hologramCamera.depthiness = Mathf.Clamp(value, 0.01f, 3); }
        }

        protected internal override void OnValidate() {
            Depth = Depth;
            Size = Size;
            NearClipPlane = NearClipPlane;
            FarClipPlane = FarClipPlane;
            if (TransformMode == TransformMode.Camera)
                FocalPlane = FocalPlane;

            UpdateAutomaticFields();
        }

        internal void UpdateAutomaticFields() {
            switch (TransformMode) {
                case TransformMode.Volume:
                    hologramCamera.focalPlane = CalculateFocalPlane();
                    break;
                case TransformMode.Camera:
                    hologramCamera.size = FocalPlane * Mathf.Tan(FieldOfView / 2 * Mathf.Deg2Rad);
                    break;
            }
        }

        public void CalculateCameraClipFactors(float size, float nearClipPlane, float farClipPlane, float focalPlane, float depthiness,
            out float nearClipFactor, out float farClipFactor) {
            
            nearClipFactor = (focalPlane - nearClipPlane) / (size * depthiness);
            farClipFactor = (farClipPlane - focalPlane) / (size * depthiness);
        }

        public void CalculateCameraClipPlanes(float size, float nearClipFactor, float farClipFactor, float focalPlane, float depthiness,
            out float nearClipPlane, out float farClipPlane) {
            nearClipPlane = Mathf.Max(focalPlane - size * nearClipFactor * depthiness, 0.01f);
            farClipPlane = Mathf.Max(focalPlane + size * farClipFactor * depthiness, nearClipPlane);
        }

        public void GetCameraClipPlanes(out float nearClipPlane, out float farClipPlane) => GetCameraClipPlanes(Depthiness, out nearClipPlane, out farClipPlane);
        private void GetCameraClipPlanes(float depthiness, out float nearClipPlane, out float farClipPlane) {
            switch (TransformMode) {
                case TransformMode.Volume:
                    nearClipPlane = Mathf.Max(FocalPlane - Size * NearClipFactor * depthiness, 0.01f);
                    farClipPlane = Mathf.Max(FocalPlane + Size * FarClipFactor * depthiness, nearClipPlane);
                    break;
                case TransformMode.Camera:
                    nearClipPlane = Mathf.Max(NearClipPlane - (FocalPlane - NearClipPlane) * Depthiness, 0.01f);
                    farClipPlane = Mathf.Max(FarClipPlane + (FarClipPlane - FocalPlane) * Depthiness, nearClipPlane);
                    break;
                default:
                    throw new NotSupportedException("Unsupported " + nameof(LookingGlass.TransformMode) + ": " + TransformMode + "!");
            }
        }

        public void CalculateCameraClipPlanesWithoutDepthiness(out float nearClipPlane, out float farClipPlane) =>
            GetCameraClipPlanes(1, out nearClipPlane, out farClipPlane);

        private float CalculateFocalPlane() {
            if (UseFrustumTarget)
                return Mathf.Abs(FrustumTarget.localPosition.z);
            return Size / Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad);
        }

        public void CopyFromCamera(Camera camera) {
            TransformMode = TransformMode.Camera;
            NearClipPlane = camera.nearClipPlane;
            FarClipPlane = camera.farClipPlane;
            FocalPlane = Mathf.Min(10, Mathf.Lerp(camera.nearClipPlane, camera.farClipPlane, 0.35f));
            FieldOfView = camera.fieldOfView;
            Depth = camera.depth;
            //NOTE: We also don't set ALL the properties currently!
            //Should we?

            //NOTE: We're fighting with our own logic here, we might want to
            //customize the setter for TransformMode somehow to NOT maintain the Volume
            hologramCamera.transform.position = camera.transform.position;
        }

        public void SetCamera(Camera camera) {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            Transform transform = hologramCamera.transform;
            float size = Size;
            float distance = hologramCamera.CameraProperties.FocalPlane;

            switch (SizeMode) {
                case SizeMode.SizeSetsScale:
                    transform.localScale = new Vector3(size, size, size);
                    break;
                case SizeMode.ScaleSetsSize:
                    Vector3 localScale = transform.localScale;
                    float largestScaleComponent = Mathf.Max(localScale.x, localScale.y, localScale.z);
                    size = Size = largestScaleComponent;
                    break;
            }

            camera.orthographic = false;
            if (UseFrustumTarget)
                camera.fieldOfView = 2 * Mathf.Atan(Mathf.Abs(size / FrustumTarget.localPosition.z)) * Mathf.Rad2Deg;
            else
                camera.fieldOfView = FieldOfView;

            float aspect = hologramCamera.QuiltSettings.renderAspect;
            camera.aspect = aspect;

            GetCameraClipPlanes(Depthiness, out float nearClipPlane, out float farClipPlane);
            camera.nearClipPlane = nearClipPlane;
            camera.farClipPlane = farClipPlane;

            camera.clearFlags = ClearFlags;

            //TODO: Does this work properly in HDRP?
            //(I had seen somewhere that we need to change a field on the HDAdditionalCameraData component)
            //camera.backgroundColor = BackgroundColor;

            camera.depth = Depth;

            camera.cullingMask = CullingMask;
            camera.renderingPath = RenderingPath;
            camera.useOcclusionCulling = UseOcclusionCulling;
            camera.allowHDR = AllowHDR;
            camera.allowMSAA = AllowMSAA;
            camera.allowDynamicResolution = AllowDynamicResolution;
        }
    }
}
