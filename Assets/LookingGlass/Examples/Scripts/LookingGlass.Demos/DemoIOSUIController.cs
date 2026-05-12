using System;
using System.IO;
using UnityEngine;
using LookingGlass.Mobile;
using UnityEngine.UI;

namespace LookingGlass.Demos {
    public class DemoIOSUIController : MonoBehaviour {

        public static event Action<bool> TogglePairPopUp;
        public static event Action<bool> ToggleTestPopUp;

        [Tooltip("Reference to the template scene GameObject that is activated when the setup is complete. Replace with yours if needed")]
        public GameObject mainScene;
        [Tooltip("Reference to the main UI Canvas GameObject that is activated when the setup is complete. Replace with yours if needed")]
        public Canvas mainCanvas;
        [Tooltip("Array of UI pages (GameObjects) that represent different stages of the setup process (e.g., Get Started, Connect, Load Calibration)")]
        [SerializeField] private GameObject[] pages;
        [Tooltip("In Editor, skip the pairing process")]
        [SerializeField] private bool skipIOSPair = true;


        /// <summary>
        /// Event triggered when the Looking Glass display setup is completed, such as after loading a calibration file.
        /// </summary>
        public static event Action onLKGDisplaySetup;

        /// <summary>
        /// Enum representing the different types of UI pages in the setup process. Includes: None, GetStarted, Connect, LoadCalibration, Done.
        /// </summary>
        public enum PageType
        {
            None = -1,
            GetStarted = 0,
            Connect = 1,
            LoadCalibration = 2,
            Done = 3
        };

        /// <summary>
        /// Returns the current page type based on the currentPage index.
        /// </summary>
        public PageType CurrentPageType => (PageType)(currentPage);

        /// <summary>
        /// Checks if the calibration file (visual.json) exists at the specified path.
        /// </summary>
        public static bool isCalibrationFileExist =>
            File.Exists(MobileDMAController.VisualJsonFilePath);

        private MobileDMAController dMAController;
        /// <summary>
        /// Tracks the index of the currently active UI page.
        /// </summary>
        private int currentPage = -1;

        /// <summary>
        /// Indicates whether this is the first session (i.e., no calibration file exists).
        /// </summary>
        private bool firstSession = false;

        private void OnEnable()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Display.onDisplaysUpdated += AutoPageUpdate;
#else
            LKGDisplaySystem.onReload += AutoPageUpdate;
#endif


            PopupPairHandler.OnAddCalibrationRequested += PickCalibrationFile;
            PopupPairHandler.OnShowTestImageRequested += ShowTestPage;
            PopupPairHandler.OnPairDone += ShowMainScene;

            PopupTestHandler.OnTestDone += ShowMainScene;
            PopupTestHandler.OnPickCalibrationRequsted += PickCalibrationFile;
        }

        private void OnDisable()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Display.onDisplaysUpdated -= AutoPageUpdate;
#else
            LKGDisplaySystem.onReload -= AutoPageUpdate;
#endif
            PopupPairHandler.OnAddCalibrationRequested -= PickCalibrationFile;
            PopupPairHandler.OnShowTestImageRequested -= ShowTestPage;
            PopupPairHandler.OnPairDone -= ShowMainScene;

            PopupTestHandler.OnTestDone -= ShowMainScene;
            PopupTestHandler.OnPickCalibrationRequsted -= PickCalibrationFile;
        }

        private void Awake()
        {
            firstSession = !isCalibrationFileExist;
#if !UNITY_EDITOR
            if(mainCanvas != null)
                mainCanvas.targetDisplay = 0;
#endif
        }

        void Start() {
            dMAController = FindObjectOfType<MobileDMAController>();
            if (dMAController == null) {
                Debug.LogError("MobileDMAController is missing.");
                this.enabled = false;
            }

            int i = 0;
            foreach (GameObject page in pages) {
                if (page == null) {
                    Debug.LogWarning("Page is missing");
                    continue;
                }
                PageHandler pageHandler = page.GetComponent<PageHandler>();
                if (pageHandler == null) {
                    Debug.LogError("PageHandler is missing on Page " + i);
                    continue;
                }
                page.GetComponent<PageHandler>().SetPageIndex(i);
                i++;
                page.SetActive(false);
            }
            HideCalibratePage();
            HideTestPage();
#if UNITY_EDITOR
            if (skipIOSPair) {
                ToggleMainSceneAndCanvas(true);
                currentPage = 5;
                return;
            }
#endif
            ToggleMainSceneAndCanvas(false);

            if (firstSession)
                SwitchToPage(PageType.GetStarted);
            else
                SwitchToPage(PageType.Connect);

        }

        void ToggleMainSceneAndCanvas(bool isOn) {
            if(mainScene)
                mainScene.SetActive(isOn);
            if(mainCanvas)
                mainCanvas.gameObject.SetActive(isOn);
        }

        public static async System.Threading.Tasks.Task<bool> isDisplayConnected()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return Display.displays.Length == 2;
