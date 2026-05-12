using System;
using UnityEngine;
using LookingGlass.Toolkit;
using UnityEngine.Serialization;

namespace LookingGlass {
    /// <summary>
    /// Represents the settings that a <see cref="QuiltCapture"/> may use when recording, instead of using the values on its <see cref="HologramCamera"/> component.
    /// </summary>
    [Serializable]
    public struct QuiltCaptureOverrideSettings {
        [Tooltip("The quilt settings to use during recording.")]
        [FormerlySerializedAs("renderSettings")]
        public QuiltSettings quiltSettings;

        [Tooltip("The near clip factor to use during recording.")]
        public float nearClipFactor;

        public QuiltCaptureOverrideSettings(LKGDeviceType type) {
            quiltSettings = QuiltSettings.GetDefaultFor(type);
            nearClipFactor = HologramCamera.DefaultNearClipFactor;
        }

        public QuiltCaptureOverrideSettings(HologramCamera source) {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            quiltSettings = source.QuiltSettings;
            nearClipFactor = source.CameraProperties.NearClipFactor;  //TODO: We need to handle the case of TransformMode.Camera using nearClipPlane instead!
        }

        public bool Equals(QuiltCaptureOverrideSettings source) {
            if (quiltSettings.Equals(source.quiltSettings)
                && nearClipFactor == source.nearClipFactor)
                return true;
            return false;
        }
    }
}
