using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace RSL.Core
{
    /// <summary>Request Quest body access independently of the active scene or an OVRManager.</summary>
    public static class QuestTrackingPermissions
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static PermissionCallbacks _callbacks;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RequestTrackingPermissions()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Hand access is granted at install time through the Meta project manifest settings.
            // Body access also needs a runtime request on Horizon OS versions that prompt for it.
            const string bodyPermission = OVRPermissionsRequester.BodyTrackingPermission;
            if (Permission.HasUserAuthorizedPermission(bodyPermission))
                return;

            _callbacks = new PermissionCallbacks();
            _callbacks.PermissionGranted += permission =>
                Debug.Log($"Quest tracking permission granted: {permission}");
            _callbacks.PermissionDenied += permission =>
                Debug.LogWarning($"Quest tracking permission denied: {permission}. Enable it in the headset app permissions to use body tracking.");
            try
            {
                Permission.RequestUserPermission(bodyPermission, _callbacks);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Could not request Quest body tracking permission: {exception.Message}");
            }
#endif
        }
    }
}
