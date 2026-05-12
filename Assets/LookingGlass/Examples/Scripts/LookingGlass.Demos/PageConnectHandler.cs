using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace LookingGlass.Demos {
    public class PageConnectHandler : PageHandler {
        [SerializeField] Sprite spIPhone, spIPad;
        [SerializeField] string sIPhoneGuide, sIPadGuide;
        [SerializeField] Image imgGuide;
        [SerializeField] Text txtGuide;

        protected override void OnNextClicked()
        {
            controller.OnLoadCalibration();
        }

        public static bool isIPhone(){
            string model = SystemInfo.deviceModel.ToLower();
            return model.Contains("iphone");
        }

        private async void OnEnable()
        {
            if (isIPhone())
            {
                imgGuide.sprite = spIPhone;
                txtGuide.text = sIPhoneGuide;

            }
            else
            {
                imgGuide.sprite = spIPad;
                txtGuide.text = sIPadGuide;
            }

#if UNITY_EDITOR
            nextButton.gameObject.SetActive(true);
#else
            nextButton.gameObject.SetActive(false);
#endif
            if (await DemoIOSUIController.isDisplayConnected())
                OnNextClicked();
        }
    }
}
