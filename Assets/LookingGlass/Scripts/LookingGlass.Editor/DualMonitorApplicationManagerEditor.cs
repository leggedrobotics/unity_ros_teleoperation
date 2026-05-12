// Dual Monitor Application is only supported on 2018 or later
#if UNITY_2018_4_OR_NEWER || UNITY_2019_1_OR_NEWER

// imported packages below
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using LookingGlass.DualMonitorApplication;

using Debug = UnityEngine.Debug;

namespace LookingGlass.Editor.DualMonitorApplication
{
    [HelpURL("https://look.glass/unitydocs")]
    [CustomEditor(typeof(DualMonitorApplicationManager))]
    public class DualMonitorApplicationManagerEditor : UnityEditor.Editor
    {
        [Serializable]
        public struct CustomBuildResults
        {
            public bool success;
            public BuildReport lkgBuildReport;
            public BuildReport extendedUIBuildReport;
        }

        private const string proBuildDirPrefsName = "_PROBUILDDIR";

        private static string PrefsBuildKey => Application.productName + proBuildDirPrefsName;

        public override void OnInspectorGUI()
        {
            if (
                Application.platform != RuntimePlatform.OSXEditor
                && Application.platform != RuntimePlatform.WindowsEditor
            )
            {
                EditorGUILayout.HelpBox(
                    "Dual Monitor Application Build is Windows and macOS Only.",
                    MessageType.Error
                );
            }

            // setup
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Open or create the windowed UI scene",
                EditorStyles.wordWrappedLabel
            );
            if (GUILayout.Button("Setup/Open Scenes"))
            {
                SetupScenes();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Build for the Dual Monitor Application. "
                    + "This is a special build command that creates two executables that automatically work for the monitor and the Looking Glass. "
                    + "(normal builds won't work without extensive manual configuration, attempting this is not recommended)",
                EditorStyles.wordWrappedLabel
            );
            GUI.color = Color.Lerp(Color.white, Color.green, 0.2f);
            if (GUILayout.Button("Build (Dual Monitor Application)"))
            {
                BuildProScenesInteractive(false);
            }
            GUI.color = Color.white;

            // disabling for now because it does not work
            // EditorGUILayout.Space();
            // EditorGUILayout.LabelField(
            //     "Same as above, but will build and run",
            //     EditorStyles.wordWrappedLabel
            // );
            // if (GUILayout.Button("Build and Run (Dual Monitor Application)"))
            // {
            //     BuildProScenesInteractive(true);
            // }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Open the folder containing the Dual Monitor Application build",
                EditorStyles.wordWrappedLabel
            );
            if (GUILayout.Button("Open Builds Folder"))
            {
                OpenBuildsFolder();
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }

        private static string GetPrefsBuildFolderPath()
        {
            return EditorPrefs.GetString(PrefsBuildKey, Path.GetFullPath("."));
        }

        private static void SaveBuildDirToPrefs(string folderPath)
        {
            EditorPrefs.SetString(PrefsBuildKey, folderPath);
        }

