using UnrealBuildTool;
public class Humanitarian_UE5EditorTarget : TargetRules {
    public Humanitarian_UE5EditorTarget(TargetInfo target) : base(target) {
        Type=TargetType.Editor;
        DefaultBuildSettings=BuildSettingsVersion.V5;
        IncludeOrderVersion=EngineIncludeOrderVersion.Unreal5_5;
        ExtraModuleNames.Add("Humanitarian_UE5");
    }
}
