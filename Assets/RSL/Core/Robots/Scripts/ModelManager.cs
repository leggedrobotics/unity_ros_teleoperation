using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using RSL.Robots;
using UvgRos.TF2;

namespace RSL.Core.Robots
{
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
        // Live list of frames currently known to the TF tree -- lets the
        // user pick e.g. "world" instead of typing it blind into rootFrame.
        // Populated/refreshed by RefreshRootFrameOptions(); wire a UI button
        // to that method, same pattern as the topics-list button in
        // UvgRosConnectionEditor.
        public TMP_Dropdown rootFrameDropdown;
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
            _root.GetComponent<TF2Attachment>().FrameID = startRootFrame;

            rootFrame.onEndEdit.AddListener(delegate {
                ChangeRootFrame(rootFrame.text);
            });

            if (rootFrameDropdown != null)
            {
                rootFrameDropdown.onValueChanged.AddListener(delegate {
                    string picked = rootFrameDropdown.options[rootFrameDropdown.value].text;
                    rootFrame.text = picked;
                    ChangeRootFrame(picked);
                });
                RefreshRootFrameOptions();
            }


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
            _root.GetComponent<TF2Attachment>().FrameID = newRootFrame;

            PlayerPrefs.SetString("rootFrame", newRootFrame);
            PlayerPrefs.Save();
        }

        // Wire to a UI "Refresh" button (or call periodically) to repopulate
        // rootFrameDropdown from whatever frames the TF tree currently knows
        // about -- doesn't include anything not yet seen on the wire.
        public void RefreshRootFrameOptions()
        {
            if (rootFrameDropdown == null) return;

            List<string> names = new List<string>(TF2System.GetOrCreateInstance().GetTransformNames("/tf"));
            names.Sort();

            string current = rootFrame != null ? rootFrame.text : null;
            rootFrameDropdown.ClearOptions();
            rootFrameDropdown.AddOptions(names);

            int currentIndex = current != null ? names.IndexOf(current) : -1;
            if (currentIndex >= 0)
                rootFrameDropdown.SetValueWithoutNotify(currentIndex);
        }

        public void ToggleModel()
        {
            _enabled = !_enabled;

            _currentModel.SetActive(_enabled);
            _toggleImage.sprite = _enabled ? hideRobotSprite : showRobotSprite;
        }
    }
}
