//NOTE: This is in the runtime assembly, because some code in there needs to run delayed code when running in the UnityEditor

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace LookingGlass {
    /// <summary>
    /// A helper class for waiting a given number of Unity editor updates.
    /// </summary>
    public static class EditorUpdates {
        private abstract class WaitingAction {
            public Action callback;

            public abstract bool CheckUpdate();
        }

        private class FrameWaitingAction : WaitingAction {
            public int framesRemaining;

            public override bool CheckUpdate() {
                framesRemaining--;
                return framesRemaining <= 0;
            }
        }

        private class ConditionalWaitingAction : WaitingAction {
            public Func<bool> condition;

            public override bool CheckUpdate() {
                return condition();
            }
        }

        /// <summary>
        /// A collection of actions that are waiting to be called after varying numbers of frames to pass in the Unity editor.
        /// </summary>
        private static List<WaitingAction> waiting;

        static EditorUpdates() {
            EditorApplication.update -= CheckUpdate;
            EditorApplication.update += CheckUpdate;
            AssemblyReloadEvents.beforeAssemblyReload += () => {
                EditorApplication.update -= CheckUpdate;
            };
        }

        public static void DelayUntil(Task task, Action callback) {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            DelayUntil(() => task.IsCompleted, callback);
        }
        public static void DelayUntil(Func<bool> condition, Action callback) {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            Delay(new ConditionalWaitingAction() {
                condition = condition,
                callback = callback
            });
        }
        public static void Delay(int frames, Action callback) {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            Delay(new FrameWaitingAction() {
                callback = callback,
                framesRemaining = frames
            });
        }
        private static void Delay(WaitingAction action) {
            if (waiting == null)
                waiting = new List<WaitingAction>();
            waiting.Add(action);
        }

        private static void CheckUpdate() {
            if (waiting == null || waiting.Count <= 0)
                return;
            for (int i = waiting.Count - 1; i >= 0; i--) {
                WaitingAction w = waiting[i];

                if (w.CheckUpdate()) {
                    try {
                        w.callback();
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                    waiting.RemoveAt(i);
                }
            }
        }

        public static void ForceUnityRepaintImmediate() {
            // Spawn an object, then immediately destroy it.
            // This forces Unity to repaint scene, but does not generate a diff in the Unity scene serialization which would require scene to be re-saved
            // Repainting the scene causes Unity to recalculate UI positions for resized GameViewWindow : EditorWindow
            GameObject go = new GameObject();
            GameObject.DestroyImmediate(go);
        }

        public static void ForceUnityRepaint() {
            EditorUpdates.Delay(5, () => {
                ForceUnityRepaintImmediate();
            });
        }
    }
}
#endif
