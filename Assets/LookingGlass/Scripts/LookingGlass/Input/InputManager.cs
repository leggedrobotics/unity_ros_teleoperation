using System;
using System.Collections.Generic;
using UnityEngine;

namespace LookingGlass {
    /// <summary>
    /// Manages hardware inputs on types of Looking Glass displays including modern and classic.
    /// </summary>
    /// <remarks>
    /// This is an updated version of the
    /// <see href="https://docs.lookingglassfactory.com/Unity/Scripts/InputManager">ButtonManager</see>
    /// designed to work with both Gen1 and Gen2 hardware.
    /// </remarks>
    public class InputManager {
        /// <summary>
        /// Different button states we can check for.
        /// </summary>
        [Serializable]
        private enum ButtonState {
            /// <summary>
            /// The button was pressed on this frame.
            /// </summary>
            Down,

            /// <summary>
            /// The button was released on this frame.
            /// </summary>
            Up,

            /// <summary>
            /// The button is held.
            /// </summary>
            Held
        }

        /// <summary>
        /// Used in a lookup table to map a <see cref="HardwareButton"/> to one or more <see cref="KeyCode"/>s or
        /// <see cref="PortraitKeyCode"/>s.
        /// </summary>
        private class KeyMap {
            private List<KeyCode> keys = new List<KeyCode>();
            private List<PortraitKeyCode> extendedKeys = new List<PortraitKeyCode>();

            /// <summary>
            /// Gets the keys in the map.
            /// </summary>
            public List<KeyCode> Keys => keys;

            /// <summary>
            /// Gets the extended keys in the map.
            /// </summary>
            public List<PortraitKeyCode> ExtendedKeys => extendedKeys;
        }

        private const string CLASSIC_JOY_KEY = "hologramCamera";
        private const float JOY_CHECK_INTERVAL = 3;

        private static readonly Lazy<HardwareButton[]> AllHardwareButtons = new Lazy<HardwareButton[]>(() =>
            (HardwareButton[]) Enum.GetValues(typeof(HardwareButton)));
        private static Dictionary<HardwareButton, KeyMap> buttonKeyMap;
        private static int classicJoyNumber = -2;
        private static InputEmulationMode emulationMode = InputEmulationMode.Always;
        private static bool searchForClassic = true;
        private static float timeSinceClassicCheck = -3;

        /// <summary>
        /// Gets or sets a value that indicates whether to search for classic hardware.
        /// </summary>
        /// <remarks>
        /// Classic hardware appears as a Joystick device. If this property is <c>true</c> (default) the manager will
        /// search for the proper joystick after every 3 seconds until one is found.
        /// </remarks>
        public static bool SearchForClassic {
            get { return searchForClassic; }
            set { searchForClassic = value; }
        }

        /// <summary>
        /// Gets or sets a value that indicates when to emulate hardware buttons.
        /// </summary>
        public static InputEmulationMode EmulationMode {
            get { return emulationMode; }
            set {
                if (value == emulationMode)
                    return;
                UpdateEmulationBindings();
            }
        }

        /// <summary>
        /// Initializes the <see cref="InputManager"/> singleton.
        /// </summary>
        static InputManager() {
            AddDefaultBindings();
            UpdateEmulationBindings();
        }

        /// <summary>
        /// Adds the default key bindings.
        /// </summary>
        private static void AddDefaultBindings() {
            // Add KeyMap entries that map the media keys to Portrait buttons
            GetKeyMap(HardwareButton.Forward).ExtendedKeys.Add(PortraitKeyCode.MediaNext);
            GetKeyMap(HardwareButton.Back).ExtendedKeys.Add(PortraitKeyCode.MediaPrevious);
            GetKeyMap(HardwareButton.PlayPause).ExtendedKeys.Add(PortraitKeyCode.MediaPlayPause);
        }

