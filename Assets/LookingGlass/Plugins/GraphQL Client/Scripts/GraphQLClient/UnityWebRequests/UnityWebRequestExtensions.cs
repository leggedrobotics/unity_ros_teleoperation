using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using GraphQLClient.EventCallbacks;

#if HAS_NEWTONSOFT_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace GraphQLClient {
    public static class UnityWebRequestExtensions {
        private static T GetRequestOrDefault<T>(UnityWebRequest request) => (request is T unityWebRequestReturn) ? unityWebRequestReturn : default;

        public static async Task<UnityWebRequest> SendAsync(this UnityWebRequest request, NetworkErrorBehaviour errorBehaviour, int timeoutMs) => await SendAsync<UnityWebRequest>(request, errorBehaviour, timeoutMs);
        public static async Task<T> SendAsync<T>(this UnityWebRequest request, NetworkErrorBehaviour errorBehaviour, int timeoutMs) {
            string prevStackTrace = Environment.StackTrace;

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            request.SendWebRequest().completed += a => tcs.SetResult(request.result == UnityWebRequest.Result.Success);
            Task<bool> webRequestTask = tcs.Task;

            Task completed;
            bool success = false;
            if (timeoutMs > 0) {
                completed = await Task.WhenAny(webRequestTask, Task.Delay(timeoutMs));
                if (completed == webRequestTask)
                    success = webRequestTask.Result;
            } else {
                success = await webRequestTask;
                completed = webRequestTask;
            }

            if (errorBehaviour != NetworkErrorBehaviour.Silent && completed != webRequestTask) {
                string timeoutMessage = "No response from the server within " + ((float) timeoutMs / 1000).ToString("F2") + "sec!";
                switch (errorBehaviour) {
                    case NetworkErrorBehaviour.Exception:
                        tcs.SetCanceled();
                        Debug.LogError(timeoutMessage);
                        return GetRequestOrDefault<T>(request);
                    case NetworkErrorBehaviour.ErrorLog:
                        Debug.LogError(timeoutMessage);
                        return GetRequestOrDefault<T>(request);
                    default:
                        throw new NotSupportedException("Unsupported " + nameof(NetworkErrorBehaviour) + ": " + errorBehaviour);
                }
            }

            if (!success) {
                if (errorBehaviour == NetworkErrorBehaviour.Silent)
                    return GetRequestOrDefault<T>(request);
                string errorMessage = "ERROR from " + request.url + " (" + request.responseCode + " " + (HttpStatusCode) request.responseCode + ") response from the web request!\n"
                            + request.error + "\n\n" + request.downloadHandler.text;
                Debug.LogError("PREVIOUS STACK TRACE =\n" + prevStackTrace);
                switch (errorBehaviour) {
                    case NetworkErrorBehaviour.Exception:
                        throw new Exception(errorMessage);
                    case NetworkErrorBehaviour.ErrorLog:
                        Debug.LogError(errorMessage);
                        return GetRequestOrDefault<T>(request);
                    default:
                        throw new NotSupportedException("Unsupported " + nameof(NetworkErrorBehaviour) + ": " + errorBehaviour);
                }
            }

            //NOTE: This if statement is so that if this method is called as SendAsync<UnityWebRequest>, that just returns back the web request object.
            if (request is T unityWebRequestReturn)
                return unityWebRequestReturn;
            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }

        private static async Task<bool> SendAsync(this UnityWebRequest request) {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            request.SendWebRequest().completed += a => tcs.SetResult(request.result == UnityWebRequest.Result.Success);
            return await tcs.Task;
        }

        private static async Task<T> SendGraphQLWebRequest<T>(UnityWebRequest request, string queryName, NetworkErrorBehaviour errorBehaviour, int timeoutMs, string authToken = null) {
            try {
                new OnGraphQLRequestStarted().FireEvent();
                if (!string.IsNullOrEmpty(authToken))
                    request.SetRequestHeader("Authorization", "Bearer " + authToken);
                Task sendTask = request.SendAsync(errorBehaviour, timeoutMs);

                try {
                    await sendTask;
                } catch (Exception e) {
                    new OnGraphQLRequestEnded(e).FireEvent();
                    throw;
                }

                if (request.result != UnityWebRequest.Result.Success)
                    return GetRequestOrDefault<T>(request);

#if HAS_NEWTONSOFT_JSON
                JObject json = JObject.Parse(request.downloadHandler.text);

                if (json.TryGetValue("errors", out JToken errorsAsToken)) {
                    JArray errors = errorsAsToken.Value<JArray>();
                    Debug.LogError("The GraphQL API at " + request.url + " returned with " + errors.Count + " errors:\n" + errors.Parent);
                } else {
                    Debug.Log("RESPONSE from " + request.url + ":\n" + request.downloadHandler.text);
                }

                if (request is T unityWebRequestReturn)
                    return unityWebRequestReturn;
                return JsonUtility.FromJson<T>(json["data"][queryName].ToString());
#else
                throw GraphQLJsonUtility.GetMissingNewtonsoftJsonException();
#endif
            } finally {
                new OnGraphQLRequestEnded(request.downloadHandler.text).FireEvent();
            }
        }

        public static async Task<UnityWebRequest> PostAsync(string url, string queryName, string query, string authToken = null) => await PostAsync<UnityWebRequest>(url, queryName, query, authToken);
        public static async Task<T> PostAsync<T>(string url, string queryName, string query, string authToken = null) {
            Debug.Log(url);
            string jsonData = GraphQLJsonUtility.ToJson(new { query = query });
            using (UnityWebRequest request =
#if UNITY_2022_2_OR_NEWER
                UnityWebRequest.Post(url, jsonData, "application/json")
#else
                UnityWebRequest.Post(url, jsonData)
#endif
                ) {
#if !UNITY_2022_2_OR_NEWER
                request.SetRequestHeader("Content-Type", "application/json");
                byte[] postData = Encoding.ASCII.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(postData);
#endif
                T result = await SendGraphQLWebRequest<T>(request, queryName, NetworkErrorBehaviour.Exception, 0, authToken);

                request.disposeDownloadHandlerOnDispose = true;
                request.disposeUploadHandlerOnDispose = true;
                request.disposeCertificateHandlerOnDispose = true;
                return result;
            }
        }

        public static async Task<UnityWebRequest> GetAsync(string url, string queryName, string authToken = null) => await GetAsync(url, queryName, authToken);
        public static async Task<T> GetAsync<T>(string url, string queryName, string authToken = null) =>
            await SendGraphQLWebRequest<T>(UnityWebRequest.Get(url), queryName, NetworkErrorBehaviour.ErrorLog, 0, authToken);
    }
}
