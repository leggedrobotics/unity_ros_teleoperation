using System;
using UnityEngine;
using FFmpegOut;
using LookingGlass.Toolkit;

namespace LookingGlass {
    [Serializable]
    public struct QuiltRecordingSettings {
        internal static QuiltRecordingSettings Default => new QuiltRecordingSettings() {
            codec = FFmpegPreset.VP8Default,
            frameRate = 30,
            compression = 20,
            targetBitrateInMegabits = 90,
            cameraOverrideSettings = new QuiltCaptureOverrideSettings(LKGDeviceType.GoPortrait)
        };

        public FFmpegPreset codec;
        public float frameRate;
        public int compression;
        public int targetBitrateInMegabits;
        public QuiltCaptureOverrideSettings cameraOverrideSettings;

        public QuiltRecordingSettings(FFmpegPreset preset, float frameRate, int compression, int targetBitrateInMegabits, QuiltCaptureOverrideSettings renderSettings) {
            this.codec = preset;
            this.frameRate = frameRate;
            this.compression = compression;
            this.targetBitrateInMegabits = targetBitrateInMegabits;
            this.cameraOverrideSettings = renderSettings;
        }

        public bool Equals(QuiltRecordingSettings source) {
            if (codec == source.codec &&
                frameRate == source.frameRate &&
                compression == source.compression &&
                targetBitrateInMegabits == source.targetBitrateInMegabits &&
                cameraOverrideSettings.Equals(source.cameraOverrideSettings))
                return true;
            return false;
        }

        private static readonly QuiltRecordingSettings[] PresetSettings = new QuiltRecordingSettings[] {
            new QuiltRecordingSettings(FFmpegPreset.VP8Default, 30, 20, 90, new QuiltCaptureOverrideSettings(LKGDeviceType.GoPortrait)),
            new QuiltRecordingSettings(FFmpegPreset.VP8Default, 30, 20, 60, new QuiltCaptureOverrideSettings(LKGDeviceType.PortraitGen2)),
            new QuiltRecordingSettings(FFmpegPreset.VP9Default, 30, 20, 90, new QuiltCaptureOverrideSettings(LKGDeviceType._16inLandscapeGen3)),
            new QuiltRecordingSettings(FFmpegPreset.VP9Default, 30, 20, 90, new QuiltCaptureOverrideSettings(LKGDeviceType._16inPortraitGen3)),
            new QuiltRecordingSettings(FFmpegPreset.VP9Default, 30, 20, 90, new QuiltCaptureOverrideSettings(LKGDeviceType._27inLandscapeGen3)),
            new QuiltRecordingSettings(FFmpegPreset.VP9Default, 30, 20, 90, new QuiltCaptureOverrideSettings(LKGDeviceType._27inPortraitGen3)),
            new QuiltRecordingSettings(FFmpegPreset.VP9Default, 30, 20, 150, new QuiltCaptureOverrideSettings(LKGDeviceType._32inLandscapeGen3)),
            new QuiltRecordingSettings(FFmpegPreset.VP9Default, 30, 20, 150, new QuiltCaptureOverrideSettings(LKGDeviceType._32inPortraitGen3)),
            new QuiltRecordingSettings(FFmpegPreset.VP9Default, 30, 20, 150, new QuiltCaptureOverrideSettings(LKGDeviceType._65inLandscapeGen2)),
            new QuiltRecordingSettings(FFmpegPreset.VP8Default, 30, 20, 90, new QuiltCaptureOverrideSettings(LKGDeviceType._16inGen2)),
            new QuiltRecordingSettings(FFmpegPreset.VP9Default, 30, 20, 150, new QuiltCaptureOverrideSettings(LKGDeviceType._32inGen2))
        };

        public static QuiltRecordingSettings GetSettings(QuiltRecordingPreset preset) {
            if (preset == QuiltRecordingPreset.Custom)
                return PresetSettings[0];

            int index = (int) preset;
            return PresetSettings[index];
        }
    }
}
