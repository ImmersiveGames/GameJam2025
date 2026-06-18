using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.AudioRuntime.Playback.Bootstrap
{
    public static class AudioCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                "Audio",
                new[] { "RuntimePolicy" },
                new[] { "Preferences" },
                AudioInstaller.Install,
                AudioRuntimeComposer.ComposeRuntime,
                "AudioInstaller.Install",
                "AudioRuntimeComposer.ComposeRuntime",
                description: "Audio core runtime, thin bridges and explicit cue playback wiring.");
    }
}
