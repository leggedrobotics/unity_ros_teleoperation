using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RSL.Core.Menu
{
    public class Quit : MonoBehaviour
    {
        
        public void QuitGame()
        {
            Debug.Log("Quit Game");
            Application.Quit();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}
