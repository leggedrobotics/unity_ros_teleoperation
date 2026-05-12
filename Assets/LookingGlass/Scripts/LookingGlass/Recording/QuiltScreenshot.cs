using System;

namespace LookingGlass {
    public static class QuiltScreenshot {
        public static QuiltCaptureOverrideSettings GetSettings(QuiltScreenshotPreset preset) {
            switch (preset) {
                case QuiltScreenshotPreset.LookingGlassGo: return QuiltRecordingSettings.GetSettings(QuiltRecordingPreset.LookingGlassGo).cameraOverrideSettings;
                case QuiltScreenshotPreset.LookingGlassPortrait: return QuiltRecordingSettings.GetSettings(QuiltRecordingPreset.LookingGlassPortrait).cameraOverrideSettings;
                case QuiltScreenshotPreset.LookingGlass16Landscape: return QuiltRecordingSettings.GetSettings(QuiltRecordingPreset.LookingGlass16Landscape).cameraOverrideSettings;
                case QuiltScreenshotPreset.LookingGlass16Portrait: return QuiltRecordingSettings.GetSettings(QuiltRecordingPreset.LookingGlass16Portrait).cameraOverrideSettings;
                case QuiltScreenshotPreset.LookingGlass27Landscape: return QuiltRecordingSettings.GetSettings(QuiltRecordingPreset.LookingGlass27Landscape).cameraOverrideSettings;
                case QuiltScreenshotPreset.LookingGlass27Portrait: return QuiltRecordingSettings.GetSettings(QuiltRecordingPreset.LookingGlass27Portrait).cameraOverrideSettings;
                case QuiltScreenshotPreset.LookingGlass32Landscape: return QuiltRecordingSettings.GetSettings(QuiltRecordingPreset.LookingGlass32Landscape).cameraOverrideSettings;
                case QuiltScreenshotPreset.LookingGlass32Portrait: return QuiltRecordingSettings.GetSettings(QuiltRecordingPreset.LookingGlass32Portrait).cameraOverrideSettings;
                case QuiltScreenshotPreset.LookingGlass65: return QuiltRecordingSettings.GetSettings(QuiltRecordingPreset.LookingGlass65).cameraOverrideSettings;
                case QuiltScreenshotPreset.LookingGlass16Gen2: return QuiltRecordingSettings.GetSettings(QuiltRecordingPreset.LookingGlass16Gen2).cameraOverrideSettings;
                case QuiltScreenshotPreset.LookingGlass32Gen2: return QuiltRecordingSettings.GetSettings(QuiltRecordingPreset.LookingGlass32Gen2).cameraOverrideSettings;
            }
            throw new NotSupportedException("Unsupported preset type: " + preset);
        }
    }
}
