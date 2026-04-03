using _ImmersiveGames.NewScripts.Infrastructure.Composition;
namespace _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Bootstrap
{
    public static class LevelFlowCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "GameplaySessionFlow",
                installerDependencies: new[] { "WorldReset", "SceneComposition" },
                bootstrapDependencies: new[] { "Navigation" },
                installer: bootstrapConfig => LevelLifecycleInstaller.Install(bootstrapConfig),
                bootstrap: bootstrapConfig => LevelLifecycleBootstrap.ComposeRuntime(bootstrapConfig),
                installerEntry: "LevelLifecycleInstaller.Install",
                runtimeComposerEntry: "LevelLifecycleBootstrap.ComposeRuntime",
                description: "GameplaySessionFlow boundary: prepare, intro, playing, outcome, post-run, continuity downstream.");
    }
}
