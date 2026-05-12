using System;
using UnityEngine;

namespace LookingGlass {
    //Based on Semantic Versioning, but converts to a System.Version.
    [Serializable]
    public class SerializableVersion : ISerializationCallbackReceiver {
        private Version value = new Version();
        [SerializeField] private int major;
        [SerializeField] private int minor;
        [SerializeField] private int patch;

        public Version Value {
            get {
                if (value == null || HasDifferences)
                    RegenerateValue();
                return value;
            }
        }
        public int Major => major;
        public int Minor => minor;
        public int Patch => patch;

        private bool HasDifferences =>
            value.Major != major ||
            value.Minor != minor ||
            value.Build != patch;

        public void OnBeforeSerialize() {
            major = value.Major;
            minor = value.Minor;
            patch = value.Build;
        }

        public void OnAfterDeserialize() {
            if (value == null || HasDifferences)
                RegenerateValue();
        }

        private void RegenerateValue() {
            value = new Version(major, minor, patch);
        }

        public void CopyFrom(Version source) {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            major = source.Major;
            minor = source.Minor;
            patch = source.Build;
            RegenerateValue();
        }
    }
}
