// Saved profiles live in one JSON file under Application.persistentDataPath,
// not in PlayerPrefs: a layout is a few KB per viewer, and on Android
// PlayerPrefs is a single XML shared-prefs file rewritten on every Save().
// PlayerPrefs only keeps the name of the last profile used.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RSL.Core.Menu
{
    public static class ConnectionProfileStore
    {
        public const string FileName = "connection_profiles.json";
        public const string LastUsedKey = "connection_profile_last";
        // The palm menu's old IP preset list, comma-separated (ROSManager).
        const string LegacyIpsKey = "ips";

        [Serializable]
        class StoreFile
        {
            public List<ConnectionProfile> profiles = new List<ConnectionProfile>();
        }

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static string LastUsed
        {
            get => PlayerPrefs.GetString(LastUsedKey, "");
            set { PlayerPrefs.SetString(LastUsedKey, value ?? ""); PlayerPrefs.Save(); }
        }

        /// <summary>All saved profiles, sorted by name. Never throws: a missing or corrupt file reads as empty.</summary>
        public static List<ConnectionProfile> LoadAll()
        {
            var profiles = new List<ConnectionProfile>();
            try
            {
                if (File.Exists(FilePath))
                    profiles = JsonUtility.FromJson<StoreFile>(File.ReadAllText(FilePath))?.profiles ?? profiles;
            }
            catch (Exception e)
            {
                // A hand-edited or truncated file must not take the menu down with it.
                Debug.LogWarning($"[Profiles] could not read {FilePath} ({e.GetType().Name}: {e.Message}) -- starting empty");
            }
            profiles.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            return profiles;
        }

        public static void SaveAll(List<ConnectionProfile> profiles)
        {
            var file = new StoreFile { profiles = profiles ?? new List<ConnectionProfile>() };
            // Write then move, so a crash mid-write cannot leave a half file behind.
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(file, true));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);
        }

        public static ConnectionProfile Find(List<ConnectionProfile> profiles, string name) =>
            profiles.Find(p => string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>Adds the profile, replacing one with the same name (names are case-insensitive).</summary>
        public static void Upsert(ConnectionProfile profile)
        {
            var profiles = LoadAll();
            profiles.RemoveAll(p => string.Equals(p.name, profile.name, StringComparison.OrdinalIgnoreCase));
            profiles.Add(profile);
            SaveAll(profiles);
        }

        public static bool Delete(string name)
        {
            var profiles = LoadAll();
            int removed = profiles.RemoveAll(p => string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));
            if (removed > 0) SaveAll(profiles);
            if (string.Equals(LastUsed, name, StringComparison.OrdinalIgnoreCase)) LastUsed = "";
            return removed > 0;
        }

        /// <summary>
        /// One-time import of the old IP preset list as connection-only
        /// profiles, so presets saved by earlier builds still show up. Runs
        /// only while no profile file exists yet.
        /// </summary>
        public static void MigrateLegacyIps(int port)
        {
            if (File.Exists(FilePath) || !PlayerPrefs.HasKey(LegacyIpsKey)) return;
            var profiles = new List<ConnectionProfile>();
            foreach (string raw in PlayerPrefs.GetString(LegacyIpsKey).Split(','))
            {
                string ip = raw.Trim();
                if (ip.Length == 0) continue;
                string name = $"{ip}:{port}";
                if (Find(profiles, name) == null)
                    profiles.Add(new ConnectionProfile { name = name, ip = ip, port = port });
            }
            if (profiles.Count == 0) return;
            SaveAll(profiles);
            Debug.Log($"[Profiles] imported {profiles.Count} saved IP(s) from the old preset list");
        }
    }
}
