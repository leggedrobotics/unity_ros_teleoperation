using UnityEngine;

namespace LookingGlass.Demos
{
    public class XYSpotlight : MonoBehaviour
    {
        [SerializeField] private float xRange;
        [SerializeField] private float yRange;
        [SerializeField] private Transform spotlight;
        [SerializeField] private float lightZ;

        private Light li;

        private void Start()
        {
            li = spotlight.GetComponent<Light>();
        }

        public void SetSliderPos(float x, float y)
        {
            float x_ = Remap(x, -2.25f, 2.25f, -xRange, xRange);
            float y_ = Remap(y, -1f, 1f, -yRange, yRange);
            spotlight.position = new Vector3(x_, y_, lightZ);
        }

        public void SetLightIntensity(float intensity)
        {
            li.intensity = intensity;
        }
        private float Remap(float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}