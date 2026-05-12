using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WebSocketSharp;


#if HAS_NEWTONSOFT_JSON
using Newtonsoft.Json.Linq;
#endif

namespace LookingGlass.Toolkit.Bridge {
    public class BridgeConnectionHTTP : IDisposable {
        public const string DefaultURL = "localhost";
        public const int DefaultPort = 33334;
        public const int DefaultWebSocketPort = 9724;

        private int port;
        private int webSocketPort;
        private string url;

        private ILogger logger;
        private IHttpSender httpSender;
        private BridgeWebSocketClient webSocket;
        private volatile bool lastConnectionState = false;

        private Stopwatch timer = new Stopwatch();

        private Orchestration session;
        private string currentPlaylistName = "";

        public int Port => port;
        public int WebSocketPort => webSocketPort;

        public BridgeLoggingFlags LoggingFlags { get; set; } = BridgeLoggingFlags.None;
        public Dictionary<int, Display> AllDisplays { get; private set; }
        public Dictionary<int, Display> LKGDisplays { get; private set; }

        private Dictionary<string, List<Action<string>>> eventListeners;
        private DisplayEvents monitorEvents;
        private HashSet<Action<bool>> connectionStateListeners;

        public BridgeConnectionHTTP(string url = DefaultURL, int port = DefaultPort, int webSocketPort = DefaultWebSocketPort) : this(null, null, url, port, webSocketPort) { }
        public BridgeConnectionHTTP(ILogger logger, IHttpSender httpSender, string url = DefaultURL, int port = DefaultPort, int webSocketPort = DefaultWebSocketPort) {
            this.url = url;
            this.port = port;
            this.webSocketPort = webSocketPort;

            //TODO: Use DI instead of putting defaults here:
            if (logger == null)
                logger = new ConsoleLogger();
            if (httpSender == null)
                httpSender = new DefaultHttpSender();
            this.logger = logger;
            this.httpSender = httpSender;

            AllDisplays = new Dictionary<int, Display>();
            LKGDisplays = new Dictionary<int, Display>();

            eventListeners = new Dictionary<string, List<Action<string>>>();
            monitorEvents = new DisplayEvents(this);
            connectionStateListeners = new HashSet<Action<bool>>();
        }

        public bool Connect(int timeoutSeconds = 3000) {
            httpSender.TimeoutSeconds = timeoutSeconds;
            if (webSocket != null) {
                webSocket.Dispose();
                webSocket = null;
            }
            webSocket = new BridgeWebSocketClient(UpdateListeners);
            return UpdateConnectionState(webSocket.TryConnect($"ws://{url}:{webSocketPort}/event_source"));
        }


        public bool UpdateConnectionState(bool state) {
            lastConnectionState = state;
            foreach (Action<bool> callback in connectionStateListeners)
                callback(lastConnectionState);
            return lastConnectionState;
        }

        private string GetURL(string endpoint)
        {
            return $"http://{url}:{port}/{endpoint}";
        }

        public int AddListener(string name, Action<string> callback) {
            if (eventListeners.ContainsKey(name)) {
                int id = eventListeners[name].Count;
                eventListeners[name].Add(callback);
                return id;
            } else {
                List<Action<string>> callbacks = new() { callback };

                eventListeners.Add(name, callbacks);
                return 0;
            }
        }

        public void RemoveListener(string name, Action<string> callback) {
            if (eventListeners.ContainsKey(name)) {
                if (eventListeners[name].Contains(callback)) {
                    eventListeners[name].Remove(callback);
                }
            }
        }

        public void AddConnectionStateListener(Action<bool> callback) {
            if (!connectionStateListeners.Contains(callback)) {
                connectionStateListeners.Add(callback);
                callback(lastConnectionState);
            }
        }

        public void RemoveConnectionStateListener(Action<bool> callback) {
            if (connectionStateListeners.Contains(callback)) {
                connectionStateListeners.Remove(callback);
            }
        }

        private void UpdateListeners(string message) {
#if HAS_NEWTONSOFT_JSON
            JToken json = JObject.Parse(message)["payload"]?["value"];

            if (json != null) {
                string eventName = json["event"]["value"].ToString();
                string eventData = json.ToString();

                Console.WriteLine(eventName + "\n" + eventData);

                if (eventListeners.ContainsKey(eventName)) {
                    foreach (Action<string> listener in eventListeners[eventName]) {
                        listener(eventData);
                    }
                }

                // special case for listeners with empty names
                if (eventListeners.ContainsKey("")) {
                    foreach (var listener in eventListeners[""]) {
                        listener(eventData);
                    }
                }
            }
#endif
        }