#else
            if (LKGDisplaySystem.IsLoading)
            {
                await LKGDisplaySystem.WaitForCalibrations();
            }
            else
            {
                bool result = await LKGDisplaySystem.Reconnect();
                if (!result)
                    Debug.LogWarning("Connecting to LKG Bridge and retrieving LKG displays & calibration failed to complete fully.");
            }
            return LKGDisplaySystem.LKGDisplayCount > 0;
#endif
        }

        private async void AutoPageUpdate()
        {
#if UNITY_EDITOR
            if (skipIOSPair)
                return;
#endif
            bool isLKGConnected = await isDisplayConnected();
            if (isLKGConnected)
            {
                bool notUpdateForFirstSession = firstSession && CurrentPageType == PageType.GetStarted;

                if (notUpdateForFirstSession
                    || CurrentPageType > PageType.Connect)
                    return;

                OnLoadCalibration();
            }
            else
            {
                Debug.Log("display disconnected at page " + CurrentPageType);
                HideCalibratePage();
                HideTestPage();
                ToggleMainSceneAndCanvas(false);

                if (CurrentPageType <= PageType.Connect)
                    return;

                SwitchToPage(PageType.Connect);
            }
        }

        public void OnLoadCalibration()
        {
            if (!isCalibrationFileExist)
            {
                 Debug.Log("No calibration file exists. Go to Load Calibration File Page");
                // go to next pageh
                SwitchToPage(PageType.LoadCalibration);
            }
            else
            {
                // check player prefs
                if (!PopupPairHandler.DontShowAgain)
                {
                    ShowCalibratePage();
                    Debug.Log("Calibration file exists. And dont-show-again is not on. Show Calibration Page");
                }
                else
                {
                    ShowMainScene();
                    Debug.Log("Detect calibration file exists. And dont-sho- again is on. Go to template scene");
                }
            }
        }

        public void GoToNextPage() =>
            InternalSwitchToPage(currentPage + 1);
        public void GoToPreviousPage() =>
            InternalSwitchToPage(currentPage - 1);
        public void ShowMainScene() {
            InternalSwitchToPage(5);
            HideCalibratePage();
            HideTestPage();
            ToggleMainSceneAndCanvas(true);
        }
        public void ShowCalibratePage() => TogglePairPopUp?.Invoke(true);
        public void HideCalibratePage() => TogglePairPopUp?.Invoke(false);
        public void ShowTestPage() => ToggleTestPopUp?.Invoke(true);
        public void HideTestPage() => ToggleTestPopUp?.Invoke(false);

        public void SwitchToPage(PageType pageType) => InternalSwitchToPage((int)pageType);
        public void SwitchToPage(int pageIndex) => InternalSwitchToPage(pageIndex);

        private void InternalSwitchToPage(int pageIndex)
        {
            if (currentPage == pageIndex)
                return;

            if (currentPage >= 0 && currentPage < pages.Length && pages[currentPage] != null)
                pages[currentPage].gameObject.SetActive(false);

            currentPage = pageIndex;

            if (pageIndex >= 0 && pageIndex < pages.Length && pages[pageIndex] != null)
                pages[pageIndex].gameObject.SetActive(true);
        }

        public void HandleLoadVisualJson()
        {
            // On iOS pick a visual.json file
            // On Mac / Editor, just refresh the displays which includes reloading the calibration visual.json        
#if UNITY_IOS
            PickCalibrationFile(true);
#else
            dMAController.RefreshDisplays();
            GoToNextPage();
#endif
        }

        public void PickCalibrationFile(bool goNextWhenDone)
        {
#if UNITY_IOS
            // Don't attempt to import/export files if the file picker is already open
            //if( NativeFilePicker.IsFilePickerBusy() )
            //	return;
            // Use UTIs on iOS
            string fileType = NativeFilePicker.ConvertExtensionToFileType("txt");
            Debug.Log("txt's MIME/UTI is: " + fileType);
            string[] fileTypes = new string[] { fileType, "public.text", "public.plain-text" };
            NativeFilePicker.Permission permission = NativeFilePicker.PickFile((path) =>
            {
                if (path == null)
                    Debug.Log("File Picker Operation cancelled");
                else
                {
                    Debug.Log("Picked file: " + path);
                    dMAController.LoadedVisualJsonPath = path;

                    onLKGDisplaySetup?.Invoke();

                    if (goNextWhenDone)
                        ShowMainScene(); 
                }
            }, fileTypes);
            Debug.Log("Permission result: " + permission);
#else
            Debug.LogWarning("Calibration file picking is only supported on iOS.");
#endif
        }
    }
}
