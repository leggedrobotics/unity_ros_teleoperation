using UnityEngine;
using System.Threading.Tasks;
using Unity.XR.CoreUtils;
using System.ComponentModel;

public class XRStateManager : MonoBehaviour
{
    public GameObject anchorPrefab;

    private GameObject originAnchor;

    public double originDistanceThreshold = 0.1; // Meters

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

    bool TrackingAcquired = false;
    void OnTrackingAcquired()
    {
        Debug.Log("Tracking acquired, creating anchor");
        TrackingAcquired = true;
    }

    void Update()
    {
        if (originAnchor == null || !originAnchor.TryGetComponent<OVRSpatialAnchor>(out _))
        {
            Debug.Log("No anchor created yet, saving position");
            if (TrackingAcquired) CreateOriginAnchor();
        } else
        {
            LocalizeOrigin();
        }
    }

    public Transform target;
    void TeleportToTarget()
    {
        XROrigin xrOrigin = FindObjectOfType<XROrigin>();
        xrOrigin.MoveCameraToWorldLocation(target.position);
        xrOrigin.MatchOriginUpCameraForward(target.up, target.forward);
        Debug.Log("XR Origin repositioned to match target.");
    }

    public async void CreateSpatialAnchor()
    {
        // Get controller pose in world space
        Vector3 pos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        Quaternion rot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);

        // Instantiate the prefab at the controller pose
        GameObject go = Instantiate(anchorPrefab, pos, rot);

        // Add the anchor component
        OVRSpatialAnchor anchor = go.AddComponent<OVRSpatialAnchor>();

        // IMPORTANT: Force the anchor to use the prefab's world pose
        // anchor.transform.SetPositionAndRotation(pos, rot);

        // // Wait for localization so it becomes world‑locked
        // await anchor.WhenLocalizedAsync();

        // Debug.Log("Anchor created and localized at controller position.");
    }

    // Flag to prevent multiple simultaneous anchor creation attempts during startup
    private bool creatingAnchor = false;
    async void CreateOriginAnchor()
    {
        if (creatingAnchor)
        {
            return;
        }
        creatingAnchor = true;
        await Task.Delay(3000);
        // GameObject xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>().gameObject;
        // xrOrigin = origin.gameObject;
        // Instantiate the prefab at the origin
        // originAnchor = Instantiate(anchorPrefab, xrOrigin.transform.position, xrOrigin.transform.rotation);
        originAnchor = Instantiate(anchorPrefab, xrOrigin.transform.position, Quaternion.Inverse(xrOrigin.transform.rotation));
        // originAnchor.transform.eulerAngles = new Vector3(0, originAnchor.transform.eulerAngles.y, 0); // Keep only the yaw rotation to ensure the anchor is upright (otherwise bad things happen)

        // Add the anchor component
        OVRSpatialAnchor anchor = originAnchor.AddComponent<OVRSpatialAnchor>();

        // Wait for localization so it becomes world‑locked
        await anchor.WhenLocalizedAsync();

        Debug.Log("Origin anchor created and localized at world origin.");
    }

    public XROrigin xrOrigin;

    async void LocalizeOrigin()
    {
        if (originAnchor == null)
        {
            Debug.LogWarning("Origin anchor not created yet");
            return;
        }

        OVRSpatialAnchor anchor = originAnchor.GetComponent<OVRSpatialAnchor>();

        if (anchor == null)
        {
            Debug.LogWarning("Origin anchor component missing");
            return;
        }

        // Wait for localization so it becomes world‑locked
        await anchor.WhenLocalizedAsync();


        float distanceToOrigin = Vector3.Distance(originAnchor.transform.position, Vector3.zero);
        Debug.LogWarning("Anchor Distance " + originDistanceThreshold + " " + distanceToOrigin);


        if (distanceToOrigin < originDistanceThreshold)
        {
            Debug.LogWarning("Skipping because " + (distanceToOrigin < originDistanceThreshold));
            return;
        }

        // GameObject xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>().gameObject;
        // xrOrigin.transform.SetPositionAndRotation(originAnchor.transform.position, originAnchor.transform.rotation);
        // origin.position = originAnchor.transform.position;
        // XROrigin xrOrigin = FindObjectOfType<XROrigin>();
        Debug.Log($"Origin position: {xrOrigin.transform.position}");


        Quaternion anchorYaw = Quaternion.Euler(0, originAnchor.transform.rotation.eulerAngles.y, 0);

        Quaternion newOriginRotation = xrOrigin.transform.rotation * Quaternion.Inverse(anchorYaw);
        // xrOrigin.transform.rotation = newOriginRotation;
        // Vector3 newOriginPosition = xrOrigin.transform.position - originAnchor.transform.position;
        // xrOrigin.transform.position = newOriginPosition;
        Vector3 newOriginPosition = Quaternion.Inverse(anchorYaw) * (xrOrigin.transform.position - originAnchor.transform.position);
        Debug.LogWarning("Anchor rotation: " + originAnchor.transform.rotation.eulerAngles);

        // xrOrigin.transform.rotation = newOriginRotation;
        xrOrigin.transform.SetPositionAndRotation(newOriginPosition, newOriginRotation);
        Debug.LogWarning("New Anchor rotation: " + originAnchor.transform.rotation.eulerAngles);

        // xrOrigin.MoveCameraToWorldLocation(-originAnchor.transform.position);
        // xrOrigin.MatchOriginUpCameraForward(originAnchor.transform.up, originAnchor.transform.forward);
        Debug.Log("XR Origin repositioned to match origin anchor at " + originAnchor.transform.position);
    }
}
