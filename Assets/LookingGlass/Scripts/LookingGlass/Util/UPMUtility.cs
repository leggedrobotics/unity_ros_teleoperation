//NOTE: This class is in the runtime LookingGlass assembly,
//because we have a URPPackageDetector MonoBehaviour that can't
//be defined in an editor-only assembly.
#if UNITY_EDITOR
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System;

namespace LookingGlass {
    /// <summary>
    /// Contains helper methods for the Unity Package Manager (UPM).
    /// </summary>
    public static class UPMUtility {
        private class UnityWaitTask {
            private Func<bool> condition;
            private Func<object> result;
            private TaskCompletionSource<object> tcs;

            public bool IsCompleted => tcs.Task.IsCompleted;

            public static async Task<T> StartNew<T>(Request<T> upmRequest) {
                if (upmRequest == null)
                    throw new ArgumentNullException(nameof(upmRequest));
                return await StartNew(() => {
                    if (upmRequest.Status == StatusCode.Failure) {
                        Error e = upmRequest.Error;
                        throw new AggregateException("A UPM error occurred! (Error code: " + e.errorCode + ")\n" + e.message);
                    }
                    return upmRequest.IsCompleted;
                }, () => upmRequest.Result);
            }

            public static async Task<T> StartNew<T>(Func<bool> condition, Func<T> result) {
                if (condition == null)
                    throw new ArgumentNullException(nameof(condition));
                if (result == null)
                    throw new ArgumentNullException(nameof(result));

                UnityWaitTask wait = new UnityWaitTask() {
                    condition = condition,
                    result = () => result(),
                    tcs = new TaskCompletionSource<object>()
                };
                EditorApplication.update += wait.CheckUpdate;
                return (T) await wait.tcs.Task;
            }

            private void CheckUpdate() {
                bool isDone = false;
                try {
                    isDone = condition();
                } catch (Exception e) {
                    EditorApplication.update -= CheckUpdate;
                    tcs.SetException(e);
                    Debug.LogException(e);
                    return;
                }

                if (isDone) {
                    EditorApplication.update -= CheckUpdate;
                    tcs.SetResult(result());
                }
            }
        }

        public static Task<T> AwaitRequest<T>(Request<T> upmRequest) => UnityWaitTask.StartNew<T>(upmRequest);
    }
}
#endif
