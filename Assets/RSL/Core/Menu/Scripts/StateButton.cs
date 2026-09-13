using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace RSL.Core.Menu
{
    // Deprecated: uGUI cycling-icon button from before the UI Toolkit
    // migration. New cycling/multi-state buttons should use a UI Toolkit
    // Button with a class-based state pattern instead (see ToggleButton's
    // own deprecation note for the established alternative). Not deleted:
    // still driving whatever uGUI prefabs haven't been ported yet.
    [System.Obsolete("uGUI-only state-cycling button, superseded by UI Toolkit's class-based state pattern. Do not use in new code.")]
    public class StateButton : MonoBehaviour
    {
        public int startIndex = 0;
        public TMPro.TMP_Text label;
        public string[] states;
        public Sprite[] icons;

        public UnityEvent<int> onStateChanged;
        public UnityEvent onToggle;

        private int _index = 0;
        private Image _image;

        private Button _button;
        // Start is called before the first frame update
        void Start()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
            _image = transform.Find("Image/Image").GetComponent<Image>();

            _index = startIndex;
        }

        void OnClick()
        {
            _index = (_index + 1) % icons.Length;
            _image.sprite = icons[_index];
            onStateChanged?.Invoke(_index);
            onToggle?.Invoke();
            if (label != null && states.Length > _index)
                label.text = states[_index];
        }

        void OnValidate()
        {
            if (startIndex >= icons.Length)
                startIndex = icons.Length - 1;
            if (startIndex < 0)
                startIndex = 0;
            _image = transform.Find("Image/Image").GetComponent<Image>();
            if (icons.Length > 0 && _image != null)
                _image.sprite = icons[startIndex % icons.Length];

        }
        

    }
}
