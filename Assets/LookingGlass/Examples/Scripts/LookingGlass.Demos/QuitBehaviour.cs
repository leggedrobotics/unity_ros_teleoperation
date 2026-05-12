using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LookingGlass.Demos {
    public class QuitBehaviour : MonoBehaviour {
        private void Update() {
            bool esc = false;
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = InputSystem.GetDevice<Keyboard>();
            if (keyboard != null) {
                if (keyboard.escapeKey.isPressed)
                    esc = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
                esc = true;
#endif

            if (esc)
                Application.Quit();
        }
    }
}
