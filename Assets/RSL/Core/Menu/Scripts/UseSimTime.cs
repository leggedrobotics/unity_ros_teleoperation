using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using UvgRos.TF2;

namespace RSL.Core.Menu
{
    public class UseSimTime : MonoBehaviour
    {
        private TMPro.TextMeshProUGUI label;
        // Start is called before the first frame update
        void Start()
        {

            bool useSimTime = PlayerPrefs.GetInt("use_sim_time", 0) == 1;
            TF2Stream.UseSimTime = useSimTime;

            label = GetComponent<TMPro.TextMeshProUGUI>();
            label.text = "Use Sim Time: " + TF2Stream.UseSimTime;

            PlayerPrefs.SetInt("use_sim_time", TF2Stream.UseSimTime ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        public void ToggleSimTime()
        {
            TF2Stream.UseSimTime = !TF2Stream.UseSimTime;
            label.text = "Use Sim Time: " + TF2Stream.UseSimTime;
            PlayerPrefs.SetInt("use_sim_time", TF2Stream.UseSimTime ? 1 : 0);
            PlayerPrefs.Save();
        }

    }
}
