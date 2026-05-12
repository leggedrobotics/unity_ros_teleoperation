//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.IO;
using System.Text;
using UnityEngine;
#if HAS_NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LookingGlass.DualMonitorApplication {
    [HelpURL("https://look.glass/unitydocs")]
    [RequireComponent(typeof(InterProcessCommunicator))]
    public class DualMonitorApplicationBaseIPC : MonoBehaviour {
        private const string PORT_FOLDER_NAME = "ipc";
        private const string PORT_FILE_NAME = "ports.json";

        public DualMonitorApplicationDisplay display;

        [Header("InterProcess Communicator")]
        [Tooltip("Automatically handle the IPC. If you change this to false, you have to set the reference and configure the IPC yourself")]
        public bool automaticallyHandleIPC = true;

        [Tooltip("IPC being referenced. Don't worry about this if this if automaticallyHandleIPC is set to true")]
        public InterProcessCommunicator ipc;

        [Tooltip("Set this to true to show debug UI (via OnGUI) that shows ports.")]
        public bool debugGUI = false;

        private FileSystemWatcher fileWatcher;
        private object syncRoot = new();
        private bool isDirty = false;

        [Serializable]
        public struct PortData
        {
            public int senderPort;
            public int receiverPort;
        }

        /// <summary>
        /// <para>The directory path of the local file that stores ports.</para>
        /// <para>This file is shared between the two applications, and is stored in the 2D UI's persistent data path.</para>
        /// </summary>
        private string PortFolderPath {
            get {
                string folder = Application.persistentDataPath;
#if !UNITY_EDITOR
                if (display != DualMonitorApplicationDisplay.Window2D)
                    folder += DualMonitorApplicationManager.extendedUIString;
#endif
                return Path.Combine(folder, PORT_FOLDER_NAME);
            }
        }
        /// <summary>
        /// The file path of the local file that stores ports.
        /// </summary>
        private string PortFilePath => Path.Combine(PortFolderPath, PORT_FILE_NAME);

        public virtual void Awake()
        {
#if !UNITY_2018_4_OR_NEWER && !UNITY_2019_1_OR_NEWER
            Debug.LogError("[LookingGlass] Dual Monitor Application requires Unity version 2018.4 or newer!");
# endif

            ipc = GetComponent<InterProcessCommunicator>();

            // automatically handle the ports
            if (automaticallyHandleIPC)
            {
                Directory.CreateDirectory(PortFolderPath); //NOTE: Directory.CreateDirectory(...) does nothing if the folder already exists.

                if (display == DualMonitorApplicationDisplay.Window2D)
                {
                    // find a random available ports and save to a local file
                    int startPort = UnityEngine.Random.Range(6000, 7100);
                    int endPort = startPort + 1000;
                    ipc.receiverPort = FreePortFinder.FindFreePort(startPort, endPort);
                    ipc.senderPort = FreePortFinder.FindFreePort(startPort - 2000, startPort - 1000);
                    SavePorts(ipc.senderPort, ipc.receiverPort);
                }
                else
                {
                    // add a watcher to the local file to sync changes of ports
                    fileWatcher = new(PortFolderPath, PORT_FILE_NAME);
                    fileWatcher.EnableRaisingEvents = true;

                    fileWatcher.Changed += OnChanged;
                    fileWatcher.Created += OnChanged;

                    (ipc.receiverPort, ipc.senderPort) = GetPorts();
                }
                ipc.role = InterProcessCommunicator.Role.Both;
            }

            ipc.OnMessageReceived += ReceiveMessage;
        }

        public virtual void OnDestroy() {
            ipc.OnMessageReceived -= ReceiveMessage;
            if (fileWatcher != null)
            {
                fileWatcher.Dispose();
                fileWatcher = null;
            }
        }

        public virtual void Update() {
            bool esc = false;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = InputSystem.GetDevice<Keyboard>();
            if (keyboard != null) {
                if (keyboard.escapeKey.wasPressedThisFrame)
                    esc = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
                esc = true;
#endif

            // basic quit command. will quit the non-focused application through IPC as well
            if (esc) {
                IPCQuit();
                return;
            }

            // update the ports when needed
            bool shouldUpdate = false;
            lock (syncRoot)
            {
                shouldUpdate = isDirty;
                if (shouldUpdate)
                    isDirty = false;
            }

            if (shouldUpdate)
            {
                (ipc.receiverPort, ipc.senderPort) = GetPorts();
                ipc.ApplyPortChanges();
            }
        }

        private void OnGUI() {
            // showing the debug infos when debugGUI is true
            if (!debugGUI)
                return;

            GUIStyle boxStyle = new(GUI.skin.box);
            boxStyle.fontSize = 50;
            boxStyle.wordWrap = true;
            GUI.Box(new Rect(0, 0, Screen.width, 300), $"sender port: {ipc.senderPort} receiver port: {ipc.receiverPort} \n {PortFilePath}", boxStyle);
        }

        // quits both the current application and any recipients
        public virtual void IPCQuit() {
            ipc.SendData("quit");
            Application.Quit();
        }

        public virtual void ReceiveMessage(string message)
        {
            // basic quit message receiver
            switch (message) {
                case "quit":
                    Application.Quit();
                    break;
            }
        }

        /// <summary>
        /// <para>Retrieves the ports from the local shared file.</para>
        /// <para>If the file at <see cref="PortFilePath"/> is NOT found, the ports from the IPC in this application are returned instead.</para>
        /// </summary>
        /// <returns>The sender and receiver ports, respectively.</returns>
        public (int, int) GetPorts()
        {
            if (File.Exists(PortFilePath))
            {
                byte[] bytes = File.ReadAllBytes(PortFilePath);
                string json = Encoding.UTF8.GetString(bytes);
#if HAS_NEWTONSOFT_JSON
                PortData data = JsonConvert.DeserializeObject<PortData>(json);
                return (data.senderPort, data.receiverPort);
#endif
            }
            return (ipc.senderPort, ipc.receiverPort);
        }

        /// <summary>
        /// Saves the currently-used IPC ports to local file
        /// </summary>
        /// <param name="_senderPort"></param>
        /// <param name="_receiverPort"></param>
        public void SavePorts(int _senderPort, int _receiverPort)
        {
#if HAS_NEWTONSOFT_JSON
            string json = JsonConvert.SerializeObject(new PortData()
            {
                senderPort = _senderPort,
                receiverPort = _receiverPort
            }, Formatting.Indented);
            File.WriteAllBytes(PortFilePath, Encoding.UTF8.GetBytes(json));
#endif
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            lock (syncRoot)
            {
                // when file is changed, mark isDirty to true
                isDirty = true;
            }
        }
    }
}
