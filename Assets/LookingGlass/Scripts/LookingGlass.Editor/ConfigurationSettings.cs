using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LookingGlass.Toolkit;

namespace LookingGlass.Editor {
    [InitializeOnLoad]
    public class ConfigurationSettings : EditorWindow {
        private class Setting {
            public string label;
            public bool on;
            public bool isQualitySetting;

            public Action settingChange;

            public Setting(string label, bool on, bool isQualitySetting, Action settingChange) {
                this.label = label;
                this.on = on;
                this.isQualitySetting = isQualitySetting;
                this.settingChange = settingChange;
            }
        }

        private List<Setting> settings = new List<Setting> {
            new Setting("Shadow Distance: 5000", true, true,
                () => { QualitySettings.shadowDistance = 5000f; }
            ),
            new Setting("Shadow Projection: Close Fit", true, true,
                () => { QualitySettings.shadowProjection = ShadowProjection.CloseFit; }
            ),
            new Setting("Splash Screen: off (pro/plus only)", true, false,
                () => { PlayerSettings.SplashScreen.show = false; }
            ),
            new Setting("Run in Background", true, false,
                () => { PlayerSettings.runInBackground = true; }
            ),
            // No display resolution dialog after 2019.3
            // On Windows, we use SetParams to implement auto display targetting so we don't need this to be enabled by default
            // Instead, we recommend disable it on Windows
#if !UNITY_2019_3_OR_NEWER

#if UNITY_EDITOR_WIN
            new Setting("Resolution Dialog: disabled", true, false,
                () => { PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Disabled; }
            ),
#elif UNITY_EDITOR_OSX
            new Setting("Resolution Dialog: enabled", true, false,
                () => { PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Enabled; }
            ),
#endif
    
#endif
       
            new Setting("Use Fullscreen Window", true, false,
#if UNITY_2018_1_OR_NEWER
                () => { PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; }
#else
                () => { PlayerSettings.defaultIsFullScreen = true; }
#endif
            ),
#if UNITY_EDITOR_WIN
            new Setting("Window Build: 64 bit", true, false,
                ()=>{ EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);}
            ),
#endif
            new Setting("Add customize size to Game view and default to Portrait size", true, false,
                AddAllCustomSizesAndSetDefault
            )
        };

        private static void AddAllCustomSizesAndSetDefault() {
            ILKGDeviceTemplateSystem system = LookingGlass.Toolkit.ServiceLocator.Instance.GetSystem<ILKGDeviceTemplateSystem>();
            if (system == null) {
                Debug.LogError("Failed to find " + nameof(ILKGDeviceTemplateSystem) + " to gather all the LKG device settings from.");
                return;
            }
            foreach (LKGDeviceTemplate settings in system.GetAllTemplates()) {
                string deviceName = settings.calibration.GetDeviceType().GetNiceName();
                int width = settings.calibration.screenW;
                int height = settings.calibration.screenH;

                bool sizeExists = LookingGlass.GameViewExtensions.FindSize(width, height, deviceName, out int index);
                if (!sizeExists)
                    LookingGlass.GameViewExtensions.AddCustomSize(width, height, deviceName);
            }

            LKGDeviceTemplate defaultSettings = system.GetTemplate(LKGDeviceType.PortraitGen2);
            EditorWindow[] gameViews = LookingGlass.GameViewExtensions.FindAllGameViews();
            foreach (EditorWindow gameView in gameViews) {
                LookingGlass.GameViewExtensions.SetGameViewResolution(gameView, defaultSettings.calibration.screenW, defaultSettings.calibration.screenH, defaultSettings.calibration.GetDeviceType().GetNiceName());
            }
        }

        private const string editorPrefName = "LookingGlass_1.2.0_";

        static ConfigurationSettings() {
            EditorApplication.update += CheckIfPromptedYet;
        }

        private static void CheckIfPromptedYet() {
            if (!EditorPrefs.GetBool(editorPrefName + PlayerSettings.productName, false)) {
                Init();
            }
            EditorApplication.update -= CheckIfPromptedYet;
        }

        [MenuItem("LookingGlass/Setup Player Settings", priority = 50)]
        private static void Init() {
            ConfigurationSettings window = EditorWindow.GetWindow<ConfigurationSettings>();
            window.Show();
        }

        private void OnEnable() {
            titleContent = new GUIContent("LookingGlass Settings");
            float spacing = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            Vector2 size = new Vector2(360, 130 + spacing * settings.Count);
            maxSize = size;
            minSize = size;
        }

        private void OnGUI() {
            EditorGUILayout.HelpBox(
                "It is recommended you change the following project settings " +
                "to ensure the best performance for your LookingGlass application",
                MessageType.Warning
            );

            EditorGUILayout.LabelField("Select which options to change:", EditorStyles.miniLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (Setting s in settings) {
                EditorGUILayout.BeginHorizontal();
                s.on = EditorGUILayout.ToggleLeft(s.label, s.on);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = EditorGUIUtility.isProSkin ? Color.green : Color.Lerp(Color.green, Color.white, 0.5f);
            if (GUILayout.Button("Apply Changes")) {
                string[] qualitySettingNames = QualitySettings.names;
                int currentQuality = QualitySettings.GetQualityLevel();

                for (int i = 0; i < qualitySettingNames.Length; i++) {
                    QualitySettings.SetQualityLevel(i, false);
                    foreach (Setting s in settings) {
                        if (s.isQualitySetting) {
                            s.settingChange();
                        }
                    }
                }

                foreach (Setting s in settings) {
                    if (!s.isQualitySetting) {
                        s.settingChange();
                    }
                }

                QualitySettings.SetQualityLevel(currentQuality, true);
                EditorPrefs.SetBool(editorPrefName + PlayerSettings.productName, true);
                Debug.Log("[LookingGlass] Applied! By default, this popup will no longer appear, but you can access it by clicking LookingGlass/Setup Player Settings");
                Close();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = EditorGUIUtility.isProSkin ? Color.yellow : Color.Lerp(Color.yellow, Color.white, 0.5f);

            if (GUILayout.Button("Never display this popup again")) {
                EditorPrefs.SetBool(editorPrefName + PlayerSettings.productName, true);
                //REVIEW: [CRT-4039] Probably should be updated instructions?
                Debug.Log("[LookingGlass] Player Settings popup hidden--to show again, click LookingGlass → Setup Player Settings in the Unity top menu bar.");
                Close();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }
    }
}
