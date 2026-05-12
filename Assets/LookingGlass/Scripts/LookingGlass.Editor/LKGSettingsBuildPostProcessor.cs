using System.IO;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace LookingGlass.Editor {
    public class LKGSettingsBuildPostProcessor : IPostprocessBuildWithReport {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report) {
            //NOTE: The BuildReport.summary.outputPath is...
            //  WINDOWS: The .exe file path itself.
            //  MACOS:  The .app file path.
            string buildFolder = report.summary.outputPath;
            buildFolder = Path.GetDirectoryName(buildFolder);
            buildFolder = buildFolder.Replace('\\', '/');

            //NOTE: From DualMonitorApplicationManager, we don't explicitly set the Process's StartInfo's WorkingDirectory, so the 3D app's working directory is inherited from the parent (2D app) process.
            //  For ease of JSON file management, we can just use this to our advantage and ONLY COPY ONE JSON file to the root app, and skip copying to builds nested inside of another Unity app:
            if (buildFolder.Contains("_Data") || buildFolder.Contains("StreamingAssets"))
                return;

            if (File.Exists(LKGSettingsSystem.FileName)) {
                string outFilePath = Path.Combine(buildFolder, LKGSettingsSystem.FileName).Replace('\\', '/');
                File.Copy(LKGSettingsSystem.FileName, outFilePath, true);

                Debug.Log("Copied " + LKGSettingsSystem.FileName + " alongside your build:\n" + outFilePath);
            }
        }
    }
}