        /// <summary>
        /// Check to see if the specified button matches the specified state.
        /// </summary>
        /// <param name="button">
        /// The <see cref="HardwareButton"/> to check.
        /// </param>
        /// <param name="state">
        /// The <see cref="ButtonState"/> to check for.
        /// </param>
        /// <returns>
        /// <c>true</c> if the button matches the specified state; otherwise <c>false</c>.
        /// </returns>
        private static bool CheckButtonState(HardwareButton button, ButtonState state) {
#if !ENABLE_LEGACY_INPUT_MANAGER
            throw new InvalidOperationException("The " + nameof(InputManager) + " relies on the old UnityEngine.Input API!\n" +
                "You must enable it in your project settings for now.\n" +
                "We plan to update this code eventually to support the new InputSystem as well.");
#endif

            // If we haven't found a classic joystick yet and we're searching for one, try to search again now
            if ((searchForClassic) && (classicJoyNumber < 1)) {
                DoClassicSearch();
            }

            // Which functions are we using to test keys and extended keys?
            Func<KeyCode, bool> keyFunc;
            Func<PortraitKeyCode, bool> extendedKeyFunc;
            switch (state) {
                case ButtonState.Down:
                    keyFunc = Input.GetKeyDown;
                    extendedKeyFunc = InputExtensions.GetKeyDown;
                    break;
                case ButtonState.Up:
                    keyFunc = Input.GetKeyUp;
                    extendedKeyFunc = InputExtensions.GetKeyUp;
                    break;
                case ButtonState.Held:
                    keyFunc = Input.GetKey;
                    extendedKeyFunc = InputExtensions.GetKey;
                    break;
                default:
                    throw new InvalidOperationException("Unknown state '" + state + "'.");
            }

            // Get the KeyMap for the specified hardware button
            KeyMap keyMap = GetKeyMap(button);

            // Check standard keys first
            foreach (KeyCode key in keyMap.Keys)
                if (keyFunc(key))
                    return true;

            // Check extended keys
            foreach (PortraitKeyCode eKey in keyMap.ExtendedKeys)
                if (extendedKeyFunc(eKey))
                    return true;

            // No key or extended key matched the target state
            return false;
        }

        /// <summary>
        /// Searches for a joystick representing a classic hardware display.
        /// </summary>
        private static void DoClassicSearch() {
            // If already found, ignore
            if (classicJoyNumber > 0)
                return;

            // If too little time has passed since last check, ignore
            if (Time.unscaledTime - timeSinceClassicCheck < JOY_CHECK_INTERVAL)
                return;

            // Checking now
            timeSinceClassicCheck = Time.unscaledTime;

            // Get all joystick names
            string[] joyNames = Input.GetJoystickNames();

            // Look at each name
            for (int i = 0; i < joyNames.Length; i++) {
                if (joyNames[i].ToLower().Contains(CLASSIC_JOY_KEY)) {
                    classicJoyNumber = i + 1; // Unity joystick IDs are 1 bound not 0 bound
                    //Debug.Log(joyNames[i]);
                    break;
                }
            }

            // Warn if not found, but only once
            if (classicJoyNumber == -2) {
                Debug.LogWarning(nameof(InputManager) + " - No LookingGlass joystick found but will continue to search.");
                classicJoyNumber = -1;
            }

            // If the joystick has been found, add KeyMap entries that map the joystick buttons to classic hardware buttons
            if (classicJoyNumber > 0) {
                GetKeyMap(HardwareButton.Square).Keys.Add(JoyButtonToCode(classicJoyNumber, 0));
                GetKeyMap(HardwareButton.Left).Keys.Add(JoyButtonToCode(classicJoyNumber, 1));
                GetKeyMap(HardwareButton.Right).Keys.Add(JoyButtonToCode(classicJoyNumber, 2));
                GetKeyMap(HardwareButton.Circle).Keys.Add(JoyButtonToCode(classicJoyNumber, 3));
            }
        }

        /// <summary>
        /// Gets the <see cref="KeyMap"/> for the specified button, creating it if necessary.
        /// </summary>
        /// <param name="button">
        /// The <see cref="HardwareButton"/> to get the <see cref="KeyMap"/> for.
        /// </param>
        /// <returns>
        /// The <see cref="KeyMap"/> for the button.
        /// </returns>
        private static KeyMap GetKeyMap(HardwareButton button) {
            // Make sure the overall lookup table is crated
            if (buttonKeyMap == null) {
                buttonKeyMap = new Dictionary<HardwareButton, KeyMap>();
            }

            // Try to get an existing KeyMap. If not found, create it.
            if (!buttonKeyMap.TryGetValue(button, out KeyMap keyMap)) {
                // Create it
                keyMap = new KeyMap();

                // Store it
                buttonKeyMap[button] = keyMap;
            }

            // Return the map
            return keyMap;
        }

