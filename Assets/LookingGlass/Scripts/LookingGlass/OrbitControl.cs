using System;
using LookingGlass.Toolkit;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LookingGlass {
    public class OrbitControl : MonoBehaviour {

        private static OrbitControl instance;
        public static OrbitControl Instance {
            get {
                if (instance != null) return instance;
                instance = FindObjectOfType<OrbitControl>();
                return instance;
            }
        }

        [NonSerialized] public bool touchInputThisFrame = false;

        private Vector2 rMomentum;
        private Vector3 rLastPos;

        [Header("Rotation variables")]
        [SerializeField][Range(0, 2)] private float rotateSpeed = 0.5f;
        [SerializeField][Range(0, 1)] private float rotateDrag = 0.1f;

        [SerializeField][Range(0, 90)] private float yMax = 80;
        [SerializeField][Range(-90, 0)] private float yMin = -80;

        private Vector2 pMomentum;
        private Vector3 pLastPos;

        [Header("Pan")]
        [SerializeField][Range(0, 2)] private float panSpeed = 0.7f;
        [SerializeField][Range(0, 1)] private float panDrag = 0.1f;

        private float zMomentum;
        private float zLastYPos;

        [Header("Zoom")]
        [SerializeField][Range(0, 10)] private float mouseZoomSpeed = 1f;
        [SerializeField][Range(0, .2f)] private float multitouchZoomSpeed = .1f;
        [SerializeField][Range(0, 1)] private float zoomDrag = 0.2f;
        [SerializeField] private float multitouchZoomThreshold = 20f;
        [SerializeField] private float multitouchMaxJump = 100f;
        [SerializeField] private float minCamSize = 0.01f;
        [SerializeField] private float maxCamSize = 500f;
        private bool validRotationStart = false;
        private bool refocusingToPosition = false;
        private Vector3 refocusToPosition;
        private Vector3 startRefocusPosition;

        [Header("Double Click")]
        [SerializeField] private AnimationCurve refocusToPointCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float refocusToTime = 0.5f;
        [SerializeField] private float maxDistBetweenClicksMouse = 10f;
        [SerializeField] private float maxDistBetweenClicksTouch = 20f;
        [SerializeField] private float doubleClickTime = 0.5f;
        [SerializeField] private float doubleClickTimeTouch = 1f;
        private float doubleClickTimer;
        private float refocusToTimer;
        private bool oneClick = false;
        private bool oneTouch = false;
        private Vector3 oneClickPos;

        [Header("UI Event System")]
        [SerializeField] private EventSystem eventSys = null;

        [NonSerialized] public bool canInteract = true;

        [Header("Multitouch Remaps")]
        public Vector2 multitouchRemapCursorY = new Vector2(0.05f, 0.95f);
        public Vector2 multitouchRemapCursorX = new Vector2(0.05f, 0.95f);

        private Vector2[] lastTouchPositions;

        [SerializeField] private Renderer cursorRend;

        private Vector3 mousePosOnTouchEnd;

        private void Awake() {
#if !ENABLE_LEGACY_INPUT_MANAGER
            //TODO: Update this script.. it heavily relies upon UnityEngine.Input.
            //For InputSystem's touch support (high-level), see:
            //      https://docs.unity3d.com/Packages/com.unity.inputsystem@1.5/manual/Touch.html
            //      https://docs.unity3d.com/Packages/com.unity.inputsystem@1.5/api/UnityEngine.InputSystem.EnhancedTouch.Touch.html

            Debug.LogWarning(this + " relies on the old UnityEngine.Input API!\n" +
                "It must be updated to use the new InputSystem, but more information is needed to maintain feature parity with the current touch input usage.");
            enabled = false;
            return;
#endif
        }

        private void Start() {
            lastTouchPositions = new Vector2[2];
            if (cursorRend == null) {
                cursorRend = FindObjectOfType<Cursor3D>().GetComponentInChildren<Renderer>();
            }
        }

        private void Update() {
            bool touchLastFrame = touchInputThisFrame;
            touchInputThisFrame = Input.touchCount > 0;

            if (touchInputThisFrame) {
                if (!touchLastFrame && cursorRend.enabled)
                    cursorRend.enabled = false;
            } else if (touchLastFrame) {
                mousePosOnTouchEnd = Input.mousePosition;
            } else if (!cursorRend.enabled && Vector3.SqrMagnitude(mousePosOnTouchEnd - Input.mousePosition) > 0 && !Cursor3D.Instance.uiCursorMode) {
                cursorRend.enabled = true;
            }

            bool overUI = false;
            if (eventSys != null) {
                overUI = eventSys.IsPointerOverGameObject();
            }

            if (refocusingToPosition) {
                refocusToTimer += Time.deltaTime;
                refocusToTimer = Mathf.Clamp(refocusToTimer, 0, refocusToTime);
                float t = refocusToPointCurve.Evaluate(refocusToTimer / refocusToTime);
                HologramCamera.Instance.transform.position = Vector3.Lerp(startRefocusPosition, refocusToPosition, t);
                if (refocusToTimer == refocusToTime) {
                    refocusingToPosition = false;
                    refocusToTimer = 0f;
                    validRotationStart = false;
                } else {
                    HandleDoubleClick();
                    return;
                }
            }

            if (canInteract && !overUI) {
                RotateInputStart();
                Pan();
                Zoom();
            }

            RotateInputContinue(overUI);
            if (!touchInputThisFrame || Input.touchCount == 1) //This seems sketch
                Rotate();
        }

        private void RotateInputStart() {
            if (Input.GetMouseButtonDown(0)) {
                validRotationStart = true;
                if (!touchInputThisFrame)
                    rLastPos = Input.mousePosition;
                else
                    rLastPos = MultitouchPosition(0);
                rMomentum = Vector2.zero;
            }
        }

        private void RotateInputContinue(bool overUI) {
            if (Input.GetMouseButton(0)) {
                if (validRotationStart) {
                    Vector3 delta = Input.mousePosition - rLastPos;
                    if (touchInputThisFrame)
                        delta = (Vector3) MultitouchPosition(0) - rLastPos;
                    delta *= Time.deltaTime * rotateSpeed;

                    rMomentum += new Vector2(-delta.y, delta.x);

                    rLastPos = Input.mousePosition;
                    if (touchInputThisFrame)
                        rLastPos = MultitouchPosition(0);
                }
            } else if (Input.GetMouseButtonUp(0)) {
                validRotationStart = false;
            }
        }

        private void Rotate() {
            float newY = transform.eulerAngles.x + rMomentum.x;

            if (newY < yMin || (newY < 360 + yMin && newY > 120)) {
                rMomentum = new Vector2(0, rMomentum.y);
                transform.eulerAngles = new Vector3(yMin, transform.eulerAngles.y, 0);
            } else if (newY > yMax && newY < 360 + yMin) {
                rMomentum = new Vector2(0, rMomentum.y);
                transform.eulerAngles = new Vector3(yMax, transform.eulerAngles.y, 0);
            } else {
                transform.RotateAround(transform.position, transform.right, rMomentum.x);
            }

            transform.RotateAround(transform.position, Vector3.up, rMomentum.y);
        }

        private void Zoom() {
            if (!touchInputThisFrame) {
                SimpleZoom();
            } else if (Input.touchCount > 1) {
                MultitouchZoom();
            }
            HandleDoubleClick();
        }

        private void SimpleZoom() {
            float mouseScrollChange = Input.GetAxis("Mouse ScrollWheel");
            mouseScrollChange = Mathf.Clamp(mouseScrollChange, -1f, 1f);

            if (mouseScrollChange != 0) {
                HologramCamera h = HologramCamera.Instance;
                float size = h.CameraProperties.Size;
                float newSize = size - mouseScrollChange * mouseZoomSpeed * size;
                h.CameraProperties.Size = Mathf.Clamp(newSize, minCamSize, maxCamSize);
            }
        }

        private float Remap(float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        private Vector2 MultitouchPosition(int touchIndex) {
            Calibration cal = HologramCamera.Instance.Calibration;
            float minXVal = cal.screenW * multitouchRemapCursorX.x;
            float minYVal = cal.screenH * multitouchRemapCursorY.x;

            float x = Input.GetTouch(touchIndex).position.x * multitouchRemapCursorX.y + minXVal;
            float y = Input.GetTouch(touchIndex).position.y * multitouchRemapCursorY.y + minYVal;

            return new Vector2(x, y);
        }

        private void MultitouchZoom() {
            if (Input.GetTouch(1).phase == TouchPhase.Began) {
                lastTouchPositions[0] = MultitouchPosition(0);
                lastTouchPositions[1] = MultitouchPosition(1);
                return;
            }
            float distLastFrame = Vector2.Distance(lastTouchPositions[0], lastTouchPositions[1]);
            float distThisFrame = Vector2.Distance(MultitouchPosition(0), MultitouchPosition(1));

            float delta = distLastFrame - distThisFrame;

            if (Mathf.Abs(delta) < multitouchZoomThreshold)
                return;

            delta -= multitouchZoomThreshold * Mathf.Sign(delta);

            if (Mathf.Abs(delta) > multitouchMaxJump) {
                delta = multitouchMaxJump * Mathf.Sign(delta);
            }

            HologramCamera h = HologramCamera.Instance;
            float size = h.CameraProperties.Size;
            float newSize = size + delta * multitouchZoomSpeed * size;
            h.CameraProperties.Size = Mathf.Clamp(newSize, minCamSize, maxCamSize);

            lastTouchPositions[0] = MultitouchPosition(0);
            lastTouchPositions[1] = MultitouchPosition(1);
        }

        private void HandleDoubleClick() {
            if (oneClick) {
                doubleClickTimer += Time.deltaTime;
                if (doubleClickTimer >= doubleClickTime) {
                    oneClick = false;
                    doubleClickTimer = 0;
                }
            } else if (oneTouch) {
                doubleClickTimer += Time.deltaTime;
                if (doubleClickTimer >= doubleClickTimeTouch) {
                    oneTouch = false;
                    doubleClickTimer = 0;
                }
            }

            if (touchInputThisFrame && Input.GetTouch(0).phase == TouchPhase.Began) {
                if (oneTouch) {
                    if (Vector3.Distance(oneClickPos, MultitouchPosition(0)) < maxDistBetweenClicksTouch)
                        StartRaycastRefocus(Cursor3D.Instance.GetOverObject());
                } else {
                    oneTouch = true;
                    oneClickPos = MultitouchPosition(0);
                    doubleClickTimer = 0;
                }
            }

            if (Input.GetMouseButtonDown(0)) {
                if (oneClick) {
                    if (Vector3.Distance(oneClickPos, Input.mousePosition) < maxDistBetweenClicksMouse)
                        StartRaycastRefocus(Cursor3D.Instance.GetOverObject());
                } else {
                    oneClick = true;
                    oneClickPos = Input.mousePosition;
                    doubleClickTimer = 0;
                }
            }
        }

        private void StartRaycastRefocus(bool overObject) {
            Vector3 vec = Vector3.zero;

            if (overObject) {
                vec = Cursor3D.Instance.GetWorldPos();
            }

            refocusingToPosition = true;
            refocusToPosition = vec;
            refocusToTimer = 0f;
            startRefocusPosition = HologramCamera.Instance.transform.position;
            oneClick = false;
            oneTouch = false;
            doubleClickTimer = 0;
            rMomentum = Vector2.zero;
            pMomentum = Vector2.zero;
            zMomentum = 0f;
        }

        private void Pan() {
            if (!touchInputThisFrame) {
                if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) {
                    pLastPos = Input.mousePosition;
                    pMomentum = Vector2.zero;
                } else if (Input.GetMouseButton(1) || Input.GetMouseButton(2)) {
                    Vector3 delta = Input.mousePosition - pLastPos;
                    float adjustedSpeed = panSpeed / 100f;
                    delta *= Time.deltaTime * adjustedSpeed * HologramCamera.Instance.CameraProperties.Size;

                    pMomentum -= new Vector2(delta.x, delta.y);

                    pLastPos = Input.mousePosition;
                }
            } else {
                if (Input.touchCount > 1 && Input.GetTouch(1).phase == TouchPhase.Began) {
                    pLastPos = (MultitouchPosition(0) + MultitouchPosition(1)) / 2.0f;
                    pMomentum = Vector2.zero;
                } else if (Input.touchCount > 1) {
                    Vector3 currentPos = (MultitouchPosition(0) + MultitouchPosition(1)) / 2.0f;
                    Vector3 delta = currentPos - pLastPos;
                    float adjustedSpeed = panSpeed / 100f;
                    delta *= Time.deltaTime * adjustedSpeed * HologramCamera.Instance.CameraProperties.Size;

                    pMomentum -= new Vector2(delta.x, delta.y);

                    pLastPos = currentPos;
                }
            }

            transform.position += transform.right * pMomentum.x;
            transform.position += transform.up * pMomentum.y;
        }

        private void FixedUpdate() {
            rMomentum *= 1 - rotateDrag;
            pMomentum *= 1 - panDrag;
            zMomentum *= 1 - zoomDrag;
        }
    }
}
