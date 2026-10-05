using System;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;

/// <summary>Headless APK build driven via `-executeMethod CIBuild.BuildQuestApk`
/// -- this project has no existing batch-mode build automation (docs/build.md
/// assumes a manual Editor GUI build), so this is the minimal equivalent:
/// build off the SAME "Quest" Build Profile the GUI build uses (Assets/Settings/
/// Build Profiles/Quest.asset -- NOT ProjectSettings/EditorBuildSettings.asset's
/// own scene list, which currently has Main.unity disabled and 2dMain.unity
/// enabled instead). Keystore/key passwords come from environment variables,
/// never hardcoded here, so this file is safe to keep around.</summary>
public static class CIBuild
{
    private const string ProfilePath = "Assets/Settings/Build Profiles/Quest.asset";

    public static void BuildQuestApk()
    {
        var outputPath = Environment.GetEnvironmentVariable("CI_APK_OUTPUT");
        if (string.IsNullOrEmpty(outputPath))
        {
            Console.Error.WriteLine("[CIBuild] CI_APK_OUTPUT env var not set");
            EditorApplication.Exit(1);
            return;
        }

        var keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
        var keyaliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS");
        if (string.IsNullOrEmpty(keystorePass) || string.IsNullOrEmpty(keyaliasPass))
        {
            Console.Error.WriteLine("[CIBuild] ANDROID_KEYSTORE_PASS/ANDROID_KEYALIAS_PASS env vars not set");
            EditorApplication.Exit(1);
            return;
        }
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasPass = keyaliasPass;

        var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
        if (profile == null)
        {
            Console.Error.WriteLine($"[CIBuild] could not load build profile at {ProfilePath}");
            EditorApplication.Exit(1);
            return;
        }

        var options = new BuildPlayerWithProfileOptions
        {
            buildProfile = profile,
            locationPathName = outputPath,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        Console.WriteLine($"[CIBuild] result={report.summary.result} totalErrors={report.summary.totalErrors} "
                           + $"totalWarnings={report.summary.totalWarnings} outputPath={report.summary.outputPath}");
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
