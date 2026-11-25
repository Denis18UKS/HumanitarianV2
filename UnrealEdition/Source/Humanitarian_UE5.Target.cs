using UnrealBuildTool;
public class Humanitarian_UE5Target : TargetRules {
    public Humanitarian_UE5Target(TargetInfo target) : base(target) {
        Type=TargetType.Game;
        DefaultBuildSettings=BuildSettingsVersion.Latest;
        IncludeOrderVersion=EngineIncludeOrderVersion.Latest;
        ExtraModuleNames.Add("Humanitarian_UE5");
    }
}
