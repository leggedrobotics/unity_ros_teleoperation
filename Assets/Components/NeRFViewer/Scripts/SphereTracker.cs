using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SphereTracker))]
public class SphereTrackerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SphereTracker myScript = (SphereTracker)target;
        if(GUILayout.Button("Toggle"))
        {
            myScript.Toggle();
        }
    }
}
#endif

public class SphereTracker : MonoBehaviour
{
    public float tolerance = 0.5f;
    public Transform target;
    public TMPro.TextMeshProUGUI opacityText;

    public Animator viewerAnimator;
    private Animator sphereAnimator;

    public float spawnSpeed = 0.1f;

    private bool _spawned = false;
    private bool _enabled = false;
    private Material _material;
    private NerfRender _nerfRender;

    void Start()
    {
        if(target == null)
        {
            target = Camera.main.transform;
        }
        _material = GetComponent<Renderer>().material;
        GetComponent<Renderer>().enabled = _enabled;
        _nerfRender = GetComponent<NerfRender>();

        sphereAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        if(_enabled && Vector3.Distance(transform.position, target.position) > tolerance)
        {
            transform.position = target.position;
            Render();
        }
    }

    public void SetAlpha(float alpha)
    {
        _material.SetFloat("_Alpha", alpha);
        opacityText.text = (alpha*100).ToString("0.00") + "%";
    }

    public void Toggle()
    {
        _enabled = !_enabled;
        sphereAnimator.SetBool("Present", _enabled);
        // viewerAnimator.SetBool("Open", !_enabled);
    }

    void Render()
    {
        if(_nerfRender != null)
        {
            _nerfRender.Render();
        }
    }
}
