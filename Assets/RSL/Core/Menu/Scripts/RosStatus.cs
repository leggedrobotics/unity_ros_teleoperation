using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.UI;
using TMPro;

namespace RSL.Core.Menu
{
    public class RosStatus : MonoBehaviour
    {
        ROSConnection ros;
        public GameObject ipSetting;
        public GameObject portSetting;

        public string defaultIP = "10.42.0.1";
        public string defaultPort = "10000";

        private RawImage _rawImage;
        private TMPro.TMP_InputField _ipText;
        private TMPro.TextMeshProUGUI _portText;

        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();

            _rawImage = GetComponent<RawImage>();
            _ipText = ipSetting.GetComponent<TMPro.TMP_InputField>();
            _portText = portSetting.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            // try to load ip from player prefs
            string ip = defaultIP;
            if (PlayerPrefs.HasKey("ip"))
            {
                ip = PlayerPrefs.GetString("ip");
            }
            
            ros.RosIPAddress = ip;
            _ipText.text = ip;
            
            int port = int.Parse(defaultPort);
            if (PlayerPrefs.HasKey("port"))
            {
                port = PlayerPrefs.GetInt("port");
            }

            ros.RosPort = port;
            _portText.text = port.ToString();

            Debug.Log("Connecting to " + ros.RosIPAddress + ":" + ros.RosPort);
            ros.Connect();
        }

        public void OnIPDone(string ip)
        {
            ros.RosIPAddress = ip;
            PlayerPrefs.SetString("ip", ip);
            PlayerPrefs.Save();
            ros.Disconnect();
            ros.Connect();
            Debug.Log("Set IP to " + ip);

        }

        public void OnPortDone(string port)
        {
            ros.RosPort = int.Parse(port);
            PlayerPrefs.SetInt("port", ros.RosPort);
            PlayerPrefs.Save();
        }

        void Update()
        {
            _rawImage.color = ros.HasConnectionError ? Color.red : Color.green;
        }
    }
}
