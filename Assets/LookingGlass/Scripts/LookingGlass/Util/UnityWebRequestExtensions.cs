using UnityEngine.Networking;

namespace LookingGlass {
    public static class UnityWebRequestExtensions {
        public static void FullyDispose(this UnityWebRequest request) {
            request.disposeDownloadHandlerOnDispose = true;
            request.disposeUploadHandlerOnDispose = true;
            request.disposeCertificateHandlerOnDispose = true;
            request.Dispose();
        }
    }
}