        public List<Display> GetAllDisplays() {
            List<Display> displays = new();
            foreach (KeyValuePair<int, Display> pair in AllDisplays)
                displays.Add(pair.Value);
            return displays;
        }

        public List<Display> GetLKGDisplays() {
            List<Display> displays = new();
            foreach (KeyValuePair<int, Display> pair in LKGDisplays)
                displays.Add(pair.Value);
            return displays;
        }

        public string TrySendMessage(string endpoint, string content) => TrySendMessage(endpoint, content, LoggingFlags);
        public string TrySendMessage(string endpoint, string content, BridgeLoggingFlags loggingFlags) {
            try {
                if ((loggingFlags & BridgeLoggingFlags.Messages) != 0)
                    PrintMessage(endpoint, content);
                
                string url = GetURL(endpoint);

                string response = httpSender.Send(HttpSenderMethod.Put, url, content);
                if ((loggingFlags & BridgeLoggingFlags.Responses) != 0)
                    PrintResponse(endpoint, response);

                UpdateConnectionState(true);
                return response;
            } catch (Exception e) {
                logger.LogException(e);
                UpdateConnectionState(false);
                return null;
            }
        }

        public Task<string> SendMessageAsync(string endpoint, string content) => SendMessageAsync(endpoint, content, LoggingFlags);
        public async Task<string> SendMessageAsync(string endpoint, string content, BridgeLoggingFlags loggingFlags) {
            try {
                if ((loggingFlags & BridgeLoggingFlags.Messages) != 0)
                    PrintMessage(endpoint, content);
                string response = await httpSender.SendAsync(HttpSenderMethod.Put, GetURL(endpoint), content);
                if ((loggingFlags & BridgeLoggingFlags.Responses) != 0)
                    PrintResponse(endpoint, response);
                UpdateConnectionState(true);
                return response;
            } catch (Exception e) {
                logger.LogException(e);
                UpdateConnectionState(false);
                throw;
            }
        }

        private string GetPrefix() => "[LKG Toolkit]";
        private string GetPrefix(string endpoint) => "[LKG Toolkit] \"" + endpoint + "\"";

        private string GetMessageLogString(string endpoint, string content) => GetPrefix() + " Sent to \"" + endpoint + "\":\n" + content;
        private string GetResponseLogString(string endpoint, string response) => GetPrefix(endpoint) + " responded:\n" + response;
        private string GetTimeLogString(string endpoint, TimeSpan time) => GetPrefix(endpoint) + " completed in " + time.TotalMilliseconds.ToString("F0") + "ms";

        private void PrintMessage(string endpoint, string content) => logger.Log(GetMessageLogString(endpoint, content));
        private void PrintResponse(string endpoint, string response) => logger.Log(GetResponseLogString(endpoint, response));
        private void PrintTime(string endpoint, TimeSpan time) => logger.Log(GetTimeLogString(endpoint, time));

        public bool TryEnterOrchestration(string name = "default") {
            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            string message =
                $@"
                {{
                    ""name"": ""{name}""
                }}
                ";

            string resp = TrySendMessage("enter_orchestration", message);

            if (resp != null) {
                if (Orchestration.TryParse(resp, out Orchestration newSession)) {
                    session = newSession;
                    if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                        PrintTime("enter_orchestration message parsed", timer.Elapsed);
                    return true;
                }
            }

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                PrintTime("enter_orchestration", timer.Elapsed);

            return false;
        }

        public bool TryExitOrchestration() {
            if (session == null)
                return false;

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            string message =
                $@"
                {{
                    ""orchestration"": ""{session.Token}""
                }}
                ";

            string resp = TrySendMessage("exit_orchestration", message);

            if (resp != null) {
                session = default;
                if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                    PrintTime("exit_orchestration", timer.Elapsed);
                return true;
            }

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                PrintTime("exit_orchestration", timer.Elapsed);
            return false;
        }

        public bool TryTransportControlsPlay() {
            if (session == null)
                return false;

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            string message =
                $@"
                {{
                    ""orchestration"": ""{session.Token}""
                }}
                ";

            string resp = TrySendMessage("transport_control_play", message);

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                PrintTime("transport_control_play", timer.Elapsed);

            return resp != null;
        }

        public bool TryTransportControlsPause() {
            if (session == null)
                return false;

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            string message =
                $@"
                {{
                    ""orchestration"": ""{session.Token}""
                }}
                ";

            string resp = TrySendMessage("transport_control_pause", message);

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                PrintTime("transport_control_pause", timer.Elapsed);

            return resp != null;
        }

