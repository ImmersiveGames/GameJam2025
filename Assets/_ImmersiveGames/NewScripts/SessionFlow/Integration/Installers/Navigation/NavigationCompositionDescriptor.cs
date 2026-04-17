using ImmersiveGames.GameJam2025.Infrastructure.Composition;
namespace ImmersiveGames.GameJam2025.Orchestration.Navigation.Bootstrap
{
    public static class NavigationCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "Navigation",
                installerDependencies: System.Array.Empty<string>(),
                bootstrapDependencies: new[] { "SceneFlow" },
                installer: bootstrapConfig => NavigationInstaller.Install(bootstrapConfig),
                bootstrap: bootstrapConfig => NavigationBootstrap.ComposeRuntime(bootstrapConfig),
                installerEntry: "NavigationInstaller.Install",
                runtimeComposerEntry: "NavigationBootstrap.ComposeRuntime",
                description: "Navigation boundary: NavigationCore + NavigationAdapters + NavigationCompatibility.");
    }
}

