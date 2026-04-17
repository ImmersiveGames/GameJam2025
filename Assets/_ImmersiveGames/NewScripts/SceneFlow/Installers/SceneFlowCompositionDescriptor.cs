using ImmersiveGames.GameJam2025.Infrastructure.Composition;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Bootstrap
{
    public static class SceneFlowCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "SceneFlow",
                installerDependencies: new[] { "RuntimePolicy" },
                bootstrapDependencies: System.Array.Empty<string>(),
                installer: bootstrapConfig => SceneFlowInstaller.Install(bootstrapConfig),
                bootstrap: bootstrapConfig => SceneFlowBootstrap.ComposeRuntime(bootstrapConfig),
                installerEntry: "SceneFlowInstaller.Install",
                runtimeComposerEntry: "SceneFlowBootstrap.ComposeRuntime",
                description: "SceneFlow boundary: transition macro, loading, fade, navigation.");
    }
}

