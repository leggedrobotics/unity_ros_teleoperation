// Storage for the named IP/port profiles behind ConnectionSettings.uxml's
// "Connection Profile" dropdown.
//
// The uGUI palmmenu had no such concept -- it kept a single IP and a single port
// in PlayerPrefs ("ip"/"port") and edited them in place. The profile dropdown
// and its Add/Delete buttons are new with the UI Toolkit panel, so this is the
// backing store they needed.
//
// IMPORTANT -- the "ip"/"port" prefs stay authoritative for the ACTIVE
// connection. RosStatus.Start reads them directly to decide what to connect to,
// and so does anything else that came from the palmmenu era. Profiles are stored
// alongside under their own key and applying one writes through to "ip"/"port",
// rather than replacing that contract. That way a build with this panel and a
// build without it still agree on where to connect.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Core.Menu
{
    [Serializable]
    public struct ConnectionProfile
    {
        public string name;
        public string ip;
        public int port;

        public ConnectionProfile(string name, string ip, int port)
        {
            this.name = name;
            this.ip = ip;
            this.port = port;
        }

        public string Describe() => $"{name} ({ip}:{port})";
    }

    public static class ConnectionProfiles
    {
        public const string ProfilesKey = "connection_profiles";
        public const string ActiveKey = "connection_profile_active";
        // Shared with RosStatus -- see the file header.
        public const string IpKey = "ip";
        public const string PortKey = "port";

        public const int MinPort = 1;
        public const int MaxPort = 65535;

        // JsonUtility cannot serialize a bare List<T>, so it goes in a wrapper.
        [Serializable]
        private class Store
        {
            public List<ConnectionProfile> profiles = new List<ConnectionProfile>();
        }

        public static List<ConnectionProfile> Load()
        {
            string json = PlayerPrefs.GetString(ProfilesKey, null);
            if (string.IsNullOrEmpty(json)) return new List<ConnectionProfile>();

            try
            {
                Store store = JsonUtility.FromJson<Store>(json);
                return store?.profiles ?? new List<ConnectionProfile>();
            }
            catch (Exception e)
            {
                // Corrupt or hand-edited prefs must not brick the panel -- start
                // empty and say so rather than throwing during OnEnable.
                Debug.LogWarning($"[ConnectionProfiles] Could not read '{ProfilesKey}' " +
                                 $"({e.GetType().Name}) -- starting with no saved profiles.");
                return new List<ConnectionProfile>();
            }
        }

        public static void Save(List<ConnectionProfile> profiles)
        {
            var store = new Store { profiles = profiles ?? new List<ConnectionProfile>() };
            PlayerPrefs.SetString(ProfilesKey, JsonUtility.ToJson(store));
            PlayerPrefs.Save();
        }

        public static int ActiveIndex
        {
            get => PlayerPrefs.GetInt(ActiveKey, -1);
            set { PlayerPrefs.SetInt(ActiveKey, value); PlayerPrefs.Save(); }
        }

        /// <summary>
        /// Writes a profile through to the prefs that actually decide the live
        /// connection. Does not itself reconnect -- the caller owns that, since
        /// reconnecting is visible behaviour and not always wanted.
        /// </summary>
        public static void ApplyAsActive(ConnectionProfile profile)
        {
            PlayerPrefs.SetString(IpKey, profile.ip);
            PlayerPrefs.SetInt(PortKey, profile.port);
            PlayerPrefs.Save();
        }

        // ---- Validation -----------------------------------------------------

        /// <summary>
        /// Accepts an IPv4 literal or a hostname: letters, digits, dots and
        /// hyphens. Deliberately looser than a strict dotted-quad check -- the
        /// ROS host is often given by name on these networks, and
        /// ROSConnection.RosIPAddress takes either.
        /// </summary>
        public static bool IsValidHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host)) return false;
            if (host.Length > 253) return false;
            if (host.StartsWith(".") || host.EndsWith(".")) return false;
            if (host.Contains("..")) return false;

            foreach (char c in host)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                          || (c >= '0' && c <= '9') || c == '.' || c == '-';
                if (!ok) return false;
            }
            return true;
        }

        public static bool IsValidPort(long port) => port >= MinPort && port <= MaxPort;

        public static bool TryParsePort(string text, out int port)
        {
            port = 0;
            // uint first so a pasted value above int.MaxValue is rejected as
            // out-of-range rather than throwing/overflowing.
            if (!uint.TryParse(text?.Trim(), out uint parsed)) return false;
            if (!IsValidPort(parsed)) return false;
            port = (int)parsed;
            return true;
        }

        /// <summary>Unique-ifies a profile name so the dropdown never shows duplicates.</summary>
        public static string UniqueName(string desired, List<ConnectionProfile> existing)
        {
            string baseName = string.IsNullOrWhiteSpace(desired) ? "Profile" : desired.Trim();
            string candidate = baseName;
            int suffix = 2;
            while (existing.Exists(p => string.Equals(p.name, candidate, StringComparison.OrdinalIgnoreCase)))
                candidate = $"{baseName} {suffix++}";
            return candidate;
        }
    }
}
