using UnityEngine;
using UnityEngine.UI;
using LookingGlass.Toolkit;
using System.Collections.Generic;
using System;

namespace LookingGlass.Demos
{
    public class PopupTestHandler : MonoBehaviour
    {
        [SerializeField] private Button loadCalButton, clearTestImgButton, doneButton;
        [SerializeField] private Toggle dontShowAgainToggle;
        [SerializeField] private Texture testQuilt;
        [SerializeField] private QuiltPreset testQuiltPreset;

        private bool isEnabled = false;

        private bool preAutomaticQuiltPreset;
        private QuiltPreset prevQuiltPreset;
        private Dictionary<RenderStep, bool> renderStackEnableds = new Dictionary<RenderStep, bool>();

        public static event Action<bool> OnPickCalibrationRequsted;
        public static event Action OnTestDone;
        private RenderStep testRenderStep = null;

        private void Awake()
        {
            dontShowAgainToggle.onValueChanged.AddListener(onValueChanged);
            loadCalButton.onClick.AddListener(OnAddCalibration);
            clearTestImgButton.onClick.AddListener(OnClearTestImage);
            doneButton.onClick.AddListener(OnDoneClicked);

        }

        private void OnEnable()
        {
            HologramCamera hologramCamera = HologramCamera.Instance;
            if (hologramCamera == null)
                return;

            renderStackEnableds.Clear();

            // restore old status
            RenderStack renderSteps = hologramCamera.RenderStack;
            foreach (RenderStep renderStep in renderSteps)
            {
                if(renderStep != this.testRenderStep)
                    renderStackEnableds.Add(renderStep, renderStep.IsEnabled);
            }

            // show test image
            // check if the last render step is a quilt
            if (renderSteps[renderSteps.Count - 1].RenderType != RenderStep.Type.Quilt
                || renderSteps[renderSteps.Count - 1].QuiltTexture == null
                || renderSteps[renderSteps.Count - 1].QuiltTexture != testQuilt)
            {
                RenderStep step = new RenderStep(RenderStep.Type.Quilt);
                step.QuiltSettings = testQuiltPreset.QuiltSettings;
                step.QuiltTexture = testQuilt;

                renderSteps.Add(step);

                testRenderStep = step;
            }else{
                if(testRenderStep == null){
                    testRenderStep = renderSteps[renderSteps.Count - 1];
                }
            }

            prevQuiltPreset = hologramCamera.QuiltPreset;
            preAutomaticQuiltPreset = hologramCamera.AutomaticQuiltPreset;

            ToggleTestImage(true);

            dontShowAgainToggle.isOn = PopupPairHandler.DontShowAgain;
        }

        private void OnDisable()
        {
            OnClearTestImage();
        }

        private void onValueChanged(bool value)
        {
            PopupPairHandler.ToggleDontShowAgain(value);
        }

        private void OnAddCalibration()
        {
            OnPickCalibrationRequsted?.Invoke(false);
        }

        private void OnClearTestImage()
        {
            ToggleTestImage(false);
        }

        private void OnShowTestImage()
        {
            ToggleTestImage(true);
        }

        private void ToggleTestImage(bool isEnabled)
        {
            if (isEnabled == this.isEnabled)
                return;

            this.isEnabled = isEnabled;

            if (HologramCamera.Instance != null)
            {
                // clear test image
                int index = 0;
                foreach (var renderStep in HologramCamera.Instance.RenderStack)
                {
                    if (renderStep == this.testRenderStep)
                    {
                        renderStep.IsEnabled = isEnabled;
                    } else {
                        if (!renderStackEnableds.ContainsKey(renderStep))
                            renderStackEnableds.Add(renderStep, renderStep.IsEnabled);
                        if (isEnabled)
                            renderStep.IsEnabled = false; // disable all
                        else
                            renderStep.IsEnabled = renderStackEnableds[renderStep]; // set it back
                    }
                    index++;
                }
            }

            if (!isEnabled)
            {
                clearTestImgButton.onClick.RemoveListener(OnClearTestImage);
                clearTestImgButton.GetComponentInChildren<Text>().text = "Show test image";
                clearTestImgButton.onClick.AddListener(OnShowTestImage);
                HologramCamera.Instance?.SetQuiltPreset(preAutomaticQuiltPreset, prevQuiltPreset);
            }
            else
            {
                clearTestImgButton.onClick.RemoveListener(OnShowTestImage);
                clearTestImgButton.GetComponentInChildren<Text>().text = "Clear test image";
                clearTestImgButton.onClick.AddListener(OnClearTestImage);
                HologramCamera.Instance?.SetQuiltPreset(false, testQuiltPreset);
            }
        }

        private void OnDoneClicked()
        {
            ToggleTestImage(false);
            OnTestDone?.Invoke();
        }
    }
}
