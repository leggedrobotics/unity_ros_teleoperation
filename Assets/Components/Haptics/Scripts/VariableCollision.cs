using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

#endif

public class VariableCollision : MonoBehaviour
{
    public bool isRight;
    public int index;
    public Gradient gradient;
    
    private Material _material;
    private float _maxDistance;
    
    void Start()
    {
        _material = GetComponent<Renderer>().material;        
        _maxDistance = transform.localScale.magnitude / 2;
    }

    private void OnTriggerExit(Collider other) {
        _material.color = Color.white;
        HandManager.Instance.UpdateValue(isRight, index, 0);
    }


    private void OnTriggerStay(Collider other) {
        if (other.isTrigger || other.tag == "hand") return; 
        float distance = Vector3.Distance(other.ClosestPoint(transform.position), transform.position);

        float value = 1 - distance / _maxDistance;
        value = Mathf.Clamp01(value*value);
        _material.color = gradient.Evaluate(value);

        HandManager.Instance.UpdateValue(isRight, index, (int)(100*value));

    }
}
