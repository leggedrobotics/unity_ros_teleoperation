using UnityEngine;
using UnityEngine.Serialization;

namespace LookingGlassExamples {
    /// <summary>
    /// A simple component that rotates a <see cref="Transform"/> around its local axes.
    /// </summary>
    public class Rotator : MonoBehaviour {
        [Tooltip("The rate at which this object will rotate, measured in degrees per second along the " + nameof(Transform) + "'s local axes.")]
        [SerializeField] private Vector3 angularVelocity;

        public Vector3 AngularVelocity {
            get { return angularVelocity; }
            set { angularVelocity = value; }
        }

        private void Update() {
            float dt = Time.deltaTime;
            transform.rotation = Quaternion.Euler(angularVelocity * dt) * transform.rotation;
        }
    }
}
