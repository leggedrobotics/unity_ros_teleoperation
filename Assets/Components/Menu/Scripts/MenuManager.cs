using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(MenuManager))]
public class MenuManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MenuManager menuManager = (MenuManager)target;
        if (GUILayout.Button("Red"))
        {
            menuManager.ConnectionColor(Color.red);
        }
        if (GUILayout.Button("Green"))
        {
            menuManager.ConnectionColor(Color.green);
        }

        for(int i = 0; i < menuManager.menus.Length; i++)
        {
            if (GUILayout.Button("Toggle " + menuManager.menus[i].name))
            {
                menuManager.ToggleMenu(i);
            }
        }
    }
}
#endif

public class MenuManager : MonoBehaviour
{
    public UnityEvent<bool> MenuState;


    public GameObject[] menus;


    private Material _leftEnd;
    private Material _rightEnd;
    private DebugLogger[] _loggers;

    private int _open = -1;

    private GameObject[] _menus;

    private void Awake() 
    {
        _leftEnd = GetComponent<Renderer>().materials[1];
        _rightEnd = GetComponent<Renderer>().materials[2];        
        // _menus = new GameObject[] {wifiMenu, settingMenu, cameraMenu, lidarMenu};
        _loggers = FindObjectsOfType<DebugLogger>();


        foreach(GameObject menu in menus)
        {
            menu.SetActive(false);
        }
    }

    public void ConnectionColor(Color c)
    {
        _leftEnd.color = c;
        _rightEnd.color = c;
    }

    public void ToggleLoggers()
    {
        foreach(DebugLogger logger in _loggers)
        {
            logger.toggleDebug();
        }
    }

    public void OnRosStatus(bool connected)
    {
        ConnectionColor(connected ? Color.green : Color.red);
    }

    private void UpdateState()
    {
        for(int i = 0; i < menus.Length; i++)
        {
            menus[i].SetActive(i == _open);
        }
    }

    public void ToggleWifiMenu()
    {
        _open = _open == 0 ? -1 : 0;
        UpdateState();
    }

    public void ToggleSettingMenu()
    {
        _open = _open == 1 ? -1 : 1;
        UpdateState();
    }

    public void ToggleCameraMenu()
    {
        _open = _open == 2 ? -1 : 2;
        UpdateState();
    }

    public void ToggleLidarMenu()
    {
        _open = _open == 3 ? -1 : 3;
        UpdateState();
    }

    public void ToggleMenu(int i)
    {
        _open = _open == i ? -1 : i;
        UpdateState();

        // MenuState.Invoke(_open != -1);
    }
}
