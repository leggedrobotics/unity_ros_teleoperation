using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RSL.Core.Interaction
{
    public class ControllerMove : MonoBehaviour
    {
        public InputActionReference joyStick;
        public InputActionReference joyStickPressed;
        public float speed = 1.0f;
        // Start is called before the first frame update
        void Start()
        {
        }

        void Update()
        {
            Vector2 move = joyStick.action.ReadValue<Vector2>();
            transform.Translate(new Vector3(move.x, move.y, 0) * speed * Time.deltaTime);
        }
    }
}
