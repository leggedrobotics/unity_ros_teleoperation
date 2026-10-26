using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UvgRos;

namespace RSL.Core.Menu
{
    public class ROSManager : MonoBehaviour
    {
        // public delegate void ConnectionColor(Color color);
        // public ConnectionColor OnConnectionColor;
        // public event ConnectionColor OnConnectionColor;

        public UnityEvent<bool> OnConnectionColor;
        public UnityEvent<bool> OnConnectionStagnant;

        public string defaultIP = "10.42.0.1";
        public string defaultPort = "10000";

        public GameObject ipSetting;
        public GameObject portSetting;
        private string _ip;
        private int _port;

        private TMPro.TMP_InputField _ipText;
        private TMPro.TMP_InputField _portText;

        private UvgRosConnection _ros;
        private bool _connected = false;
        private bool _stagnant = false;

        public GameObject menu;

        void Start()
        {
            if(PlayerPrefs.HasKey("ip"))
            {
                _ip = PlayerPrefs.GetString("ip");
            }
            else
            {
                _ip = defaultIP;
            }

            if(PlayerPrefs.HasKey("port"))
            {
                _port = PlayerPrefs.GetInt("port");
            }
            else
            {
                _port = int.Parse(defaultPort);
            }


            _ipText = ipSetting.GetComponent<TMPro.TMP_InputField>();
            _portText = portSetting.GetComponentInChildren<TMPro.TMP_InputField>();

            _ros = UvgRosConnection.GetOrCreateInstance();

            menu.SetActive(false);

            _ros.RosPort = _port;
            _ros.RosIPAddress = _ip;

            _ros.Connect();

            _ipText.text = _ip;
            _portText.text = _port.ToString();

            _connected = !_ros.HasConnectionError;

            OnConnectionColor.Invoke(_connected);

        }

        public void ToggleMenu()
        {
            menu.SetActive(!menu.activeSelf);
        }

        public void OnIPDone(string ip)
        {
            _ros.Disconnect();
            _ip = ip;
            _ros.RosIPAddress = _ip;
            PlayerPrefs.SetString("ip", _ip);
            PlayerPrefs.Save();
            _ros.Connect();
        }

        public void OnPortDone(string port)
        {
            _ros.Disconnect();
            _port = int.Parse(port);
            _ros.RosPort = _port;
            PlayerPrefs.SetInt("port", _port);
            PlayerPrefs.Save();
            _ros.Connect();
        }

        public string CurrentIP => _ip;
        public int CurrentPort => _port;

        /// <summary>
        /// Switches to ip:port with a single reconnect (OnIPDone + OnPortDone
        /// would reconnect twice), updating the fields and saved prefs. Does
        /// not throw when the server is unreachable.
        /// Used by ProfileManager when a profile is loaded.
        /// </summary>
        public void SetConnection(string ip, int port)
        {
            _ip = ip;
            _port = port;
            if (_ipText != null) _ipText.text = _ip;
            if (_portText != null) _portText.text = _port.ToString();
            PlayerPrefs.SetString("ip", _ip);
            PlayerPrefs.SetInt("port", _port);
            PlayerPrefs.Save();
            try
            {
                _ros.Connect(_ip, _port);
            }
            catch (System.Exception e)
            {
                // Unreachable is a normal state for a saved profile (robot off,
                // other network): keep the settings, the status light shows it.
                Debug.LogWarning($"[ROSManager] could not connect to {_ip}:{_port}: {e.Message}");
            }
        }

        void Update()
        {
            if(_connected != !_ros.HasConnectionError)
            {
                _connected = !_ros.HasConnectionError;
                OnConnectionColor.Invoke(_connected);
            }
            // Was `_ros.LastMessageReceivedRealtime - Time.time < 2.5` against
            // ROSConnection -- almost always true (a stamp bounded by the
            // session's elapsed time, minus a monotonically growing Time.time,
            // is usually deeply negative), so _stagnant was effectively always
            // true regardless of real traffic. UvgRosConnection.
            // SecondsSinceLastMessage is an elapsed-time value, so the
            // corrected, presumably-intended check is the other way around:
            // stagnant when MORE than 2.5s have passed since the last message.
            if(_stagnant != _ros.SecondsSinceLastMessage > 2.5)
            {
                _stagnant = _ros.SecondsSinceLastMessage > 2.5;
                OnConnectionStagnant.Invoke(_stagnant);
            }

        }
    }
}
