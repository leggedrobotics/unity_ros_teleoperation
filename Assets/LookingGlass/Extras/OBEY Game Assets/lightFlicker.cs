using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass.Extras
{
    public class lightFlicker : MonoBehaviour 
    {
        public Color A;
        public Color B;

        public Light flickerLight;

        // Use this for initialization
        void Start ()
        {
            if (!flickerLight)
                flickerLight = GetComponent<Light>();
        }
        
        // Update is called once per frame
        void Update ()
        {
            if (!flickerLight)
            {
                enabled = false;
                return;
            }


            flickerLight.color = new Color(Random.Range(A.r, B.r), Random.Range(A.g, B.g), Random.Range(A.b, B.b));
        }
    }
}
