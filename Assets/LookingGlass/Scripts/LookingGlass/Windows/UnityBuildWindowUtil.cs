using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LookingGlass {
    public static class UnityBuildWindowUtil {
        private static Regex mainDisplayRegex;
        private static Regex secondaryDisplayRegex;
        private static Regex gameViewRegex;

        public static IntPtr GetMainWindowPtr() {
            if (mainDisplayRegex == null)
                mainDisplayRegex = new Regex("^" + Application.productName + "$");

            foreach (WindowsOSWindow window in WindowsOSWindow.GetAll()) {
                string title = window.GetTitle();
                if (mainDisplayRegex.IsMatch(title))
                    return window.Handle;
            }
            return IntPtr.Zero;
        }

        public static IEnumerable<IntPtr> GetSecondaryWindowPtrs() {
            if (secondaryDisplayRegex == null)
                secondaryDisplayRegex = new Regex("Unity Secondary Display");

            foreach (WindowsOSWindow window in WindowsOSWindow.GetAll()) {
                string title = window.GetTitle();
                if (secondaryDisplayRegex.IsMatch(title))
                    yield return window.Handle;
            }
        }

        public static IEnumerable<IntPtr> GetGameViewWindowPtrs() {
            if (gameViewRegex == null)
                gameViewRegex = new Regex("LookingGlass Game View");

            foreach (WindowsOSWindow window in WindowsOSWindow.GetAll()) {
                string title = window.GetTitle();
                if (gameViewRegex.IsMatch(title))
                    yield return window.Handle;
            }
        }
    }
}
