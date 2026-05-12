using System;

namespace FFmpegOut {
    /// <summary>
    /// Represents a key-value string pair containing a metadata tag name and value.
    /// </summary>
    [Serializable]
    public struct MediaMetadataPair {
        public string key;
        public string value;

        public MediaMetadataPair(string key, string value) {
            this.key = key;
            this.value = value;
        }
    }
}