        public bool TryTransportControlsNext() {
            if (session == null)
                return false;

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            string message =
                $@"
                {{
                    ""orchestration"": ""{session.Token}""
                }}
                ";

            string resp = TrySendMessage("transport_control_next", message);

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                PrintTime("transport_control_next", timer.Elapsed);

            return resp != null;
        }

        public bool TryTransportControlsPrevious() {
            if (session == null)
                return false;

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            string message =
            $@"
            {{
                ""orchestration"": ""{session.Token}""
            }}
            ";

            string resp = TrySendMessage("transport_control_previous", message);

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                PrintTime("transport_control_previous", timer.Elapsed);

            return resp != null;
        }

        public bool TryShowWindow(bool showWindow, int head = -1) {
            if (session == null)
                return false;

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            string message =
            $@"
            {{
                ""orchestration"": ""{session.Token}"",
                ""show_window"": ""{showWindow}"",
                ""head_index"": {head}
            }}
            ";

            string resp = TrySendMessage("show_window", message);

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                PrintTime("show_window", timer.Elapsed);

            return resp != null;
        }

        public bool TrySubscribeToEvents() {
            if (session == null)
                return false;

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            if (!webSocket.Connected())
                return false;

            string message =
                $@"
                {{
                    ""subscribe_orchestration_events"": ""{session.Token}""
                }}
                ";

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                PrintTime("TrySubscribeToEvents", timer.Elapsed);

            return webSocket.TrySendMessage(message);
        }


        public bool TryUpdatingParameter(string playlistName, int playlistItem, Parameters param, float newValue) {
            if (session == null)
                return false;

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            string message =
                $@"
                {{
                    ""orchestration"": ""{session.Token}"",
                    ""name"": ""{playlistName}"",
                    ""index"": ""{playlistItem}"",
                    ""{ParameterUtils.GetParamName(param)}"": ""{(ParameterUtils.IsFloatParam(param) ? newValue : (int) newValue)}"",
                }}
                ";

            string resp = TrySendMessage("update_playlist_entry", message);

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                PrintTime("update_playlist_entry", timer.Elapsed);

            return resp != null;
        }


        public bool TryUpdatingParameter(string playlistName, Parameters param, float newValue) {
            if (session == null)
                return false;

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            string message =
                $@"
                {{
                    ""orchestration"": ""{session.Token}"",
                    ""name"": ""{playlistName}"",
                    ""{ParameterUtils.GetParamName(param)}"": ""{(ParameterUtils.IsFloatParam(param) ? newValue : (int) newValue)}"",
                }}
                ";

            string resp = TrySendMessage("update_current_entry", message);

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                PrintTime("update_current_entry", timer.Elapsed);

            return resp != null;
        }

        public bool TryUpdateDevices() => TryUpdateDevices(LoggingFlags);
        public bool TryUpdateDevices(BridgeLoggingFlags loggingFlags) => UpdateDevicesAsync(loggingFlags).Result;

