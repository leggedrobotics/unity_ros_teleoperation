using UnityEngine;
using UnityEngine.EventSystems;

namespace LookingGlass.Demos {
    public class XYSlider : MonoBehaviour {
        [SerializeField] private RectTransform handle; // Reference to the handle (draggable icon)`
        [SerializeField] private RectTransform squareArea; // Reference to the square area
        [SerializeField] private Vector2 outputValues; // To store the normalized x and y output (range from -1 to 1)
        [SerializeField] private bool isHovering = false;
        [SerializeField] private XYSpotlight xySpotlight;

        private Vector2 squareSize;
        private bool isDragging = false;
        private Vector3 originalScale;
        private float scaleFactor = 1.15f; // Scale factor when dragging

        private ModelController modelController;

        private void Start() {
            modelController = FindObjectOfType<ModelController>();
            xySpotlight = FindObjectOfType<XYSpotlight>();
            // Store the initial position of the square and its size
            squareSize = squareArea.sizeDelta / 2; // Dividing by 2 to get half the width/height

            // Store the original scale of the handle
            originalScale = handle.localScale;
        }

        public void OnPointerDown(BaseEventData eventData) {
            modelController.UIRaycastHack = true;
            isHovering = true;
        }

        public void OnDrag(BaseEventData eventData) {
            if (!isDragging) {
                isDragging = true;
            }

            // Get mouse or touch position in local space relative to the square
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(squareArea, Input.mousePosition, null, out localPoint);

            // Clamp the handle position to stay within the bounds of the square
            localPoint.x = Mathf.Clamp(localPoint.x, -squareSize.x, squareSize.x );
            localPoint.y = Mathf.Clamp(localPoint.y, -squareSize.y, squareSize.y);

            // Move the handle to the new clamped position
            handle.localPosition = localPoint;

            // Calculate the normalized output values (-1 to 1) based on the handle's position
            outputValues = new Vector2( localPoint.x / squareSize.x, localPoint.y / squareSize.y);
            xySpotlight.SetSliderPos(outputValues.x, outputValues.y);
        }


        public void OnEndDrag(BaseEventData eventData) {
            modelController.UIRaycastHack = false;
            isHovering = false;
            isDragging = false;

        }

        public void Update() {
            Vector3 tarScale;
            if (isHovering) {
                tarScale = originalScale * scaleFactor;
            } else {
                tarScale = originalScale;
            }

            handle.localScale = Vector3.Lerp(originalScale, tarScale, .2f);
        }

    }
}
