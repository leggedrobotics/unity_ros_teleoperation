#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class LKGIOSBuildPostProcessor {
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path) {
        if (buildTarget == BuildTarget.iOS) {
            //NOTE: Delete FFmpeg in iOS builds:
            string ffmpegPath = Path.Combine(path, "Data/Raw/FFmpegOut");
            if (Directory.Exists(ffmpegPath)) {
                Directory.Delete(ffmpegPath, true);
                Debug.Log("FFmpeg directory removed from iOS build.");
            }

            //NOTE: Enable Documents in Finder:
            string plistPath = path + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            PlistElementDict dictionary = plist.root;
            dictionary.SetBoolean("UIFileSharingEnabled", true);
            dictionary.SetBoolean("LSSupportsOpeningDocumentsInPlace", true);
            File.WriteAllText(plistPath, plist.WriteToString());
        }
    }
}
#endif
