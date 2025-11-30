using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;


#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SensorManager))]
public class SensorManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();


        SensorManager myScript = (SensorManager)target;
        if (GUILayout.Button("Add Sensor"))
        {
            myScript.AddSensor();
        }
        if (GUILayout.Button("Clear All"))
        {
            myScript.ClearAll();
        }
        if (GUILayout.Button("Serialize"))
        {
            Debug.Log(myScript.Serialize());
        }
        if (GUILayout.Button("Deserialize"))
        {
            myScript.Deserialize("{\"data\":[\"{\\\"position\\\":{\\\"x\\\":0.916685938835144,\\\"y\\\":0.0751071348786354,\\\"z\\\":0.0008342347573488951},\\\"rotation\\\":{\\\"x\\\":0.08095825463533402,\\\"y\\\":0.25364258885383608,\\\"z\\\":-0.22871945798397065,\\\"w\\\":0.9363752603530884},\\\"scale\\\":{\\\"x\\\":0.010000000707805157,\\\"y\\\":0.009999999776482582,\\\"z\\\":0.010000000707805157},\\\"topicName\\\":\\\"test2\\\",\\\"trackingState\\\":1,\\\"flip\\\":false,\\\"stereo\\\":false}\",\"{\\\"position\\\":{\\\"x\\\":0.5,\\\"y\\\":0.07999999821186066,\\\"z\\\":0.0},\\\"rotation\\\":{\\\"x\\\":0.0,\\\"y\\\":0.0,\\\"z\\\":0.0,\\\"w\\\":1.0},\\\"scale\\\":{\\\"x\\\":0.010000000707805157,\\\"y\\\":0.009999999776482582,\\\"z\\\":0.010000000707805157},\\\"topicName\\\":\\\"test1\\\",\\\"trackingState\\\":0,\\\"flip\\\":false,\\\"stereo\\\":false}\"]}");
        }
    }
}
#endif

[System.Serializable]
public struct SensorManagerData
{
    public string[] data;
}

/// <summary>
/// Extennd this as needed to add custom properties that need to be saved for a sensor feed, shaders, volume, etc.
/// </summary>
public class ISensorData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public string topicName;
    public int trackingState;
}


/// <summary>
/// SensorManager is the base class for all sensor managers.
/// It is an abstract class that provides the basic structure for any collection of sensor streams that need to have spawners
/// such as cameras, depth, services etc.
/// </summary>

public abstract class SensorManager : MonoBehaviour, IComparable<SensorManager>
{
    public string name = "DEFAULT";
    public string tag = "default";
    public GameObject sensorPrefab;
    public TMPro.TextMeshProUGUI count;
    protected List<GameObject> sensors;
    protected ROSConnection _ros;
    protected Image _icon;

    private string _sceneName = "";


    private void Awake()
    {
        _ros = ROSConnection.GetOrCreateInstance();
        sensors = new List<GameObject>();

        _sceneName = SceneManager.GetActiveScene().name;

        if (PlayerPrefs.HasKey(_sceneName+"_"+name+"_layout"))
        {
            Deserialize(PlayerPrefs.GetString(_sceneName+"_"+name+"_layout"));
        }

        // Register on quit in case disabled
        Application.quitting += OnApplicationQuit;

        count.text = sensors.Count.ToString();
    }

    public void AddSensor()
    {
        Transform target = Camera.main.transform;
        GameObject sensor = Instantiate(sensorPrefab, target.position + (target.forward * 0.5f), Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up));
        sensor.GetComponentInChildren<SensorStream>().manager = this;
        sensors.Add(sensor);
        count.text = sensors.Count.ToString();
    }

    public void Remove(GameObject sensor)
    {
        sensors.Remove(sensor);
        count.text = sensors.Count.ToString();
        Destroy(sensor);
    }

    public void ClearAll()
    {
        foreach (GameObject sensor in sensors)
        {
            Destroy(sensor);
        }
        sensors.Clear();
        count.text = sensors.Count.ToString();
    }
    
    void OnApplicationQuit()
    {
        Debug.Log("Saving layout for "+name+" in scene "+_sceneName);
        PlayerPrefs.SetString(_sceneName+"_"+name+"_layout", Serialize());
        PlayerPrefs.Save();
    }

    
    public string Serialize()
    {
        SensorManagerData data = new SensorManagerData();
        data.data = new string[sensors.Count];

        for (int i = 0; i < sensors.Count; i++)
        {
            data.data[i] = sensors[i].GetComponent<SensorStream>().Serialize();
        }


        return JsonUtility.ToJson(data);
    }
    public void Deserialize(string data)
    {
        ClearAll();

        SensorManagerData sensorData = JsonUtility.FromJson<SensorManagerData>(data);


        foreach (string d in sensorData.data)
        {
            ISensorData img = JsonUtility.FromJson<ISensorData>(d);
            GameObject image = Instantiate(sensorPrefab, img.position, img.rotation);
            image.transform.localScale = img.scale;
            image.GetComponent<SensorStream>().Deserialize(d);
            image.GetComponent<SensorStream>().manager = this;
            sensors.Add(image);  
        }
        count.text = sensors.Count.ToString();
    }

    public int CompareTo(SensorManager other)
    {
        if (other == null) return 1;
        return string.Compare(name, other.name, StringComparison.Ordinal);
    }
}
