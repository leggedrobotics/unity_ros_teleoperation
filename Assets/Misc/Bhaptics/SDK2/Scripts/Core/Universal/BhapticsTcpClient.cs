using System.Collections;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Net;
using UnityEngine.Networking;

namespace Bhaptics.SDK2.Universal
{

    [Serializable]
    public class UdpMessage 
    {
        public string userId = "";
        public int port = 8000;
    }

    public class BhapticsTcpClient
    {
        public bool connected = false;
        public bool connecting = false;
        private volatile bool _isListening = false;
        private volatile bool _isTcpLoop = false;

        #region private members 	
        private TcpClient socketConnection;
        private Thread sdkThread;
        private Thread udpThread;
        private Thread pingThread;

        private string _apiKey;
        private string _appId;
        private string _serverIp = "";
        private int _serverPort;
        
        private DateTime last = new DateTime();
        #endregion

        
        
        public void Initialize(string appId, string apiKey, string defaultConfig)
        {
            _appId = appId;
            _apiKey = apiKey;
            
            StartUdpListener();
        }
        
        public int Play(string eventName) 
        {
            if (!connected) 
            {
                return -1;
            }

            return Play(eventName, 1f, 1f, 0f, 0f);
        }

        public int Play(string eventName, float intensity, float duration, float angleX, float offsetY)
        {
            if (!connected)
            {
                return -1;
            }

            BhapticsLogManager.Log("[bhaptics-universal] Play: " + eventName + ", " +  DateTime.UtcNow.Millisecond);

            int requesetId = UnityEngine.Random.Range(1, int.MaxValue);

            var playMessage = new PlayMessage
            {
                eventName = eventName,
                requestId = requesetId,
                intensity = intensity,
                duration = duration,
                offsetAngleX = angleX,
                offsetY = offsetY,
            };
            var message = TactHubMessage.SdkPlay(playMessage);

            SendMessageToTactHub(JsonUtility.ToJson(message));

            return requesetId;
        }

        public void StopAll()
        {
            if (!connected)
            {
                return;
            }
            BhapticsLogManager.Log("[bhaptics-universal] StopAll: ");
            var message = TactHubMessage.StopAll;

            SendMessageToTactHub(JsonUtility.ToJson(message));
        }

        public void StopByEventId(string eventName)
        {
            if (!connected)
            {
                return;
            }

            BhapticsLogManager.Log("[bhaptics-universal] StopByEventId: " + eventName);
            var message = TactHubMessage.SdkStopByEventId(eventName);

            SendMessageToTactHub(JsonUtility.ToJson(message));
        }

        public void StopByRequestId(int requestId)
        {
            if (!connected)
            {
                return;
            }

            BhapticsLogManager.Log("[bhaptics-universal] StopByEventId: " + requestId);
            var message = TactHubMessage.SdkStopByRequestId(requestId);

            SendMessageToTactHub(JsonUtility.ToJson(message));
        }

        public void Ping()
        {
            if (!connected)
            {
                return;
            }

            var message = TactHubMessage.SdkPingServer();
            SendMessageToTactHub(JsonUtility.ToJson(message));
        }

        public void Pause()
        {
            StopUdpListener();
        }

        public void Destroy()
        {
            _isTcpLoop = false;
            _isListening = false;
        }


        private void StartUdpListener()
        {
            if (_isListening)
            {
                BhapticsLogManager.LogFormat("[bhaptics-universal] StartUdpListener listening...");
                return;
            }

            try
            {
                _isListening = true;
                BhapticsLogManager.Log("[bhaptics-universal] StartUdpListener ");
                udpThread = new Thread(UdpBroadcastingListenLoop);
                udpThread.IsBackground = true;
                udpThread.Start();
            }
            catch (Exception e)
            {
                BhapticsLogManager.LogFormat("[bhaptics-universal] ReceiveUDP exception {0}", e.Message);
                _isListening = false;
            }
        }

        private void StopUdpListener()
        {
            _isListening = false;
        }

        private void ConnectToTactHub(string ip, int port)
        {
            if (connected) {
                BhapticsLogManager.LogFormat("[bhaptics-universal] ConnectToTactHub: connected {0}:{1}", ip, port);
                return;
            }
            if (connecting) {
                BhapticsLogManager.LogFormat("[bhaptics-universal] ConnectToTactHub: connecting {0}:{1}", ip, port);
                return;
            }

            try
            {
                connecting = true;
                _serverIp = ip;
                _serverPort = port;
                BhapticsLogManager.LogFormat("[bhaptics-universal] ConnectToTactHub {0}:{1}", ip, port);
                _isTcpLoop = true;
                sdkThread = new Thread(TactHubLoop)
                {
                    IsBackground = true
                };
                sdkThread.Start();
                pingThread = new Thread(PingLoop)
                {
                    IsBackground = true
                };
                pingThread.Start();

                connected = true;
                connecting = false;
            }
            catch (Exception e)
            {
                BhapticsLogManager.Log("ConnectToTactHub exception " + e.Message);
            }
        }
        
