using _ImmersiveGames.NewScripts.Infrastructure.Composition;

namespace _ImmersiveGames.NewScripts.Modules.LevelFlow.Bootstrap
{
    public static class LevelFlowCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "LevelFlow",
                installerDependencies: new[] { "WorldReset", "SceneComposition" },
                bootstrapDependencies: new[] { "Navigation" },
                installer: bootstrapConfig => LevelFlowInstaller.Install(bootstrapConfig),
                bootstrap: bootstrapConfig => LevelFlowBootstrap.ComposeRuntime(bootstrapConfig));
    }
}
