#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using LookingGlass.Toolkit;

namespace LookingGlass {
    /// <summary>
    /// <para>Contains extension methods for dealing with <see cref="GameView"/>s, which are an internal derived type of <see cref="EditorWindow"/>.</para>
    /// </summary>
    public static class GameViewExtensions {
        public static readonly Type GameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        public static readonly Type GUIViewType = typeof(Editor).Assembly.GetType("UnityEditor.GUIView");
        public static readonly Type GameViewSizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        public static readonly Type GameViewSizeGroupType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeGroup");
        public static readonly Type GameViewSizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");

        //internal enum values for UnityEditor.GameViewSizeType:
        private const int GameViewSizeType_AspectRatio = 0;
        private const int GameViewSizeType_FixedResolution = 1;

        private static BindingFlags bindingFlags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public;

        private static object gameViewSizesInstance;
        private static MethodInfo getGroup;

        //GameViewSizeGroup API Members:
        private static MethodInfo getDisplayTexts;
        private static MethodInfo addCustomSize;
        private static MethodInfo removeCustomSize;

        public static readonly Lazy<Regex> SizeDisplayTextPattern = new Lazy<Regex>(() =>
            new Regex("(?<label>[A-Za-z]+)\\(?(?<width>[0-9]+)x(?<height>[0-9]+)\\)?"));

        public static readonly Lazy<Regex> CleanupDisplayTextPattern = new Lazy<Regex>(() =>
            new Regex("(^[0-9]*x[0-9]*$)|((?i)TEST(?-i))|(Looking Glass)"));

        private static bool didCleanupThisSession = false;

        internal static Func<EditorWindow, bool> editorIsGameViewPaired = null;

        static GameViewExtensions() {
            getGroup = GameViewSizesType.GetMethod("GetGroup");

            getDisplayTexts = GameViewSizeGroupType.GetMethod("GetDisplayTexts");
            addCustomSize = GameViewSizeGroupType.GetMethod("AddCustomSize");
            removeCustomSize = GameViewSizeGroupType.GetMethod("RemoveCustomSize");
        }

        public static GameViewSizeGroupType CurrentGameViewSizeGroupType {
            get {
                Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(GameViewSizesType);
                PropertyInfo instanceProperty = singletonType.GetProperty("instance");
                gameViewSizesInstance = instanceProperty.GetValue(null, null);
                PropertyInfo currentGroupTypeProperty = gameViewSizesInstance.GetType().GetProperty("currentGroupType");
                return (GameViewSizeGroupType) (int) currentGroupTypeProperty.GetValue(gameViewSizesInstance, null);
            }
        }

        public static EditorWindow[] FindAllGameViews() => (EditorWindow[]) Resources.FindObjectsOfTypeAll(GameViewType);

        public static void SetGameViewTargetDisplay(this EditorWindow gameView, int targetDisplay) {
#if UNITY_2019_3_OR_NEWER
			GameViewType.GetMethod("set_targetDisplay", bindingFlags).Invoke(gameView, new object[] { targetDisplay });
#else
            FieldInfo targetDisplayField = GameViewType.GetField("m_TargetDisplay", bindingFlags);
            targetDisplayField.SetValue(gameView, targetDisplay);
#endif
        }

        public static int GetGameViewTargetDisplay(this EditorWindow gameView) {
#if UNITY_2019_3_OR_NEWER
            return (int) GameViewType.GetMethod("get_targetDisplay", bindingFlags).Invoke(gameView, new object[] { });
#else
            FieldInfo targetDisplayField = GameViewType.GetField("m_TargetDisplay", bindingFlags);
            return (int) targetDisplayField.GetValue(gameView);
#endif
        }

        public static void SetGameViewZoom(this EditorWindow gameView, float scale = 1) {
            FieldInfo zoomAreaField = GameViewType.GetField("m_ZoomArea", bindingFlags);
            object zoomArea = zoomAreaField.GetValue(gameView);
            FieldInfo scaleField = zoomArea.GetType().GetField("m_Scale", bindingFlags);
            scaleField.SetValue(zoomArea, new Vector2(scale, scale));
        }

