using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
public static class BuildNightRoute {
    [MenuItem("Humanitarian/Build Windows Night Route")]
    public static void Windows(){
        Directory.CreateDirectory("Build/Windows");
        var report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/NightRoute.unity"},"Build/Windows/Humanitarian_Unity.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Windows build failed: "+report.summary.result);
    }
}
