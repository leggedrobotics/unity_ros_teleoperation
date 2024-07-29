using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ModelManager))]
public class ModelManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        

        ModelManager myScript = (ModelManager)target;
        for(int i=0; i<myScript.robots.Length; i++)
        {
            if(GUILayout.Button("Change to " + myScript.robots[i].name))
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

    public Robot[] robots = new Robot[]
    {
            new Robot { name = "ALMA", modelRoot = null, RobotSprite = null },
            new Robot { name = "Dynaarm", modelRoot = null, RobotSprite = null },
            new Robot { name = "Anymal", modelRoot = null, RobotSprite = null },
            new Robot { name = "Panda", modelRoot = null, RobotSprite = null },
    };


    public int startRobotIndex = 0;
    public Robot currentRobot;
    public Sprite showRobotSprite;
    public Sprite hideRobotSprite;
    public Button toggleModel;
    public Dropdown robotDropdown;
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


        if(startVisible)
        {
            ChangeModel(startRobotIndex);
        }


        Debug.Log("Current robot is " + currentRobot.name + (startVisible ? " and being spawned " : " and hidden"));

        toggleModel.onClick.AddListener(ToggleModel);
        _toggleImage = toggleModel.transform.GetChild(0).GetChild(1).GetComponent<Image>();

        robotDropdown.ClearOptions();
        List<string> robotNames = new List<string>();
        foreach(Robot robot in robots)
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
        currentRobot = robots[modelIndex];
        Debug.Log("Changed to model of " + currentRobot);

        PlayerPrefs.SetInt("startRobotIndex", modelIndex);
        PlayerPrefs.Save();


        if(_currentModel != null)
            Destroy(_currentModel);
    
        _currentModel = Instantiate(currentRobot.modelRoot);
        _currentModel.transform.SetParent(_root.transform);

        if(_inited)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);
        }

    }

    public void ToggleModel()
    {
        _enabled = !_enabled;

        _currentModel.SetActive(_enabled);
        _toggleImage.sprite = _enabled ? hideRobotSprite : showRobotSprite;
    }
}