        public static void SetMinGameViewZoom(this EditorWindow gameView) {
            FieldInfo zoomAreaField = GameViewType.GetField("m_ZoomArea", bindingFlags);
            object zoomArea = zoomAreaField.GetValue(gameView);
            Type zoomAreaType = zoomArea.GetType();
            PropertyInfo xScaleMinProperty = zoomAreaType.GetProperty("hScaleMin", bindingFlags);
            PropertyInfo yScaleMinProperty = zoomAreaType.GetProperty("vScaleMin", bindingFlags);

            FieldInfo scaleField = zoomArea.GetType().GetField("m_Scale", bindingFlags);
            scaleField.SetValue(zoomArea, new Vector2(
                (float) xScaleMinProperty.GetValue(zoomArea),
                (float) yScaleMinProperty.GetValue(zoomArea)
            ));
            gameView.Repaint();
        }

        public static void SetGameViewResolution(this EditorWindow gameView, int width, int height, string deviceTypeName) {
            GameViewSizeGroupType groupType = CurrentGameViewSizeGroupType;
            if (!FindSize(groupType, width, height, deviceTypeName, out int index))
                AddCustomSize(groupType, width, height, deviceTypeName);

            PropertyInfo selectedSizeIndexProp = GameViewType.GetProperty("selectedSizeIndex", bindingFlags);
            selectedSizeIndexProp.SetValue(gameView, index, null);

            EditorUpdates.ForceUnityRepaintImmediate();
            gameView.Repaint();
        }

        public static Vector2 GetTargetSize(this EditorWindow gameView) {
            PropertyInfo property = GameViewType.GetProperty("targetSize", bindingFlags);
            return (Vector2) property.GetValue(gameView);
        }

        public static Vector2 GetTargetRenderSize(this EditorWindow gameView) {
            PropertyInfo property = GameViewType.GetProperty("targetRenderSize", bindingFlags);
            return (Vector2) property.GetValue(gameView);
        }

        public static void SetFreeAspectSize(this EditorWindow gameView) {
            PropertyInfo property = GameViewType.GetProperty("selectedSizeIndex", bindingFlags);
            property.SetValue(gameView, 0);
        }

        public static int GetSelectedSizeIndex(this EditorWindow gameView) {
            PropertyInfo property = GameViewType.GetProperty("selectedSizeIndex", bindingFlags);
            return (int) property.GetValue(gameView);
        }

        public static void SetShowToolbar(this EditorWindow gameView, bool value) {
            PropertyInfo property = GameViewType.GetProperty("showToolbar", bindingFlags);

            //NOTE: The GameView.showToolbar API member doesn't exist in Unity 2018.4.
            //It DOES exist by 2019.4, but I haven't tested which versions exactly it's in, since it's just a nice-to-have feature to hide it.
            //So if it doesn't exist yet, don't worry about it -- just do nothing and return:
            if (property == null)
                return;
            
            property.SetValue(gameView, value);
        }

        //TODO: Is there really no way to maximize an EditorWindow through code?
        //NOTE: EditorWindow.maximized does NOT provide the functionality we want, it's a different feature that only works when the window is docked.
        /// <summary>
        /// Sends a mouse-down and mouse-up event near the top-right of the <paramref name="window"/>, in hopes of maximizing the window.
        /// Note that on non-Windows platforms, this method does nothing.
        /// This has been tested to work on Unity 2018.4 - 2021.2.
        /// </summary>
        /// <param name="window"></param>
        public static void AutoClickMaximizeButtonOnWindows(this EditorWindow window) {
#if UNITY_EDITOR_WIN
            Event maximizeMouseDown = new Event() {
                type = EventType.MouseDown,
                button = 0,
                mousePosition = new Vector2(0 + window.position.size.x - 25, 8),
            };

            Event maximizeMouseUp = new Event(maximizeMouseDown) {
                type = EventType.MouseUp
            };
            window.SendEvent(maximizeMouseDown);
            window.SendEvent(maximizeMouseUp);
            EditorApplication.QueuePlayerLoopUpdate();
            window.Repaint();
#endif
        }

        public static void SetFocus(this EditorWindow gameView) {
            MethodInfo method = GameViewType.GetMethod("SetFocus", bindingFlags | BindingFlags.InvokeMethod);
            method.Invoke(gameView, new object[] { true });
        }

