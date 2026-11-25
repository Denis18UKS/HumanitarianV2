using UnrealBuildTool;
public class Humanitarian_UE5EditorTarget : TargetRules {
    public Humanitarian_UE5EditorTarget(TargetInfo target) : base(target) {
        Type=TargetType.Editor;
        DefaultBuildSettings=BuildSettingsVersion.Latest;
        IncludeOrderVersion=EngineIncludeOrderVersion.Latest;
        ExtraModuleNames.Add("Humanitarian_UE5");
    }
}
