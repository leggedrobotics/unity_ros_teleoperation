using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Core.Menu
{
    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(Numpad))]
    public class NumpadEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Numpad numpad = (Numpad)target;
            if (GUILayout.Button("1"))
            {
                numpad.ButtonPressed(1);
            }
            if (GUILayout.Button("2"))
            {
                numpad.ButtonPressed(2);
            }
            if (GUILayout.Button("3"))
            {
                numpad.ButtonPressed(3);
            }
            if (GUILayout.Button("4"))
            {
                numpad.ButtonPressed(4);
            }
            if (GUILayout.Button("5"))
            {
                numpad.ButtonPressed(5);
            }
            if (GUILayout.Button("6"))
            {
                numpad.ButtonPressed(6);
            }
            if (GUILayout.Button("7"))
            {
                numpad.ButtonPressed(7);
            }
            if (GUILayout.Button("8"))
            {
                numpad.ButtonPressed(8);
            }
            if (GUILayout.Button("9"))
            {
                numpad.ButtonPressed(9);
            }
            if (GUILayout.Button("0"))
            {
                numpad.ButtonPressed(0);
            }
            if (GUILayout.Button("."))
            {
                numpad.ButtonPressed(-1);
            }
            if (GUILayout.Button("Clear"))
            {
                numpad.ButtonPressed(-2);
            }
        }
    }
    #endif

    public class Numpad : MonoBehaviour
    {
        public delegate void NumpadEvent(int number);
        public NumpadEvent OnButtonPressed;
        

        // Start is called before the first frame update
        void Start()
        {
            
        }

        public void ButtonPressed(int number)
        {
            OnButtonPressed?.Invoke(number);
        }
    }
}
