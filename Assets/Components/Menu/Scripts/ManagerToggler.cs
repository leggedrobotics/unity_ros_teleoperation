using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RSL.Core.Menu
{
    #if UNITY_EDITOR
    using UnityEditor;
    [CustomEditor(typeof(ManagerToggler))]

    class ManagerTogglerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ManagerToggler myScript = (ManagerToggler)target;
            if (GUILayout.Button("Setup"))
            {
                myScript.Setup();
            }
            if (GUILayout.Button("Populate Menu"))
            {
                myScript.PopulateMenu();
            }
            if (GUILayout.Button("Update Menus"))
            {
                myScript.UpdateMenus();
            }
            foreach (var button in myScript.GetComponentsInChildren<Button>())
            {
                if (GUILayout.Button(button.transform.parent.name))
                {
                    button.onClick.Invoke();
                }
            }
        }
    }


    #endif

    public class ManagerToggler : MonoBehaviour
    {
        public GameObject togglePrefab;
        public Sprite activeSprite;
        public Sprite inactiveSprite;
        public SensorManager[] _managers;
        private MenuTemplate[] _menuTemplates;
        private GameObject _menu;

        public void Start()
        {
            Setup();
            PopulateMenu();

        }

        public void Setup()
        {
            _managers = FindObjectsByType<SensorManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _menuTemplates = FindObjectsOfType<MenuTemplate>();
            _menu = GameObject.Find("Menu");

            // Sort managers by name
            System.Array.Sort(_managers, (x, y) => x.name.CompareTo(y.name));
        }

        public void UpdateMenus()
        {
            foreach (var menuTemplate in _menuTemplates)
            {
                menuTemplate.SetupRows();
            }
        }

        public void PopulateMenu()
        {
            // Clear existing toggles
            for(int i = _menu.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(_menu.transform.GetChild(0).gameObject);
            }

            float yOffset = -14f;
            foreach (var manager in _managers)
            {
                GameObject toggle = Instantiate(togglePrefab, _menu.transform);
                toggle.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, yOffset, 0);
                toggle.transform.localRotation = Quaternion.identity;
                toggle.transform.localScale = Vector3.one;

                toggle.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = manager.name;
                Image buttonImage = toggle.transform.Find("Toggle/Image/Image").GetComponent<Image>();

                toggle.GetComponentInChildren<Button>().onClick.AddListener(delegate { OnButtonClick(manager, buttonImage); });
                toggle.GetComponentInChildren<Image>().sprite = manager.gameObject.activeSelf ? activeSprite : inactiveSprite;

                toggle.name = manager.name + " Toggle";

                yOffset -= manager.GetComponent<RectTransform>().sizeDelta.y - 2f;
            }
        }

        public void OnButtonClick(SensorManager manager, Image buttonImage)
        {
            bool isActive = manager.gameObject.activeSelf;
            manager.ClearAll();
            manager.gameObject.SetActive(!isActive);
            buttonImage.sprite = isActive ? inactiveSprite : activeSprite;

            UpdateMenus();
        }
    }
}
