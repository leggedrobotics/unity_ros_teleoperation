using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VersionIndicator : MonoBehaviour
{
    private TMPro.TextMeshProUGUI _text;

    void Start()
    {
        _text = GetComponent<TMPro.TextMeshProUGUI>();
        _text.text = "v" + Application.version;
    }

    public void ToggleVisible()
    {
        _text.enabled = !_text.enabled;
    }
}
