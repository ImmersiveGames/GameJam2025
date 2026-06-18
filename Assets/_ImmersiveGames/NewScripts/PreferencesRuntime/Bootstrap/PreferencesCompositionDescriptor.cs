using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Bootstrap
{
    public static class PreferencesCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                "Preferences",
                new[] { "Audio", "Save" },
                Array.Empty<string>(),
                runtimeModeConfig => PreferencesInstaller.Install(runtimeModeConfig),
                runtimeModeConfig => PreferencesBootstrap.ComposeRuntime(runtimeModeConfig),
                "PreferencesInstaller.Install",
                "PreferencesBootstrap.ComposeRuntime",
                description: "Canonical audio/video preferences runtime state with persistence through PreferencesRuntimePipeline -> PreferencesSaveAdapter -> ISaveService/SaveRuntime.");
    }
}
