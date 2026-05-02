using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace RSL.Core.Menu
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(StatusIndicator))]
    public class StatusIndicatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            StatusIndicator myScript = (StatusIndicator)target;
            if (GUILayout.Button("Toggle"))
            {
                myScript.gameObject.GetComponent<Button>().onClick.Invoke();
            }
        }
    }
    #endif

    public class StatusIndicator : MonoBehaviour
    {
        public Sprite neutralIcon;
        public Sprite connectedIcon;
        public Sprite disconnectedIcon;

        public Material[] statusMaterials;

        private Image _image;
        private RawImage _rawImage;
        private bool _connected = false;
        void Awake()
        {
            _image = GetComponentInChildren<Image>();
            _rawImage = GetComponent<RawImage>();

            _image.sprite = neutralIcon;
            _rawImage.texture = null;
        }
        
        public void OnRosConnection(bool connected)
        {
            _connected = connected;
            _image.sprite = connected ? connectedIcon : disconnectedIcon;
            _rawImage.color = connected ? Color.green : Color.red;

            foreach (Material material in statusMaterials)
            {
                material.color = connected ? Color.green : Color.red;
            }
        }

        public void OnDelay(bool stagnant)
        {
            if (_connected)
            {
            _image.sprite = stagnant ? neutralIcon : connectedIcon;

            }
        }


    }
}
