using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.PreferencesRuntime.Bootstrap
{
    public static class PreferencesCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "Preferences",
                installerDependencies: new[] { "Audio", "Save" },
                bootstrapDependencies: Array.Empty<string>(),
                installer: runtimeModeConfig => PreferencesInstaller.Install(runtimeModeConfig),
                bootstrap: runtimeModeConfig => PreferencesBootstrap.ComposeRuntime(runtimeModeConfig),
                installerEntry: "PreferencesInstaller.Install",
                runtimeComposerEntry: "PreferencesBootstrap.ComposeRuntime",
                description: "Canonical audio/video preferences runtime state with persistence through PreferencesRuntimePipeline -> PreferencesSaveAdapter -> ISaveService/SaveRuntime.");
    }
}

