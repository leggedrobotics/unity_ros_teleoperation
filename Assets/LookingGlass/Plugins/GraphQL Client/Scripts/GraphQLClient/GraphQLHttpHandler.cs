using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using GraphQLClient.EventCallbacks;

#if HAS_NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace GraphQLClient {
    public class GraphQLHttpHandler {
        //Use this to subscribe to a graphql endpoint
        public static async Task<ClientWebSocket> WebsocketConnect(string subscriptionUrl, string details, string authToken = null, string socketId = "1", string protocol = "graphql-ws") {
            string subUrl = subscriptionUrl.Replace("http", "ws");
            string id = socketId;
            ClientWebSocket cws = new ClientWebSocket();
            cws.Options.AddSubProtocol(protocol);
            if (!String.IsNullOrEmpty(authToken))
                cws.Options.SetRequestHeader("Authorization", "Bearer " + authToken);
            Uri u = new Uri(subUrl);
            try {
                await cws.ConnectAsync(u, CancellationToken.None);
                if (cws.State == WebSocketState.Open)
                    Debug.Log("connected");
                await WebsocketInit(cws);
                await WebsocketSend(cws, id, details);
            } catch (Exception e) {
                Debug.Log("woe " + e.Message);
            }

            return cws;
        }

        public static async Task<ClientWebSocket> WebsocketConnect(ClientWebSocket cws, string subscriptionUrl, string details, string socketId = "1") {
            string subUrl = subscriptionUrl.Replace("http", "ws");
            string id = socketId;
            Uri u = new Uri(subUrl);
            try {
                await cws.ConnectAsync(u, CancellationToken.None);
                if (cws.State == WebSocketState.Open)
                    Debug.Log("connected");
                await WebsocketInit(cws);
                await WebsocketSend(cws, id, details);
            } catch (Exception e) {
                Debug.Log("woe " + e.Message);
            }

            return cws;
        }

        static async Task WebsocketInit(ClientWebSocket cws) {
            string jsonData = "{\"type\":\"connection_init\"}";
            ArraySegment<byte> b = new ArraySegment<byte>(Encoding.ASCII.GetBytes(jsonData));
            await cws.SendAsync(b, WebSocketMessageType.Text, true, CancellationToken.None);
            GetWsReturn(cws);
        }

        static async Task WebsocketSend(ClientWebSocket cws, string id, string details) {
            string jsonData = GraphQLJsonUtility.ToJson(new { id = $"{id}", type = "start", payload = new { query = details } });
            ArraySegment<byte> b = new ArraySegment<byte>(Encoding.ASCII.GetBytes(jsonData));
            await cws.SendAsync(b, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        //Call GetWsReturn to wait for a message from a websocket. GetWsReturn has to be called for each message
        static async void GetWsReturn(ClientWebSocket cws) {
            ArraySegment<byte> buf = new ArraySegment<byte>(new byte[1024]);
            buf = WebSocket.CreateClientBuffer(1024, 1024);
            WebSocketReceiveResult r;
            string result = "";
            do {
                r = await cws.ReceiveAsync(buf, CancellationToken.None);
                result += Encoding.UTF8.GetString(buf.Array ?? throw new ApplicationException("Buf = null"), buf.Offset,
                    r.Count);
            } while (!r.EndOfMessage);

            if (String.IsNullOrEmpty(result))
                return;

#if HAS_NEWTONSOFT_JSON
            JObject obj = null;
            try {
                obj = JObject.Parse(result);
            } catch (JsonReaderException e) {
                throw new ApplicationException(e.Message);
            }

            string subType = (string) obj["type"];
            switch (subType) {
                case "connection_ack": {
                        Debug.Log("init_success, the handshake is complete");
                        OnSubscriptionHandshakeComplete subscriptionHandshakeComplete =
                            new OnSubscriptionHandshakeComplete();
                        subscriptionHandshakeComplete.FireEvent();
                        GetWsReturn(cws);
                        break;
                    }
                case "error": {
                        throw new ApplicationException("The handshake failed. Error: " + result);
                    }
                case "connection_error": {
                        throw new ApplicationException("The handshake failed. Error: " + result);
                    }
                case "data": {
                        OnSubscriptionDataReceived subscriptionDataReceived = new OnSubscriptionDataReceived(result);
                        subscriptionDataReceived.FireEvent();
                        GetWsReturn(cws);
                        break;
                    }
                case "ka": {
                        GetWsReturn(cws);
                        break;
                    }
                case "subscription_fail": {
                        throw new ApplicationException("The subscription data failed");
                    }

            }
#else
            throw GraphQLJsonUtility.GetMissingNewtonsoftJsonException();
#endif
        }

        public static async Task WebsocketDisconnect(ClientWebSocket cws, string socketId = "1") {
            string jsonData = $"{{\"type\":\"stop\",\"id\":\"{socketId}\"}}";
            ArraySegment<byte> b = new ArraySegment<byte>(Encoding.ASCII.GetBytes(jsonData));
            await cws.SendAsync(b, WebSocketMessageType.Text, true, CancellationToken.None);
            await cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            OnSubscriptionCanceled subscriptionCanceled = new OnSubscriptionCanceled();
            subscriptionCanceled.FireEvent();
        }
    }
}
