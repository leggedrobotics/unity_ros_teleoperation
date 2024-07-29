using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIPositionChanger : MonoBehaviour
{
    private Transform _cameraTransform;
    private Transform _parentTransform;
    private RectTransform _rectTransform;
    private Vector2 _anchorMax;
    private Vector2 _anchorMin;
    private Vector2 _pivot;


    private float _rotation;
    

    private void Start()
    {
        _cameraTransform = Camera.main.transform;
        _rectTransform = GetComponent<RectTransform>();
        _anchorMax = _rectTransform.anchorMax;
        _anchorMin = _rectTransform.anchorMin;
        _pivot = _rectTransform.pivot;
        _rotation = _rectTransform.localEulerAngles.x;
    }

    void Update()
    {
        if(_cameraTransform.position.y > transform.position.y)
        {
            _rectTransform.anchorMax = new Vector2(_anchorMax.x, 1-_anchorMax.y);
            _rectTransform.anchorMin = new Vector2(_anchorMin.x, 1-_anchorMin.y);
            _rectTransform.pivot = new Vector2(_pivot.x, 1-_pivot.y);
            _rectTransform.localEulerAngles = new Vector3(-_rotation, 0, 0);
        }
        else
        {
            _rectTransform.anchorMax = _anchorMax;
            _rectTransform.anchorMin = _anchorMin;
            _rectTransform.pivot = _pivot;
            _rectTransform.localEulerAngles = new Vector3(_rotation, 0, 0);
        }
        
    }
}
