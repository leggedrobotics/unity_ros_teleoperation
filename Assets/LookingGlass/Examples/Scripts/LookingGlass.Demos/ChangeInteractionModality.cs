using UnityEngine;
using UnityEngine.UI;

namespace LookingGlass.Demos {
    public class ChangeInteractionModality : MonoBehaviour {

        [SerializeField] private GameObject[] lightingActiveUIElements;
        [SerializeField] private GameObject[] rotationActiveUIElements;

        [SerializeField] private Button lightControlButton, rotateControlButton;

        private XYSpotlight lightingControls;
        private ModelController rotationControls;

        private void Start() {
            lightingControls = GetComponent<XYSpotlight>();
            rotationControls = GetComponent<ModelController>();

            SwitchControls(true);

            lightControlButton.onClick.AddListener(() => SwitchControls(true));
            rotateControlButton.onClick.AddListener(() => SwitchControls(false));

        }

        public void SwitchControls(bool isLighting) {
            lightingControls.enabled = isLighting;
            rotationControls.enabled = !isLighting;

            for (int i = 0; i < lightingActiveUIElements.Length; i++) {
                lightingActiveUIElements[i].SetActive(isLighting);
            }
            for (int i = 0; i < rotationActiveUIElements.Length; i++) {
                rotationActiveUIElements[i].SetActive(!isLighting);
            }
        }
    }
}
