using System.Collections;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.AudioCommon;
using System;

namespace RSL.Sensors.Audio
{
    public class AudioReceiver : MonoBehaviour
    {
        private ROSConnection ros;
        private AudioSource audioSource;
        private CircularBuffer<float> audioBuffer;
        private string audioDataTopic = "/audio/robot_audio";
        private int sampleRate = 16000;
        private int channelCount = 1;
        private int clipLengthSeconds = 1;
        private AudioClip streamingClip;
        public bool isActive = true;
        private float lastSample = 0.0f;

        IEnumerator StartPlaybackWhenReady()
        {
            // Wait until buffer is at least 0.5 second full
            while (audioBuffer.Count < sampleRate / 2)
            {
                yield return null;
            }

            double scheduledTime = AudioSettings.dspTime + 0.1; // Wait 100ms for safety
            audioSource.PlayScheduled(scheduledTime);
        }

        void Start()
        {
            // Set up audio source
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.0f;

            // Add audio filters
            var lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            lowPass.cutoffFrequency = 8000;

            var highPass = gameObject.AddComponent<AudioHighPassFilter>();
            highPass.cutoffFrequency = 150;

            // Increase buffer to hold 4 seconds of audio
            audioBuffer = new CircularBuffer<float>(sampleRate * 2);

            // Create streaming audio clip
            streamingClip = AudioClip.Create("StreamingAudio", sampleRate * clipLengthSeconds, channelCount, sampleRate, true, OnAudioRead);
            audioSource.clip = streamingClip;

            // Connect to ROS and subscribe
            ros = ROSConnection.GetOrCreateInstance();
            ros.Subscribe<AudioDataMsg>(audioDataTopic, ReceiveAudioMessage);

            // Wait for buffer to fill before playing
            StartCoroutine(StartPlaybackWhenReady());
        }

        void ReceiveAudioMessage(AudioDataMsg msg)
        {
            if (!isActive) return;

            byte[] byteData = msg.data;
            float[] floatData = new float[byteData.Length / 2];

            // Convert 16-bit PCM bytes to float
            for (int i = 0; i < floatData.Length; i++)
            {
                short sample = (short)(byteData[i * 2] | (byteData[i * 2 + 1] << 8));
                floatData[i] = sample / 32768.0f;
            }

            lock (audioBuffer)
            {
                foreach (var sample in floatData)
                {
                    audioBuffer.PushBack(sample);
                }
            }
        }

        void OnAudioRead(float[] data)
        {
            lock (audioBuffer)
            {
                if (audioBuffer.Count == 0)
                {
                    Debug.LogWarning("Audio underrun!");
                }
                for (int i = 0; i < data.Length; i++)
                {
                    if (audioBuffer.Count > 0)
                    {
                        lastSample = audioBuffer.PopFront();
                        data[i] = lastSample;
                    }
                    else
                    {
                        // Prevent clicking by repeating last sample
                        data[i] = lastSample;
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (ros != null)
            {
                ros.Unsubscribe(audioDataTopic);
            }
        }

        public void toggleSpeaker()
        {
            isActive = !isActive;
        }

    }
}
