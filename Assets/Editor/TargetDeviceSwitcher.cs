#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RSL.Editor
{
    public static class TargetDeviceSwitcher
    {
        private const string ManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
        private const string OpenXRSettingsPath = "Assets/XR/Settings/OpenXR Package Settings.asset";

        [MenuItem("XR/Switch Target Device/Switch to Galaxy XR (Android XR)", false, 1)]
        public static void SwitchToGalaxyXR()
        {
            SetDefineSymbols(addDefine: "GALAXY_XR", removeDefine: "META_QUEST");
            ConfigureOpenXRForGalaxyXR();
            UpdateManifestForGalaxyXR();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=green>[XR Switcher]</color> Successfully switched target profile to <b>Galaxy XR</b> (Scripting define: GALAXY_XR).");
        }

        [MenuItem("XR/Switch Target Device/Switch to Meta Quest", false, 2)]
        public static void SwitchToMetaQuest()
        {
            SetDefineSymbols(addDefine: "META_QUEST", removeDefine: "GALAXY_XR");
            ConfigureOpenXRForMetaQuest();
            UpdateManifestForMetaQuest();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=cyan>[XR Switcher]</color> Successfully switched target profile to <b>Meta Quest</b> (Scripting define: META_QUEST).");
        }

        private static void SetDefineSymbols(string addDefine, string removeDefine)
        {
            NamedBuildTarget target = NamedBuildTarget.Android;
            PlayerSettings.GetScriptingDefineSymbols(target, out string[] currentDefines);
            
            System.Collections.Generic.List<string> definesList = new System.Collections.Generic.List<string>(currentDefines);
            if (definesList.Contains(removeDefine))
            {
                definesList.Remove(removeDefine);
            }
            if (!definesList.Contains(addDefine))
            {
                definesList.Add(addDefine);
            }

            PlayerSettings.SetScriptingDefineSymbols(target, definesList.ToArray());
        }

        private static void ConfigureOpenXRForGalaxyXR()
        {
            if (!File.Exists(OpenXRSettingsPath)) return;

            string content = File.ReadAllText(OpenXRSettingsPath);

            // Disable Meta-exclusive features on Android
            content = SetFeatureEnabled(content, "MetaQuestFeature Android", false);
            content = SetFeatureEnabled(content, "MetaXRFeature Android", false);
            content = SetFeatureEnabled(content, "OpenXRLifeCycleFeature Android", false);
            content = SetFeatureEnabled(content, "DisplayUtilitiesFeature Android", false);
            content = SetFeatureEnabled(content, "MetaXRFoveationFeature Android", false);
            content = SetFeatureEnabled(content, "MetaHandTrackingAim Android", false);

            // Enable standard Khronos controller and hand profiles
            content = SetFeatureEnabled(content, "KHRSimpleControllerProfile Android", true);
            content = SetFeatureEnabled(content, "HandInteractionProfile Android", true);

            File.WriteAllText(OpenXRSettingsPath, content);
        }

        private static void ConfigureOpenXRForMetaQuest()
        {
            if (!File.Exists(OpenXRSettingsPath)) return;

            string content = File.ReadAllText(OpenXRSettingsPath);

            // Enable Meta features on Android
            content = SetFeatureEnabled(content, "MetaQuestFeature Android", true);
            content = SetFeatureEnabled(content, "MetaXRFeature Android", true);
            content = SetFeatureEnabled(content, "OpenXRLifeCycleFeature Android", true);
            content = SetFeatureEnabled(content, "DisplayUtilitiesFeature Android", true);
            content = SetFeatureEnabled(content, "MetaXRFoveationFeature Android", true);
            content = SetFeatureEnabled(content, "MetaHandTrackingAim Android", true);

            // Enable standard Khronos controller and hand profiles
            content = SetFeatureEnabled(content, "KHRSimpleControllerProfile Android", true);
            content = SetFeatureEnabled(content, "HandInteractionProfile Android", true);

            File.WriteAllText(OpenXRSettingsPath, content);
        }

        private static string SetFeatureEnabled(string assetContent, string featureName, bool enabled)
        {
            int index = assetContent.IndexOf("m_Name: " + featureName);
            if (index == -1) return assetContent;

            int enabledIndex = assetContent.IndexOf("m_enabled: ", index);
            if (enabledIndex == -1 || enabledIndex > index + 200) return assetContent;

            int targetValIndex = enabledIndex + "m_enabled: ".Length;
            char currentVal = assetContent[targetValIndex];
            char newVal = enabled ? '1' : '0';

            if (currentVal != newVal)
            {
                assetContent = assetContent.Remove(targetValIndex, 1).Insert(targetValIndex, newVal.ToString());
            }

            return assetContent;
        }

        private static void UpdateManifestForGalaxyXR()
        {
            if (!File.Exists(ManifestPath)) return;

            string manifest = File.ReadAllText(ManifestPath);
            manifest = manifest.Replace("com.oculus.feature.PASSTHROUGH\" android:required=\"true\"", "com.oculus.feature.PASSTHROUGH\" android:required=\"false\"");
            manifest = manifest.Replace("oculus.software.overlay_keyboard\" android:required=\"true\"", "oculus.software.overlay_keyboard\" android:required=\"false\"");
            
            if (!manifest.Contains("org.khronos.openxr.intent.category.IMMERSIVE_HMD"))
            {
                manifest = manifest.Replace(
                    "</intent-filter>",
                    "</intent-filter>\n            <intent-filter>\n                <action android:name=\"android.intent.action.MAIN\"/>\n                <category android:name=\"org.khronos.openxr.intent.category.IMMERSIVE_HMD\"/>\n            </intent-filter>"
                );
            }

            File.WriteAllText(ManifestPath, manifest);
        }

        private static void UpdateManifestForMetaQuest()
        {
            if (!File.Exists(ManifestPath)) return;

            string manifest = File.ReadAllText(ManifestPath);
            manifest = manifest.Replace("com.oculus.feature.PASSTHROUGH\" android:required=\"true\"", "com.oculus.feature.PASSTHROUGH\" android:required=\"false\"");
            manifest = manifest.Replace("oculus.software.overlay_keyboard\" android:required=\"true\"", "oculus.software.overlay_keyboard\" android:required=\"false\"");
            File.WriteAllText(ManifestPath, manifest);
        }

        [MenuItem("XR/Build/Build Galaxy XR APK", false, 10)]
        public static void BuildGalaxyXRAPK()
        {
            SwitchToGalaxyXR();
            BuildAPK("builds/GalaxyXR_main.apk");
        }

        [MenuItem("XR/Build/Build Meta Quest APK", false, 11)]
        public static void BuildMetaQuestAPK()
        {
            SwitchToMetaQuest();
            BuildAPK("builds/MetaQuest_main.apk");
        }

        private static void BuildAPK(string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = new string[] { "Assets/Scenes/2dMain.unity" },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            Debug.Log($"Build completed with result: {report.summary.result} at {outputPath}");
        }
    }
}
#endif
