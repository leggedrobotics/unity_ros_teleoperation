using System;

namespace LookingGlass {
    [Serializable]
    public class PluginSemanticVersion : SemanticVersion {
        public override bool IsReadOnly => !HologramCamera.isDevVersion;

        public PluginSemanticVersion(string value) : base(value) { }
    }
}