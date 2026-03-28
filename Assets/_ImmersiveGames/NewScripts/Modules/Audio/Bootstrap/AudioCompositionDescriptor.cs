using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Bootstrap
{
    public static class AudioCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "Audio",
                installerDependencies: new[] { "RuntimePolicy" },
                bootstrapDependencies: Array.Empty<string>(),
                installer: bootstrapConfig => AudioInstaller.Install(bootstrapConfig),
                bootstrap: bootstrapConfig => AudioRuntimeComposer.ComposeRuntime(bootstrapConfig),
                installerEntry: "AudioInstaller.Install",
                runtimeComposerEntry: "AudioRuntimeComposer.ComposeRuntime",
                description: "Audio defaults, services and runtime wiring.");
    }
}
