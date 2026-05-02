using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Core.Menu
{
    [RequireComponent(typeof(Rigidbody))]
    public class MouseDraggable : MonoBehaviour
    {
        private Vector3 offset;
        private float distanceToCamera;
        private bool dragging = false;
        private Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void OnMouseDown()
        {
            distanceToCamera = Vector3.Distance(Camera.main.transform.position, transform.position);
            Vector3 mouseWorldPoint = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distanceToCamera));
            offset = transform.position - mouseWorldPoint;
            dragging = true;
        }

        void OnMouseDrag()
        {
            if (dragging)
            {
                // Convert screen point to world position
                Vector3 mouseWorldPoint = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distanceToCamera));
                Vector3 targetPosition = mouseWorldPoint + offset;

                // Project the target position onto the plane defined by the camera's forward direction
                Plane dragPlane = new Plane(Camera.main.transform.forward, transform.position);
                float distance;
                Ray ray = new Ray(mouseWorldPoint, Camera.main.transform.forward);
                if (dragPlane.Raycast(ray, out distance))
                {
                    targetPosition = ray.GetPoint(distance);
                }

                rb.MovePosition(targetPosition);
            }
        }


        void OnMouseUp()
        {
            dragging = false;
        }
    }
}
