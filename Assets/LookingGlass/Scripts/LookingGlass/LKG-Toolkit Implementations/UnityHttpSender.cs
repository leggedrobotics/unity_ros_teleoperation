using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using LookingGlass.Toolkit;

using Debug = UnityEngine.Debug;

namespace LookingGlass {
    public class UnityHttpSender : IHttpSender {
        private int timeoutSeconds = 0;

        public int TimeoutSeconds {
            get { return timeoutSeconds; }
            set { timeoutSeconds = value; }
        }

        private UnityWebRequest CreateRequestInner(HttpSenderMethod method, string url, string content = null) {
            switch (method) {
                case HttpSenderMethod.Get: return UnityWebRequest.Get(url);

                case HttpSenderMethod.Post: {
                        UnityWebRequest request =
#if UNITY_2022_2_OR_NEWER
                            UnityWebRequest.Post(url, content, "application/json");
#else
                            UnityWebRequest.Post(url, content);
                        request.SetRequestHeader("Content-Type", "application/json");
#endif
                        return request;
                }
                case HttpSenderMethod.Put: return UnityWebRequest.Put(url, content);
                default:
                    throw new NotSupportedException("Unsupported HTTP method: " + method);
            }
        }

        private UnityWebRequest CreateRequest(HttpSenderMethod method, string url, string content = null) {
            UnityWebRequest request = CreateRequestInner(method, url, content);
            request.timeout = timeoutSeconds;
            return request;
        }

        public string Send(HttpSenderMethod method, string url, string content) {
            UnityWebRequest request = CreateRequest(method, url, content);
            request.SendWebRequest();
            try {
                Stopwatch s = Stopwatch.StartNew();
                while (!request.isDone) {
                    if (timeoutSeconds > 0 && s.Elapsed.TotalSeconds > timeoutSeconds)
                        throw new TimeoutException("Synchronous HTTP message took longer than the maximum set time of " + timeoutSeconds + "sec!");
                }
                string result = request.downloadHandler.text;
                return result;
            } finally {
                request.FullyDispose();
            }
        }

        public Task<string> SendAsync(HttpSenderMethod method, string url, string content) {
            TaskCompletionSource<string> tcs = new();
            UnityWebRequest request = CreateRequest(method, url, content);

            request.SendWebRequest().completed += operation => {
                UnityWebRequest.Result result = request.result;

                if (result == UnityWebRequest.Result.Success) {
                    tcs.SetResult(request.downloadHandler.text);
                } else {
                    tcs.SetException(new UnityException("The " + nameof(UnityWebRequest) + " failed with result: " + result + ":\n\n" + request.error));
                }

                request.FullyDispose();
            };

            return tcs.Task;
        }
    }
}
