using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.SessionFlow.GameLoop.Installers
{
    public static class GameLoopCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "GameLoop",
                installerDependencies: System.Array.Empty<string>(),
                bootstrapDependencies: new[] { "SceneFlow", "SessionIntegration" },
                installer: _ => GameLoopCoreInstaller.Install(),
                bootstrap: bootstrapConfig => GameLoopBootstrap.ComposeRuntime(bootstrapConfig),
                installerEntry: "GameLoopCoreInstaller.Install",
                runtimeComposerEntry: "GameLoopBootstrap.ComposeRuntime",
                description: "GameLoop boundary: core executor only; integrations are split into named installers.");
    }
}

