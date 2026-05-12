using System;
using UnityEngine;
using UnityEngine.Serialization;
using LookingGlass.Toolkit;

namespace LookingGlass {
    [Serializable]
    public class RenderStep {
        [Serializable]
        public enum Type {
            CurrentHologramCamera = 0,
            Quilt = 1,
            GenericTexture = 2
        }

        [SerializeField] private bool isEnabled = true;
        [SerializeField] private Type renderType;
        [SerializeField] private Texture quiltTexture;
        [FormerlySerializedAs("renderSettings")]
        [SerializeField] private QuiltSettings quiltSettings;
        [SerializeField] private Texture texture;
        [SerializeField] private Camera postProcessCamera;

        public bool IsEnabled {
            get { return isEnabled; }
            set { isEnabled = value; }
        }

        public Type RenderType {
            get { return renderType; }
            set { renderType = value; }
        }

        public Texture QuiltTexture {
            get { return quiltTexture; }
            set { quiltTexture = value; }
        }

        public QuiltSettings QuiltSettings {
            get { return quiltSettings; }
            set { quiltSettings = value; }
        }

        public Texture Texture {
            get { return texture; }
            set { texture = value; }
        }

        public Camera PostProcessCamera {
            get { return postProcessCamera; }
        }

        public RenderStep(Type renderType) {
            RenderType = renderType;
        }
    }
}
