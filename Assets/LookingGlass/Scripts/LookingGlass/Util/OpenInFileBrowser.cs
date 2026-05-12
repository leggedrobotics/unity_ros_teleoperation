// Thanks to https://github.com/needle-mirror/com.unity.recorder/blob/master/Editor/Sources/FileNameGeneratorDrawer.cs
using System.IO;
using UnityEngine;

namespace LookingGlass{
    // A utility class for hologramCamera recorder inspector to open file browser
    static class OpenInFileBrowser
    {
        static void OpenInOSX(string path, bool openInsideFolder)
        {
            var osxPath = path.Replace("\\", "/");

            if (!osxPath.StartsWith("\""))
            {
                osxPath = "\"" + osxPath;
            }

            if (!osxPath.EndsWith("\""))
            {
                osxPath = osxPath + "\"";
            }

            var arguments = (openInsideFolder ? "" : "-R ") + osxPath;

            try
            {
                System.Diagnostics.Process.Start("open", arguments);
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                // tried to open mac finder in windows
                // just silently skip error
                // we currently have no platform define for the current OS we are in, so we resort to this
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it
            }
        }

        static void OpenInWindows(string path, bool openInsideFolder)
        {
            var winPath = path.Replace("/", "\\"); // windows explorer doesn't like forward slashes

            try
            {
                System.Diagnostics.Process.Start("explorer.exe", (openInsideFolder ? "/root," : "/select,") + winPath);
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                // tried to open win explorer in mac
                // just silently skip error
                // we currently have no platform define for the current OS we are in, so we resort to this
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it
            }
        }

        public static void Open(string path)
        {
            if (!File.Exists(path))
                path = Path.GetDirectoryName(path);

            var openInsideFolder = Directory.Exists(path);

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                OpenInWindows(path, openInsideFolder);
            }
            else if (Application.platform == RuntimePlatform.OSXEditor)
            {
                OpenInOSX(path, openInsideFolder);
            }
        }
    }
}