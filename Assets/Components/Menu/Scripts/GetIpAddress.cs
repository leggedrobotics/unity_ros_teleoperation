using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using Unity.Robotics.ROSTCPConnector;

namespace RSL.Core.Menu
{
    public class GetIpAddress : MonoBehaviour
    {
        private TMPro.TextMeshProUGUI _text;

        private string _ipAddress;

        private ROSConnection _ros;

        void Start()
        {
            _text = GetComponent<TMPro.TextMeshProUGUI>();
            _ros = ROSConnection.GetOrCreateInstance();
            _ipAddress = GetLocalIPv4();
            _text.text = _ros.rosVersion.ToString() + " : " + _ipAddress;
        }

        public string GetLocalIPv4()
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
        }

        public void Update()
        {
            if (_text.enabled)
            {
                _text.text = _ros.rosVersion.ToString() + " : " + _ipAddress;
            }
        }

        public void ToggleVisible()
        {
            _text.enabled = !_text.enabled;
        }
    }
}
