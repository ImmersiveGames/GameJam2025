using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.Installers.Navigation
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

