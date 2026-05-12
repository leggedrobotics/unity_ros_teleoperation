using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace LookingGlass
{
    public class UISetup : MonoBehaviour
    {
        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern bool SetWindowPos(IntPtr hwnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(System.String className, System.String windowName);

#if UNITY_STANDALONE_WIN
        void Awake()
        {
            Screen.fullScreen = false;
            SetPosition(0, 0);
            Screen.SetResolution(940, 860, false);
        }
#endif

        public static void SetPosition(int x, int y, int resX = 0, int resY = 0)
        {
        
            //SetWindowPos(FindWindow(null, Application.productName), 0, x, y, resX, resY, resX * resY == 0 ? 1 : 0);
            SetWindowPos(FindWindow(null, Application.productName), 0, x, y, resX, resY, resX * resY == 0 ? 1 : 0);

        }
    }
}