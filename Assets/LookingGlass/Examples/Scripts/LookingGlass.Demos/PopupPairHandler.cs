using UnityEngine;
using UnityEngine.UI;
using LookingGlass.Mobile;
using LookingGlass.Toolkit;
using System;

namespace LookingGlass.Demos
{
    public class PopupPairHandler : MonoBehaviour
    {
        private const string DONTSHOWAGAINKEY = "DONTSHOWAGAINCALIBRATE";
        public static bool DontShowAgain => PlayerPrefs.GetInt(DONTSHOWAGAINKEY, 0) == 1;
        public static void ToggleDontShowAgain(bool value) => PlayerPrefs.SetInt(DONTSHOWAGAINKEY, value ? 1 : 0);

        [SerializeField] private Button loadCalButton, showTestImgButton, doneButton;
        [SerializeField] private Toggle dontShowAgainToggle;
        [SerializeField] private Text titleText;

        public static event Action<bool> OnAddCalibrationRequested;
        public static event Action OnShowTestImageRequested;
        public static event Action OnPairDone;


        private void Awake()
        {
            MobileDMAController.onCalibrationLoaded += SetTitle;
        }

        private void OnDestroy()
        {
            MobileDMAController.onCalibrationLoaded -= SetTitle;
        }

        private void Start()
        {
            dontShowAgainToggle.onValueChanged.AddListener(ToggleDontShowAgain);
            loadCalButton.onClick.AddListener(OnAddCalibration);
            showTestImgButton.onClick.AddListener(OnShowTestImage);
            doneButton.onClick.AddListener(OnDoneClicked);
        }

        private void OnEnable()
        {
            dontShowAgainToggle.isOn = DontShowAgain;
            if (HologramCamera.Instance != null)
                SetTitle(HologramCamera.Instance.Calibration);
        }

        public void SetTitle(Calibration calibration)
        {
            titleText.text = "Using calibration for " + calibration.serial;
        }

        private void OnAddCalibration() => OnAddCalibrationRequested?.Invoke(false);
        private void OnShowTestImage() => OnShowTestImageRequested?.Invoke();
        private void OnDoneClicked() => OnPairDone?.Invoke();
    }
}
