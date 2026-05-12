//Copyright 2017-2021 Looking Glass Factory Inc.
//All rights reserved.
//Unauthorized copying or distribution of this file, and the source code contained herein, is strictly prohibited.

using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LookingGlass.Demos {
    public class DemoCursorInteraction : MonoBehaviour {
        [SerializeField] private GameObject hologramCamera;
        [SerializeField] private Cursor3D cursor;

        private Vector3 nextPosition = Vector3.back;

        private void Update() {
            bool getNextWorldPos = false;

#if ENABLE_INPUT_SYSTEM
            Mouse mouse = InputSystem.GetDevice<Mouse>();
            if (mouse != null) {
                if (mouse.leftButton.wasPressedThisFrame)
                    getNextWorldPos = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
                getNextWorldPos = true;
#endif

            if (getNextWorldPos)
                nextPosition = cursor.GetWorldPos();

            hologramCamera.transform.position = Vector3.Slerp(hologramCamera.transform.position, nextPosition, 0.1f);
            hologramCamera.transform.LookAt(Vector3.zero);
        }
    }
}
