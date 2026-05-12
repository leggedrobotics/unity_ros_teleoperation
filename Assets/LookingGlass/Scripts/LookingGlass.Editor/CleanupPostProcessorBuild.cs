// grabbed from https://forum.unity.com/threads/burstdebuginformation_donotship-in-builds.1172273/#post-8274465

using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using UnityEngine;
using UnityEngine.Assertions;

public class CleanupPostProcessorBuild
{
    // [PostProcessBuild(2000)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
#if UNITY_IOS
        Debug.Log("iOSBuildPostProcess is now postprocessing iOS Project");

        var projectPath = PBXProject.GetPBXProjectPath(path);

        var project = new PBXProject();
        project.ReadFromFile(projectPath);

        var targetGuid = project.GetUnityMainTargetGuid();

        project.SetBuildProperty(targetGuid, "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD", "NO");

        try
        {
            var projectInString = File.ReadAllText(projectPath);
            projectInString = projectInString.Replace(
                "SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD = YES;",
                $"SUPPORTS_MAC_DESIGNED_FOR_IPHONE_IPAD = NO;"
            );
            File.WriteAllText(projectPath, projectInString);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        project.WriteToFile(projectPath);
#endif
        string outputPath = path;

        try
        {
            string applicationName = Path.GetFileNameWithoutExtension(outputPath);
            string outputFolder = Path.GetDirectoryName(outputPath);
            Assert.IsNotNull(outputFolder);

            outputFolder = Path.GetFullPath(outputFolder);

            //Delete Burst Debug Folder
            string burstDebugInformationDirectoryPath = Path.Combine(
                outputFolder,
                $"{applicationName}_BurstDebugInformation_DoNotShip"
            );

            if (Directory.Exists(burstDebugInformationDirectoryPath))
            {
                Debug.Log(
                    $" > Deleting Burst debug information folder at path '{burstDebugInformationDirectoryPath}'..."
                );

                Directory.Delete(burstDebugInformationDirectoryPath, true);
            }

            //Delete il2cpp Debug Folder
            string il2cppDebugInformationDirectoryPath = Path.Combine(
                outputFolder,
                $"{applicationName}_BackUpThisFolder_ButDontShipItWithYourGame"
            );

            if (Directory.Exists(il2cppDebugInformationDirectoryPath))
            {
                Debug.Log(
                    $" > Deleting Burst debug information folder at path '{il2cppDebugInformationDirectoryPath}'..."
                );

                Directory.Delete(il2cppDebugInformationDirectoryPath, true);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                $"An unexpected exception occurred while performing build cleanup: {e}"
            );
        }
    }
}
