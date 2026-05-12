#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace LookingGlass {
    [ExecuteAlways]
    [InitializeOnLoad]
    public class URPPackageDetector : MonoBehaviour {
        [Serializable]
        private class PostDomainReloadData {
            public string progressName;
            public bool foundURPPreviously;
            public bool addedURPSuccessfully;
            public bool setURPAsset;
        }

        private const string URPPackageName = "com.unity.render-pipelines.universal";
        private const string AskToDetectURPKey = "askToDetectURP";
        private const string AskToUseURPAssetKey = "askToUseURPAsset";
        private const string IsInProgressKey = "isInURPDetectionProgress";
        private const string URPAssetGUID = "319c0cf4cf9259d45b1b49e042b596dd";
        private const string PersistentFilePath = "Temp/URP Package Detector.json";
        private const int StepCount = 6;

        static URPPackageDetector() {
            if (File.Exists(PersistentFilePath)) {
                //NOTE: We DO want to block here, so that we deterministically delete/consume the file BEFORE any MonoBehaviours attempt
                //To see if the file still exists.
                //aka, we don't want to use async file reading here:
                string jsonText = File.ReadAllText(PersistentFilePath);
                File.Delete(PersistentFilePath);

                EditorApplication.delayCall += () => {
                    _ = ContinueMethod(JsonUtility.FromJson<PostDomainReloadData>(jsonText));
                };
            }

            EditorApplication.quitting -= CleanupBeforeQuit;
            EditorApplication.quitting += CleanupBeforeQuit;
        }

        private static void CleanupBeforeQuit() {
            PlayerPrefs.DeleteKey(IsInProgressKey);
        }

        [SerializeField] private bool ask = true;

        private bool prevAskValue = true;

        private bool IsInProgress => PlayerPrefs.HasKey(IsInProgressKey);

        #region Unity Messages
        private void Reset() {
            gameObject.tag = "EditorOnly";
        }

        private void OnValidate() {
            if (ask != prevAskValue) {
                prevAskValue = ask;
                PlayerPrefs.SetInt(AskToDetectURPKey, ask ? 1 : 0);
            }
        }

        private void Awake() {
            if (PlayerPrefs.HasKey(AskToDetectURPKey))
                ask = prevAskValue = PlayerPrefs.GetInt(AskToDetectURPKey) != 0;
        }

        private void OnEnable() {
            if (PlayerPrefs.GetInt(AskToDetectURPKey, 1) != 0)
                _ = CheckIfUserNeedsURPInstalled();
        }

        private void OnDestroy() {
            PlayerPrefs.DeleteKey(IsInProgressKey);
        }
        #endregion

        private async Task CheckIfUserNeedsURPInstalled() {
            if (IsInProgress)
                return;
            PlayerPrefs.SetInt(IsInProgressKey, 1);

            int progressId = -1;
            string progressName = "URP Package Detection";
            try {
                await Task.Delay(500);
                progressId = Progress.Start(progressName);

                ListRequest listRequest = Client.List(false, true);
                Task<PackageCollection> listTask = UPMUtility.AwaitRequest(listRequest);
                Progress.Report(progressId, 1, StepCount, "Listing packages");
                PackageCollection list = await listTask;

                //WARNING: For some reason, our Progress object was auto-failing here, even though everything was successful...
                //No idea why.
                //Adding some async delay at the start of this method BEFORE calling Progress.Start(...) solved the issue.
                //It may have to do with assembly reload events or something?
                Assert.AreEqual(Progress.GetStatus(progressId), Progress.Status.Running);
                Progress.Report(progressId, 2, StepCount, "Determining if URP is installed");

                bool foundURP = false;
                foreach (PackageInfo package in list) {
                    if (package.name == URPPackageName) {
                        foundURP = true;
                        break;
                    }
                }
                await Task.Delay(500);

                int choice = 0;
                if (foundURP) {
                    PlayerPrefs.DeleteKey(IsInProgressKey);
                    Progress.Finish(progressId);
                    GameViewExtensions.RepaintAllViewsImmediately();
                } else {
                    choice = EditorUtility.DisplayDialogComplex("Install URP?",
                        "This scene requires URP (Universal Render Pipeline) to be installed.\n\n" +
                        "Would you like to install this from Unity's package registry?", "Yes", "No", "Don't Show Again");
                    switch (choice) {
                        case 0:
                            bool setURPAsset = false;
                            if (!foundURP) {
                                if (!PlayerPrefs.HasKey(AskToUseURPAssetKey)) {
                                    RenderPipelineAsset existingRPAsset =
#if UNITY_6000_0_OR_NEWER
                                        GraphicsSettings.defaultRenderPipeline;
#else
                                        GraphicsSettings.renderPipelineAsset;
#endif

                                    string existingRPAssetPath;
                                    if (existingRPAsset == null || string.IsNullOrEmpty(existingRPAssetPath = AssetDatabase.GetAssetPath(existingRPAsset)) || AssetDatabase.AssetPathToGUID(existingRPAssetPath) != URPAssetGUID)
                                        setURPAsset = EditorUtility.DisplayDialog("Use URP Asset?", "Would you also like to use LookingGlass's default URP asset?", "Yes", "No");
                                    PlayerPrefs.SetInt(AskToUseURPAssetKey, setURPAsset ? 1 : 0);
                                } else {
                                    setURPAsset = PlayerPrefs.GetInt(AskToUseURPAssetKey) != 0;
                                }
                            }

                            //NOTE: Running off the main thread now, so we don't get interrupted and cut off by the domain reload..
                            //The domain reload occurs AFTER the AddRequest below completes, and is hard to coordinate with..

                            //The only other idea I had that could work is using [InitializeOnLoad] with a static constructor, but
                            //It would make the code much more fragmented and ugly.

                            AddRequest addRequest = null;
                            if (!foundURP) {
                                EditorApplication.LockReloadAssemblies();
                                try {
                                    Progress.Report(progressId, 3, StepCount, "Downloading URP");
                                    Task<PackageInfo> addTask = null;
                                    addRequest = Client.Add(URPPackageName);
                                    addTask = UPMUtility.AwaitRequest(addRequest);

                                    await Task.Delay(3000);

                                    do {
                                        Progress.Report(progressId, 4, StepCount, "Installing URP");
                                        await Task.Delay(1000);
                                        //NOTE: Keep reporting each second, so Unity doesn't think we stop responding (which it does after 5sec with no new reports)
                                    } while (!addTask.IsCompleted);
                                    PackageInfo result = addTask.Result;

                                    if (addRequest.Status == StatusCode.Success) {
                                        string folderPath = Path.GetDirectoryName(PersistentFilePath);
                                        Directory.CreateDirectory(folderPath);

                                        //NOTE: Can't use File.WriteAllTextAsync(...) because that requires .NET Standard 2.1. Soon (TM)
                                        using (StreamWriter writer = File.CreateText(PersistentFilePath))
                                            await writer.WriteLineAsync(JsonUtility.ToJson(new PostDomainReloadData() {
                                                progressName = progressName,
                                                foundURPPreviously = foundURP,
                                                addedURPSuccessfully = addRequest.Status == StatusCode.Success,
                                                setURPAsset = setURPAsset
                                            }));
                                    }
                                    //NOTE: For some reason, Unity fails our task somewhere along the
                                    //2 domain reload processes that I've been observing after this line.
                                    //So, let's not confuse the user -- let's make the progress just disappear for a short bit and then re-appear (in the continue method below),
                                    //rather than showing an error and then quickly hiding it (which would be bad UX).
                                    Progress.Finish(progressId);
                                } finally {
                                    EditorApplication.UnlockReloadAssemblies();
                                }
                                //WARNING: --- ASSEMBLY RELOAD ---
                            };
                            break;
                        case 1:
                            PlayerPrefs.DeleteKey(IsInProgressKey);
                            Progress.Finish(progressId);
                            break;
                        case 2:
                            PlayerPrefs.DeleteKey(IsInProgressKey);
                            Progress.Finish(progressId);
                            PlayerPrefs.SetInt(AskToDetectURPKey, 0);
                            prevAskValue = ask = false;
                            break;
                    }
                }
                GameViewExtensions.RepaintAllViewsImmediately();
                Assert.AreNotEqual(Progress.Status.Failed, Progress.GetStatus(progressId));
            } catch (Exception e) {
                Debug.LogException(e);
                Progress.Finish(progressId, Progress.Status.Failed);
            } finally {
                PlayerPrefs.DeleteKey(AskToUseURPAssetKey);
            }
        }

        private static async Task ContinueMethod(PostDomainReloadData data) {
            int progressId = -1;
            try {
                foreach (Progress.Item progressItem in Progress.EnumerateItems()) {
                    if (progressItem.name == data.progressName) {
                        progressId = progressItem.id;
                        break;
                    }
                }
                if (progressId < 0) {
                    progressId = Progress.Start(data.progressName);
                    Progress.Report(progressId, 5, StepCount, "Finished installing URP!");
                    GameViewExtensions.RepaintAllViewsImmediately();
                }
                await Task.Delay(250);

                if (data.addedURPSuccessfully) {
                    if (data.setURPAsset) {
                        Progress.Report(progressId, 6, StepCount, "Setting render pipeline");
                        await Task.Delay(250);

                        GraphicsSettings.defaultRenderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(URPAssetGUID));
                        Debug.Log("Set the project's graphics settings to use LookingGlass's URP render pipeline asset!");
                        AssetDatabase.SaveAssets();

                        if (HologramCamera.Count > 0) {
                            RuntimePreviewInternal.ToggleEditorPreview();
                            await Task.Delay(500);

                            HologramCamera[] hologramCameras = HologramCamera.All.ToArray();
                            foreach (HologramCamera h in hologramCameras)
                                h.gameObject.SetActive(false);

                            await Task.Delay(30);

                            foreach (HologramCamera h in hologramCameras)
                                h.gameObject.SetActive(true);
                            RuntimePreviewInternal.ToggleEditorPreview();
                            foreach (EditorWindow gameView in GameViewExtensions.FindAllGameViews())
                                gameView.SetMinGameViewZoom();
                        }
                    }

                    await Task.Delay(250);
                    Progress.Finish(progressId);
                    GameViewExtensions.RepaintAllViewsImmediately();
                }
            } catch (Exception e) {
                Debug.LogException(e);
                Progress.Finish(progressId, Progress.Status.Failed);
            } finally {
                PlayerPrefs.DeleteKey(IsInProgressKey);
            }
        }
    }
}
#endif
