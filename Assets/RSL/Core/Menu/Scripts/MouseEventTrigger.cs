using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.EventSystems;
using RSL.Telemetry.Pose;

namespace RSL.Core.Menu
{
    public class MouseEventTrigger : MonoBehaviour
    {
        public delegate void PoseHitDelegate(Vector3 hitPosition);
        public PoseHitDelegate firstSelected;
        public PoseHitDelegate dragSelected;
        public PoseHitDelegate lastSelected;

        public bool isBeingDragged = false;
        private Vector3 start;

        void Start()
        {
            // Find pose publisher in scene
            PosePublisher publisher = FindObjectOfType<PosePublisher>();
            if (publisher != null)
            {
                // Subscribe to its events
                firstSelected += publisher.FirstSelected;
                lastSelected += publisher.LastSelected;
                dragSelected += publisher.OnPositionUpdate;
            }
        }

        void OnMouseDown()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider == GetComponent<Collider>() && !IsPointerOverUI())
                {
                    start = GetMouseWorldPosition();
                    isBeingDragged = true;

                    firstSelected?.Invoke(start);
                }
            }
        }

        void OnMouseDrag()
        {
            if (!isBeingDragged) return;

            Vector3 worldPos = GetMouseWorldPosition();
            worldPos.y = start.y; // Keep original y position
            dragSelected?.Invoke(worldPos);
        }

        void OnMouseUp()
        {
            if (!isBeingDragged) return;

            Vector3 worldPos = GetMouseWorldPosition();
            worldPos.y = start.y;

            lastSelected?.Invoke(worldPos);

            isBeingDragged = false;
        }

        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mousePos = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                mousePos.z = hit.distance;
                return Camera.main.ScreenToWorldPoint(mousePos);
            }
            return Vector3.zero; // Return a default value if no hit is detected
        }
        
        private bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}