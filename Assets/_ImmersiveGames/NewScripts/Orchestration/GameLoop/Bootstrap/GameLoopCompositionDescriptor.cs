using _ImmersiveGames.NewScripts.Infrastructure.Composition;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.Bootstrap
{
    public static class GameLoopCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "GameLoop",
                installerDependencies: System.Array.Empty<string>(),
                bootstrapDependencies: new[] { "SceneFlow" },
                installer: _ => GameLoopInstaller.Install(),
                bootstrap: bootstrapConfig => GameLoopBootstrap.ComposeRuntime(bootstrapConfig),
                installerEntry: "GameLoopInstaller.Install",
                runtimeComposerEntry: "GameLoopBootstrap.ComposeRuntime",
                description: "GameLoop boundary: lifecycle macro, play, pause, resume, run-start, run-end.");
    }
}
