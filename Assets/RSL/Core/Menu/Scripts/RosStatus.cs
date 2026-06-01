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
        public Numpad numpad;

        public string defaultIP = "10.42.0.1";
        public string defaultPort = "10000";

        private RawImage _rawImage;
        private TMPro.TMP_InputField _ipText;
        private TMPro.TextMeshProUGUI _portText;

        private uint mode = 0; // 0 = off, 1 = ip, 2 = port

        void Start()
        {
            ros = ROSConnection.GetOrCreateInstance();

            _rawImage = GetComponent<RawImage>();
            _ipText = ipSetting.GetComponent<TMPro.TMP_InputField>();
            _portText = portSetting.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            numpad.gameObject.SetActive(false);
            numpad.OnButtonPressed += OnNumPadButtonPressed;

            // ipSetting.GetComponent<Button>().onClick.AddListener(OnIP);
            // portSetting.GetComponent<Button>().onClick.AddListener(OnPort);

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

        private void OnNumPadButtonPressed(int num)
        {
            TMPro.TextMeshProUGUI _text =  _portText;
            string text = _text.text;
            if (num == -2 && text.Length > 0)
            {
                text = text.Substring(0, text.Length - 1);
            } 
            else if (num == -1 && mode == 1) // disable period for port
            {
                text += ".";
            }
            else
            {
                text += num.ToString();
            }

            _text.text = text;

            if (mode == 1)
            {
                ros.RosIPAddress = text;
            }
            else
            {
                ros.RosPort = int.Parse(text);
            }
            
        }

        private void OnIP()
        {
            if (mode == 1)
            {
                mode = 0;
                numpad.gameObject.SetActive(false);
                ros.Disconnect();
                ros.Connect();
                PlayerPrefs.SetString("ip", ros.RosIPAddress);
                PlayerPrefs.Save();
            }
            else
            {
                mode = 1;
                numpad.gameObject.SetActive(true);
            }
        }

        private void OnPort()
        {
            if (mode == 2)
            {
                mode = 0;
                numpad.gameObject.SetActive(false);
                ros.Disconnect();
                ros.Connect();
                PlayerPrefs.SetInt("port", ros.RosPort);
                PlayerPrefs.Save();
            }
            else
            {
                mode = 2;
                numpad.gameObject.SetActive(true);
            }
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
