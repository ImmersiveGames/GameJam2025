using _ImmersiveGames.NewScripts.Infrastructure.Composition;
namespace _ImmersiveGames.NewScripts.Experience.Audio.Bootstrap
{
    public static class AudioCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "Audio",
                installerDependencies: new[] { "RuntimePolicy" },
                bootstrapDependencies: new[] { "Preferences" },
                installer: AudioInstaller.Install,
                bootstrap: AudioRuntimeComposer.ComposeRuntime,
                installerEntry: "AudioInstaller.Install",
                runtimeComposerEntry: "AudioRuntimeComposer.ComposeRuntime",
                description: "Audio defaults, services and runtime wiring.");
    }
}
