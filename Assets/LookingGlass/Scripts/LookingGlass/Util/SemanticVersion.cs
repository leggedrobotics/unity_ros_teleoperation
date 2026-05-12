using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// <para>
    /// Represents a version in the form of <c>MAJOR.MINOR.PATCH-LABEL</c>.<br />
    /// It may contain an optional "v" prefix.
    /// </para>
    /// 
    /// Some examples include:
    /// <list type="bullet">
    /// <item><c>1.0.3</c></item>
    /// <item><c>v1.0.3</c></item>
    /// <item><c>v1.6.2-alpha3</c></item>
    /// <item><c>v2.0.9-rc6</c></item>
    /// </list>
    /// </summary>
    [Serializable]
    public class SemanticVersion : ISerializationCallbackReceiver {
        private static readonly Lazy<Regex> Pattern = new Lazy<Regex>(() =>
            new Regex("(?<prefix>v?)(?<major>[0-9]+)\\.(?<minor>[0-9]+)(\\.(?<patch>[0-9]+))?\\-?(?<label>[a-z0-9]*)")
        );

        [SerializeField] internal string value = "";

        //Automatically updated from the "value" field above:
        [SerializeField] internal bool isValid;
        [SerializeField] internal string prefix;
        [SerializeField] internal int major;
        [SerializeField] internal int minor;
        [SerializeField] internal int patch;
        [SerializeField] internal string label;

        [NonSerialized] private Version systemVersion;
        [NonSerialized] private bool bypassReadOnly = false;

        public bool IsValid => isValid;
        public virtual bool IsReadOnly => false;

        /// <summary>
        /// <para>The full string name of the version.</para>
        /// <para>
        /// For example,<br />
        /// <list type="bullet">
        /// <item>v1.0.0</item>
        /// <item>2.11.0</item>
        /// <item>v1.6.2-alpha3</item>
        /// </list>
        /// </para>
        /// </summary>
        public string Value {
            get { return value; }
            set {
                ValidateBeforeModifying();
                this.value = (value == null) ? "" : value;
                ParseFromFullName();
            }
        }

        public bool HasPrefix => !string.IsNullOrEmpty(prefix);
        public string Prefix {
            get { return prefix; }
            set {
                ValidateBeforeModifying();
                prefix = (value == null) ? "" : value;
                RegenerateFullName(false);
            }
        }

        public int Major {
            get { return major; }
            set {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The major version must be 0 or greater!");
                ValidateBeforeModifying();
                major = value;
                RegenerateFullName(true);
            }
        }

        public int Minor {
            get { return minor; }
            set {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The minor version must be 0 or greater!");
                ValidateBeforeModifying();
                minor = value;
                RegenerateFullName(true);
            }
        }

        public int Patch {
            get { return patch; }
            set {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The patch version must be 0 or greater!");
                ValidateBeforeModifying();
                patch = value;
                RegenerateFullName(true);
            }
        }

        public bool HasLabel => !string.IsNullOrEmpty(label);
        public string Label {
            get { return label; }
            set {
                ValidateBeforeModifying();
                label = (value == null) ? "" : value;
                RegenerateFullName(false);
            }
        }

        public Version SystemVersion => systemVersion;

        public SemanticVersion(string value) {
            try {
                bypassReadOnly = true;
                Value = value;
            } finally {
                bypassReadOnly = false;
            }
        }

        public void OnBeforeSerialize() {
            ParseFromFullName();
        }

        public void OnAfterDeserialize() { }

        private void ValidateBeforeModifying() {
            if (bypassReadOnly)
                return;
            if (IsReadOnly)
                throw new InvalidOperationException("This version is readonly!");
        }

        private void ParseFromFullName() {
            Match m = Pattern.Value.Match(value);

            if (m.Success) {
                try {
                    GroupCollection groups = m.Groups;
                    prefix = groups["prefix"].Value;
                    major = int.Parse(groups["major"].Value);
                    minor = int.Parse(groups["minor"].Value);
                    try {
                        patch = int.Parse(groups["patch"].Value);
                    } catch (FormatException) {
                        patch = 0;
                    }
                    label = groups["label"].Value;
                    isValid = true;
                    RegenerateSystemVersion();
                } catch (Exception e) {
                    Debug.LogException(e);
                    Clear();
                }
            } else {
                Clear();
            }
        }

        private void RegenerateFullName(bool needsSystemVersionRegeneratd) {
            value = prefix + major + '.' + minor + '.' + patch + (HasLabel ? '-' + label : "");
            if (needsSystemVersionRegeneratd)
                RegenerateSystemVersion();
        }

        private void RegenerateSystemVersion() {
            systemVersion = new Version(major, minor, patch);
        }

        private void Clear() {
            isValid = false;

            prefix =
            label =
                "";

            major =
            minor =
            patch =
                0;

            RegenerateSystemVersion();
        }

        public void IncreaseMajor() {
            ValidateBeforeModifying();
            major++;
            minor = 0;
            patch = 0;
            RegenerateFullName(true);
        }

        public void IncreaseMinor() {
            ValidateBeforeModifying();
            minor++;
            patch = 0;
            RegenerateFullName(true);
        }

        public override int GetHashCode() => systemVersion.GetHashCode();
        public override string ToString() => value;
        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            switch (obj) {
                case Version otherSysVersion:
                    return otherSysVersion.Equals(systemVersion);
                case SemanticVersion otherVersion:
                    bool prefixEqualOrDiffersInV;
                    if (prefix == otherVersion.prefix) {
                        prefixEqualOrDiffersInV = true;
                    } else {
                        prefixEqualOrDiffersInV =
                            (string.IsNullOrWhiteSpace(prefix) || prefix == "v") &&
                            (string.IsNullOrWhiteSpace(otherVersion.prefix) || otherVersion.prefix == "v");
                    }

                    return prefixEqualOrDiffersInV &&
                        major == otherVersion.major &&
                        minor == otherVersion.minor &&
                        patch == otherVersion.patch &&
                        label == otherVersion.label;
            }
            return false;
        }

        public static implicit operator Version(SemanticVersion v) => v.systemVersion;
        public static implicit operator SemanticVersion(string value) => new SemanticVersion(value);

        public static bool operator ==(SemanticVersion left, SemanticVersion right) => left.Equals(right);
        public static bool operator !=(SemanticVersion left, SemanticVersion right) => !(left == right);
        public static bool operator >(SemanticVersion left, SemanticVersion right) => left.systemVersion > right.systemVersion;
        public static bool operator <(SemanticVersion left, SemanticVersion right) => left.systemVersion < right.systemVersion;
        public static bool operator >=(SemanticVersion left, SemanticVersion right) => left.systemVersion >= right.systemVersion;
        public static bool operator <=(SemanticVersion left, SemanticVersion right) => left.systemVersion <= right.systemVersion;

        public static bool operator ==(Version left, SemanticVersion right) => left.Equals(right.systemVersion);
        public static bool operator !=(Version left, SemanticVersion right) => !(left == right.systemVersion);
        public static bool operator >(Version left, SemanticVersion right) => left > right.systemVersion;
        public static bool operator <(Version left, SemanticVersion right) => left < right.systemVersion;
        public static bool operator >=(Version left, SemanticVersion right) => left >= right.systemVersion;
        public static bool operator <=(Version left, SemanticVersion right) => left <= right.systemVersion;

        public static bool operator ==(SemanticVersion left, Version right) => left.systemVersion.Equals(right);
        public static bool operator !=(SemanticVersion left, Version right) => !(left.systemVersion == right);
        public static bool operator >(SemanticVersion left, Version right) => left.systemVersion > right;
        public static bool operator <(SemanticVersion left, Version right) => left.systemVersion < right;
        public static bool operator >=(SemanticVersion left, Version right) => left.systemVersion >= right;
        public static bool operator <=(SemanticVersion left, Version right) => left.systemVersion <= right;
    }
}
