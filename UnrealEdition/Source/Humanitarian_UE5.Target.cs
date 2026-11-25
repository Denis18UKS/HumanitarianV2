using UnrealBuildTool;
public class Humanitarian_UE5Target : TargetRules {
    public Humanitarian_UE5Target(TargetInfo target) : base(target) {
        Type=TargetType.Game;
        DefaultBuildSettings=BuildSettingsVersion.V5;
        IncludeOrderVersion=EngineIncludeOrderVersion.Unreal5_5;
        ExtraModuleNames.Add("Humanitarian_UE5");
    }
}
