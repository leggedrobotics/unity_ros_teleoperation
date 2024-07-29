using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
