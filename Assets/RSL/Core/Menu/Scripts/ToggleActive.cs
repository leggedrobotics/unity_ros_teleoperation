using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Core.Menu
{
    public class ToggleActive : MonoBehaviour
    {
        public bool startActive = false; // Whether to start with children active or not
        GameObject[] _children;

        void Start()
        {
            _children = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                _children[i] = transform.GetChild(i).gameObject;
                _children[i].SetActive(startActive);
            }
        }

        public void Toggle()
        {
            bool active = !_children[0].activeSelf;
            foreach (GameObject child in _children)
            {
                child.SetActive(active);
            }
        }
    }
}
