using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace LookingGlass {
    [Serializable]
    public abstract class PropertyGroup : ISerializationCallbackReceiver {
        [SerializeField, HideInInspector] protected HologramCamera hologramCamera;

        internal void Init(HologramCamera hologramCamera) {
            Assert.IsNotNull(hologramCamera);
            this.hologramCamera = hologramCamera;
            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        public void OnBeforeSerialize() {
            //For some reason, there are some times where OnBeforeSerialize is called before...
            //  - The LookingGlass constructor
            //  - LookingGlass.Awake
            //  - LookingGlass.OnEnable
            //So to prevent unnecessary NullReferenceExceptions, let's use inheritance here with our own OnValidate()
            //To only be called when the hologramCamera field is non-null:
            if (hologramCamera != null)
                OnValidate();
        }
        public void OnAfterDeserialize() { }

        protected internal virtual void OnValidate() { }
    }
}
