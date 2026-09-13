using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using RSL.Core.Menu;

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

        /// <summary>Is the microphone currently transmitting.</summary>
        public bool MicActive => micActive;

        /// <summary>Is the speaker currently receiving.</summary>
        public bool SpeakerActive => speakerActive;

        // micButton/speakerButton are uGUI objects on the old manager panel and
        // are null when the driving UI is a UXML row. They used to be
        // dereferenced unguarded from toggleMic, toggleSpeaker and deleteAll.
        private static void SetToggleSprite(GameObject button, bool active)
        {
            if (button == null) return;
            var toggle = button.GetComponent<ToggleButton>();
            if (toggle == null) return;
            if (active) toggle.setActiveSprite(); else toggle.setInactiveSprite();
        }

        /// <summary>
        /// Adds the mic and speaker toggles to this manager's row, and makes Clear
        /// also tear down the mic/speaker objects -- the uGUI panel wired its Clear
        /// button to deleteAll, not ClearAll, so clearing audio has always meant
        /// both.
        /// </summary>
        public override void BindUi(VisualElement row)
        {
            base.BindUi(row);
            if (row == null) return;

            Button mic = row.Q<Button>("Mic");
            if (mic != null) mic.clicked += () => { toggleMic(); RefreshUi(); };

            Button speaker = row.Q<Button>("Speaker");
            if (speaker != null) speaker.clicked += () => { toggleSpeaker(); RefreshUi(); };

            Button clear = row.Q<Button>("Clear");
            if (clear != null) clear.clicked += () => { deleteAll(); RefreshUi(); };
        }

        public override void RefreshUi()
        {
            base.RefreshUi();
            if (Row == null) return;
            Row.Q<Button>("Mic")?.EnableInClassList("is-on", micActive);
            Row.Q<Button>("Speaker")?.EnableInClassList("is-on", speakerActive);
        }

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

                SetToggleSprite(micButton, true);
                micActive = true;

            } else {

                //dissabling the microphone
                AudioTransmitter micTransmitter = mic.GetComponent<AudioTransmitter>();
                if (micTransmitter != null && micTransmitter.isActive)
                {
                    micTransmitter.toggleMic(); // Activate the microphone
                }

                SetToggleSprite(micButton, false);
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

                SetToggleSprite(speakerButton, true);
                speakerActive = true;

            } else {

                //dissabling the speaker
                AudioReceiver speakerReceiver = speaker.GetComponent<AudioReceiver>();
                if (speakerReceiver != null && speakerReceiver.isActive)
                {
                    speakerReceiver.toggleSpeaker(); // Activate the speaker
                }

                SetToggleSprite(speakerButton, false);
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
                SetToggleSprite(micButton, false);
            }

            if (speaker != null)
            {
                Destroy(speaker);
                speaker = null;
                speakerActive = false;
                SetToggleSprite(speakerButton, false);
            }
        }

    }
}

