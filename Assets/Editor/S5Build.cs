using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class S5Build
{
    public static void Build()
    {
        Directory.CreateDirectory("Builds");
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = "Builds/mvp-ar.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("BUILD_RESULT=" + report.summary.result +
                  " size=" + report.summary.totalSize +
                  " errors=" + report.summary.totalErrors);
        EditorApplication.Exit(
            report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
