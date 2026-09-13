using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.UI;
using UnityEngine.UIElements;
// UnityEngine.UI and UnityEngine.UIElements both define Button (and
// Image). This class now touches both worlds -- the uGUI count label it
// still drives, and the UXML row it binds -- so the UI Toolkit types are
// aliased and the one uGUI type is written out in full at its use site.
using Button = UnityEngine.UIElements.Button;
using System;
using UnityEngine.SceneManagement;

namespace RSL.Core
{
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

        /// <summary>How many sensors this manager currently owns.</summary>
        // Read-only view for UI that reflects the manager rather than owning it.
        // The uGUI row could read its own `count` label back; a UI Toolkit panel
        // has no label of its own to read.
        public int SensorCount => sensors != null ? sensors.Count : 0;

        [Tooltip("This manager's OWN row UI. MenuTemplate instantiates it into the " +
                 "menu list and then calls BindUi on it. Leave empty to use the " +
                 "template's default row -- override it when a manager type needs " +
                 "controls the default row does not have.")]
        public VisualTreeAsset managerUi;

        // The row instance currently representing this manager in a menu list.
        // Null until MenuTemplate binds one, and replaced wholesale whenever the
        // list is rebuilt -- so nothing here may cache child elements.
        // The row instance currently representing this manager. protected so a
        // subclass can reach its own extra controls; replaced wholesale on every
        // rebuild, so nothing may cache child elements across binds.
        protected VisualElement Row;

        /// <summary>Raised whenever the sensor count changes.</summary>
        // Lets a panel refresh on the edge instead of polling every frame. The
        // uGUI row never needed this because the manager pushed straight into
        // the TMP label it owned.
        public event System.Action<SensorManager> CountChanged;
        protected List<GameObject> sensors;
        protected ROSConnection _ros;
        // Fully qualified: UnityEngine.UIElements also defines an Image, and
        // both namespaces are in scope now that managers carry UXML rows.
        protected UnityEngine.UI.Image _icon;

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

            UpdateCount();
        }

        public void AddSensor()
        {
            Transform target = Camera.main.transform;
            GameObject sensor = Instantiate(sensorPrefab, target.position + (target.forward * 0.5f), Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up));
            sensor.GetComponentInChildren<SensorStream>().manager = this;
            sensors.Add(sensor);
            UpdateCount();
        }

        public void Remove(GameObject sensor)
        {
            sensors.Remove(sensor);
            UpdateCount();
            Destroy(sensor);
        }

        public void ClearAll()
        {
            foreach (GameObject sensor in sensors)
            {
                Destroy(sensor);
            }
            sensors.Clear();
            UpdateCount();
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
            UpdateCount();
        }

        /// <summary>
        /// Pushes the count to the uGUI label if there is one, then notifies
        /// anything reflecting this manager.
        /// </summary>
        protected void UpdateCount()
        {
            // Null whenever the driving UI is UI Toolkit rather than the uGUI
            // row -- that TMP label only exists on the latter. It used to be
            // written unguarded from five places, so a manager without a row
            // threw on Awake.
            if (count != null) count.text = SensorCount.ToString();
            RefreshUi();
            CountChanged?.Invoke(this);
        }

        /// <summary>
        /// Wires an instance of this manager's row UI. Override to bind extra
        /// controls a specific manager type adds, calling base first.
        /// </summary>
        public virtual void BindUi(VisualElement row)
        {
            Row = row;
            if (row == null) return;

            Label nameLabel = row.Q<Label>("ManagerName");
            if (nameLabel != null) nameLabel.text = name;

            Button add = row.Q<Button>("Add");
            if (add != null) add.clicked += AddSensor;

            Button clear = row.Q<Button>("Clear");
            if (clear != null) clear.clicked += ClearAll;


            HideLegacyPanel();
            RefreshUi();
        }

        /// <summary>
        /// Turns off this manager's own uGUI panel once a UXML row represents it.
        /// </summary>
        // Every manager prefab is a little world-space uGUI panel in its own right:
        // MenuTemplate used to RE-PARENT that panel into the menu. Nothing moves it
        // now, so without this the old panel keeps rendering wherever it sits and
        // the menu appears twice over.
        //
        // The CANVAS is disabled, not the GameObject: the manager must keep running
        // (its Update, its ROS subscriptions, the sensors it owns), and its uGUI
        // references must stay valid for whichever code still pokes them.
        private void HideLegacyPanel()
        {
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true))
                canvas.enabled = false;
        }

        /// <summary>
        /// Show/hide this manager. Mirrors the old ManagerToggler button: hiding
        /// CLEARS the sensors first, so the spawned world objects go away rather
        /// than being orphaned in the scene with no way to reach them.
        ///
        /// Kept public but NOT surfaced on the sensor rows -- visibility belongs
        /// to the debug UI, not to the everyday sensor menu.
        /// </summary>
        public void ToggleVisible()
        {
            bool wasActive = gameObject.activeSelf;
            ClearAll();
            gameObject.SetActive(!wasActive);
            RefreshUi();
        }

        /// <summary>Pushes current state into the bound row, if there is one.</summary>
        // virtual: a manager type with extra controls in its row extends this
        // to refresh them, rather than the base class knowing about them.
        public virtual void RefreshUi()
        {
            if (Row == null) return;

            Label countLabel = Row.Q<Label>("Count");
            if (countLabel != null) countLabel.text = SensorCount.ToString();

            // Still reflected even though this menu no longer offers the toggle:
            // a manager can be hidden from the debug UI, and a row that looked
            // live while its manager was off would just spawn sensors nothing
            // displays.
            bool active = gameObject.activeSelf;
            Row.Q<Button>("Add")?.SetEnabled(active);
            Row.Q<Button>("Clear")?.SetEnabled(active);
            Row.Q("SensorRow")?.EnableInClassList("unavailable", !active);
        }

        public int CompareTo(SensorManager other)
        {
            if (other == null) return 1;
            return string.Compare(name, other.name, StringComparison.Ordinal);
        }
    }
}
