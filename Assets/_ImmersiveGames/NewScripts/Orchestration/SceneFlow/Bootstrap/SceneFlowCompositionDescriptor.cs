using _ImmersiveGames.NewScripts.Infrastructure.Composition;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Bootstrap
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
                description: "Scene transition, loading subcapability e fade runtime.");
    }
}
