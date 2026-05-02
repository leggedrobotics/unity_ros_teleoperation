using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using TMPro;

namespace RSL.Sensors.ServiceCaller
{
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(ServiceStream))]
    public class ServiceStreamEditor : RSL.Core.SensorStreamEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ServiceStream myScript = (ServiceStream)target;
            if (GUILayout.Button("Subscribe to Service"))
            {
                myScript.SubscribeToService();
            }
            if (GUILayout.Button("Trigger Service"))
            {
                myScript.TriggerService();
            }
        }
    }
    #endif

    public class ServiceStream : RSL.Core.SensorStream
    {
        public TextMeshProUGUI topicText;
        public TMPro.TMP_InputField topicInputField;


        // Start is called before the first frame update
        void Awake()
        {
            topicText.text = topicName;
            topicInputField.text = topicName;

            _ros = ROSConnection.GetOrCreateInstance();
        }

        public override void OnTopicChange(string newTopic)
        {
            topicName = newTopic;
            topicText.text = topicName;
            topicInputField.text = topicName;
            Debug.Log($"Topic changed to: {topicName}");
            _ros.RegisterRosService<EmptyRequest, EmptyResponse>(topicName);

        }

        public void SubscribeToService()
        {
            OnTopicChange(topicInputField.text);
        }

        public void TriggerService()
        {
            Debug.Log($"Triggering service: {topicName}");
            _ros.SendServiceMessage<EmptyResponse>(topicName, new EmptyRequest(), ServiceCallback);
        }

        private void ServiceCallback(EmptyResponse response)
        {
            Debug.Log($"Service response received: {response}");
        }

        public override void ToggleTrack(int trackId)
        {
            // Add your logic here
            Debug.Log($"Toggling track with ID: {trackId}");
        }
    }
}
