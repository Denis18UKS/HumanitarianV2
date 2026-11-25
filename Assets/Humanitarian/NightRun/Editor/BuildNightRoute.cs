using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
public static class BuildNightRoute {
    [MenuItem("Humanitarian/Build Windows Night Route")]
    public static void Windows(){
        foreach(var name in new[]{"NightLit","NightGlass"}){
            var material=UnityEngine.Resources.Load<UnityEngine.Material>(name);
            if(!material || !material.shader || material.shader.name!="Universal Render Pipeline/Lit")
                throw new System.InvalidOperationException("Не удалось импортировать URP/Lit: "+name);
        }
        foreach(var name in new[]{"Asphalt","Grass","Brick","MenuCamaro","Ghost_Camaro_Textures","Ghost_Camaro_V5_FX","Glass_V5","Side_Tint_V5"})
            if(!UnityEngine.Resources.Load<UnityEngine.Texture2D>(name))
                throw new System.IO.InvalidDataException("Отсутствует текстура "+name);
        if(!UnityEngine.Resources.Load<UnityEngine.Shader>("NightSurface") || !UnityEngine.Resources.Load<UnityEngine.Texture2D>("FuelDial"))
            throw new System.IO.InvalidDataException("Нет ресурсов приборной панели или погоды.");
        Directory.CreateDirectory("Build/Windows");
        var report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/NightRoute.unity"},"Build/Windows/Humanitarian_Unity.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Windows build failed: "+report.summary.result);
    }
}
