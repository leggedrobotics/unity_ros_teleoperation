using UnityEngine;
using UnityEngine.UI;

namespace LookingGlass.Demos {
    public class PageHandler : MonoBehaviour
    {
        [SerializeField] protected Button nextButton, backButton;

        protected DemoIOSUIController controller;
        private int pageIndex;

        private void Awake()
        {
            controller = GetComponentInParent<DemoIOSUIController>();
            if (nextButton != null) {
                nextButton.onClick.AddListener(OnNextClicked);
            }
            if (backButton != null) {
                backButton.onClick.AddListener(OnBackClicked);
            }
        }

        public void SetPageIndex(int pageIndex)
        {
            this.pageIndex = pageIndex;
        }

        protected virtual void OnNextClicked() {
            controller.SwitchToPage(pageIndex + 1);
        }

        protected virtual void OnBackClicked() {
            controller.SwitchToPage(pageIndex - 1);
        }
    }

}
