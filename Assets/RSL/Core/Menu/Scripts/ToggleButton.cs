using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Core.Menu
{
    public class ToggleButton : MonoBehaviour
    {
        // Start is called before the first frame update
        public Sprite activeSprite;
        public Sprite inactiveSprite;
        public GameObject icon;

        void Start()
        {
            icon.GetComponent<UnityEngine.UI.Image>().sprite = inactiveSprite;
        }
        
        public void setActiveSprite()
        {
            icon.GetComponent<UnityEngine.UI.Image>().sprite = activeSprite;
        }

        public void setInactiveSprite()
        {
            icon.GetComponent<UnityEngine.UI.Image>().sprite = inactiveSprite;
        }
    }
}
