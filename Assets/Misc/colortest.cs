using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [ExecuteInEditMode]
public class colortest : MonoBehaviour
{
    
    private Material _material;
    void Start()
    {
        _material = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion rotation = Quaternion.LookRotation(transform.position, Vector3.up);

        Debug.Log("Rotation: " + rotation);


        Color c = new Color(rotation.x, rotation.y, rotation.z, 1f);
        _material.color = c;
    }
}
