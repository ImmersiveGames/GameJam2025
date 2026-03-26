using _ImmersiveGames.NewScripts.Infrastructure.Composition;

namespace _ImmersiveGames.NewScripts.Modules.Navigation.Bootstrap
{
    public static class NavigationCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "Navigation",
                installerDependencies: System.Array.Empty<string>(),
                bootstrapDependencies: new[] { "SceneFlow" },
                installer: bootstrapConfig => NavigationInstaller.Install(bootstrapConfig),
                bootstrap: bootstrapConfig => NavigationBootstrap.ComposeRuntime(bootstrapConfig));
    }
}
