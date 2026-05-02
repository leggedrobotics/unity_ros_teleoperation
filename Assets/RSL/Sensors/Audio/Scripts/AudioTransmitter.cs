using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.AudioCommon;

namespace RSL.Sensors.Audio
{
    public class AudioTransmitter : MonoBehaviour
    {

        private ROSConnection ros;
        private AudioClip micClip;
        private string audioDataTopic = "/audio/unity_audio";
        private int sampleRate = 16000;
        private List<float> audioBuffer = new List<float>();
        private int pos = 0;
        private int lastPos = 0;
        public bool isActive = true;

        float[] audioSample;
        // Start is called before the first frame update
        void Start()
        {   
            //staring microphone
            micClip = Microphone.Start(null, true, 1, sampleRate);

            // Connects to ROS and registers the audio data topic
            ros = ROSConnection.GetOrCreateInstance();
            ros.RegisterPublisher<AudioDataMsg>(audioDataTopic);

            if (micClip == null)
            {
                Debug.LogError("Microphone not found");
            }
        }

        // Update is called once per frame
        void Update()
        {
        if (isActive)
            {
                GetMicrophoneData();
                PublishAudioData();
            }
        }

        // Get audio data from the microphone
        void GetMicrophoneData()
        {       
            if ((pos = Microphone.GetPosition(null)) > 0)
            {   
                if (lastPos > pos)
                {
                    lastPos = 0;
                }

                if (pos > lastPos)
                {
                    int length = pos - lastPos;
                    float[] audioSample = new float[length];
                    micClip.GetData(audioSample, lastPos);
                    lastPos = pos;

                    lock (audioBuffer)
                    {
                        audioBuffer.AddRange(audioSample);
                    }
                }
            }
        }

        // Publish audio data
        void PublishAudioData()
        {   
            lock (audioBuffer)
            {
                if (audioBuffer.Count > 0)
                {
                    AudioDataMsg msg = new AudioDataMsg();
                    msg.data = new byte[audioBuffer.Count * 2];

                    //adapting float format to S16LE compatible audio stream
                    for (int i = 0; i < audioBuffer.Count; i += 1)
                    {
                        short sample = (short)(audioBuffer[i] * 32768.0f);
                        msg.data[i * 2] = (byte)sample;
                        msg.data[i * 2 + 1] = (byte)(sample >> 8);
                    }

                    //punlishing audio data
                    ros.Publish(audioDataTopic, msg);
                    //Debug.Log("Published audio data: " + msg.data.Length + " bytes");

                    audioBuffer.Clear();
                }
            }
        }

        public void toggleMic()
        {
            isActive = !isActive;
        }
    }
}
