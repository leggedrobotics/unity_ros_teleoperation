using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(DebugLogger))]
public class DebugLoggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DebugLogger myScript = (DebugLogger)target;
        if(GUILayout.Button("Toggle Debug Mode"))
        {
            myScript.toggleDebug();
        }
    }
}
#endif

public class DebugLogger : MonoBehaviour
{
    public static bool active = true;
    public static Gradient debugGradient = new Gradient();
    public int qsize = 150;  // number of messages to keep
    public bool startActive = false;

    private TMPro.TextMeshProUGUI text;
    
    Queue myLogQueue = new Queue();

    public delegate void ToggleDebug();
    public ToggleDebug toggleDebug;

    // Captures Debug.Log output and displays it in a GUI Text for the User to see in the app

    void OnEnable() {
        text = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        text.gameObject.SetActive(startActive);
        toggleDebug = ToggleDebugMode;
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable() {
        Application.logMessageReceived -= HandleLog;
    }

    void OnAwake(){
        var colors = new GradientColorKey[2];
        colors[0] = new GradientColorKey(Color.red, 0.0f);
        colors[1] = new GradientColorKey(Color.green, 1.0f);
        var alphas = new GradientAlphaKey[2];
        alphas[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphas[1] = new GradientAlphaKey(1.0f, 1.0f);
        debugGradient.SetKeys(colors, alphas);
    }

    public void ToggleDebugMode(){
        text.gameObject.SetActive(!text.gameObject.activeSelf);
        active = text.gameObject.activeSelf;
    }


    void HandleLog(string logString, string stackTrace, LogType type) {
        string colorTag = "<color=\"";
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                colorTag += "red\">";
                break;
            case LogType.Warning:
                colorTag += "orange\">";
                break;
            case LogType.Log:
            default:
                colorTag += "black\">";
                break;
        }
        myLogQueue.Enqueue(colorTag + "[" + type + "] : " + logString + "</color>");
        if (type == LogType.Exception)
            myLogQueue.Enqueue(stackTrace);
        while (myLogQueue.Count > qsize)
            myLogQueue.Dequeue();

        text.SetText(string.Join("\n", myLogQueue.ToArray()));
    }


}
