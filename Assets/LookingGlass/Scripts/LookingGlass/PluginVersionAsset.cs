using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass {
    public class PluginVersionAsset : ScriptableObject {
        [SerializeField] private PluginSemanticVersion version = new PluginSemanticVersion("v1.5.0");

        public SemanticVersion Version => version;
    }
}
