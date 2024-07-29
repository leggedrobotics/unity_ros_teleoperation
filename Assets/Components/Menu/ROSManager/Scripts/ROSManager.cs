using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using Unity.Robotics.ROSTCPConnector;


public class ROSManager : MonoBehaviour
{
    // public delegate void ConnectionColor(Color color);
    // public ConnectionColor OnConnectionColor;
    // public event ConnectionColor OnConnectionColor;

    public UnityEvent<bool> OnConnectionColor;
    public UnityEvent<bool> OnConnectionStagnant;


    public Dropdown ipDropdown;

    private List<string> _ips;
    public string defaultIP = "10.42.0.1";
    public string defaultPort = "10000";

    public GameObject ipSetting;
    public GameObject portSetting;
    private string _ip;
    private int _port;

    private TMPro.TMP_InputField _ipText;
    private TMPro.TMP_InputField _portText;

    private ROSConnection _ros;
    private bool _connected = false;
    private bool _stagnant = false;

    public GameObject menu;

    void Start()
    {
        _ips = new List<string>();
        string ips = "";
        if (PlayerPrefs.HasKey("ips"))
        {
            ips = PlayerPrefs.GetString("ips");
        }
        foreach(string ip in ips.Split(',')) {
            _ips.Add(ip);
        }

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

        _ros = ROSConnection.GetOrCreateInstance();

        menu.SetActive(false);

        ipDropdown.ClearOptions();
        ipDropdown.AddOptions(_ips);

        ipDropdown.onValueChanged.AddListener(delegate {
            OnPreset(ipDropdown.value);
        });




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

    public void OnPreset(int index)
    {
        _ip = _ips[index];
        _ipText.text = _ip;
        OnIPDone(_ip);
    }

    void Update()
    {
        if(_connected != !_ros.HasConnectionError)
        {
            _connected = !_ros.HasConnectionError;
            OnConnectionColor.Invoke(_connected);
        }
        if(_stagnant != _ros.LastMessageReceivedRealtime - Time.time < 2.5)
        {
            _stagnant = _ros.LastMessageReceivedRealtime - Time.time < 2.5;
            OnConnectionStagnant.Invoke(_stagnant);
        }

    }

    public void DeleteIP()
    {
        _ips.Remove(_ip);
        PlayerPrefs.SetString("ips", string.Join(",", _ips));
        PlayerPrefs.Save();

        ipDropdown.ClearOptions();
        ipDropdown.AddOptions(_ips);
    }

    public void SaveIP()
    {
        if(!_ips.Contains(_ip))
        {
            _ips.Insert(0, _ip);
        }
        PlayerPrefs.SetString("ips", string.Join(",", _ips));
        PlayerPrefs.Save();

        ipDropdown.ClearOptions();
        ipDropdown.AddOptions(_ips);
    }
}
