using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using LookingGlass.Toolkit;
using LookingGlass.Toolkit.Bridge;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

#if HAS_NEWTONSOFT_JSON
using Newtonsoft.Json.Linq;
#endif

using ToolkitDisplay = LookingGlass.Toolkit.Display;

namespace LookingGlass {
    /// <summary>
    /// A callback for receiving notifications when calibrations are refreshed from the underlying system.
    /// </summary>
    public delegate void CalibrationRefreshEvent();

    /// <summary>
    /// <para>Contains access to Looking Glass display calibration data for all currently-connected Looking Glass displays to the system.</para>
    /// <para>Note that this class is NOT thread safe, and is expected to only be accessed on the Unity main thread.</para>
    /// <remarks>
    /// <para>This class automatically connects to LKG Bridge on start (via static constructor, and <c>[InitializeOnLoad]</c> in the editor) and queries for connected displays, but you may manually call <see cref="Reconnect"/> or <see cref="ReloadCalibrations()"/>s yourself as well.</para>
    /// <para>
    /// Additionally, LKG Bridge is queried every few seconds to detect device changes.<br />
    /// LKG Bridge uses websockets events to notify listeners of display changes, but these currently don't work with WebSocketSharp and Unity for some reason currently.
    /// </para>
    /// </remarks>
    /// </summary>
    public static class LKGDisplaySystem {
        private static BridgeConnectionHTTP bridgeConnection;
        private static bool connected = false;

        private static ToolkitDisplay[] lkgDisplays;
        private static TaskCompletionSource<bool> connectTcs;
        private static List<Task<bool>> calibrationCalls = new();

        private static bool disposed = false;
        private static CoroutineRunner coroutineRunner;

        /// <summary>
        /// An event that fires off when Looking Glass Bridge has been successfully connected to, and the status of currently-connected Looking Glass displays has been updated/retrieved.
        /// </summary>
        public static event CalibrationRefreshEvent onReload;

        internal static BridgeConnectionHTTP BridgeConnection => bridgeConnection;

        /// <summary>
        /// Is the system currently connecting to LKG Bridge and/or awaiting the list of connected LKG displays and their calibrations?
        /// </summary>
        public static bool IsLoading {
            get {
                CleanCalibrationCalls();
                return (connectTcs != null && !connectTcs.Task.IsCompleted) || calibrationCalls.Count > 0;
            }
        }

        /// <summary>
        /// Gets a copy of the LKG display data in the array of  displays that are found to be currently connected.
        /// </summary>
        /// <param name="index">Note that this is an arbitrary index, and should NOT be relied on for persistence.</param>
        public static ToolkitDisplay Get(int index) => new ToolkitDisplay(lkgDisplays[index]);

        /// <summary>
        /// The number of currently-connected Looking Glass displays.
        /// </summary>
        public static int LKGDisplayCount => lkgDisplays?.Length ?? 0;

        /// <summary>
        /// A collection of all the currently-connected Looking Glass displays.
        /// </summary>
        public static IEnumerable<ToolkitDisplay> LKGDisplays {
            get {
                if (lkgDisplays != null)
                    foreach (ToolkitDisplay display in lkgDisplays)
                        yield return new ToolkitDisplay(display);
            }
        }

        /// <summary>
        /// If <see cref="Reconnect"> is currently waiting to connect to LKG Bridge,
        /// this returns a Task that will complete once that connection has been established.
        /// If there is no in-progress connection attempt at the moment, a completed Task is returned instead.
        /// </summary>
        public static Task WaitForConnected() {
            if (connectTcs != null)
                return connectTcs.Task;
            return Task.CompletedTask;
        }

        /// <summary>
        /// <para>Waits for all currently-pending requests resulting from <see cref="ReloadCalibrations()"/> at the moment of calling this method.</para>
        /// <para>Subsequent calls to <see cref="ReloadCalibrations()"/> after this method is called will not be awaited in this call.</para>
        /// </summary>
        public static Task WaitForCalibrations() {
            if (IsLoading) {
                CleanCalibrationCalls();
                if (connectTcs != null && !connectTcs.Task.IsCompleted) {
                    Task[] tasks = new Task[calibrationCalls.Count + 1];
                    for (int i = 0; i < calibrationCalls.Count; i++)
                        tasks[i] = calibrationCalls[i];
                    tasks[tasks.Length - 1] = connectTcs.Task;
                    return Task.WhenAll(tasks);
                }
                return Task.WhenAll(calibrationCalls);
            }
            return Task.CompletedTask;
        }

