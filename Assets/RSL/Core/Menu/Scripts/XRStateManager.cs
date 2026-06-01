using UnityEngine;
using System.Threading.Tasks;
using Unity.XR.CoreUtils;
using System.ComponentModel;

namespace RSL.Core.Menu
{
    public class XRStateManager : MonoBehaviour
    {
        // Prefab object to be instantiated at each anchor position for debugging purposes
        public GameObject anchorPrefab;
        // If true, the anchor prefab will be instantiated at the origin for debugging. Otherwise, an empty GameObject will be used as the anchor to avoid visual clutter.
        public bool SpawnAnchorPrefabForDebugging = false;

        // Threshold distance to consider the anchor already close enough to the origin to skip re-centering (to avoid jitter when the anchor is already near the origin)
        public double originDistanceThreshold = 0.1; // Meters

        // Delay before creating the origin anchor on startup (to give system time to acquire tracking and stabilize the initial pose)
        public int originAnchorDelay = 1000; // Milliseconds

        // Reference to the XROrigin in the scene
        public XROrigin xrOrigin;

        // Reference to the anchor that holds the origin when first initialized
        private GameObject originAnchor;

        // Flag to prevent multiple simultaneous anchor creation attempts during startup
        private bool creatingAnchor = false;

        // Flag to track whether tracking has been acquired at least once (to avoid trying to create an anchor before tracking is available)
        bool TrackingAcquired = false;

        void Start()
        {
            // OVRManager.HMDMounted += OnHMDMounted;
            OVRManager.TrackingAcquired += OnTrackingAcquired;
        }

        void OnHMDMounted()
        {
            Debug.Log("HMD mounted, creating anchor at controller position");
            LocalizeOrigin();
        }

        void OnTrackingAcquired()
        {
            Debug.Log("Tracking acquired, creating anchor");
            TrackingAcquired = true;
        }

        void Update()
        {
            if (originAnchor == null || !originAnchor.TryGetComponent<OVRSpatialAnchor>(out _))
            {
                // Debug.Log("No anchor created yet, saving position");
                if (TrackingAcquired) CreateOriginAnchor();
            } else
            {
                LocalizeOrigin();
            }
        }

        async void CreateOriginAnchor()
        {
            if (creatingAnchor)
            {
                return;
            }
            creatingAnchor = true;
            await Task.Delay(originAnchorDelay);
            // OVRManager.display.RecenterPose(); // Recenter the pose because for some reason it's not correct the first time

            // Instantiate the prefab at the origin
            if (SpawnAnchorPrefabForDebugging)
            {
                originAnchor = Instantiate(anchorPrefab, xrOrigin.transform.position, Quaternion.Inverse(xrOrigin.transform.rotation));
            } else {
                originAnchor = new GameObject("OriginAnchor");
                originAnchor.transform.SetPositionAndRotation(xrOrigin.transform.position, Quaternion.Inverse(xrOrigin.transform.rotation));
            }

            // Add the anchor component
            OVRSpatialAnchor anchor = originAnchor.AddComponent<OVRSpatialAnchor>();

            // Wait for localization so it becomes world‑locked
            await anchor.WhenLocalizedAsync();

            // Debug.Log("Origin anchor created and localized at world origin.");
        }

        async void LocalizeOrigin()
        {
            if (originAnchor == null)
            {
                // Debug.LogWarning("Origin anchor not created yet");
                return;
            }

            OVRSpatialAnchor anchor = originAnchor.GetComponent<OVRSpatialAnchor>();

            if (anchor == null)
            {
                // Debug.LogWarning("Origin anchor component missing");
                return;
            }

            // Wait for localization so it becomes world‑locked
            await anchor.WhenLocalizedAsync();


            float distanceToOrigin = Vector3.Distance(originAnchor.transform.position, Vector3.zero);
            // Debug.LogWarning("Anchor Distance " + originDistanceThreshold + " " + distanceToOrigin);


            if (distanceToOrigin < originDistanceThreshold)
            {
                // Debug.LogWarning("Skipping because " + (distanceToOrigin < originDistanceThreshold));
                return;
            }

            // Debug.Log($"Origin position: {xrOrigin.transform.position}");

            Quaternion anchorYaw = Quaternion.Euler(0, originAnchor.transform.rotation.eulerAngles.y, 0); // Keep only yaw rotation of the anchor to prevent unwanted tilting of the XR Origin

            Quaternion newOriginRotation = xrOrigin.transform.rotation * Quaternion.Inverse(anchorYaw);
            Vector3 newOriginPosition = Quaternion.Inverse(anchorYaw) * (xrOrigin.transform.position - originAnchor.transform.position);
            // Debug.LogWarning("Anchor rotation: " + originAnchor.transform.rotation.eulerAngles);

            xrOrigin.transform.SetPositionAndRotation(newOriginPosition, newOriginRotation);
            // Debug.LogWarning("New Anchor rotation: " + originAnchor.transform.rotation.eulerAngles);

            // Debug.Log("XR Origin repositioned to match origin anchor at " + originAnchor.transform.position);
        }
    }
} // namespace RSL.Core.Menu
