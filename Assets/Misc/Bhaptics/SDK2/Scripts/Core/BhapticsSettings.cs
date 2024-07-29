using System;
using UnityEngine;

namespace Bhaptics.SDK2
{
    [Serializable]
    public class BhapticsConfig
    {
        public string appId = "";
        public string apiKey = "";
    }

    public class BhapticsSettings : ScriptableObject
    {
        private static BhapticsSettings instance;

        [SerializeField] private string appName = "";
        [SerializeField] private string appId = "";
        [SerializeField] private string apiKey = "";
        [SerializeField] private int lastDeployVersion = -1;
        [SerializeField] private MappingMetaData[] eventData;
        [HideInInspector, SerializeField] private string defaultDeploy = "";


        #region Properties
        public static BhapticsSettings Instance
        {
            get
            {
                LoadInstance();

                return instance;
            }
        }

        public string AppName
        {
            get
            {
                return appName;
            }
            set
            {
                appName = value;
            }
        }

        public string AppId
        {
            get
            {
                return appId;
            }
            set
            {
                appId = value;
            }
        }

        public string ApiKey
        {
            get
            {
                return apiKey;
            }
            set
            {
                apiKey = value;
            }
        }

        public MappingMetaData[] EventData
        {
            get
            {
                return eventData;
            }
            set
            {
                eventData = value;
            }
        }

        public int LastDeployVersion
        {
            get
            {
                return lastDeployVersion;
            }
            set
            {
                lastDeployVersion = value;
            }
        }

        public string DefaultDeploy
        {
            get
            {
                return defaultDeploy;
            }
            set
            {
                defaultDeploy = value;
            }
        }
        #endregion







        public static void VerifyScriptableObject()
        {
            LoadInstance();
        }

        public static void ResetInstance()
        {
            if (instance == null)
            {
                return;
            }

            BhapticsLogManager.LogFormat("[bHaptics] Reset App Setup: {0}", instance.AppName);
            instance.appName = "";
            instance.appId = "";
            instance.apiKey = "";
            instance.eventData = null;
            instance.lastDeployVersion = -1;
            instance.defaultDeploy = "";
#if UNITY_EDITOR
            var localFolderPath = BhapticsSDK2.GetResourcePath(true);
            var assetPath = System.IO.Path.Combine(localFolderPath, "BhapticsSettings.asset");
            UnityEditor.EditorUtility.SetDirty(instance);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        private static void LoadInstance()
        {
            if (instance == null)
            {
                BhapticsLogManager.LogFormat("Load BhapticsSettings.asset");
                instance = Resources.Load<BhapticsSettings>("BhapticsSettings");

                if (instance == null)
                {
                    instance = CreateInstance<BhapticsSettings>();
#if UNITY_EDITOR
                    var localFolderPath = BhapticsSDK2.GetResourcePath(true);
                    var assetPath = System.IO.Path.Combine(localFolderPath, "BhapticsSettings.asset");

                    UnityEditor.AssetDatabase.CreateAsset(instance, assetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
#endif
                }
            }
        }
    }
}