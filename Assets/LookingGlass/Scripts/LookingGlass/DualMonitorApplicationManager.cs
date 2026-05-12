//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using LookingGlass.Toolkit;

using Debug = UnityEngine.Debug;

namespace LookingGlass.DualMonitorApplication
{
    // enum for pro workstation stuff
    public enum DualMonitorApplicationDisplay
    {
        LookingGlass,
        Window2D
    }

    [HelpURL("https://look.glass/unitydocs")]
    public class DualMonitorApplicationManager : MonoBehaviour
    {
        // cause path.combine seems to be glitchy?
        public const string separator =
#if UNITY_EDITOR_WIN
            "\\";
#else
            "/";
#endif
        public DualMonitorApplicationDisplay display;
        public static DualMonitorApplicationManager instance;
        public const string extendedUIString = "_extendedUI";
        public const string lkgDisplayString = "LKGDisplay";
        public const int sidePanelResolutionX = 600;
        public const int sidePanelResolutionY = 800;

        public bool autoLaunchLKGApp = false;

        [Tooltip(
            "Use this if you are building for a Looking Glass with a built-in 2D side panel (deprecated)"
        )]
        public bool usesSidePanel = false;

        Process process;
        StreamWriter messageStream;

        private async Task StartProcessAfterDisplaysReady() {
            try {
                await LKGDisplaySystem.WaitForCalibrations();
                StartProcess();
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public void StartProcess()
        {
            try
            {
                process = new Process();
                process.EnableRaisingEvents = false;
#if UNITY_STANDALONE_WIN
                process.StartInfo.FileName = Path.Combine(
                    Application.streamingAssetsPath,
                    lkgDisplayString + ".exe"
                );
#elif UNITY_STANDALONE_OSX
                string appDir = Path.Combine(
                    Application.streamingAssetsPath,
                    lkgDisplayString + ".app",
                    "Contents",
                    "MacOS"
                );
                string[] dirFiles = Directory.GetFiles(appDir);
                if (dirFiles.Length != 1)
                    throw new Exception("error loading lkg app from streaming assets");
                    
                process.StartInfo.FileName = dirFiles[0];
                int lkgIndex = MacWindowing.FindLkgIndex();

                Calibration cal = LKGDisplaySystem.Get(0).calibration;
                Debug.Log($"calibrations found: {LKGDisplaySystem.LKGDisplayCount}");

                process.StartInfo.ArgumentList.Add("-monitor");
                string lkgIndexArg = (lkgIndex + 1).ToString();
                Debug.Log($"opening app with arg: -monitor {lkgIndexArg}");
                process.StartInfo.ArgumentList.Add(lkgIndexArg);

                process.StartInfo.ArgumentList.Add("-screen-fullscreen");
                process.StartInfo.ArgumentList.Add("1");
                process.StartInfo.ArgumentList.Add("-screen-width");
                process.StartInfo.ArgumentList.Add(cal.screenW.ToString());
                process.StartInfo.ArgumentList.Add("-screen-height");
                process.StartInfo.ArgumentList.Add(cal.screenH.ToString());
#endif
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardError = true;
                process.OutputDataReceived += new DataReceivedEventHandler(DataReceived);
                process.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);
                process.Start();
                process.BeginOutputReadLine();
                messageStream = process.StandardInput;

                UnityEngine.Debug.Log("Successfully launched app");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Unable to launch app: " + e.GetType().Name + ": " + e.Message + "\n\n" + e.StackTrace);
            }
        }

        public void CloseProcess()
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
            }
        }

        void DataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            // Handle it
            UnityEngine.Debug.Log(eventArgs.Data);
        }

        void ErrorReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            UnityEngine.Debug.LogError(eventArgs.Data);
        }

        // must specify display before creation
        public DualMonitorApplicationManager(DualMonitorApplicationDisplay display)
        {
            this.display = display;
        }

        void Awake()
        {
            // only one should exist at a time, check for existing instances on awake
            var existingManagers = FindObjectsOfType<DualMonitorApplicationManager>();
            if (existingManagers.Length > 1)
            {
                // delete self if found
                DestroyImmediate(gameObject);
                return;
            }

            // otherwise this should be the only manager, make it an instance and keep it from being destroyed on scene change
            instance = this;
            DontDestroyOnLoad(this.gameObject);

            // if this is the side panel scene
            if (!Application.isEditor && display == DualMonitorApplicationDisplay.Window2D)
            {
                if (usesSidePanel)
                {
                    // first adjust position
                    UnityEngine.Display.displays[0].SetParams(
                        sidePanelResolutionX,
                        sidePanelResolutionY,
                        0,
                        0
                    );
                }

                // launch the lkg version of the application
                if (process == null)
                {
                    if (autoLaunchLKGApp)
                    {
                        StartProcessAfterDisplaysReady();
                    }
                }
            }
        }

        void OnApplicationQuit()
        {
            CloseProcess();
        }
    }
}
