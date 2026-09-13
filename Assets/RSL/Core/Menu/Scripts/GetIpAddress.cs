using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using Unity.Robotics.ROSTCPConnector;

namespace RSL.Core.Menu
{
    public class GetIpAddress : MonoBehaviour
    {
        private TMPro.TextMeshProUGUI _text;

        private string _ipAddress;

        private ROSConnection _ros;

        void Start()
        {
            _text = GetComponent<TMPro.TextMeshProUGUI>();
            _ros = ROSConnection.GetOrCreateInstance();
            _ipAddress = GetLocalIPv4();
            _text.text = _ros.rosVersion.ToString() + " : " + _ipAddress;
        }

        public string GetLocalIPv4()
        {
            // AddressList[0] is simply whatever the resolver returns first, which
            // is not necessarily IPv4 -- on this machine it was an IPv6 link-local
            // address (fe80::...), so the menu displayed that despite the name.
            // Filter to IPv4 and prefer a routable address over loopback.
            //
            // WHICH IPv4 also matters: this machine has nine (Tailscale, three
            // ZeroTier, two vEthernet, Ethernet, Wi-Fi, loopback) and the resolver
            // order is arbitrary -- it listed Tailscale first. What the operator
            // wants is the address the ROBOT sees, so ask the OS which interface it
            // would route to the ROS host over and report that one.
            string routed = AddressRoutedToRos();
            if (routed != null) return routed;

            try
            {
                IPAddress loopback = null;
                foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (IPAddress.IsLoopback(address))
                    {
                        if (loopback == null) loopback = address;
                        continue;
                    }
                    return address.ToString();
                }
                // Only 127.0.0.1 available -- still better to show than nothing.
                return loopback != null ? loopback.ToString() : "no IPv4";
            }
            catch (SocketException e)
            {
                // A machine with no resolvable hostname throws here. Caught because
                // this used to run unguarded in Start() and took the whole component
                // down with it, leaving the label permanently blank.
                Debug.LogWarning("[GetIpAddress] Could not resolve local IPv4: " + e.Message);
                return "unavailable";
            }
        }

        /// <summary>
        /// The local IPv4 the OS would use to reach the configured ROS host, or
        /// null if that cannot be determined.
        /// </summary>
        private string AddressRoutedToRos()
        {
            if (_ros == null || string.IsNullOrWhiteSpace(_ros.RosIPAddress)) return null;
            try
            {
                // Connecting a UDP socket SENDS NOTHING -- it only asks the routing
                // table which local endpoint would be used. No packets, no handshake.
                using (var probe = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    probe.Connect(_ros.RosIPAddress, _ros.RosPort);
                    var local = probe.LocalEndPoint as IPEndPoint;
                    if (local == null) return null;
                    // A loopback answer means the host is localhost, which tells the
                    // operator nothing -- fall back to the interface list instead.
                    if (IPAddress.IsLoopback(local.Address)) return null;
                    return local.Address.ToString();
                }
            }
            catch (Exception)
            {
                // Unresolvable host, no route, no network: fall back rather than
                // taking the label down.
                return null;
            }
        }

        public void Update()
        {
            if (_text.enabled)
            {
                _text.text = _ros.rosVersion.ToString() + " : " + _ipAddress;
            }
        }

        public void ToggleVisible()
        {
            _text.enabled = !_text.enabled;
        }
    }
}
