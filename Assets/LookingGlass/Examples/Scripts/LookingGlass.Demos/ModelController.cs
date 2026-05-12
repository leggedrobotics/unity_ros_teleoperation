using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LookingGlass.Demos {
    public class ModelController : MonoBehaviour {
        [SerializeField] private Transform target;
        [SerializeField] private GameObject rotationInteractionZone;

        [Header("Rotation Settings")]
        [SerializeField] private float rotateSpeed = 0.1f;
        [SerializeField] private float dampingFactor = 0.9f;
        [SerializeField] private float minVelocity = 0.01f;
        [SerializeField] private bool canRotate = true;

        private bool uiRaycastHack;
        public bool UIRaycastHack {
            set
            {
                uiRaycastHack = value;
            }
        }

        [Header("Zoom Settings")]
        [SerializeField] private float minZoomFactor = 0.5f;
        [SerializeField] private float maxZoomFactor = 2.0f;
        [SerializeField] private float zoomZOffset = 0.5f;
        [SerializeField] private float zoomLerpSpeed = 10f; // Higher = more responsive, lower = smoother
        [SerializeField] private float rotationVelocity;
        public float RotationVelocity => rotationVelocity;

        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 initialScale;
        private float currentZoomFactor = 1f;
        private bool isDragging = false;

        // Pinch variables
        private bool isPinching = false;
        private float initialPinchDistance;
        private float pinchStartZoomFactor;

        public void Initialize(Vector3 initPos, Quaternion initRot, Vector3 initScale) {
            initialPosition = initPos;
            initialRotation = initRot;
            initialScale = initScale;

            if (target != null) {
                target.localPosition = initPos;
                target.localRotation = initRot;
                target.localScale = initScale;
            }

            // Reset states
            currentZoomFactor = 1f;
            rotationVelocity = 0f;
            isDragging = false;
            isPinching = false;
            canRotate = true;
        }

        private void Start() {
            Initialize(target.position, target.rotation, target.localScale);
        }

        private void Update() {
            if (target == null) return;

            // Only allow rotation if not pinching
            if (canRotate && !uiRaycastHack && !isPinching) {
                HandleTouchRotation();
                HandleMouseRotation();
            }

            HandlePinchToZoom();

            // Apply rotation inertia if no input and not pinching
            if (!isDragging && !isPinching && Mathf.Abs(rotationVelocity) > minVelocity) {
                DoRotation();
                rotationVelocity *= dampingFactor;
            } else if (Mathf.Abs(rotationVelocity) <= minVelocity && !isPinching) {
                rotationVelocity = 0f;
            }
        }

        private void HandleTouchRotation() {
            if (Input.touchCount == 1) {
                Touch touch = Input.GetTouch(0);
                if (!IsOverRotationInteractionObject(touch.position)) return;

                if (touch.phase == TouchPhase.Began && !isDragging) {
                    isDragging = true;
                }

                if (touch.phase == TouchPhase.Moved) {
                    float rotationX = touch.deltaPosition.x * rotateSpeed;
                    rotationVelocity = -rotationX;
                    DoRotation();
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
                    isDragging = false;
                }
            }
        }

        private void HandleMouseRotation() {
            if (Input.GetMouseButtonDown(0)) {
                if (!IsOverRotationInteractionObject(Input.mousePosition)) return;
                if (!isDragging) {
                    isDragging = true;
                }
            }

            if (Input.GetMouseButton(0) && isDragging) {
                float rotationX = Input.GetAxis("Mouse X") * rotateSpeed * 100f;
                rotationVelocity = -rotationX;
                DoRotation();
            }

            if (Input.GetMouseButtonUp(0)) {
                isDragging = false;
            }
        }

        private void DoRotation() {
            target.Rotate(0f, rotationVelocity * Time.deltaTime, 0f, Space.Self);
        }

        private bool IsOverRotationInteractionObject(Vector2 position) {
            PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
            pointerEventData.position = position;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerEventData, results);

            for (int i = 0; i < results.Count; i++) {
                if (results[i].gameObject == rotationInteractionZone)
                    return true;
            }

            return false;
        }

        private void HandlePinchToZoom() {
            if (Input.touchCount == 2) {
                Touch touch1 = Input.GetTouch(0);
                Touch touch2 = Input.GetTouch(1);

                if (!IsOverRotationInteractionObject(touch1.position) || !IsOverRotationInteractionObject(touch2.position))
                    return;

                float currentDistance = Vector2.Distance(touch1.position, touch2.position);

                if (!isPinching) {
                    // Pinch start
                    isPinching = true;
                    isDragging = false;
                    rotationVelocity = 0f;

                    initialPinchDistance = currentDistance > 0 ? currentDistance : 1f;
                    pinchStartZoomFactor = currentZoomFactor;
                } else {
                    if (currentDistance > 0f && initialPinchDistance > 0f) {
                        float distanceRatio = currentDistance / initialPinchDistance;
                        float desiredZoomFactor = pinchStartZoomFactor * distanceRatio;
                        float clampedZoom = Mathf.Clamp(desiredZoomFactor, minZoomFactor, maxZoomFactor);

                        currentZoomFactor = Mathf.Lerp(currentZoomFactor, clampedZoom, Time.deltaTime * zoomLerpSpeed);
                        ApplyZoom();
                    }
                }

                // If either touch ended, pinch will end next frame
                if (touch1.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Canceled ||
                    touch2.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Canceled) {
                    isPinching = false;
                }
            } else {
                isPinching = false;
            }
        }

        private void ApplyZoom() {
            Vector3 newScale = initialScale * currentZoomFactor;
            target.localScale = newScale;

            float zoomProgress = (currentZoomFactor - 1f);
            Vector3 newPosition = initialPosition + target.transform.forward * (zoomZOffset * zoomProgress);
            target.localPosition = newPosition;
        }
    }
}