        private void PingLoop() 
        {
            while (connected) {
                var current = DateTime.Now;
                long diffTicks = current.Ticks - last.Ticks;
                var diffSpan = new TimeSpan(diffTicks);
                // 1s = 10,000,000 ticks
                if (diffSpan.TotalSeconds > 1)
                {
                    Ping();
                    last = current;
                }
            }

        }



        private void UdpBroadcastingListenLoop() 
        {
            BhapticsLogManager.Log("[bhaptics-universal] UdpBroadcastingListenLoop");
            var udpEndPoint = new IPEndPoint(IPAddress.Any, 15884);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(udpEndPoint);

            while (_isListening)
            {
                if (connected) 
                {
                    BhapticsLogManager.Log("[bhaptics-universal] UdpBroadcastingListenLoop: connected");
                    StopUdpListener();
                    break;
                }

                var sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint remote = sender;

                var data = new byte[1024];

                int receivedSize = socket.ReceiveFrom(data, ref remote);

                var endPoint = remote as IPEndPoint;
                
                var str = (Encoding.UTF8.GetString(data, 0, receivedSize));

                UdpMessage message = JsonUtility.FromJson<UdpMessage>(str);

                if (connected || connecting) {
                    continue;
                }
                BhapticsLogManager.LogFormat("[bhaptics-universal] UdpBroadcastingListenLoop Message received from {0}:", remote.ToString());
                
                ConnectToTactHub(endPoint.Address.ToString(), message.port);

                BhapticsLogManager.LogFormat("[bhaptics-universal] UdpBroadcastingListenLoop ip: {0}:{1}, userId: {2}", endPoint.Address.ToString(), endPoint.Port , message.userId);
            }

            socket.Close();
            udpThread = null;
        }
        
        private void TactHubLoop()
        {
            try
            {
                socketConnection = new TcpClient(_serverIp, _serverPort);

                var bytes = new byte[1024];
                while (_isTcpLoop)
                {
                    using (var stream = socketConnection.GetStream()) {
                        InitializeSdk();

                        if (!stream.CanRead)
                        {
                            continue;
                        }

                        int length;
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incomingData = new byte[length];
                            Array.Copy(bytes, 0, incomingData, 0, length);
                            string serverMessage = Encoding.UTF8.GetString(incomingData);
                            BhapticsLogManager.Log("[bhaptics-universal] TactHubLoop received as: " + DateTime.UtcNow.Second + ":"  + DateTime.UtcNow.Millisecond + " " + serverMessage);

                            var message = JsonUtility.FromJson<TactHubMessage>(serverMessage);


                            switch (message.type) 
                            {
                                case MessageType.ServerTokenMessage:
                                    var token = JsonUtility.FromJson<TokenMessage>(message.message);

                                    UnityMainThreadDispatcher.Instance().Enqueue(CheckAPI(token.token, token.tokenKey));
                                    break;
                                default:
                                    BhapticsLogManager.Log("[bhaptics-universal] Unknown message " + message.message);
                                    break;

                            }

                        }
                    }
 
                }
            }
            catch (SocketException socketException)
            {
                BhapticsLogManager.Log("Socket exception: " + socketException);
            }
            catch (Exception e) {
                BhapticsLogManager.Log("Exception: " + e);
            }

            _isTcpLoop = false;
            connected = false;
            connecting = false;
            socketConnection = null;

            StartUdpListener();
        }
        
        private void InitializeSdk() 
        {
            var message = TactHubMessage.InitializeMessage(_appId, _apiKey);

            SendMessageToTactHub(JsonUtility.ToJson(message));
        }
        
        private void SendMessageToTactHub(string message = "message")
        {
            if (socketConnection == null)
            {
                return;
            }
            try
            {
                //Debug.Log("SendMessageToTactHub: " +  DateTime.UtcNow.Second + ":"  + DateTime.UtcNow.Millisecond);
                var stream = socketConnection.GetStream();
                if (stream.CanWrite)
                {
                    byte[] clientMessageAsByteArray = Encoding.UTF8.GetBytes($"{message}\n");
                    stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                }
            }
            catch (SocketException ex)
            { }
        }


        IEnumerator CheckAPI(string token, string tokenKey)
        {
            string url = "https://sdk-apis.bhaptics.com/api/v1/tacthub-api/verify?token=" + token + "&token-key=" + tokenKey;

            UnityWebRequest www = UnityWebRequest.Get(url);

            yield return www.SendWebRequest();

            if (www.error == null)
            {
                BhapticsLogManager.Log(www.downloadHandler.text);
            }
            else
            {
                BhapticsLogManager.Log("[bhaptics-universal] CheckAPI() error " + www.error);
                BhapticsLogManager.Log(www.downloadHandler.text);

                // TactHub App is not valid
            }
        }
    }
}

