using System;
using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// Represents a property on a Material by its string name, but automatically
    /// converts to its integer representation for faster access with Materials.
    /// </summary>
    [Serializable]
    public struct ShaderPropertyId : ISerializationCallbackReceiver {
        [Delayed]
        [SerializeField] private string name;
        private int id;

        /// <summary>The name of the property.</summary>
        public string Name {
            get { return name; }
        }

        /// <summary>Unity's integer representation of this shader property.</summary>
        public int Id {
            get { return id; }
        }

        public ShaderPropertyId(string name) {
            this.name = name;
            id = Shader.PropertyToID(name);
        }

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() {
            id = Shader.PropertyToID(name);
        }

        public static implicit operator int(ShaderPropertyId property) {
            return property.id;
        }

        public static implicit operator ShaderPropertyId(string name) {
            return new ShaderPropertyId(name);
        }

        public static explicit operator string(ShaderPropertyId property) {
            return property.name;
        }

        public override string ToString() {
            return nameof(ShaderPropertyId) + " { name = " + name + ", id = " + id + " }";
        }
    }
}