        public static void RepaintImmediately(this EditorWindow window) {
            try {
                MethodInfo method = GameViewType.GetMethod("RepaintImmediately", bindingFlags | BindingFlags.InvokeMethod);
                method.Invoke(window, null);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public static void RepaintImmediately(this Editor editor) {
            try {
                MethodInfo method = typeof(Editor).GetMethod("RepaintImmediately", bindingFlags | BindingFlags.InvokeMethod);
                method.Invoke(editor, null);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public static void RepaintAllViewsImmediately() {
            try {
                MethodInfo method = GUIViewType.GetMethod("RepaintImmediately", bindingFlags | BindingFlags.InvokeMethod);
                foreach (object guiView in Resources.FindObjectsOfTypeAll(GUIViewType))
                    method.Invoke(guiView, null);
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public static object GetGroup(GameViewSizeGroupType groupType) {
            return getGroup.Invoke(gameViewSizesInstance, new object[] { (int) groupType });
        }

        public static void AddCustomSize(int width, int height, string label) => AddCustomSize(CurrentGameViewSizeGroupType, width, height, label);

        /// <summary>
        /// Adds a game view size.
        /// </summary>
        /// <param name="groupType">Build target</param>
        /// <param name="width">Width of game view resolution</param>
        /// <param name="height">Height of game view resolution</param>
        /// <param name="label">Label of game view resolution</param>
        public static void AddCustomSize(GameViewSizeGroupType groupType, int width, int height, string label) {
            CleanupCustomSizesIfNeeded();

            object[] gameViewSizeConstructorArgs = new object[] { GameViewSizeType_FixedResolution, width, height, label };

            // select a constructor which has 4 elements which are enums/ints/strings
            ConstructorInfo gameViewSizeConstructor = GameViewSizeType.GetConstructors()
                .FirstOrDefault(x => {
                    // lambda function defines a filter/predicate of ConstructorInfo objects.
                    // The first constructor, if any exists, which satisfies the predicate (true) will be returned
                    if (x.GetParameters().Length != gameViewSizeConstructorArgs.Length)
                        return false;
                    // iterate through constructor types + constructor args. If any mismatch, reject
                    for (int i = 0; i < gameViewSizeConstructorArgs.Length; i++) {
                        Type constructorParamType = x.GetParameters()[i].ParameterType;
                        Type constructorArgType = gameViewSizeConstructorArgs[i].GetType();
                        bool isMatch = constructorParamType == constructorArgType || constructorParamType.IsEnum && constructorArgType == typeof(int);
                        if (!isMatch)
                            return false;
                    }
                    // constructor with these params must be able to receive these args
                    return true;
                });

            if (gameViewSizeConstructor != null) {
                object group = GetGroup(groupType);
                object newSize = gameViewSizeConstructor.Invoke(gameViewSizeConstructorArgs);
                addCustomSize.Invoke(group, new object[] { newSize });
            }
            GameViewSizesType.GetMethod("SaveToHDD").Invoke(gameViewSizesInstance, null);
        }

        public static string GetDisplayText(int width, int height, string label) {
            if (string.IsNullOrEmpty(label))
                return width + "x" + height;
            return label + " (" + width + "x" + height + ")";
        }

        public static string[] GetSizeDisplayTexts(GameViewSizeGroupType groupType) {
            object group = GetGroup(groupType);
            return (string[]) getDisplayTexts.Invoke(group, null);
        }

        public static GameViewSizeInfo[] GetSizeInfos(GameViewSizeGroupType groupType) {
            return GetSizeDisplayTexts(groupType).Select(displayText => {
                Match m = SizeDisplayTextPattern.Value.Match(displayText);
                if (!m.Success)
                    return GameViewSizeInfo.Invalid;

                GroupCollection groups = m.Groups;
                try {
                    return new GameViewSizeInfo() {
                        width = int.Parse(groups["width"].Value),
                        height = int.Parse(groups["height"].Value),
                        label = groups["label"].Value,
                        displayText = displayText
                    };
                } catch (Exception e) {
                    Debug.LogException(e);
                    return GameViewSizeInfo.Invalid;
                }
            }).ToArray();
        }

        public static bool FindSize(int width, int height, string label, out int index) =>
            FindSize(CurrentGameViewSizeGroupType, width, height, label, out index);

        public static bool FindSize(GameViewSizeGroupType groupType, int width, int height, string label, out int index) =>
            FindSizeByDisplayText(groupType, GetDisplayText(width, height, label), out index);

        public static bool FindSizeByDisplayText(string displayText, out int index) =>
            FindSizeByDisplayText(CurrentGameViewSizeGroupType, displayText, out index);


        /// <summary>
        /// Retrieves index of a resolution in GetDisplayTexts collection, if it exists in the collection.
        /// </summary>
        /// <param name="groupType">Group to search: Standalone/Android</param>
        /// <param name="index">Index of match if a match was found, or first out-of-bounds index if no match was found</param>
        /// <returns>True if resolution in collection, false if resolution is not in collection</returns>
        public static bool FindSizeByDisplayText(GameViewSizeGroupType groupType, string displayText, out int index) {
            CleanupCustomSizesIfNeeded();

            string[] displayTexts = GetSizeDisplayTexts(groupType);

            for (int i = 0; i < displayTexts.Length; i++) {
                string other = displayTexts[i];
                if (other == displayText) {
                    index = i;
                    return true;
                }
            }

            // otherwise set to first index outside of array bounds, return false to warn user that size is not actually in array
            // inside of SetGameViewSize we will add the as-of-yet unknown size at index [first_index_outside_of_array_bounds]
            index = displayTexts.Length;
            return false;
        }

        public static void RemoveCustomSize(int index) => RemoveCustomSize(CurrentGameViewSizeGroupType, index);

        /// <summary>
        /// Removes a game view size.
        /// </summary>
        /// <param name="groupType">Build target</param>
        /// <param name="index">index of game view resolution</param>
        public static void RemoveCustomSize(GameViewSizeGroupType groupType, int index) {
            object group = GetGroup(groupType);
            removeCustomSize.Invoke(group, new object[] { index });
            GameViewSizesType.GetMethod("SaveToHDD").Invoke(gameViewSizesInstance, null);
        }

        public static void RemoveCustomSizes(Predicate<GameViewSizeInfo> match) => RemoveCustomSizes(CurrentGameViewSizeGroupType, match);
        public static void RemoveCustomSizes(GameViewSizeGroupType groupType, Predicate<GameViewSizeInfo> match) {
            GameViewSizeInfo[] infos = GetSizeInfos(groupType);
            for (int i = infos.Length - 1; i >= 0; i--) {
                if (match(infos[i]))
                    RemoveCustomSize(groupType, i);
            }
        }

        private static bool CleanupCustomSizesIfNeeded() {
            if (!didCleanupThisSession) {
                didCleanupThisSession = true;
                CleanCustomSizes();
                return true;
            }
            return false;
        }
        public static void CleanCustomSizes() => CleanCustomSizes(CurrentGameViewSizeGroupType);
        public static void CleanCustomSizes(GameViewSizeGroupType groupType) {
            RemoveCustomSizes(groupType, (GameViewSizeInfo info) => {
                return info.IsValid && CleanupDisplayTextPattern.Value.IsMatch(info.displayText);
            });
        }

        public static EditorWindow[] FindUnpairedGameViews() => FindUnpairedGameViewsInternal().ToArray();

        private static IEnumerable<EditorWindow> FindUnpairedGameViewsInternal() {
            foreach (EditorWindow gameView in FindAllGameViews())
                if (!editorIsGameViewPaired(gameView))
                    yield return gameView;
        }

        /// <summary>
        /// Updates non-Preview Game Views in the Unity editor to match the resolution of the main <see cref="HologramCamera"/>.
        /// </summary>
        /// <remarks>
        /// This is skipped (returns <c>false</c>) when any of the following are true:
        /// <list type="bullet">
        /// <item>The lkg-settings.json's <c>"skipUserGameViews"</c> field is set to <c>true</c>.</item>
        /// <item>The iOS build target is selected, and there are 0 connected Looking Glass displays.</item>
        /// </list>
        /// </remarks>
        /// <returns><c>true</c> on successfully updating all user game views, <c>false</c> otherwise or when skipped.</returns>
        public static bool UpdateUserGameViews() {
            HologramCamera main = HologramCamera.Instance;
            if (main == null)
                return false;

            if (LKGSettingsSystem.Settings.skipUserGameViews)
                return false;

#if UNITY_IOS
            if (LKGDisplaySystem.LKGDisplayCount <= 0)
                return false;
#endif

            Calibration cal = main.Calibration;
            Vector2Int resolution = new Vector2Int(cal.screenW, cal.screenH);
            string deviceNiceName = cal.GetDeviceType().GetNiceName();
            foreach (EditorWindow gameView in FindUnpairedGameViews()) {
                if (gameView.GetGameViewTargetDisplay() == (int) main.TargetDisplay)
                    gameView.SetGameViewResolution(resolution.x, resolution.y, deviceNiceName);
            }

            return true;
        }

    }
}
#endif
