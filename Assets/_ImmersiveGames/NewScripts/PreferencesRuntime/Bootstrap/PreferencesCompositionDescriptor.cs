using ImmersiveGames.GameJam2025.Infrastructure.Composition;
namespace ImmersiveGames.GameJam2025.Experience.Preferences.Bootstrap
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

