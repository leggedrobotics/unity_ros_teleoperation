using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Bhaptics.SDK2.Editor
{
    public class BhapticsSettingWindow : EditorWindow
    {
        private enum NavigationButtonType
        {
            Home = 0, Events = 1, Documentation = 2
        }

        public enum DocumentationButtonType
        {
            Unity, bHaptics, MetaQuest2
        }

        private const string DeveloperPortalGuideUrl = "https://bhaptics.notion.site/Create-haptic-events-using-bHaptics-Developer-Portal-b056c5a56e514afeb0ed436873dd87c6";
        private const string UnityHowToStartUrl = "https://bhaptics.notion.site/Plug-in-deployed-events-to-Unity-33cc33dcfa44426899a3f21c62adf66d";
        private const string UnityMigrateUrl = "https://bhaptics.notion.site/How-to-migrate-from-SDK1-old-to-SDK2-new-007c00b65129404287d9175b71fa029c";

        private const string WindowTitle = "bHaptics Developer Window";
        private const float WindowWidth = 1000f;
        private const float WindowHeight = 650f;
        private const float WindowPositionX = 300f;
        private const float WindowPositionY = 150f;

        private const float EventsButtonWidth = 335f;
        private const float EventsButtonHeight = 86f;
        private const float EventsButtonDetailWidth = 680f;
        private const float EventsButtonDetailHeight = 72f;
        private const float EventsButtonSpacing = 10f;

        private const float DocumentationButtonWidth = 580f;
        private const float DocumentationButtonHeight = 70f;
        private const float DocumentationButtonSpacing = 16f;

        private static NavigationButtonType SelectedNavigationButtionType = NavigationButtonType.Home;
        private static Vector2 CurrentEventsScrollPos = Vector2.zero;
        private static bool IsSortLastUpdated = false;
        private static bool IsViewGrid = true;

        private SerializedProperty appIdProperty;
        private SerializedProperty apiKeyProperty;

        private BhapticsSettings settings;
        private SerializedObject so;

        #region Images
        private Texture2D whiteImage;
        private Texture2D latestDeployedVersionBox;
        private Texture2D homeIcon;
        private Texture2D hapticEventsIcon;
        private Texture2D documentationIcon;
        private Texture2D homeSelectedIcon;
        private Texture2D hapticEventsSelectedIcon;
        private Texture2D documentationSelectedIcon;
        private Texture2D sampleGuidesImage;
        private Texture2D appIdApiKeyOutlineImage;
        private Texture2D eventsButtonAudioIcon;
        private Texture2D unityDocumentationImage;
        private Texture2D bHapticsDocumentationImage;
        private Texture2D metaQuest2DocumentationImage;
        #endregion

        #region GUIStyle
        private GUIStyle fontBoldStyle;
        private GUIStyle fontMediumStyle;
        private GUIStyle fontRegularStyle;
        private GUIStyle fontLightStyle;
        private GUIStyle divideStyle;
        private GUIStyle navigationButtonStyle;
        private GUIStyle selectedNavigationButtonStyle;
        private GUIStyle tempAreaStyle;
        private GUIStyle setupInputFieldStyle;
        private GUIStyle setupButtonStyle;
        private GUIStyle visitTextButtonStyle;
        private GUIStyle visitButtonStyle;
        private GUIStyle resetButtonStyle;
        private GUIStyle hapticEventsButtonStyle;
        private GUIStyle setupErrorTextStyle;
        private GUIStyle appTitleTextStyle;
        private GUIStyle blackBackgroundStyle;
        private GUIStyle latestDeployedVersionTextStyle;
        private GUIStyle refreshDeployedVersionButtonStyle;
        private GUIStyle mainTapTitleStyle;
        private GUIStyle sampleGuideButtonStyle;
        private GUIStyle copyClipboardButtonStyle;
        private GUIStyle viewAllTextStyle;
        private GUIStyle eventsButtonDetailStyle;
        private GUIStyle eventsButtonDetailSelectedStyle;
        private GUIStyle eventsButtonStyle;
        private GUIStyle eventsButtonSelectedStyle;
        private GUIStyle eventsButtonTitleStyle;
        private GUIStyle eventsButtonDeviceStyle;
        private GUIStyle eventsButtonDurationStyle;
        private GUIStyle documentationButtonStyle;
        private GUIStyle lastUpdatedToggleStyle;
        private GUIStyle lastUpdatedToggleSelectedStyle;
        private GUIStyle aZToggleStyle;
        private GUIStyle aZToggleSelectedStyle;
        private GUIStyle viewGridStyle;
        private GUIStyle viewGridSelectedStyle;
        private GUIStyle viewListStyle;
        private GUIStyle viewListSelectedStyle;
        #endregion

        private GUISkin bHapticsSkin;

        private Action OnChangePage;
        private MappingMetaData selectedEvent = null;
        private MappingMetaData[] sortedEventData;
        private string setupErrorText = string.Empty;
        private bool isValidState = false;
        private double nextForceRepaint;






        [MenuItem("bHaptics/Developer Window &b")]
        public static void ShowWindow()
        {
            // we need internet access
            if (!PlayerSettings.Android.forceInternetPermission)
            {
                PlayerSettings.Android.forceInternetPermission = true;
                BhapticsLogManager.Log("[bHaptics] Internet Access is set to Require");
            }

            var windowSize = new Vector2(WindowWidth, WindowHeight);
            var bHapticsWindow = EditorWindow.GetWindowWithRect(typeof(BhapticsSettingWindow), new Rect(new Vector2(WindowPositionX, WindowPositionY), windowSize), true, WindowTitle);
            bHapticsWindow.position = new Rect(new Vector2(WindowPositionX, WindowPositionY), windowSize);
            bHapticsWindow.minSize = windowSize;
            bHapticsWindow.Show();
        }

        private void OnEnable()
        {
            settings = BhapticsSettings.Instance;
            so = new SerializedObject(settings);
            wantsMouseMove = true;

            so.Update();

            appIdProperty = so.FindProperty("appId");
            apiKeyProperty = so.FindProperty("apiKey");

            InitializeImages();

            bHapticsSkin = Resources.Load<GUISkin>(BhapticsSettingWindowAssets.GUISkin);

            InitializeGUIStyles(bHapticsSkin);

            isValidState = settings.LastDeployVersion > 0;

            OnChangePage += ResetOnChangePage;

            BhapticsLibrary.Initialize(settings.AppId, settings.ApiKey, settings.DefaultDeploy);
            BhapticsLibrary.IsBhapticsAvailable(true);
        }

        private void OnDestroy()
        {
            BhapticsLibrary.Destroy();
        }

        private void OnGUI()
        {
            GUI.DrawTexture(new Rect(0f, 0f, WindowWidth, WindowHeight), whiteImage);

            if (!isValidState)
            {
                DrawSetupPage();

                ResetMainPage();
            }
            else
            {
                DrawAppTitle();

                DrawNavigationTap();

                DrawMainPage();
            }

            so.ApplyModifiedProperties();
            DelayRepaint();
        }

        #region Initialize Method
        private void InitializeImages()
        {
            whiteImage = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.WhiteImage, typeof(Texture2D));
            latestDeployedVersionBox = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.LatestDeployedVersionBox, typeof(Texture2D));
            homeIcon = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.HomeIcon, typeof(Texture2D));
            hapticEventsIcon = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.EventsIcon, typeof(Texture2D));
            documentationIcon = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.DocumentationIcon, typeof(Texture2D));
            homeSelectedIcon = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.HomeSelectedIcon, typeof(Texture2D));
            hapticEventsSelectedIcon = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.EventsSelectedIcon, typeof(Texture2D));
            documentationSelectedIcon = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.DocumentationSelectedIcon, typeof(Texture2D));
            sampleGuidesImage = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.SampleGuidesImage, typeof(Texture2D));
            appIdApiKeyOutlineImage = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.AppIdApiKeyOutlineImage, typeof(Texture2D));
            eventsButtonAudioIcon = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.EventsButtonAudioIcon, typeof(Texture2D));
            unityDocumentationImage = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.UnityDocumentationImage, typeof(Texture2D));
            bHapticsDocumentationImage = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.BhapticsDocumentationImage, typeof(Texture2D));
            metaQuest2DocumentationImage = (Texture2D)Resources.Load(BhapticsSettingWindowAssets.MetaQuest2DocumentationImage, typeof(Texture2D));
        }

        private void InitializeGUIStyles(GUISkin targetSkin)
        {
            tempAreaStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.TempAreaStyle));

            fontBoldStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.FontBold));
            fontMediumStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.FontMedium));
            fontRegularStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.FontRegular));
            fontLightStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.FontLight));
            divideStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.DivideStyle));
            navigationButtonStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.NavigationButtonStyle));
            selectedNavigationButtonStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.SelectedNavigationButtonStyle));
            setupInputFieldStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.SetupInputFieldStyle));
            setupButtonStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.SetupButtonStyle));
            visitTextButtonStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.VisitTextButtonStyle));
            visitButtonStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.VisitButtonStyle));
            resetButtonStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.ResetButtonStyle));
            hapticEventsButtonStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.HapticEventsButtonStyle));
            setupErrorTextStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.SetupErrorTextStyle));
            appTitleTextStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.AppTitleTextStyle));
            blackBackgroundStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.BlackBackgroundStyle));
            latestDeployedVersionTextStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.LatestDeployedVersionTextStyle));
            refreshDeployedVersionButtonStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.RefreshDeployedVersionButtonStyle));
            mainTapTitleStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.MainTapTitleStyle));
            sampleGuideButtonStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.SampleGuideButtonStyle));
            copyClipboardButtonStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.CopyClipboardButtonStyle));
            viewAllTextStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.ViewAllTextStyle));
            eventsButtonDetailStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.EventsButtonDetailStyle));
            eventsButtonDetailSelectedStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.EventsButtonDetailSelectedStyle));
            eventsButtonStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.EventsButtonStyle));
            eventsButtonSelectedStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.EventsButtonSelectedStyle));
            eventsButtonTitleStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.EventsButtonTitleStyle));
            eventsButtonDeviceStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.EventsButtonDeviceStyle));
            eventsButtonDurationStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.EventsButtonDurationStyle));
            documentationButtonStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.DocumentationButtonStyle));
            lastUpdatedToggleStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.LastUpdatedToggleStyle));
            lastUpdatedToggleSelectedStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.LastUpdatedToggleSelectedStyle));
            aZToggleStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.AZToggleStyle));
            aZToggleSelectedStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.AZToggleSelectedStyle));
            viewGridStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.ViewGridStyle));
            viewGridSelectedStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.ViewGridSelectedStyle));
            viewListStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.ViewListStyle));
            viewListSelectedStyle = new GUIStyle(GetCustomGUIStyle(targetSkin, BhapticsSettingWindowAssets.ViewListSelectedStyle));
        }
        #endregion

        private void DrawSetupPage()
        {
            GUILayout.BeginArea(new Rect(295f, 122f, 410f, 402f));

            var originCursorFlashSpeed = GUI.skin.settings.cursorFlashSpeed;
            GUI.skin.settings.cursorFlashSpeed = bHapticsSkin.settings.cursorFlashSpeed;

            GUILayout.Label("Setup", new GUIStyle(fontBoldStyle) { fontSize = 24 }, GUILayout.Height(36f));
            GUILayout.Space(40f);

            GUILayout.Label("App ID", new GUIStyle(fontBoldStyle) { fontSize = 14 });
            GUILayout.Space(14f);
            appIdProperty.stringValue = EditorGUILayout.TextField(appIdProperty.stringValue, setupInputFieldStyle, GUILayout.Height(setupInputFieldStyle.fixedHeight));
            if (appIdProperty.stringValue.Equals(string.Empty))
            {
                GUI.Label(new Rect(14f, 124f, 111f, 18f), "<color=#cccccc>Enter your App ID</color>", new GUIStyle(fontRegularStyle) { fontSize = 14 });
            }

            GUILayout.Space(14f);

            GUILayout.Label("API Key", new GUIStyle(fontBoldStyle) { fontSize = 14 });
            GUILayout.Space(14f);
            apiKeyProperty.stringValue = EditorGUILayout.TextField(apiKeyProperty.stringValue, setupInputFieldStyle, GUILayout.Height(setupInputFieldStyle.fixedHeight));
            if (apiKeyProperty.stringValue.Equals(string.Empty))
            {
                GUI.Label(new Rect(14f, 220f, 111f, 18f), "<color=#cccccc>Enter your API Key</color>", new GUIStyle(fontRegularStyle) { fontSize = 14 });
            }

            if (!setupErrorText.Equals(string.Empty))
            {
                GUILayout.Space(10f);
                GUILayout.Label(setupErrorText, setupErrorTextStyle);
                GUILayout.Space(22f);
            }
            else
            {
                GUILayout.Space(50f);
            }

            if (GUILayout.Button("LINK", setupButtonStyle))
            {
                GetAppSettings(out setupErrorText);
            }

            GUILayout.Space(20f);
            GUILayout.BeginHorizontal();
            GUILayout.Space(138f - 12f);

            if (GUILayout.Button("Forgot your App ID & API Key?", visitTextButtonStyle, GUILayout.Width(138f + 12f), GUILayout.Height(18f)))
            {
                Application.OpenURL("https://developer.bhaptics.com");
            }

            GUILayout.EndHorizontal();

            GUI.skin.settings.cursorFlashSpeed = originCursorFlashSpeed;

            GUILayout.EndArea();
        }

        private void DrawAppTitle()
        {
            GUILayout.BeginArea(new Rect(0f, 0f, WindowWidth, 54f), blackBackgroundStyle);

            GUILayout.Space(15f);
            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);
            GUILayout.Label(settings.AppName, appTitleTextStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("  ", visitButtonStyle))
            {
                Application.OpenURL("https://developer.bhaptics.com");
            }
            GUILayout.Space(20f);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawNavigationTap()
        {
            GUILayout.BeginArea(new Rect(10f, 74f, 230f, 455f));
            GUILayout.BeginVertical();

            if (DrawNavigationButton("Home", NavigationButtonType.Home))
            {
                SelectNavigationButton(NavigationButtonType.Home);
            }

            if (DrawNavigationButton("Events", NavigationButtonType.Events))
            {
                sortedEventData = GetSortedEventData(0);
                SelectNavigationButton(NavigationButtonType.Events);
            }

            if (DrawNavigationButton("Documentation", NavigationButtonType.Documentation))
            {
                SelectNavigationButton(NavigationButtonType.Documentation);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(20f, 529f, 220f, 120f));
            GUILayout.BeginVertical();

            GUILayout.Label("", divideStyle);
            GUILayout.Space(10f);

            GUI.DrawTexture(new Rect(0f, 10f, 220f, 42f), latestDeployedVersionBox);

            GUILayout.BeginHorizontal();
            GUILayout.Space(159f);
            GUILayout.Label(settings.LastDeployVersion.ToString(), latestDeployedVersionTextStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("", "Refresh"), refreshDeployedVersionButtonStyle))
            {
                if (GetAppSettings(out setupErrorText) != 0)
                {
                    ResetAppSettings();
                }
                else
                {
                    BhapticsLibrary.EditorReInitialize(settings.AppId, settings.ApiKey, settings.DefaultDeploy);
                }
            }

            GUILayout.Space(4f);

            GUILayout.EndHorizontal();

            GUILayout.Space(22f);

            if (GUILayout.Button("", resetButtonStyle))
            {
                ResetAppSettings();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private GUIStyle GetCustomGUIStyle(GUISkin targetSkin, string styleName)
        {
            for (int i = 0; i < targetSkin.customStyles.Length; ++i)
            {
                if (targetSkin.customStyles[i].name.Equals(styleName))
                {
                    return targetSkin.customStyles[i];
                }
            }

            return null;
        }

        private void DelayRepaint()
        {
            var timeSinceStartup = Time.realtimeSinceStartup;
            if (Event.current.type == EventType.MouseMove && timeSinceStartup > nextForceRepaint)
            {
                nextForceRepaint = timeSinceStartup + .05f;
                Repaint();
            }
        }

        private void ResetMainPage()
        {
            SelectedNavigationButtionType = NavigationButtonType.Home;
            CurrentEventsScrollPos = Vector2.zero;
            IsViewGrid = true;
            IsSortLastUpdated = false;
            sortedEventData = null;
        }

        private bool DrawNavigationButton(string title, NavigationButtonType buttonType)
        {
            bool isSelected = SelectedNavigationButtionType == buttonType;
            Texture2D targetIcon = homeIcon;
            GUIStyle targetStyle = isSelected ? selectedNavigationButtonStyle : navigationButtonStyle;

            switch (buttonType)
            {
                case NavigationButtonType.Home:
                    targetIcon = isSelected ? homeSelectedIcon : homeIcon;
                    break;
                case NavigationButtonType.Events:
                    targetIcon = isSelected ? hapticEventsSelectedIcon : hapticEventsIcon;
                    break;
                case NavigationButtonType.Documentation:
                    targetIcon = isSelected ? documentationSelectedIcon : documentationIcon;
                    break;
            }

            return GUILayout.Button(new GUIContent("     " + title, targetIcon), targetStyle);
        }

        private void SelectNavigationButton(NavigationButtonType targetType)
        {
            if (!SelectedNavigationButtionType.Equals(targetType))
            {
                OnChangePage?.Invoke();
            }

            SelectedNavigationButtionType = targetType;
        }

        private void DrawMainPage()
        {
            GUILayout.BeginArea(new Rect(280f, 90f, 720f, 560f));

            switch (SelectedNavigationButtionType)
            {
                case NavigationButtonType.Home:
                    ShowHomeTap();
                    break;
                case NavigationButtonType.Events:
                    ShowHapticEventsTap();
                    break;
                case NavigationButtonType.Documentation:
                    ShowDocumentationTap();
                    break;
            }

            GUILayout.EndArea();
        }

        private void ShowHomeTap()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Welcome to bHaptics SDK2 !", mainTapTitleStyle);

            GUILayout.BeginArea(new Rect(0f, 40f, 350f, 176f));
            GUI.DrawTexture(new Rect(0f, 0f, 350f, 176f), sampleGuidesImage);

            GUILayout.Label("How to start",
                new GUIStyle(fontBoldStyle)
                {
                    fontSize = 16,
                    padding = new RectOffset(20, 0, 20, 0)
                });

            GUILayout.Label("<color=#646464>It is recommended that you view the\nGuide documentation before you begin.</color>",
                new GUIStyle(fontRegularStyle)
                {
                    fontSize = 14,
                    wordWrap = true,
                    padding = new RectOffset(20, 0, 10, 0)
                });
            GUILayout.Space(24f);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Developer Portal Guide", sampleGuideButtonStyle, GUILayout.Width(160f)))
            {
                Application.OpenURL(DeveloperPortalGuideUrl);
            }

            GUILayout.Space(10f);

            if (GUILayout.Button("Unity SDK2 Guide", sampleGuideButtonStyle, GUILayout.Width(140f)))
            {
                Application.OpenURL(UnityHowToStartUrl);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            DrawAppIdApiKeyButton(new Rect(360f, 40f, 320f, 176f), "App ID", settings.AppId);
            GUILayout.Space(10f);
            DrawAppIdApiKeyButton(new Rect(360f, 133f, 320f, 176f), "API Key", settings.ApiKey);

            GUILayout.BeginArea(new Rect(0f, 250f, 680f, 24f));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Events", mainTapTitleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("View All", viewAllTextStyle))
            {
                SelectNavigationButton(NavigationButtonType.Events);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(0f, 284f, 680f, 236f));

            if (settings.EventData != null)
            {
                int startIndex = 0;
                int endIndex = settings.EventData.Length > 3 ? startIndex + 3 : settings.EventData.Length;

                for (int i = startIndex; i < endIndex; ++i)
                {
                    DrawEventsButtonDetail(i, settings.EventData[i]);
                    if (i != endIndex - 1)
                    {
                        GUILayout.Space(10f);
                    }
                }
            }

            GUILayout.EndArea();

        }

        private void ShowHapticEventsTap()
        {
            if (settings.EventData == null)
            {
                return;
            }

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Events", new GUIStyle(mainTapTitleStyle) { fontSize = 22, });
            GUILayout.Space(6f);
            GUILayout.Label("<color=#959595>" + settings.EventData.Length.ToString() + "</color>",
                new GUIStyle(fontMediumStyle)
                {
                    fontSize = 16,
                    margin = new RectOffset(0, 0, 3, 0)
                });
            GUILayout.FlexibleSpace();
            GUILayout.Label("<color=#646464>Sortby:</color>",
                new GUIStyle(fontRegularStyle)
                {
                    fontSize = 13,
                    margin = new RectOffset(0, 0, 5, 0)
                });
            GUILayout.Space(10f);
            if (GUILayout.Button("", IsSortLastUpdated ? aZToggleStyle : aZToggleSelectedStyle))
            {
                IsSortLastUpdated = false;
                sortedEventData = GetSortedEventData(1);
            }
            GUI.Label(new Rect(452f, 9f, 79f, 16f), "A-Z", new GUIStyle(fontRegularStyle) { fontSize = 13 });
            GUILayout.Space(10f);
            if (GUILayout.Button("", IsSortLastUpdated ? lastUpdatedToggleSelectedStyle : lastUpdatedToggleStyle))
            {
                IsSortLastUpdated = true;
                sortedEventData = GetSortedEventData(0);
            }
            GUI.Label(new Rect(509f, 9f, 79f, 16f), "Last Updated", new GUIStyle(fontRegularStyle) { fontSize = 13 });
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(20f);
            if (GUILayout.Button("", IsViewGrid ? viewGridSelectedStyle : viewGridStyle))
            {
                IsViewGrid = true;
            }
            GUILayout.Space(4f);
            if (GUILayout.Button("", IsViewGrid ? viewListStyle : viewListSelectedStyle))
            {
                IsViewGrid = false;
            }

            GUILayout.Space(40f);
            GUILayout.EndHorizontal();

            GUILayout.Space(IsViewGrid ? 16f : 26f);

            var originVerticalScrollbar = GUI.skin.verticalScrollbar;
            var originVerticalScrollbarUpButton = GUI.skin.verticalScrollbarUpButton;
            var originVerticalScrollbarDownButton = GUI.skin.verticalScrollbarDownButton;
            var originVerticalScrollbarThumb = GUI.skin.verticalScrollbarThumb;

            GUI.skin.verticalScrollbar = bHapticsSkin.verticalScrollbar;
            GUI.skin.verticalScrollbarUpButton = bHapticsSkin.verticalScrollbarUpButton;
            GUI.skin.verticalScrollbarDownButton = bHapticsSkin.verticalScrollbarDownButton;
            GUI.skin.verticalScrollbarThumb = bHapticsSkin.verticalScrollbarThumb;

            try
            {
                CurrentEventsScrollPos = GUILayout.BeginScrollView(CurrentEventsScrollPos, GUILayout.Width(695f));

                var targetEventNames = sortedEventData;
                for (int i = 0; i < targetEventNames.Length; ++i)
                {
                    if (IsViewGrid)
                    {
                        DrawEventsButton(i, targetEventNames[i]);
                    }
                    else
                    {
                        DrawEventsButtonDetail(i, targetEventNames[i]);
                    }

                    if (i != targetEventNames.Length - 1)
                    {
                        GUILayout.Space(10f);
                    }
                }

                GUILayout.Space(10f);
                GUILayout.EndScrollView();
            }
            catch
            {
                sortedEventData = GetSortedEventData(0);
                GUILayout.EndScrollView();
            }

            GUI.skin.verticalScrollbar = originVerticalScrollbar;
            GUI.skin.verticalScrollbarUpButton = originVerticalScrollbarUpButton;
            GUI.skin.verticalScrollbarDownButton = originVerticalScrollbarDownButton;
            GUI.skin.verticalScrollbarThumb = originVerticalScrollbarThumb;
        }


        private void ShowDocumentationTap()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Documentation", new GUIStyle(mainTapTitleStyle) { fontSize = 22 });

            GUILayout.BeginArea(new Rect(0f, 60f, 720f, 570f));
            DrawDocumentationButton(GetDocumentationButtonPos(0), DocumentationButtonType.bHaptics, "Create haptic events", "(bHaptics Developer Portal)", DeveloperPortalGuideUrl);
            DrawDocumentationButton(GetDocumentationButtonPos(1), DocumentationButtonType.Unity, "Plug in deployed events to Unity", "(Unity)", UnityHowToStartUrl);
            DrawDocumentationButton(GetDocumentationButtonPos(2), DocumentationButtonType.Unity, "How to migrate from SDK1(old) to SDK2(new)", "(Unity)", UnityMigrateUrl);
            //DrawDocumentationButton(GetDocumentationButtonPos(3), DocumentationButtonType.MetaQuest2, "Getting Started", "(Unity Meta Quest2)");
            GUILayout.EndArea();
        }

        private void ResetAppSettings()
        {
            BhapticsSettings.ResetInstance();
            isValidState = false;
        }

        private int GetAppSettings(out string errorMessage)
        {
            var json = BhapticsLibrary.EditorGetSettings(appIdProperty.stringValue, apiKeyProperty.stringValue, -1, out int code);
            if (code == 0)
            {
                var events = BhapticsLibrary.EditorGetEventList(appIdProperty.stringValue, apiKeyProperty.stringValue, -1, out code);
                if (code == 0)
                {
                    try
                    {
                        var rawMessage = DeployHttpMessage.CreateFromJSON(json);
                        var message = rawMessage.message;
                        if (message.version > 0)
                        {
                            settings.AppName = message.name;
                            settings.AppId = appIdProperty.stringValue;
                            settings.ApiKey = apiKeyProperty.stringValue;
                            settings.LastDeployVersion = message.version;
                            settings.DefaultDeploy = json;

                            var eventNames = new string[events.Count];
                            var eventDataArr = new MappingMetaData[events.Count];
                            for (var i = 0; i < events.Count; i++)
                            {
                                eventNames[i] = events[i].key;
                                eventDataArr[i] = events[i];
                            }
                            settings.EventData = eventDataArr;

                            errorMessage = string.Empty;
                            isValidState = settings.LastDeployVersion > 0;

                            BhapticsEventGenerator.CreateEventCsFile("BhapticsEvent", eventNames);
                            EditorUtility.SetDirty(settings);
                            AssetDatabase.SaveAssets();
                        }
                        else
                        {
                            BhapticsLogManager.LogErrorFormat("[bHaptics] Not Valid format.");
                            errorMessage = "Not Valid format";
                        }
                    }
                    catch (System.Exception e)
                    {
                        BhapticsLogManager.LogErrorFormat("[bHaptics] Exception: {0}", e.Message);
                        errorMessage = "Exception: " + e.Message;
                    }

                    return code;
                }
            }

            BhapticsLogManager.LogErrorFormat("[bHaptics] Error: {0}", BhapticsHelpers.ErrorCodeToMessage(code));
            errorMessage = BhapticsHelpers.ErrorCodeToMessage(code);
            return code;
        }

        private void DrawAppIdApiKeyButton(Rect areaRect, string title, string value)
        {
            GUILayout.BeginArea(areaRect);

            GUI.DrawTexture(new Rect(0f, 0f, 320f, 83f), appIdApiKeyOutlineImage);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20f);

            GUILayout.BeginVertical();
            GUILayout.Space(22f);
            GUILayout.Label(title, new GUIStyle(fontBoldStyle) { fontSize = 14 });
            GUILayout.Space(6f);

            var valueStyle = new GUIStyle(fontLightStyle);
            valueStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            valueStyle.fontSize = 13;
            GUILayout.Label(value, valueStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            if (GUILayout.Button("", copyClipboardButtonStyle))
            {
                GUIUtility.systemCopyBuffer = value;
                BhapticsLogManager.LogFormat("[bHaptics] Copy to Clipboard: {0}", value);
            }
            GUILayout.EndVertical();

            GUILayout.Space(20f);
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawEventsButtonDetail(int index, MappingMetaData eventData)
        {
            if (GUILayout.Button("", eventData.Equals(selectedEvent) ? eventsButtonDetailSelectedStyle : eventsButtonDetailStyle))
            {
                selectedEvent = eventData;

                BhapticsLibrary.StopAll();
                BhapticsLibrary.Play(eventData.key);
            }

            float additionalHeight = index * EventsButtonDetailHeight + index * EventsButtonSpacing;

            GUI.Label(new Rect(64f, additionalHeight + 16f, 480f, 18f), eventData.key, eventsButtonTitleStyle);

            string deviceString = ConvertOrderToDeviceType(eventData.positions);
            GUI.Label(new Rect(64f, additionalHeight + 44f, 500f, 12f), deviceString.TrimEnd(), eventsButtonDeviceStyle);

            if (eventData.isAudio)
            {
                GUI.Label(new Rect(569f, additionalHeight + 28f, 40f * 1.2f, 16f * 1.2f), eventsButtonAudioIcon);
            }

            GUI.Label(new Rect(629f, additionalHeight + 29f, 32f, 14f), (eventData.durationMillis * 0.001f).ToString("0.00") + " s", eventsButtonDurationStyle);
        }

        private void DrawEventsButton(int index, MappingMetaData eventData)
        {
            int mtp1 = index % 2;
            int mtp2 = index / 2;

            if (mtp1 == 0)
            {
                GUILayout.BeginHorizontal();
            }

            if (GUILayout.Button("", eventData.Equals(selectedEvent) ? eventsButtonSelectedStyle : eventsButtonStyle))
            {
                selectedEvent = eventData;

                BhapticsLibrary.StopAll();
                BhapticsLibrary.Play(eventData.key);
            }

            float additionalWidth = mtp1 * EventsButtonWidth + mtp1 * EventsButtonSpacing;
            float additionalHeight = mtp2 * EventsButtonHeight + mtp2 * EventsButtonSpacing;

            GUI.Label(new Rect(additionalWidth + 16f, additionalHeight + 12f, 256f, 36f), eventData.key, eventsButtonTitleStyle);

            string deviceString = ConvertOrderToDeviceType(eventData.positions);
            GUI.Label(new Rect(additionalWidth + 16f, additionalHeight + 54f, 310f, 26f), deviceString.TrimEnd(), eventsButtonDeviceStyle);

            if (mtp1 == 1 || index == settings.EventData.Length - 1)
            {
                GUILayout.EndHorizontal();
            }
        }

        private Vector2 GetDocumentationButtonPos(int index)
        {
            return new Vector2(0f, index * DocumentationButtonHeight + index * DocumentationButtonSpacing);
        }

        private Texture2D GetDocumentationImage(DocumentationButtonType buttonType)
        {
            switch (buttonType)
            {
                case DocumentationButtonType.Unity:
                    return unityDocumentationImage;

                case DocumentationButtonType.bHaptics:
                    return bHapticsDocumentationImage;

                case DocumentationButtonType.MetaQuest2:
                    return metaQuest2DocumentationImage;
            }

            return unityDocumentationImage;
        }

        private void DrawDocumentationButton(Vector2 pos, DocumentationButtonType buttonType, string mainText, string subText, string url)
        {
            GUILayout.BeginArea(new Rect(pos.x, pos.y, DocumentationButtonWidth, DocumentationButtonHeight));

            var imageStyle = new GUIStyle();
            imageStyle.normal.background = GetDocumentationImage(buttonType);
            GUI.Label(new Rect(0f, 0f, 130f, 70f), "", imageStyle);

            GUI.Label(new Rect(144f, 26f, 436f, 18f), mainText + " " + "<color=#959595>" + subText + "</color>", new GUIStyle(fontRegularStyle) { fontSize = 14 });

            if (GUILayout.Button("", documentationButtonStyle))
            {
                if (url == string.Empty)
                {
                    BhapticsLogManager.Log("[bHaptics] To be continue...");
                }
                else
                {
                    Application.OpenURL(url);
                }
            }

            GUILayout.EndArea();
        }

        private MappingMetaData[] GetSortedEventData(int sortType)
        {
            if (settings.EventData == null)
            {
                return new MappingMetaData[0];
            }

            MappingMetaData[] res = null;

            var tempEventList = new List<MappingMetaData>();
            tempEventList.AddRange(settings.EventData);
            if (sortType == 0)
            {
                tempEventList.Sort(SortByUpdateTime);
                tempEventList.Reverse();
            }
            else
            {
                tempEventList.Sort(SortByName);
            }

            res = tempEventList.ToArray();

            return res;
        }

        private void ResetOnChangePage()
        {
            selectedEvent = null;
        }

        private int SortByUpdateTime(MappingMetaData x, MappingMetaData y)
        {
            return x.updateTime.CompareTo(y.updateTime);
        }

        private int SortByName(MappingMetaData x, MappingMetaData y)
        {
            return x.key.CompareTo(y.key);
        }

        private string ConvertOrderToDeviceType(string[] deviceArr)
        {
            string res = "";
            string interval = "      ";
            string[] deviceListStr = new string[] { "Face", "Vest", "Arms", "Hands", "Feet", "Glove" };
            bool[] useDeviceArr = new bool[deviceListStr.Length];

            for (int i = 0; i < deviceArr.Length; ++i)
            {
                for (int di = 0; di < deviceListStr.Length; ++di)
                {
                    if (deviceArr[i].Contains(deviceListStr[di]))
                    {
                        useDeviceArr[di] = true;
                    }
                }
            }

            for (int i = 0; i < useDeviceArr.Length; ++i)
            {
                if (useDeviceArr[i])
                {
                    res += deviceListStr[i] + interval;
                }
            }

            return res.Trim();
        }
    }
}