        private static void CleanCalibrationCalls() {
            calibrationCalls.RemoveAll(t => t == null || t.IsCompleted);
        }

#if UNITY_EDITOR
        [Serializable]
        private class EditorPreloadedData {
            //NOTE: Turn this off to debug/visualize how long it takes for us to actually get calibration data.
            public const bool Enabled = true;

            public static string PersistentPath => Path.Combine(Application.persistentDataPath, "Editor Preload Data.json").Replace('\\', '/');
            public ToolkitDisplay[] allLKGDisplays;

            public void SetAsCurrrent() {
                LKGDisplaySystem.lkgDisplays = allLKGDisplays;
            }

            public static EditorPreloadedData GetCurrent() {
                EditorPreloadedData data = new();
                data.allLKGDisplays = LKGDisplaySystem.lkgDisplays;
                return data;
            }

            public static void SaveCurrentState() {
                string json = JsonUtility.ToJson(EditorPreloadedData.GetCurrent(), true);
                File.WriteAllText(EditorPreloadedData.PersistentPath, json);
            }
        }

        private static void OnPlayModeStateChange(PlayModeStateChange state) {
            switch (state) {
                case PlayModeStateChange.ExitingEditMode:
                    EditorPreloadedData.SaveCurrentState();
                    break;
            }
        }
#endif

        internal static void InitializeSystem() {
#if UNITY_EDITOR
            if (EditorPreloadedData.Enabled) {
                string path = EditorPreloadedData.PersistentPath;
                if (File.Exists(path)) {
                    string json = File.ReadAllText(path);
                    EditorPreloadedData data = JsonUtility.FromJson<EditorPreloadedData>(json);
                    data.SetAsCurrrent();
                }
            }
            EditorApplication.playModeStateChanged += OnPlayModeStateChange;
#endif

            if (Application.isMobilePlatform)
                return;

            _ = Reconnect();
        }

        internal static void UninitializeSystem() {
            disposed = true;
            connected = false;
#if UNITY_EDITOR
            EditorSceneManager.sceneClosing -= OnEditorSceneClosing;
            EditorSceneManager.sceneOpened -= OnEditorSceneLoad;
#endif
            if (bridgeConnection != null) {
                bridgeConnection.Dispose();
                bridgeConnection = null;
            }
        }

        private static async Task<bool> ForceReconnect() {
            connected = false;
            if (bridgeConnection != null) {
                bridgeConnection.Dispose();
                bridgeConnection = null;
            }

            GetLKGBridgePorts(out int port, out int websocketPort);
            bridgeConnection = new BridgeConnectionHTTP(
                LookingGlass.Toolkit.ServiceLocator.Instance.GetSystem<LookingGlass.Toolkit.ILogger>(),
                LookingGlass.Toolkit.ServiceLocator.Instance.GetSystem<IHttpSender>(),
                "localhost",
                port,
                websocketPort
            );

            //NOTE: Use this if you want to debug timing!
            bridgeConnection.LoggingFlags = LKGSettingsSystem.Settings.loggingFlags;

            bool result = await Task.Run(() => {
                bool success = false;

                //WARNING: Not sure why this fails the first time upon opening the Unity project after a program restart...
                //      For now, let's just immediately retry. It works well for some reason:
                int maxRetries = 2;
                for (int i = 0; i < maxRetries; i++) {
                    success = bridgeConnection.Connect();
                    if (success)
                        break;
                }
                if (!success) {
                    Debug.LogError("Failed to connect to Looking Glass Bridge. Ensure LKG Bridge is running and retry by clicking LookingGlass → Retry LKG Bridge Connection.\n" +
                        "    LKG Bridge rest port: " + port + "\n" +
                        "    LKG Bridge websockets port: " + websocketPort);
                    return false;
                }
                connected = true;
                return true;
            });

            return result;
        }

