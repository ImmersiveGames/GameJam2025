using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.Bootstrap
{
    public static class GameplayCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "Gameplay",
                installerDependencies: new[] { "Gates" },
                bootstrapDependencies: new[] { "ActorsSystem" },
                installer: _ => GameplayInstaller.Install(),
                bootstrap: _ => GameplayRuntimeBootstrap.ComposeRuntime(),
                installerEntry: "GameplayInstaller.Install",
                runtimeComposerEntry: "GameplayRuntimeBootstrap.ComposeRuntime",
                installerOnly: false,
                description: "Gameplay state/camera + actors operational execution bridge.");
    }
}

