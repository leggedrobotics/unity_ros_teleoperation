using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace RSL.Sensors.Audio
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(AudioManager))]
    public class AudioManagerEditor : RSL.Core.SensorManagerEditor
    {
    }

    #endif

    public class AudioManager : RSL.Core.SensorManager
    {
        public Sprite untracked;
        public Sprite tracked;
        private bool _allTracking = false;
        private bool micActive = false;
        private bool speakerActive = false;
        public GameObject micPrefab;
        public GameObject micButton;
        private GameObject mic;
        public GameObject speakerPrefab;
        public GameObject speakerButton;
        private GameObject speaker;

        public void toggleMic()
        {
            if (!micActive)
            {   
                //creating microphone if not already created
                if (mic == null)
                {
                    Vector3 pos = Vector3.zero;
                    Quaternion rot = Quaternion.identity;
                    mic = Instantiate(micPrefab, pos, rot);
                }
                //activates the microphone
                AudioTransmitter micTransmitter = mic.GetComponent<AudioTransmitter>();
                if (micTransmitter != null && !micTransmitter.isActive)
                {
                    micTransmitter.toggleMic(); // Activate the microphone
                }

                micButton.GetComponent<ToggleButton>().setActiveSprite();
                micActive = true;

            } else {

                //dissabling the microphone
                AudioTransmitter micTransmitter = mic.GetComponent<AudioTransmitter>();
                if (micTransmitter != null && micTransmitter.isActive)
                {
                    micTransmitter.toggleMic(); // Activate the microphone
                }

                micButton.GetComponent<ToggleButton>().setInactiveSprite();
                micActive = false;
            }
        }

        public void toggleSpeaker()
        {
            if (!speakerActive)
            {
                //creating speaker if not already created
                if (speaker == null)
                {
                    Vector3 pos = Vector3.zero;
                    Quaternion rot = Quaternion.identity;
                    speaker = Instantiate(speakerPrefab, pos, rot);
                }
                //activates the speaker
                AudioReceiver speakerReceiver = speaker.GetComponent<AudioReceiver>();
                if (speakerReceiver != null && !speakerReceiver.isActive)
                {
                    speakerReceiver.toggleSpeaker(); // Activate the speaker
                }

                speakerButton.GetComponent<ToggleButton>().setActiveSprite();
                speakerActive = true;

            } else {

                //dissabling the speaker
                AudioReceiver speakerReceiver = speaker.GetComponent<AudioReceiver>();
                if (speakerReceiver != null && speakerReceiver.isActive)
                {
                    speakerReceiver.toggleSpeaker(); // Activate the speaker
                }

                speakerButton.GetComponent<ToggleButton>().setInactiveSprite();
                speakerActive = false;
            }
        }

        public void deleteAll()
        {
            if (mic != null)
            {
                Destroy(mic);
                mic = null;
                micActive = false;
                micButton.GetComponent<ToggleButton>().setInactiveSprite();
            }

            if (speaker != null)
            {
                Destroy(speaker);
                speaker = null;
                speakerActive = false;
                speakerButton.GetComponent<ToggleButton>().setInactiveSprite();
            }
        }

    }
}

