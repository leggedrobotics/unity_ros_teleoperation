using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Bhaptics.SDK2
{
    public class BhapticsUI : MonoBehaviour
    {
        [SerializeField] private float intervalRefreshTime = 1f;
        [SerializeField] private RectTransform mainPanel;

        [Header("Devices UI")]
        [SerializeField] private Transform devicesContainer;
        [SerializeField] private Transform deviceListPageUi;
        [SerializeField] private Button deviceListNextPageButton;
        [SerializeField] private Button deviceListBackPageButton;
        [SerializeField] private Text deviceListPageText;
        [SerializeField] private BhapticsDeviceUI devicePrefab;

        [Header("No Paired Device UI")]
        [SerializeField] private GameObject noPairedDeviceUi;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button bHapticsLinkButton;
        [SerializeField] private GameObject helpUi;
        [SerializeField] private GameObject helpDescriptionPC;
        [SerializeField] private GameObject helpDescriptionQuest;
        [SerializeField] private Button helpCloseButton;





        private List<BhapticsDeviceUI> controllers = new List<BhapticsDeviceUI>();
        private BoxCollider mainPanelCollider;
        private Vector2 defaultMainPanelSize;
        private Vector2 defaultDeviceContainerSize;
        private int deviceListSize = 5;
        private int deviceListPageIndex = 0;
        private int expandHeight = 78;
        private int expandDeviceCount = 4;
        private int pageActivateDeviceCount = 6;
        private int pageExpandHeight = 28;
        private int maxPageIndex;






        private void Start()
        {
            for (int i = 0; i < deviceListSize; i++)
            {
                var go = Instantiate(devicePrefab, devicesContainer.transform);
                go.gameObject.SetActive(false);
                controllers.Add(go);
            }

            InvokeRepeating("Refresh", 1f, intervalRefreshTime);

            #region AddListener
            if (helpButton != null)
            {
                helpButton.onClick.AddListener(OnHelp);
            }

            if (helpCloseButton != null)
            {
                helpCloseButton.onClick.AddListener(CloseHelpNotification);
            }

            if (bHapticsLinkButton != null)
            {
                bHapticsLinkButton.onClick.AddListener(OpenLink);
            }

            if (deviceListNextPageButton != null)
            {
                deviceListNextPageButton.onClick.AddListener(NextPage);
            }

            if (deviceListBackPageButton != null)
            {
                deviceListBackPageButton.onClick.AddListener(BackPage);
            }

            if (mainPanel != null)
            {
                defaultMainPanelSize = mainPanel.sizeDelta;
                mainPanelCollider = mainPanel.GetComponent<BoxCollider>();
            }

            if (devicesContainer != null)
            {
                defaultDeviceContainerSize = defaultMainPanelSize;
            }
            #endregion
        }

        void OnDestroy()
        {
            CancelInvoke("Refresh");
        }







        private void Refresh()
        {
            var devices = BhapticsLibrary.GetDevices();

            maxPageIndex = Mathf.FloorToInt(devices.Count / (float)deviceListSize);

            if (devices.Count == 0)
            {
                noPairedDeviceUi.SetActive(true);

                var deviceContainerRect = devicesContainer as RectTransform;
                
                deviceContainerRect.sizeDelta = defaultDeviceContainerSize;

                var sizeDelta = mainPanel.sizeDelta;
                mainPanel.sizeDelta = defaultMainPanelSize;
                mainPanelCollider.center = new Vector3(0f, - sizeDelta.y * 0.5f, 0f);
                mainPanelCollider.size = new Vector3(sizeDelta.x, sizeDelta.y, 1f);

                deviceListPageIndex = 0;
                deviceListPageUi.gameObject.SetActive(false);
            }
            else
            {
                Vector2 currentExpandSize = Vector2.zero;

                if (devices.Count >= pageActivateDeviceCount)
                {
                    currentExpandSize = new Vector2(0f, expandHeight * (pageActivateDeviceCount - expandDeviceCount) + pageExpandHeight);
                    deviceListPageUi.gameObject.SetActive(true);
                    deviceListPageText.text = (deviceListPageIndex + 1) + " / " + (maxPageIndex + 1);

                    deviceListBackPageButton.interactable = deviceListPageIndex != 0;
                    deviceListNextPageButton.interactable = deviceListPageIndex != maxPageIndex;

                    if (deviceListPageIndex > maxPageIndex)
                    {
                        deviceListPageIndex = 0;
                    }
                }
                else
                {
                    if (devices.Count >= expandDeviceCount)
                    {
                        currentExpandSize = new Vector2(0f, expandHeight * (1 + devices.Count - expandDeviceCount));
                    }

                    deviceListPageIndex = 0;
                    deviceListPageUi.gameObject.SetActive(false);
                }

                var deviceContainerRect = devicesContainer as RectTransform;
                deviceContainerRect.sizeDelta = defaultDeviceContainerSize + currentExpandSize;

                mainPanel.sizeDelta = defaultMainPanelSize + currentExpandSize;
                mainPanelCollider.center = new Vector3(0f, -mainPanel.sizeDelta.y * 0.5f, 0f);
                mainPanelCollider.size = new Vector3(mainPanel.sizeDelta.x, mainPanel.sizeDelta.y, 1f);

                noPairedDeviceUi.SetActive(false);
                SetActiveHelpGameObject(false);
            }

            for (int i = 0; i < deviceListSize; i++)
            {
                try
                {
                    if (i <= devices.Count - 1)
                    {
                        controllers[i].RefreshDevice(devices[i + deviceListPageIndex * deviceListSize]);
                    }
                    else
                    {
                        controllers[i].gameObject.SetActive(false);
                    }
                }
                catch (System.Exception e)
                {
                    controllers[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnHelp()
        {
            SetActiveHelpGameObject(true);
        }

        private void CloseHelpNotification()
        {
            SetActiveHelpGameObject(false);
        }

        private void OpenLink()
        {
            Application.OpenURL("https://www.bhaptics.com/support/download");
        }

        private void NextPage()
        {
            deviceListPageIndex = Mathf.Clamp(deviceListPageIndex + 1, 0, maxPageIndex);
        }

        private void BackPage()
        {
            deviceListPageIndex = Mathf.Clamp(deviceListPageIndex - 1, 0, maxPageIndex);
        }

        private void SetActiveHelpGameObject(bool value)
        {
            helpUi.SetActive(value);

            if (Application.platform == RuntimePlatform.Android)
            {
                helpDescriptionQuest.SetActive(value);
                helpDescriptionPC.SetActive(false);
                return;
            }

            helpDescriptionQuest.SetActive(false);
            helpDescriptionPC.SetActive(value);
        }
    }
}
