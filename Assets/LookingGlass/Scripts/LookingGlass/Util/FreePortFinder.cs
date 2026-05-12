using System;
using System.Net;
using System.Net.Sockets;

namespace LookingGlass
{
    public static class FreePortFinder
    {
        /// <summary>
        /// Loops through all the ports on the local machine from <paramref name="startPort"/> to <paramref name="endPort"/> (both inclusive) to find the first available port.
        /// </summary>
        /// <remarks>
        /// If none are available, this throws an <see cref="Exception"/>.
        /// </remarks>
        /// <exception cref="Exception"></exception>
        public static int FindFreePort(int startPort, int endPort)
        {
            for (int port = startPort; port <= endPort; port++)
            {
                TcpListener listener = null;
                try
                {
                    listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    return port; // Found a free port
                }
                catch (SocketException)
                {
                    continue; // Port is in use, try the next one
                }
                finally
                {
                    listener?.Stop();
                }
            }

            throw new Exception("No free ports available in the specified range.");
        }
    }
}
