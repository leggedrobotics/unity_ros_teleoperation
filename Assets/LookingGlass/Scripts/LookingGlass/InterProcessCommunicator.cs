//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

using static LookingGlass.InterProcessCommunicator;

/*
 * USE
 * Sending messages between two apps on the same computer.
 *
 * INSTRUCTIONS
 * - Add this component to an object in each app.
 * - A "Sender" communicator will send messages on the senderPort
 *   to be received in the "Receiver" communicator receiverPort,
 *   so match these port numbers.
 * - If you want two "Both" communicators to talk, likewise match
 *   the sender port number on one to the receiver port number on
 *   the other, and vice versa.
 * - Message signing is not necessary, but helps validate the data
 */

namespace LookingGlass {
    public static class RoleExtensions {
        public static bool IncludesSender(this Role role) => (((int) role + 1) & ((int) Role.Sender + 1)) != 0;
        public static bool IncludesReceiver(this Role role) => (((int) role + 1) & ((int) Role.Receiver + 1)) != 0;
    }

    public class InterProcessCommunicator : MonoBehaviour {
        // subscribe to event to get message
        public delegate void MessageReceived(string message);
        public event MessageReceived OnMessageReceived;

        // communicates only with processes on same computer
        private const string LocalHostIP = "127.0.0.1";
        private const int MaxBufferSize = 65536;

        //NOTE: I can't reliably change this to fit 0, 1, 2, 3 for flags values, because we already shipped the DMA IPC prefab,
        //  and it gets complicated because...
        //  The DMA IPC prefab has usesNewFlags = true and already updated,
        //  But since peoples' DMA IPC prefab instances in scenes have NOT overridden the serialized "usesNewFlags" value, they may inherit the "true" value...
        //  So to be safe, we have to keep the enum with old values and use a conversion function to flags:
        [Serializable]
        public enum Role {
            None = -1,
            Sender = 0,
            Receiver = 1,
            Both = 2,
        }

        public Role role = Role.None;

        [Tooltip("The local port to listen on for incoming data. This must be unique on this machine.")]
        public int receiverPort = 8080;

        [Tooltip("The local port to send data to.")]
        public int senderPort = 8081;

        // for security and identification, must be same on sender and receiver
        public bool signMessages = true;

        // should be uncommon and unique per app
        public char signingChar = '☆';

        private IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
        private Queue<string> messageQueue = new Queue<string>();

        private UdpClient client;
        private UdpClient server;
        private Thread receiveThread;
        private IPEndPoint remoteEndPoint;

        private void Awake() {
#if !UNITY_2018_4_OR_NEWER && !UNITY_2019_1_OR_NEWER
            Debug.LogError("[LookingGlass] Dual Monitior Applications require Unity version 2018.4 or newer!");
# endif
            if (client == null)
                Init();
        }

        private void OnDestroy() {
            Disconnect();
        }

        private void OnApplicationQuit() {
            Disconnect();
        }

        private void Init() {
            if (role == Role.None) {
                Debug.LogWarning(this + "'s role is set to None.");
                enabled = false;
                return;
            }

            remoteEndPoint = CreateEndPoint(senderPort);

            if (role.IncludesSender()) {
                client = new UdpClient();
                client.EnableBroadcast = true;
                client.Client.SendBufferSize = MaxBufferSize;
            }
            if (role.IncludesReceiver()) {
                server = new UdpClient(receiverPort);
                server.Client.ReceiveBufferSize = MaxBufferSize;

                receiveThread = new Thread(new ThreadStart(ReceiveData));
                receiveThread.IsBackground = true;
                receiveThread.Start();

                StartCoroutine(EvaluateMessagesOnMainThread());
            }
        }

        private void Disconnect() {
            if (receiveThread != null) {
                receiveThread.Abort();
                receiveThread = null;
            }

            if (client != null) {
                client.Close();
                client = null;
            }
            if (server != null) {
                server.Close();
                server = null;
            }
        }

        private IEnumerator EvaluateMessagesOnMainThread() {
            while (true) {
                while (messageQueue.Count > 0)
                    EvaluateMessage(messageQueue.Dequeue());
                yield return null;
            }
        }

        private void EvaluateMessage(string message) {
            OnMessageReceived?.Invoke(message);
        }

        public void ApplyPortChanges() {
            // disconnect
            Disconnect();

            // then re - init
            Init();
        }

        private IPEndPoint CreateEndPoint(int port) {
            IPAddress ip;
            if (IPAddress.TryParse(LocalHostIP, out ip)) {
                return new IPEndPoint(ip, port);
            } else {
                return new IPEndPoint(IPAddress.Broadcast, port);
            }
        }

        public void SendData(string message) => SendData(message, -1);
        public void SendData(string message, int port) {
            if (!isActiveAndEnabled)
                return;
            if (!role.IncludesSender() || string.IsNullOrEmpty(message))
                return;

            if (signMessages)
                message += signingChar;

            if (client == null)
                Init();

            if (client != null) {
                byte[] data = Encoding.UTF8.GetBytes(message);

                if (data.Length > client.Client.SendBufferSize) {
                    Debug.LogError("Error UDP send: Message too big");
                    return;
                }

                try {
                    IPEndPoint destination = (port < 0) ? remoteEndPoint : CreateEndPoint(port);
                    client.Send(data, data.Length, destination);
                } catch (Exception e) {
                    Debug.LogError("Error UDP send: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n\n");
                }
            }
        }

        private void ReceiveData() {
            if (!role.IncludesReceiver())
                return;

            while (server != null) {
                try {
                    byte[] data = server.Receive(ref anyIP);
                    string message = Encoding.UTF8.GetString(data);

                    if (!string.IsNullOrEmpty(message)) {
                        if (signMessages) {
                            if (message[message.Length - 1] == signingChar) {
                                // remove endChar
                                messageQueue.Enqueue(message.Substring(0, message.Length - 1));
                            }
                        } else {
                            messageQueue.Enqueue(message);
                        }
                    }
                } catch (ThreadAbortException e) {
                    Debug.Log("Thread Abort Error: " + e.Message);
                } catch (Exception e) {
                    Debug.LogError("Error UDP receive: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n\n");
                }
            }
        }
    }
}
