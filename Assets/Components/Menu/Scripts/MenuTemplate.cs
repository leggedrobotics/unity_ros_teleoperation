using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

namespace RSL.Core.Menu
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(MenuTemplate))]
    public class MenuTemplateEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MenuTemplate myScript = (MenuTemplate)target;
            if (GUILayout.Button("Setup Rows"))
            {
                myScript.SetupRows();
            }
            if (GUILayout.Button("Toggle Menu"))
            {
                myScript.ToggleMenu();
            }
            if (GUILayout.Button("Clean"))
            {
                myScript.Clean();
            }
        }
    }
    #endif

    public class MenuTemplate : MonoBehaviour
    {
        public GameObject menu;
        public string tagFilter = ""; // Only show sensors with this tag, leave empty to show all sensors

        public SensorManager[] managers;
        public GameObject headerPrefab;

        // void Start()
        // {
        //     SetupRows();
        // }

        public void SetupRows()
        {
            Clean();

            Dictionary<string, int> groups = new Dictionary<string, int>();

            GameObject header = null;

            if (tagFilter != "")
            {
                Debug.Log("Grouping: " + tagFilter);
                header = Instantiate(headerPrefab, menu.transform);
                header.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = tagFilter;
                header.name = tagFilter + " Header";
            }

            managers = FindObjectsOfType<SensorManager>();
            System.Array.Sort(managers);
            System.Array.Reverse(managers);

            float offset = 0;
            foreach (SensorManager manager in managers)
            {
                string tag = manager.tag;

                if (tagFilter != "" && tag != tagFilter)
                {
                    continue;
                }

                if (!groups.ContainsKey(tag))
                {
                    groups.Add(tag, 1);
                }
                else
                {
                    groups[tag]++;
                }


                // Make a child of this object's rect transform
                manager.transform.SetParent(menu.transform);
                manager.transform.localPosition = Vector3.zero;
                manager.transform.localRotation = Quaternion.identity;
                manager.transform.localScale = Vector3.one;

                // Move the manager above the last row
                manager.transform.localPosition += Vector3.up * offset;
                offset += manager.GetComponent<RectTransform>().sizeDelta.y + 0.1f;
            }

            // If we have a header, move it above the first row
            if (header != null)
            {
                header.transform.SetParent(menu.transform);
                header.transform.localPosition = Vector3.zero;
                header.transform.localRotation = Quaternion.identity;
                header.transform.localScale = Vector3.one;

                // Move the header above the first row
                header.transform.localPosition += Vector3.up * (offset + 0.1f);
            }

            string debugOutput = "Found " + groups.Count + " groups:\n";
            foreach (KeyValuePair<string, int> group in groups)
            {
                debugOutput += group.Key + ": " + group.Value + "\n";
            }
            Debug.Log(debugOutput);
        }

        public void Clean()
        {
            // If we have any managers currently in the menu, move them to origin
            // and destroy and headers
            for (int i = menu.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = menu.transform.GetChild(i);
                if (child.GetComponent<SensorManager>() != null)
                {
                    child.transform.localPosition = Vector3.zero;
                    child.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    child.transform.localScale = Vector3.one;
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        public void ToggleMenu()
        {
            menu.SetActive(!menu.activeSelf);
        }
    }
}
