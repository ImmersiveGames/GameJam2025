using _ImmersiveGames.NewScripts.Infrastructure.Composition;
namespace _ImmersiveGames.NewScripts.Experience.Preferences.Bootstrap
{
    public static class PreferencesCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "Preferences",
                installerDependencies: new[] { "Audio" },
                bootstrapDependencies: System.Array.Empty<string>(),
                installer: bootstrapConfig => PreferencesInstaller.Install(bootstrapConfig),
                bootstrap: bootstrapConfig => PreferencesBootstrap.ComposeRuntime(bootstrapConfig),
                installerEntry: "PreferencesInstaller.Install",
                runtimeComposerEntry: "PreferencesBootstrap.ComposeRuntime",
                description: "Canonical audio and video preferences state and backend seam.");
    }
}
