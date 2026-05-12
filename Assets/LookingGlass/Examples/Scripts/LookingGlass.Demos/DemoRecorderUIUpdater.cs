
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LookingGlass.Demos {
    public class DemoRecorderUIUpdater : MonoBehaviour {
#if UNITY_EDITOR || !UNITY_IOS
        [SerializeField] private QuiltCapture recorder;
#endif
        [SerializeField] private GameObject uiParent;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;

#if UNITY_EDITOR || !UNITY_IOS
        public QuiltCapture Recorder {
            get { return recorder; }
            private set {
                if (recorder != null)
                    recorder.onStateChanged -= UpdateButtonStates;

                recorder = value;

                if (recorder != null)
                    recorder.onStateChanged += UpdateButtonStates;

                UpdateButtonStates(recorder != null ? recorder.State : QuiltCaptureState.NotRecording);
                UpdateShowingUI();
            }
        }

        #region Unity Messages
        private void Reset() {
            recorder = FindObjectOfType<QuiltCapture>();
        }

        private void OnEnable() {
            if (recorder == null)
                Debug.LogWarning(this + "'s recorder is not set! Unable to update UI.");

            Recorder = recorder;
        }

        private void OnDisable() {
            if (recorder != null)
                recorder.onStateChanged -= UpdateButtonStates;
            UpdateButtonStates(QuiltCaptureState.NotRecording);
        }

        private void Update() {
            UpdateShowingUI();
        }
        #endregion

        private void UpdateButtonStates(QuiltCaptureState state) {
            if (this == null || !isActiveAndEnabled)
                return;

            if (stopButton != null)
                stopButton.gameObject.SetActive(state == QuiltCaptureState.Recording || state == QuiltCaptureState.Paused);
            if (startButton != null)
                startButton.gameObject.SetActive(state == QuiltCaptureState.NotRecording);
            if (pauseButton != null)
                pauseButton.gameObject.SetActive(state == QuiltCaptureState.Recording);
            if (resumeButton != null)
                resumeButton.gameObject.SetActive(state == QuiltCaptureState.Paused);
        }

        private void UpdateShowingUI() => UpdateShowingUI(recorder != null ? recorder.CaptureMode : QuiltCaptureMode.SingleFrame);
        private void UpdateShowingUI(QuiltCaptureMode captureMode) {
            uiParent.SetActive(captureMode == QuiltCaptureMode.Manual);
        }
#else
        private void OnEnable(){
            if (startButton != null)
                startButton.GetComponent<Button>().onClick.AddListener(() => Debug.LogWarning("Recorder is not supported in iOS build"));
        }
#endif

    }
}