        public static void SetupScenes()
        {
            // first check if the scene is already open
            if (EditorSceneManager.sceneCount > 1)
            {
                // multiple scenes are open, check if the second one is an extendedUI scene
                if (
                    EditorSceneManager
                        .GetSceneAt(1)
                        .name.Contains(DualMonitorApplicationManager.extendedUIString)
                )
                {
                    Debug.Log("extendedUI scene open already");
                    return;
                }
                // multiple scenes are open, but not extended UI scene
                Debug.Log(
                    "secondary scene is open, but is not an extendedUI scene. please close extra scenes and try again."
                );
                return;
            }
            else
            {
                // if only one scene is open
                var activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.SaveOpenScenes();
                Debug.Log("active scene saved!");

                // first see if this scene is already the extended UI scene
                string secondSceneName = "";
                string newPath = "";
                if (activeScene.name.Contains(DualMonitorApplicationManager.extendedUIString))
                {
                    // if the active scene is already open, try to load the regular scene
                    secondSceneName = activeScene.name.Substring(
                        0,
                        activeScene.name.IndexOf(DualMonitorApplicationManager.extendedUIString)
                    );
                    newPath = activeScene.path;
                    newPath =
                        newPath.Substring(
                            0,
                            newPath.LastIndexOf(DualMonitorApplicationManager.extendedUIString)
                        ) + ".unity";
                    // Debug.Log(secondSceneName);
                    // Debug.Log(newPath);
                }
                else
                {
                    // try to load _extendedUI version of that scene instead
                    secondSceneName =
                        activeScene.name + DualMonitorApplicationManager.extendedUIString;
                    newPath = activeScene.path;
                    newPath = newPath.Insert(
                        newPath.LastIndexOf(".unity"),
                        DualMonitorApplicationManager.extendedUIString
                    );
                }

                bool loadedSceneSuccessfully = TryLoadingScene(newPath);
                if (loadedSceneSuccessfully)
                {
                    Debug.Log("Found existing complementary scene, loading...");
                }
                else
                {
                    Scene extendedScene = EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Additive
                    );
                    // extendedScene.name = secondSceneName;

                    EditorSceneManager.SaveScene(extendedScene, newPath);

                    Debug.Log("Unable to find existing complementary scene, creating it...");
                }

                // set active scene to lkg one, just to fix lighting
                Scene lkgScene = EditorSceneManager.GetActiveScene();
                for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    if (
                        !EditorSceneManager
                            .GetSceneAt(i)
                            .name.Contains(DualMonitorApplicationManager.extendedUIString)
                    )
                    {
                        lkgScene = EditorSceneManager.GetSceneAt(i);
                    }
                }
                EditorSceneManager.SetActiveScene(lkgScene);
            }
        }

        public static bool TryLoadingScene(string path, bool additive = true)
        {
            try
            {
                EditorSceneManager.OpenScene(
                    path,
                    additive ? OpenSceneMode.Additive : OpenSceneMode.Single
                );
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
            return true;
        }

        public static void BuildProScenesInteractive(bool buildAndRun)
        {
            // first let user select directory to save to
            string buildFolderPath = GetPrefsBuildFolderPath();

            // if it's not build and run, or if the path is still the default, open the dialog
            // or even do so if it is build and run but there isn't an existing build path
            if (
                !buildAndRun
                || buildFolderPath == Path.GetFullPath(".")
                || (buildAndRun && !Directory.Exists(buildFolderPath))
            )
            {
                string openFolderPath = GetPrefsBuildFolderPath();
                if (!Directory.Exists(openFolderPath))
                    openFolderPath = Application.dataPath.Replace("/Assets", "");
                buildFolderPath = EditorUtility.SaveFolderPanel(
                    "Choose Build Directory",
                    openFolderPath,
                    ""
                );
                if (string.IsNullOrEmpty(buildFolderPath))
                    return;
            }

            bool isFolderEmpty =
                Directory.GetDirectories(buildFolderPath).Length <= 0
                && Directory.GetFiles(buildFolderPath).Length <= 0;
            if (!isFolderEmpty)
            {
                // if only just building, give people a chance to cancel overwrite
                if (!buildAndRun)
                {
                    if (
                        !EditorUtility.DisplayDialog(
                            "Dual Monitor Application Build",
                            "Warning: Build directory is not empty. Overwrite?",
                            "Overwrite",
                            "Cancel"
                        )
                    )
                    {
                        return;
                    }
                }

                // if they confirmed, or if it's build and run, just delete and go ahead
                try
                {
                    Directory.Delete(buildFolderPath, true);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    Debug.LogWarning(
                        "[LookingGlass] Couldn't overwrite existing build. Make sure build is not running and try again"
                    );
                }
            }

            IEnumerable<string> scenesFromBuildSettings = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path);
#if UNITY_EDITOR_WIN
            string buildFilePath = buildFolderPath + "/" + Application.productName + ".exe";
#elif UNITY_EDITOR_OSX
            string buildFilePath = buildFolderPath + "/" + Application.productName + ".app";
#elif UNITY_EDITOR_LINUX
            string buildFilePath = buildFolderPath + "/" + Application.productName + ".x86_64";
#endif

            CustomBuildResults results = BuildProScenes(
                buildAndRun,
                buildFilePath,
                scenesFromBuildSettings
            );
            if (results.success)
            {
                // if the builds succeeded, record the directory to prefs
                SaveBuildDirToPrefs(buildFolderPath);
                if (!buildAndRun)
                {
                    OpenBuildsFolder();
                }
                else
                {
                    //WARNING: Sasha was experiencing a MissingReferenceException for results.extendedUIBuildReport being destroyed by this point.
                    //I haven't been getting this error.
                    ProcessStartInfo processStartInfo = new ProcessStartInfo(
                        results.extendedUIBuildReport.summary.outputPath
                    );
                    Process.Start(processStartInfo);
                }
            }
        }

        public static CustomBuildResults BuildProScenes(
            bool buildAndRun,
            string buildFilePath,
            IEnumerable<string> scenes
        )
        {
            if (PlayerSettings.runInBackground == false)
            {
                PlayerSettings.runInBackground = true;
                Debug.Log(
                    "NOTE: Set "
                        + nameof(PlayerSettings)
                        + "."
                        + nameof(PlayerSettings.runInBackground)
                        + " to true."
                );
            }

            //Obsolete API, though Unity may re-introduce it in the future:
            //if (PlayerSettings.displayResolutionDialog == ResolutionDialogSetting.Enabled) {
            //    PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.HiddenByDefault;
            //    Debug.Log("NOTE: Set " + nameof(PlayerSettings) + "." + nameof(PlayerSettings.displayResolutionDialog) + " to " + ResolutionDialogSetting.HiddenByDefault);
            //}

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            List<string> sceneNamesLKG = new List<string>();
            List<string> sceneNamesExtendedUI = new List<string>();
            foreach (string scenePath in scenes)
            {
                if (scenePath.Contains(DualMonitorApplicationManager.extendedUIString))
                {
                    sceneNamesExtendedUI.Add(scenePath);
                }
                else
                {
                    sceneNamesLKG.Add(scenePath);
                }
            }

            CustomBuildResults results = new CustomBuildResults();
            if (sceneNamesExtendedUI.Count == 0)
            {
                Debug.LogError(
                    "No \""
                        + DualMonitorApplicationManager.extendedUIString
                        + "\" scenes found! Please add the extendedUI scenes to build settings (File -> Build Settings)."
                );
                return results;
            }

            BuildReport report;
            BuildSummary summary;

            // first build the extendedUI scenes and make that an exe
            buildPlayerOptions.scenes = sceneNamesExtendedUI.ToArray();
            buildPlayerOptions.locationPathName = buildFilePath;
#if UNITY_EDITOR_WIN
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
#elif UNITY_EDITOR_OSX
            buildPlayerOptions.target = BuildTarget.StandaloneOSX;
#endif
            buildPlayerOptions.options = BuildOptions.None;

            var productName = PlayerSettings.productName;
            var applicationIdentifier = PlayerSettings.applicationIdentifier;
            try {
                PlayerSettings.productName += DualMonitorApplicationManager.extendedUIString;
                PlayerSettings.applicationIdentifier += DualMonitorApplicationManager.extendedUIString;
#if DMA_DEV_EXTENDEDUI
                buildPlayerOptions.options |= (BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport);
#endif
                report = BuildPipeline.BuildPlayer(buildPlayerOptions);
#if DMA_DEV_EXTENDEDUI
                buildPlayerOptions.options &= ~(BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport);
#endif

                summary = report.summary;
                results.extendedUIBuildReport = report;
                
                switch (summary.result)
                {
                    case BuildResult.Succeeded:
                        Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
                        break;
                    case BuildResult.Failed:
                        Debug.LogError("Build failed");
                        return results;
                }
            } catch (Exception e) {
                Debug.LogError(e);
            }
            PlayerSettings.productName = productName;
            PlayerSettings.applicationIdentifier = applicationIdentifier;


            if (sceneNamesLKG.Count == 0)
            {
                Debug.Log("No LKG scenes found! Please add the LKG scenes to build settings.");
            }
            else
            {
                string folderPath = Path.GetDirectoryName(buildFilePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(buildFilePath);

                // first build the extendedUI scenes and make that an exe
                buildPlayerOptions.scenes = sceneNamesLKG.ToArray();
#if UNITY_EDITOR_WIN
                buildPlayerOptions.locationPathName = folderPath + "/" + fileNameWithoutExtension + "_Data/StreamingAssets/" +
                    DualMonitorApplicationManager.lkgDisplayString + ".exe";
#elif UNITY_EDITOR_OSX
                buildPlayerOptions.locationPathName = folderPath + "/" + fileNameWithoutExtension + ".app/Contents/Resources/Data/StreamingAssets/" +
                    DualMonitorApplicationManager.lkgDisplayString + ".app";
#endif

#if DMA_DEV
                buildPlayerOptions.options |= (BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport);
#endif
                report = BuildPipeline.BuildPlayer(buildPlayerOptions);
#if DMA_DEV
                buildPlayerOptions.options &= ~(BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport);
#endif

                summary = report.summary;
                results.lkgBuildReport = report;

                switch (summary.result)
                {
                    case BuildResult.Succeeded:
                        results.success = true;
                        Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
                        CleanupPostProcessorBuild.OnPostProcessBuild(buildPlayerOptions.target, buildPlayerOptions.locationPathName);
                        break;
                    case BuildResult.Failed:
                        Debug.LogError("Build failed");
                        return results;
                }
            }
            return results;
        }

        public static void OpenBuildsFolder()
        {
            EditorUtility.RevealInFinder(GetPrefsBuildFolderPath());
        }

        public static bool IsSupportedTarget(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                    return true;
                default:
                    return false;
            }
        }
    }
}

#endif
