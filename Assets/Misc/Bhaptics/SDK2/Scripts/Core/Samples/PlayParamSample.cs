using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Bhaptics.SDK2
{
    public class PlayParamSample : MonoBehaviour
    {
        private readonly string SampleAppID = "p4fZNOU6ShHYBxos8Zmr";
        private readonly string SampleApiKey = "BJ4Sacbew0qVfqONoMSM";


        [Header("Canvas")]
        [SerializeField] private Canvas initCanvas;
        [SerializeField] private Canvas mainCanvas;

        [Header("Dropdown")]
        [SerializeField] private Dropdown eventsDropdown;

        [Header("Slider")]
        [SerializeField] private Slider sliderIntensity;
        [SerializeField] private Slider sliderDuration;
        [SerializeField] private Slider sliderAngleX;
        [SerializeField] private Slider sliderOffsetY;

        [Header("Text")]
        [SerializeField] private Text intensityValueText;
        [SerializeField] private Text durationValueText;
        [SerializeField] private Text angleXValueText;
        [SerializeField] private Text offsetYValueText;
        [SerializeField] private Text playButtonText;

        [Header("Button")]
        [SerializeField] private Button intensityResetButton;
        [SerializeField] private Button durationResetButton;
        [SerializeField] private Button angleXResetButton;
        [SerializeField] private Button offsetYResetButton;

        [Header("Sample")]
        [SerializeField] private BhapticsSettings sampleSettings;




        private int requestId = -1;
        private string eventName = "";
        private MappingMetaData[] events;
        private Coroutine onClickPlayCoroutine;
        private BhapticsSettings currentSettings;


        #region Properties
        private float intensity = 1f;
        private float duration = 1f;
        private float angleX = 0f;
        private float offsetY = 0.5f;
        private int selectedIndex = -1;


        public float Intensity
        {
            get
            {
                return intensity;
            }
            set
            {
                intensity = value;
                if (intensityValueText != null)
                {
                    intensityValueText.text = intensity.ToString("0.0");
                }
                if (intensityResetButton != null)
                {
                    intensityResetButton.gameObject.SetActive(!intensity.Equals(1f));
                }
            }
        }

        public float Duration
        {
            get
            {
                return duration;
            }
            set
            {
                duration = value;
                if (durationValueText != null)
                {
                    durationValueText.text = duration.ToString("0.0");
                }
                if (durationResetButton != null)
                {
                    durationResetButton.gameObject.SetActive(!duration.Equals(1f));
                }
            }
        }

        public float AngleX
        {
            get
            {
                return angleX;
            }
            set
            {
                angleX = value;
                if (angleXValueText != null)
                {
                    angleXValueText.text = angleX.ToString("0");
                }
                if (angleXResetButton != null)
                {
                    angleXResetButton.gameObject.SetActive(!angleX.Equals(0f));
                }
            }
        }

        public float OffsetY
        {
            get
            {
                return offsetY;
            }
            set
            {
                offsetY = value;
                if (offsetYValueText != null)
                {
                    offsetYValueText.text = offsetY.ToString("0.00");
                }
                if (offsetYResetButton != null)
                {
                    offsetYResetButton.gameObject.SetActive(!offsetY.Equals(0f));
                }
            }
        }

        public int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                selectedIndex = value;
            }
        }
        #endregion



        private void Start()
        {
            currentSettings = BhapticsSettings.Instance;
            CheckApplicationSetting();

        }

        private void OnDisable()
        {
            StopAllCoroutines();

            onClickPlayCoroutine = null;
        }




        public void OnClickPlay()
        {
            if (onClickPlayCoroutine == null)
            {
                onClickPlayCoroutine = StartCoroutine(OnClickPlayCor());
            }
            else
            {
                StopHaptic();

                playButtonText.text = "Play";
            }
        }

        public void SetOffsetY(string offsetYStr)
        {
            if (float.TryParse(offsetYStr, out float res))
            {
                OffsetY = res;
            }
        }

        public void OpenDeveloperPortal()
        {
            Application.OpenURL("https://developer.bhaptics.com");
        }

        public void OpenGuideLink()
        {
            Application.OpenURL("https://bhaptics.notion.site/How-to-Start-bHaptics-Unity-SDK2-Beta-33cc33dcfa44426899a3f21c62adf66d");
        }

        public void UseSampleSettings()
        {
            currentSettings = sampleSettings;
            BhapticsLibrary.Initialize(currentSettings.AppId, currentSettings.ApiKey, currentSettings.DefaultDeploy);
            CheckApplicationSetting();
        }




        private void PlayHaptic(string eventName, float intensity, float duration, float angleX, float offsetY)
        {
            this.eventName = eventName;
            requestId = BhapticsLibrary.PlayParam(eventName, intensity, duration, angleX, offsetY);
        }

        private void StopHaptic()
        {
            BhapticsLogManager.LogFormat("Stop {0}", requestId);
            BhapticsLibrary.StopInt(requestId);

            if (onClickPlayCoroutine != null)
            {
                StopCoroutine(onClickPlayCoroutine);
                onClickPlayCoroutine = null;
            }
        }

        private void ResetValues()
        {
            Duration = 1f;
            Intensity = 1f;
            AngleX = 0f;
            OffsetY = 0f;

            sliderDuration.value = Duration;
            sliderIntensity.value = Intensity;
            sliderAngleX.value = AngleX;
            sliderOffsetY.value = OffsetY;
        }

        private IEnumerator OnClickPlayCor()
        {
            playButtonText.text = "Stop";

            // You can also use the const value "BhapticsEvent.OOO" instead of "eventsDropdown.options[SelectedIndex].text".
            PlayHaptic(eventsDropdown.options[SelectedIndex].text, Duration, Intensity, AngleX, OffsetY);

            yield return null;

            while (true)
            {
                if (!BhapticsLibrary.IsPlayingByRequestId(requestId))
                {
                    break;
                }

                yield return null;
            }

            playButtonText.text = "Play";
            onClickPlayCoroutine = null;
        }

        private void SetupApplicationData()
        {
            events = currentSettings.EventData;
            BhapticsLogManager.LogFormat("[bHaptics] eventSize {0}", events.Length);

            eventsDropdown.options = new List<Dropdown.OptionData>();

            foreach (var ev in events)
            {
                var option = new Dropdown.OptionData(ev.key);
                eventsDropdown.options.Add(option);
            }

            if (events.Length > 0)
            {
                SelectedIndex = 0;
                eventsDropdown.value = SelectedIndex;
            }

            ResetValues();
        }

        private void CheckApplicationSetting()
        {
            if (currentSettings.LastDeployVersion <= 0)
            {
                initCanvas.gameObject.SetActive(true);
                mainCanvas.gameObject.SetActive(false);
                return;
            }

            SetupApplicationData();

            initCanvas.gameObject.SetActive(false);
            mainCanvas.gameObject.SetActive(true);
        }
    }
}
