using System;
using WebSocketSharp;

namespace LookingGlass.Toolkit.Bridge
{
    /// <summary>
    /// Handles asynchronous events and responses from Looking Glass Bridge.
    /// </summary>
    internal class BridgeWebSocketClient : IDisposable
    {
        private WebSocket WS;
        private Action<string> messageReceivedCallback;

        public BridgeWebSocketClient(Action<string> messageReceivedCallback)
        {
            this.messageReceivedCallback = messageReceivedCallback; 
        }

        public bool TryConnect(string url)
        {
            WS = new WebSocket(url);

            WS.OnMessage += (sender, e) =>
            {
                messageReceivedCallback(e.Data);
            };

            WS.Connect();

            return WS.IsAlive;
        }

        private void WS_OnError(object? sender, WebSocketSharp.ErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            WS?.Close();
        }

        public bool TrySendMessage(string message)
        {
            try
            {
                WS.Send(message);
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public bool Connected()
        {
            if(WS is null) return false;
            return WS.IsAlive;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
