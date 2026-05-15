using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.InputModes.Bootstrap
{
    public static class InputModesCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "InputModes",
                installerDependencies: new[] { "RuntimePolicy" },
                bootstrapDependencies: Array.Empty<string>(),
                installer: bootstrapConfig => InputModesInstaller.Install(bootstrapConfig),
                bootstrap: bootstrapConfig => InputModesRuntimeComposer.ComposeRuntime(bootstrapConfig),
                installerEntry: "InputModesInstaller.Install",
                runtimeComposerEntry: "InputModesRuntimeComposer.ComposeRuntime",
                description: "Canonical operational input mode rail (request -> coordinator -> service -> changed event).");
    }
}
