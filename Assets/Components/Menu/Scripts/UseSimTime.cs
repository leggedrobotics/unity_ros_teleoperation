using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

namespace RSL.Core.Menu
{
    public class UseSimTime : MonoBehaviour
    {
        private TMPro.TextMeshProUGUI label;
        // Start is called before the first frame update
        void Start()
        {

            bool useSimTime = PlayerPrefs.GetInt("use_sim_time", 0) == 1;
            TFStream.UseSimTime = useSimTime;

            label = GetComponent<TMPro.TextMeshProUGUI>();
            label.text = "Use Sim Time: " + TFStream.UseSimTime;

            PlayerPrefs.SetInt("use_sim_time", TFStream.UseSimTime ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        public void ToggleSimTime()
        {
            TFStream.UseSimTime = !TFStream.UseSimTime;
            label.text = "Use Sim Time: " + TFStream.UseSimTime;
            PlayerPrefs.SetInt("use_sim_time", TFStream.UseSimTime ? 1 : 0);
            PlayerPrefs.Save();
        }

    }
}