        public Task<bool> UpdateDevicesAsync() => UpdateDevicesAsync(LoggingFlags);
        public async Task<bool> UpdateDevicesAsync(BridgeLoggingFlags loggingFlags) {
            if (session == null)
                return false;

            if ((loggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            string message =
                $@"
                {{
                    ""orchestration"": ""{session.Token}""
                }}
                ";

            string response = await SendMessageAsync("available_output_devices", message, loggingFlags);
            try {
                return UpdateDisplays(response);
            } finally {
                if ((loggingFlags & BridgeLoggingFlags.Timing) != 0)
                    PrintTime("available_output_devices", timer.Elapsed);
            }
        }

        private bool UpdateDisplays(string response) {
#if HAS_NEWTONSOFT_JSON
            if (!string.IsNullOrWhiteSpace(response)) {
                JObject payloadJson = JObject.Parse(response)?["payload"]?["value"]?.Value<JObject>();

                if (payloadJson != null) {
                    Dictionary<int, Display> allDisplays = new();
                    Dictionary<int, Display> lkgDisplays = new();

                    for (int i = 0; i < payloadJson.Count; i++) {
                        JObject displayJson = payloadJson[i.ToString()]!["value"]!.Value<JObject>();
                        Display display = Display.Parse(i, displayJson);
                        if (!allDisplays.ContainsKey(display.hardwareInfo.index))
                            allDisplays.Add(display.hardwareInfo.index, display);

                        if (display.IsLKG && !lkgDisplays.ContainsKey(display.hardwareInfo.index))
                            lkgDisplays.Add(display.hardwareInfo.index, display);
                    }

                    AllDisplays = allDisplays;
                    LKGDisplays = lkgDisplays;
                }

                return true;
            }
#endif
            return false;
        }

        public bool TryDeletePlaylist(Playlist p) {
            if (session == null)
                return false;

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            if (currentPlaylistName == p.name)
                currentPlaylistName = "";

            string deleteMessage = p.GetInstanceJson(session);
            string response = TrySendMessage("delete_playlist", deleteMessage);

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                PrintTime("delete_playlist", timer.Elapsed);

            return response != null;
        }

        public bool TrySyncPlaylist(int head = -1) {
            if (session == null)
                return false;

            if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                timer.Restart();

            if (currentPlaylistName != "") {
                string message =
                $@"
                {{
                    ""orchestration"": ""{session.Token}"",
                    ""name"": ""{currentPlaylistName}"",
                    ""head_index"": {head},
                    ""crf"": 20,
                    ""pixel_format"": ""yuv420p"",
                    ""encoder"": ""h265""
                }}
                ";

                string response = TrySendMessage("sync_overwrite_playlist", message);

                if ((LoggingFlags & BridgeLoggingFlags.Timing) != 0)
                    PrintTime("sync_overwrite_playlist", timer.Elapsed);

                return response != null;
            }
            return false;
        }

        /// <summary>
        /// Attempts to show media (image or video) on a LKG display.
        /// </summary>
        /// <param name="p">The playlist to play. This may contain one or more images or videos to playback on the LKG display.</param>
        /// <param name="head">
        /// <para>
        /// Determines which LKG display to target. This is the LKG display index from <see cref="DisplayInfo.index"/>.
        /// </para>
        /// <remarks>
        /// Note that using a display index of -1 will use the first available LKG display.
        /// </remarks>
        /// </param>
        /// <returns></returns>
        public bool TryPlayPlaylist(Playlist p, int head = -1) {
            if (session == null)
                return false;

            if (currentPlaylistName == p.name) {
                string delete_message = p.GetInstanceJson(session);
                string delete_resp = TrySendMessage("delete_playlist", delete_message);
            }

            TryShowWindow(true, head);

            string message = p.GetInstanceJson(session);
            string resp = TrySendMessage("instance_playlist", message);

            string[] playlistItems = p.GetPlaylistItemsAsJson(session);

            for (int i = 0; i < playlistItems.Length; i++) {
                string pMessage = playlistItems[i];
                string pResp = TrySendMessage("insert_playlist_entry", pMessage);
            }

            currentPlaylistName = p.name;

            string playMessage = p.GetPlayPlaylistJson(session, head);
            string playResp = TrySendMessage("play_playlist", playMessage);

            return true;
        }

        public bool TrySaveout(string source, string filename) {
            string message =
                $@"
                {{
                    ""orchestration"": ""{session.Token}"",
                    ""head_index"": ""-1"",
                    ""source"": ""{source}"",
                    ""filename"": ""{filename.Replace("\\", "\\\\")}""
                }}
                ";

            string resp = TrySendMessage("source_saveout", message);

            if (!resp.IsNullOrEmpty())
                return true;
            return false;
        }

        public bool TryGetCameraParams(out float displayViewCone, out float displayViewConeVFOV, out float displayViewConeHFOV) {
            string message =
                $@"
                {{
                    ""orchestration"": ""{session.Token}"",
                    ""head_index"": ""-1"",
                }}
                ";

            string resp = TrySendMessage("get_camera_parameters", message);

#if HAS_NEWTONSOFT_JSON
            if (!string.IsNullOrEmpty(resp)) {
                JObject json = JObject.Parse(resp);
                displayViewCone = json["payload"]["value"]["viewCone"]["value"].Value<float>();
                displayViewConeHFOV = json["payload"]["value"]["hHOV"]["value"].Value<float>();
                displayViewConeVFOV = json["payload"]["value"]["vFOV"]["value"].Value<float>();
                return true;
            } else {
#endif
                displayViewCone = 0;
                displayViewConeHFOV = 0;
                displayViewConeVFOV = 0;
                return false;
#if HAS_NEWTONSOFT_JSON
            }
#endif
        }


        public bool TryReadback(string source) {
            string message =
                $@"
                {{
                    ""orchestration"": ""{session.Token}"",
                    ""head_index"": ""-1"",
                    ""source"": ""{source}"",
                }}
                ";

            string resp = TrySendMessage("source_readback", message);

            if (!resp.IsNullOrEmpty())
                return true;
            return false;
        }

        public void Dispose() {
            if (session != default) 
                TryExitOrchestration();

            webSocket.Dispose();
            if (logger is IDisposable l)
                l.Dispose();
            if (httpSender is IDisposable h)
                h.Dispose();
        }
    }
}