        /// <summary>
        /// Gets the <see cref="KeyCode"/> that represents the specified joystick and button.
        /// </summary>
        /// <param name="joystick">The number of the joystick.</param>
        /// <param name="button">The number of the button.</param>
        /// <returns>
        /// The <see cref="KeyCode"/> that represents the joystick and button.
        /// </returns>
        private static KeyCode JoyButtonToCode(int joystick, int button) {
            if (joystick < 1 || joystick > 8)
                throw new ArgumentOutOfRangeException(nameof(joystick));
            if (button < 0 || button > 19)
                throw new ArgumentOutOfRangeException(nameof(button));

            return (KeyCode) Enum.Parse(typeof(KeyCode), "Joystick" + joystick + "Button" + button);
        }

        /// <summary>
        /// Updates emulation based on the state of <see cref="EmulationMode"/> and the current app.
        /// </summary>
        private static void UpdateEmulationBindings() {
            // If emulation is enabled, add emulation key map entries as well
            if (emulationMode == InputEmulationMode.Always || (emulationMode == InputEmulationMode.EditorOnly && Application.isEditor)) {
                GetKeyMap(HardwareButton.Square).Keys.Add(KeyCode.Alpha1);
                GetKeyMap(HardwareButton.Left).Keys.Add(KeyCode.Alpha2);
                GetKeyMap(HardwareButton.Right).Keys.Add(KeyCode.Alpha3);
                GetKeyMap(HardwareButton.Circle).Keys.Add(KeyCode.Alpha4);
            } else {
                // Remove won't throw an exception if not found
                GetKeyMap(HardwareButton.Square).Keys.Remove(KeyCode.Alpha1);
                GetKeyMap(HardwareButton.Left).Keys.Remove(KeyCode.Alpha2);
                GetKeyMap(HardwareButton.Right).Keys.Remove(KeyCode.Alpha3);
                GetKeyMap(HardwareButton.Circle).Keys.Remove(KeyCode.Alpha4);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if any button is held.
        /// </summary>
        /// <returns>
        /// <c>true</c> if any button is held, <c>false</c> otherwise.
        /// </returns>
        public static bool GetAnyButton() {
            foreach (HardwareButton b in AllHardwareButtons.Value) {
                if (GetButton(b))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns <c>true</c> on the first frame that any button is pressed.
        /// </summary>
        /// <returns>
        /// <c>true</c> if any button was pressed on this frame; otherwise <c>false</c>.
        /// </returns>
        public static bool GetAnyButtonDown() {
            foreach (HardwareButton b in AllHardwareButtons.Value)
                if (GetButtonDown(b))
                    return true;
            return false;
        }

        /// <summary>
        /// Indicates if the specified button is held down.
        /// </summary>
        /// <param name="button">
        /// The <see cref="HardwareButton"/> to test.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified button is held down; otherwise <c>false</c>.
        /// </returns>
        public static bool GetButton(HardwareButton button) => CheckButtonState(button, ButtonState.Held);

        /// <summary>
        /// Returns <c>true</c> on the first frame when the specified button is pressed.
        /// </summary>
        /// The <see cref="HardwareButton"/> to test.
        /// <returns>
        /// <c>true</c> if the specified button was pressed on this frame; otherwise <c>false</c>.
        /// </returns>
        public static bool GetButtonDown(HardwareButton button) => CheckButtonState(button, ButtonState.Down);

        /// <summary>
        /// Returns <c>true</c> on the first frame when the specified button is released.
        /// </summary>
        /// <param name="button">The <see cref="HardwareButton"/> to test.</param>
        /// <returns>
        /// <c>true</c> if the specified button was released on this frame; otherwise <c>false</c>.
        /// </returns>
        public static bool GetButtonUp(HardwareButton button) => CheckButtonState(button, ButtonState.Up);
    }
}
