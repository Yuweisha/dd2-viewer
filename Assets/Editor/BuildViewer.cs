using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildViewer
{
    const string LogPath = "build_log.txt";
    const string OutDir = @"C:\Users\Schwarz\Downloads\DD2Viewer5";

    [MenuItem("Viewer/Build EXE")]
    public static void Run()
    {
        var sb = new System.Text.StringBuilder();
        try
        {
            var scenes = new[] { "Assets/Scenes/Viewer.unity" };
            EditorBuildSettings.scenes = scenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();

            PlayerSettings.productName = "DD2 Character Viewer";
            PlayerSettings.companyName = "DD2Modding";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            Directory.CreateDirectory(OutDir);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutDir + "/DD2Viewer.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.CleanBuildCache
            });

            sb.AppendLine("result: " + report.summary.result);
            sb.AppendLine("size: " + (report.summary.totalSize / 1024 / 1024) + " MB");
            sb.AppendLine("errors: " + report.summary.totalErrors);
            sb.AppendLine("out: " + OutDir + "/DD2Viewer.exe");
        }
        catch (System.Exception e) { sb.AppendLine("EX " + e); }
        File.WriteAllText(LogPath, sb.ToString());
    }
}