        /// <summary>
        /// <para>
        /// Gets the file path of the Looking Glass Bridge <c>settings.json</c> file.
        /// See the Bridge C++ source code for the source of truth of where this logic is based off of.
        /// </para>
        /// <remarks>
        /// (Specifically, search for the <c>wstring Settings::default_settings_folder()</c> function.
        /// As of the date of writing this, they can be found here:
        /// <list type="bullet">
        /// <item><a href="https://github.com/Looking-Glass/LookingGlassBridge/blob/master/service_engine/windows/Settings.cpp">Windows</a></item>
        /// <item><a href="https://github.com/Looking-Glass/LookingGlassBridge/blob/master/service_engine/macintosh/Settings.cpp">MacOS</a></item>
        /// <item><a href="https://github.com/Looking-Glass/LookingGlassBridge/blob/master/service_engine/linux/Settings.cpp">Linux</a></item>
        /// </list>
        /// </remarks>
        /// </summary>
        /// <returns>The file path of LKG Bridge's <c>settings.json</c> file, or <c>null</c> if not on implemented for the current platform.</returns>
        private static string GetLKGBridgeSettingsFilePath() {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Looking Glass/Bridge/settings.json").Replace('\\', '/');
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Application Support/Looking Glass/Bridge/settings.json");
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".lgf/Bridge/settings.json");
#else
            Debug.LogWarning("Unsupported platform for retriev LKG Bridge's settings.json file for configurable ports!");
            return null;
#endif
        }

