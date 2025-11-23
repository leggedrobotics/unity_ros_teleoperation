using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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
