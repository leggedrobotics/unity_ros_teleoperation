// Binds a UI Toolkit text field to the platform's system keyboard -- on Quest,
// the Meta system keyboard that Android's TouchScreenKeyboard raises.
//
// WHY NOT JUST UI TOOLKIT'S OWN MOBILE PATH: TextInputBaseField exposes
// `keyboardType`/`hideMobileInput` and UI Toolkit does raise TouchScreenKeyboard
// itself when focused on a mobile platform -- but that path assumes a screen
// space panel that owns real touch focus. These panels are WORLD SPACE
// (PanelSettings.m_RenderMode = 1, driven by XR ray input), so relying on the
// implicit path means relying on focus behaviour we cannot verify from the
// Editor. Worse, if UI Toolkit raises a keyboard AND this code raises one, the
// two fight over the field's text.
//
// So the field is made read-only on device and driven entirely from here: one
// keyboard, one writer, deterministic. When TouchScreenKeyboard is unsupported
// (the Editor, desktop players) the field is left normally editable so the
// panel stays usable for development -- see Attach.
//
// Usage: one SystemKeyboardField per text field, Poll() each frame from the
// owning MonoBehaviour's Update.
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace RSL.Core.Menu
{
    public class SystemKeyboardField
    {
        private readonly TextField _field;
        private readonly TouchScreenKeyboardType _keyboardType;
        private readonly Func<string, bool> _isValid;
        private readonly Action<string> _onCommit;

        private TouchScreenKeyboard _keyboard;
        private string _valueBeforeEdit;

        // True on device, false in the Editor / on desktop. Decided once:
        // TouchScreenKeyboard.isSupported does not change at runtime, and the
        // field's read-only state has to match it consistently.
        public static bool SystemKeyboardAvailable => TouchScreenKeyboard.isSupported;

        public bool IsOpen => _keyboard != null;

        private SystemKeyboardField(TextField field, TouchScreenKeyboardType keyboardType,
                                    Func<string, bool> isValid, Action<string> onCommit)
        {
            _field = field;
            _keyboardType = keyboardType;
            _isValid = isValid;
            _onCommit = onCommit;
        }

        /// <summary>
        /// Wires <paramref name="field"/> to the system keyboard. Returns null if
        /// the field is null, so callers can chain off a failed Q&lt;&gt; lookup.
        /// </summary>
        /// <param name="isValid">Gate for accepting the typed text. Rejected text
        /// reverts the field rather than committing a bad value.</param>
        /// <param name="onCommit">Called with the accepted text, once, per edit.</param>
        public static SystemKeyboardField Attach(TextField field, TouchScreenKeyboardType keyboardType,
                                                 Func<string, bool> isValid, Action<string> onCommit)
        {
            if (field == null) return null;

            var binding = new SystemKeyboardField(field, keyboardType, isValid, onCommit);

            // Set regardless: harmless where unused, and it is what tells the
            // platform which keyboard layout to raise if UI Toolkit's own mobile
            // path ever does end up driving this field.
            field.keyboardType = keyboardType;

            if (SystemKeyboardAvailable)
            {
                // Read-only stops UI Toolkit from opening a competing keyboard or
                // editing inline; the field becomes a display + tap target that
                // this class is the sole writer of.
                field.isReadOnly = true;
                // PointerDown rather than FocusIn: a read-only field's focus
                // behaviour under XR ray input is not something to depend on,
                // and a tap is the gesture a user actually makes here.
                field.RegisterCallback<PointerDownEvent>(_ => binding.OpenKeyboard());
            }
            else
            {
                // Editor/desktop: normal typing, commit on blur or Enter. Matches
                // how the uGUI menu's TMP_InputField behaved (onEndEdit).
                field.RegisterCallback<FocusOutEvent>(_ => binding.CommitFromField());
                field.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                        binding.CommitFromField();
                });
            }

            return binding;
        }

        public void SetValueWithoutNotify(string value) => _field.SetValueWithoutNotify(value ?? string.Empty);

        public string Value => _field.value;

        private void OpenKeyboard()
        {
            if (_keyboard != null) return; // already editing this field

            _valueBeforeEdit = _field.value;
            // autocorrection off, multiline off, secure off, alert off, and seed
            // the keyboard with the current value so an edit starts from it.
            _keyboard = TouchScreenKeyboard.Open(_field.value ?? string.Empty, _keyboardType,
                                                 false, false, false, false, _field.value ?? string.Empty);
        }

        /// <summary>Call every frame from the owner's Update.</summary>
        public void Poll()
        {
            if (_keyboard == null) return;

            // Mirror in-progress text so the field shows what is being typed.
            _field.SetValueWithoutNotify(_keyboard.text ?? string.Empty);

            switch (_keyboard.status)
            {
                case TouchScreenKeyboard.Status.Visible:
                    return;
                case TouchScreenKeyboard.Status.Done:
                    _keyboard = null;
                    CommitFromField();
                    return;
                default:
                    // Canceled or LostFocus -- discard the edit. Committing a
                    // half-typed IP here would drop the ROS connection.
                    _keyboard = null;
                    _field.SetValueWithoutNotify(_valueBeforeEdit);
                    return;
            }
        }

        private void CommitFromField()
        {
            string text = _field.value?.Trim() ?? string.Empty;

            if (_isValid != null && !_isValid(text))
            {
                Debug.LogWarning($"[SystemKeyboardField] Rejected '{text}' for " +
                                 $"'{_field.name}' -- reverting to '{_valueBeforeEdit}'.");
                _field.SetValueWithoutNotify(_valueBeforeEdit);
                return;
            }

            // Normalise what is displayed to what was accepted, so a trailing
            // space typed on the system keyboard does not linger in the UI.
            _field.SetValueWithoutNotify(text);
            _valueBeforeEdit = text;
            _onCommit?.Invoke(text);
        }
    }
}
