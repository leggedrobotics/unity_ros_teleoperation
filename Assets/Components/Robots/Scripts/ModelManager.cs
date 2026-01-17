using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using RSL.Robots;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ModelManager))]
public class ModelManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ModelManager myScript = (ModelManager)target;
        for(int i=0; i<myScript.robotDatabase.robots.Count; i++)
        {
            Debug.Log("Adding button for " + myScript.robotDatabase.robots[i].name);
            if(GUILayout.Button("Change to " + myScript.robotDatabase.robots[i].name))
            {
                myScript.ChangeModel(i);
            }
        }
    }
}
#endif


public class ModelManager : MonoBehaviour
{
    public static ModelManager instance;

    public RobotDatabase robotDatabase;

    public int startRobotIndex = 0;
    public RobotEntry currentRobot;
    public Sprite showRobotSprite;
    public Sprite hideRobotSprite;
    public Button toggleModel;
    public Dropdown robotDropdown;
    public TMP_InputField rootFrame;
    public bool startVisible = true;

    public bool _enabled;
    public GameObject _currentModel;
    public GameObject _root;
    private bool _inited = false;
    private Image _toggleImage;

    private void Awake()
    {        
        instance = this;

        _root = GameObject.FindWithTag("root");

        startRobotIndex = PlayerPrefs.GetInt("startRobotIndex", startRobotIndex);
        string startRootFrame = PlayerPrefs.GetString("rootFrame", "odom");

        rootFrame.text = startRootFrame;
        _root.name = startRootFrame;
        _root.GetComponent<TFAttachment>().FrameID = startRootFrame;

        rootFrame.onEndEdit.AddListener(delegate {
            ChangeRootFrame(rootFrame.text);
        });


        if (startVisible)
        {
            ChangeModel(startRobotIndex);
        }


        Debug.Log("Current robot is " + currentRobot.name + (startVisible ? " and being spawned " : " and hidden"));

        toggleModel.onClick.AddListener(ToggleModel);
        _toggleImage = toggleModel.transform.GetChild(0).GetChild(1).GetComponent<Image>();

        robotDropdown.ClearOptions();
        List<string> robotNames = new List<string>();
        foreach(RobotEntry robot in robotDatabase.robots)
        {
            robotNames.Add(robot.name);
        }
        robotDropdown.AddOptions(robotNames);
        robotDropdown.value = startRobotIndex;
        robotDropdown.onValueChanged.AddListener(delegate {
            ChangeModel(robotDropdown.value);
        });

        _toggleImage.sprite = startVisible ? hideRobotSprite : showRobotSprite;
        _enabled = startVisible;
        _inited = true;
    }

    public void ChangeModel(int modelIndex)
    {
        if(modelIndex < 0 || modelIndex >= robotDatabase.robots.Count)
        {
            Debug.LogError("Model index " + modelIndex + " is out of bounds!");
            modelIndex = 0;
        }
        currentRobot = robotDatabase.robots[modelIndex];
        Debug.Log("Changed to model of " + currentRobot);

        PlayerPrefs.SetInt("startRobotIndex", modelIndex);
        PlayerPrefs.SetString("rootFrame", currentRobot.rootFrame);
        PlayerPrefs.Save();


        if(_currentModel != null)
            Destroy(_currentModel);
    
        _currentModel = Instantiate(currentRobot.prefab);
        if(_root != null)
            _currentModel.transform.SetParent(_root.transform);

        if (_inited)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }

    }

    public void ChangeRootFrame(string newRootFrame)
    {
        _root.name = newRootFrame;
        _root.GetComponent<TFAttachment>().FrameID = newRootFrame;

        PlayerPrefs.SetString("rootFrame", newRootFrame);
        PlayerPrefs.Save();
    }

    public void ToggleModel()
    {
        _enabled = !_enabled;

        _currentModel.SetActive(_enabled);
        _toggleImage.sprite = _enabled ? hideRobotSprite : showRobotSprite;
    }
}
