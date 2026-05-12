using System;
using UnityEngine;

namespace LookingGlass.Demos{
    public class AudioRotateControl : MonoBehaviour {

        [SerializeField] float minSpeed = 0f;
        [SerializeField] float maxSpeed = 5f;

        [SerializeField] AudioSource audioSource;

        [SerializeField] float audioMaxSpeed = 0.01f;
        [SerializeField] float audioPitchMod = 0f;
        [SerializeField] float audioLerpSpeed = 0.1f;

        [SerializeField] float minPitch = .85f;
        [SerializeField] float maxPitch = 1.05f;

        [SerializeField] float minVol = 0f;
        [SerializeField] float maxVol = 1f;

        private ModelController modelController = null;

        // Settable range for remapping rotationVelocity based on audio intensity 
        private void Start() {
            if (modelController == null) {
                modelController = GetComponent<ModelController>();
            }
        }
        private void Update() {
            if (modelController == null) return;

            float newVol = Remap(Mathf.Abs(modelController.RotationVelocity), minSpeed, maxSpeed, 0f, 1f);
            if (newVol > audioSource.volume)
                audioSource.volume = newVol;
            else
                audioSource.volume = Mathf.Lerp(audioSource.volume, newVol, .2f);

            audioSource.pitch = Remap(Mathf.Abs(modelController.RotationVelocity), minSpeed, maxSpeed, minPitch, maxPitch);

        }

        private float Remap(float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}