        private static void GetLKGBridgePorts(out int restPort, out int websocketPort) {
            restPort = BridgeConnectionHTTP.DefaultPort;
            websocketPort = BridgeConnectionHTTP.DefaultWebSocketPort;

#if !HAS_NEWTONSOFT_JSON
            return;
#else
            string filePath = GetLKGBridgeSettingsFilePath();
            if (filePath != null) {
                if (File.Exists(filePath)) {
                    try {
                        string json = File.ReadAllText(filePath);

                        try {
                            JObject j = JObject.Parse(File.ReadAllText(filePath));
                            int value = j["rest_port"].Value<int>();
                            restPort = value;
                        } catch { }

                        try {
                            JObject j = JObject.Parse(File.ReadAllText(filePath));
                            int value = j["websocket_port"].Value<int>();
                            websocketPort = value;
                        } catch { }
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                } else {
                    Debug.LogWarning("Unable to find LKG Bridge settings.json at file path: " + filePath);
                }
            }
#endif
        }

        /// <summary>
        /// Attempts to asynchronously connect to Looking Glass Bridge and query it for the Looking Glass displays that are currently connected to your system.
        /// </summary>
        /// <remarks>
        /// <para>Note that this is automatically called on start (via static constructor, and <c>[InitializeOnLoad]</c> in the editor), but you may manually call this method yourself as well.</para>
        /// <para>This includes a call to <see cref="ReloadCalibrations()"/>.</para>
        /// </remarks>
        public static async Task<bool> Reconnect() {
            try {
#if !HAS_NEWTONSOFT_JSON
                return false;
#else
                if ((connectTcs != null && !connectTcs.Task.IsCompleted))
                    throw new InvalidOperationException("The calibration system is already trying to connect! Check " + nameof(IsLoading) + " first before trying to reconnect.");
                connectTcs = new TaskCompletionSource<bool>();

                DateTime startTime = DateTime.Now;
                Debug.Log("Attempting to connect to LKG Bridge at " + startTime + "...");

                bool result = true;
                if (!connected)
                    result = await ForceReconnect();

                if (!result || disposed) {
                    connectTcs.SetResult(false);
                    goto End;
                }

                result = ((Func<bool>) (() => {
                    try {
                        string versionMessage = "Connected to LKG Bridge v";
                        string response = bridgeConnection.TrySendMessage("bridge_version", "{}");
                        if (response != null)
                            versionMessage += JObject.Parse(response)["payload"]["value"].Value<string>();
                        else
                            versionMessage += "???";
                        if (bridgeConnection != null) {
                            versionMessage += ", using API v";
                            response = bridgeConnection.TrySendMessage("api_version", "{}");
                            if (response != null)
                                versionMessage += JObject.Parse(response)["payload"]["value"].Value<string>();
                            else
                                versionMessage += "???";
                        }
                        versionMessage += " at " + DateTime.UtcNow.ToLongTimeString() + " UTC";
                        versionMessage += "\n    Using LKG Bridge " + (bridgeConnection.Port == BridgeConnectionHTTP.DefaultPort ? "" : "custom ") + "rest port: " + bridgeConnection.Port;
                        versionMessage += "\n    Using LKG Bridge " + (bridgeConnection.WebSocketPort == BridgeConnectionHTTP.DefaultWebSocketPort ? "" : "custom ") + "websocket port: " + bridgeConnection.WebSocketPort;
                        versionMessage += "\n";

                        Debug.Log(versionMessage);
                    } catch (Exception e) {
                        Debug.LogWarning("Failed to retrieve LKG Bridge version and API version.");
                        Debug.LogException(e);
                    }
                    return true;
                }))();

                if (!result || disposed)
                    goto End;
                result = bridgeConnection.TryEnterOrchestration();

                if (!result || disposed) {
                    Debug.LogWarning("Failed to enter LKG Bridge orchestration.");
                    connectTcs.SetResult(false);
                    goto End;
                }

                //[GSE-718]: Add support for LKG Bridge events, when they work!
                await Task.Run(() => {
                    bridgeConnection.AddListener("Monitor Connect", (string payload) => {
                        Debug.LogWarning("EVENT! " + payload);
                    });
                    bridgeConnection.AddListener("Monitor Disconnect", (string payload) => {
                        Debug.LogWarning("EVENT! " + payload);
                    });
                    bridgeConnection.AddListener("", (string payload) => {
                        Debug.LogWarning("EVENT! " + payload);
                    });
                });

                if (!await ReloadCalibrations()) {
                    connectTcs.SetResult(false);
                    result = false;
                    goto End;
                }
                connectTcs.SetResult(true);

                RestartEventLoop();

                End:
                if (!result) {
                    SetDisplays(new ToolkitDisplay[0]);
                }
                return result;
#endif
            } catch (Exception e) {
                Debug.LogException(e);
                return false;
            }
        }

        private static bool RestartEventLoop() {
            if (coroutineRunner != null)
                return false;

            coroutineRunner = new GameObject("Coroutines").AddComponent<CoroutineRunner>();
            if (Application.isPlaying) {
                GameObject.DontDestroyOnLoad(coroutineRunner.gameObject);
            } else {
#if UNITY_EDITOR
                EditorSceneManager.sceneClosing -= OnEditorSceneClosing;
                EditorSceneManager.sceneClosing += OnEditorSceneClosing;
                EditorSceneManager.sceneOpened -= OnEditorSceneLoad;
                EditorSceneManager.sceneOpened += OnEditorSceneLoad;
#endif
            }
            coroutineRunner.gameObject.hideFlags = HideFlags.HideAndDontSave;

            //NOTE: We don't use EditorApplication.update here because it has an unnecessarily MUCH higher update frequency than an [ExecuteAlways] MonoBehaviour.Update() or coroutine
            coroutineRunner.StartCoroutine(WaitForEventsCoroutine(() => {
                if (coroutineRunner != null)
                    GameObject.DestroyImmediate(coroutineRunner.gameObject);
            }));
            return true;
        }

        private static void OnEditorSceneClosing(Scene scene, bool removingScene) {
            //NOTE: Unity halts our running coroutines on the coroutineRunner in edit mode,
            //  but does NOT call OnDestroy for us if we use HideFlags.HideAndDontSave!
            //  So, we need to detect the scene closing, and destroy it ourselves.
            if (coroutineRunner != null)
                GameObject.DestroyImmediate(coroutineRunner.gameObject);
        }

#if UNITY_EDITOR
        private static void OnEditorSceneLoad(Scene scene, OpenSceneMode mode) {
            RestartEventLoop();
        }
#endif

        /// <summary>
        /// Asynchronously queries LKG Bridge for all currently connected LKG displays,
        /// updates this class' list of them (see <see cref="LKGDisplayCount"/> and <see cref="Get(int)"/>),
        /// and updates all <see cref="HologramCamera"/>s to ensure they are targetting the right, up-to-date displays.
        /// </summary>
        /// <returns><c>true</c> upon successful calibration reload from LKG Bridge and successful handling of the data internally, <c>false</c> otherwise.</returns>
        public static Task<bool> ReloadCalibrations() => ReloadCalibrations(false, LKGSettingsSystem.Settings.loggingFlags);
        private static Task<bool> ReloadCalibrations(bool allowSkip, BridgeLoggingFlags loggingFlags) {
            Task<bool> task = ReloadCalibrationsTask(allowSkip, loggingFlags);
            calibrationCalls.Add(task);
            return task;
        }

        //WARNING: Don't call this method overload, because we need to record every Task into the list of tasks so we know whether we're loading or not currently.

        /// <inheritdoc cref="ReloadCalibrations()"/>
        /// <param name="allowSkip">If the list of connected LKG displays hasn't changed, should we skip applying any changes to the <see cref="LKGDisplaySystem"/> and <see cref="HologramCamera"/>s?</param>
        private static async Task<bool> ReloadCalibrationsTask(bool allowSkip, BridgeLoggingFlags loggingFlags) {
            try {
                OverrideLKGDisplay[] overrideDisplays = LKGSettingsSystem.Settings.overrideLKGDisplays;
                ToolkitDisplay[] nextDisplays = new ToolkitDisplay[0];

#if HAS_NEWTONSOFT_JSON
                bool useBridge = overrideDisplays == null;
                if (overrideDisplays != null) {
                    try {
                        nextDisplays = new ToolkitDisplay[overrideDisplays.Length];
                        for (int i = 0; i < overrideDisplays.Length; i++) {
                            string calibrationJSON = await File.ReadAllTextAsync(overrideDisplays[i].calibration);
                            nextDisplays[i] = new ToolkitDisplay() {
                                calibration = Calibration.Parse(calibrationJSON),
                                defaultQuilt = overrideDisplays[i].defaultQuilt,
                                hardwareInfo = overrideDisplays[i].hardwareInfo
                            };
                        }
                    } catch (Exception e) {
                        useBridge = true;
                        Debug.LogError("An error occurred while parsing override LKG displays. Using LKG Bridge instead...");
                        Debug.LogException(e);
                    }
                } 
#else
                bool useBridge = true;
#endif

                if (useBridge) {
                    if (!await bridgeConnection.UpdateDevicesAsync(loggingFlags)) {
                        Debug.LogWarning("Failed to update LKG devices.");
                        goto End;
                    }

                    nextDisplays = bridgeConnection.GetLKGDisplays().ToArray();
                    if (allowSkip && !LKGDisplaysChanged(nextDisplays))
                        goto End;
                }

                SetDisplays(nextDisplays);
                return true;
            } catch (Exception e) {
                Debug.LogException(e);
            }
End:
            return false;
        }

        private static void SetDisplays(ToolkitDisplay[] displays) {
            lkgDisplays = displays;

            HologramCamera.UpdateAllCalibrations();
#if UNITY_EDITOR
            EditorPreloadedData.SaveCurrentState();
#endif
            onReload?.Invoke();
        }

        private static bool LKGDisplaysChanged(ToolkitDisplay[] nextDisplays) {
            bool CheckThatAllExist<T>(T[] from, T[] to, Func<T, T, bool> match) {
                if (from == null)
                    return true;
                if (to == null)
                    return from.Length <= 0;

                for (int i = 0; i < from.Length; i++) {
                    bool foundSame = false;
                    for (int j = 0; j < to.Length; j++) {
                        if (match(from[i], to[j])) {
                            foundSame = true;
                            break;
                        }
                    }
                    if (!foundSame)
                        return false;
                }
                return true;
            }

            return !CheckThatAllExist(lkgDisplays, nextDisplays, (lhs, rhs) => lhs.Equals(rhs))
                || !CheckThatAllExist(nextDisplays, lkgDisplays, (lhs, rhs) => lhs.Equals(rhs));
        }

        [ExecuteAlways]
        private class CoroutineRunner : MonoBehaviour { }

        /// <summary>
        /// <para> Waits a given amount of time in seconds, in a way that is supported both in playmode, AND in the editor without playmode.</para>
        /// <para>This is a helper method because <see cref="WaitForSeconds"/> is not supported in the Unity editor.</para>
        /// </summary>
        /// <param name="seconds">The amount of time in seconds to wait.</param>
        private static IEnumerator WaitFor(float seconds) {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                TaskCompletionSource<bool> tcs = new();
                Task.Run(() => {
                    Task.Delay((int) (1000 * seconds)).Wait();
                    EditorApplication.delayCall += () => {
                        tcs.SetResult(true);
                        EditorApplication.QueuePlayerLoopUpdate();
                    };
                });
                while (!tcs.Task.IsCompleted) {
                    yield return null;
                }
            } else {
#endif
                yield return new WaitForSeconds(seconds);
#if UNITY_EDITOR
            }
#endif
        }

        private static IEnumerator WaitForEventsCoroutine(Action onDone) {
            while (!disposed) {
                if (IsLoading) {
                    Task preExistingTask = WaitForCalibrations();
                    while (!preExistingTask.IsCompleted)
                        yield return null;
                }
                Task<bool> task = ReloadCalibrations(true, LKGSettingsSystem.Settings.queryingLoggingFlags);
                while (!task.IsCompleted)
                    yield return null;

                if (task.Result) {
                    yield return WaitFor(2);
                } else {
                    yield return WaitFor(3);
                }
            }
            onDone();
        }
    }
}
