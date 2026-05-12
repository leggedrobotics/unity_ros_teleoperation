using UnityEngine;

namespace LookingGlass.Demos {
    public class DemoInputToggleButtons : MonoBehaviour {
        [SerializeField] private GameObject forwardButtonCube;
        [SerializeField] private GameObject backButtonCube;
        [SerializeField] private GameObject playPauseButtonCube;

        [SerializeField] private bool squareIsCurrentlyDown;
        [SerializeField] private bool leftIsCurrentlyDown;
        [SerializeField] private bool rightIsCurrentlyDown;
        [SerializeField] private bool circleIsCurrentlyDown;

        private void Start() {
            forwardButtonCube.SetActive(false);
            backButtonCube.SetActive(false);
            playPauseButtonCube.SetActive(false);
        }

        private void Update() {
            ToggleCubes();
            SpawnSpheres();
        }

        private void ToggleCubes() {
            if (InputManager.GetButtonDown(HardwareButton.Forward))
                forwardButtonCube.SetActive(!forwardButtonCube.activeSelf);
            if (InputManager.GetButtonDown(HardwareButton.Back))
                backButtonCube.SetActive(!backButtonCube.activeSelf);
            if (InputManager.GetButtonDown(HardwareButton.PlayPause))
                playPauseButtonCube.SetActive(!playPauseButtonCube.activeSelf);
        }

        private void SpawnSpheres() {
            if (InputManager.GetButtonDown(HardwareButton.Square))
                MakeNewSphereAt(new Vector3(-3, -5, 0));
            if (InputManager.GetButtonDown(HardwareButton.Left))
                MakeNewSphereAt(new Vector3(-1, -5, 0));
            if (InputManager.GetButtonDown(HardwareButton.Right))
                MakeNewSphereAt(new Vector3(1, -5, 0));
            if (InputManager.GetButtonDown(HardwareButton.Circle))
                MakeNewSphereAt(new Vector3(3, -5, 0));

            squareIsCurrentlyDown = InputManager.GetButton(HardwareButton.Square);
            leftIsCurrentlyDown = InputManager.GetButton(HardwareButton.Left);
            rightIsCurrentlyDown = InputManager.GetButton(HardwareButton.Right);
            circleIsCurrentlyDown = InputManager.GetButton(HardwareButton.Circle);
        }

        private void MakeNewSphereAt(Vector3 instanceLocation) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = instanceLocation + Random.insideUnitSphere * 0.25f;
            go.GetComponent<Renderer>().material.color = Random.ColorHSV();
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.AddForce(Vector3.up * 12, ForceMode.Impulse);
        }
    }
}
