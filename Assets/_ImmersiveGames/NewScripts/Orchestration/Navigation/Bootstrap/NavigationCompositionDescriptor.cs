using _ImmersiveGames.NewScripts.Core.Infrastructure.Composition;
namespace _ImmersiveGames.NewScripts.Orchestration.Navigation.Bootstrap
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
                description: "Navigation catalog e runtime de dispatch.");
    }
}
