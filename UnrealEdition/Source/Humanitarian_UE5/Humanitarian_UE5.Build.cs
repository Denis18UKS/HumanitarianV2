using UnrealBuildTool;
public class Humanitarian_UE5 : ModuleRules {
    public Humanitarian_UE5(ReadOnlyTargetRules target) : base(target) {
        PCHUsage=PCHUsageMode.UseExplicitOrSharedPCHs;
        PublicDependencyModuleNames.AddRange(new[]{"Core","CoreUObject","Engine","InputCore","Json","ProceduralMeshComponent"});
    }
}
