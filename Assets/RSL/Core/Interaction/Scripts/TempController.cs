using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RSL.Core.Interaction
{
    public class TempController : MonoBehaviour
    {
        public float speed = 10.0f;
        public float rotationSpeed = 100.0f;
        public Camera camera;
        


        // Start is called before the first frame update
        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            
        }

        // Update is called once per frame
        void Update()
        {
            // unlock the cursor
            if (Keyboard.current.escapeKey.isPressed)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            // input from keyboard and mouse
            Vector2 move = Keyboard.current.wKey.isPressed ? new Vector2(0, 1) : Keyboard.current.sKey.isPressed ? new Vector2(0, -1) : Vector2.zero;
            move += Keyboard.current.aKey.isPressed ? new Vector2(-1, 0) : Keyboard.current.dKey.isPressed ? new Vector2(1, 0) : Vector2.zero;
            Vector2 look = Mouse.current.delta.ReadValue();
            

            // move the player
            transform.position += transform.forward * move.y * speed * Time.deltaTime;
            transform.position += transform.right * move.x * speed * Time.deltaTime;

            // rotate the player
            transform.Rotate(new Vector3(0, look.x * rotationSpeed * Time.deltaTime, 0));

            // rotate the camera
            camera.transform.Rotate(new Vector3(-look.y * rotationSpeed * Time.deltaTime, 0, 0));

            // keep the camera from rotating too far
        }
    }
}
