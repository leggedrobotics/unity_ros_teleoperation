using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Core.Menu
{
    public class ClearSettings : MonoBehaviour
    {
        public void ClearAllSettings()
        {
            foreach (var key in PlayerPrefs.GetString("PlayerPrefsKeys", "").Split(','))
            {
                if (key != "ips" && key != "ip" && key != "port")
                {
                PlayerPrefs.DeleteKey(key);
                }
            }
            PlayerPrefs.Save();
            Debug.Log("Cleared all settings");
        }
    }
}
