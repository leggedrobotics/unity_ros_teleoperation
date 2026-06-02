using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Core.Menu
{
    public class CameraControls : MonoBehaviour
    {
        private bool _isActive = true;    
        public float moveSpeed = 1f;
        public float rotationSpeed = 1000f;

        public void ToggleActive()
        {
            _isActive = !_isActive;
        }

        // Update is called once per frame
        void Update()
        {
            if(!_isActive)
                return;
            // Camera movement with WASD keys
            float horizontal = Input.GetAxis("Horizontal"); // A/D keys
            float vertical = Input.GetAxis("Vertical"); // W/S keys
            float upDown = 0f;
            if (Input.GetKey(KeyCode.Q)) // Q key for up
            {
                upDown = 1f;
            }
            else if (Input.GetKey(KeyCode.E)) // E key for down
            {
                upDown = -1f;
            }
            Vector3 moveDirection = new Vector3(horizontal, upDown, vertical).normalized;

            transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);
            // Camera rotation with mouse drag
            if (Input.GetMouseButton(1)) // Right mouse button
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime, Space.World);
                transform.Rotate(Vector3.left, mouseY * rotationSpeed * Time.deltaTime);
            }
        }
    }
}
