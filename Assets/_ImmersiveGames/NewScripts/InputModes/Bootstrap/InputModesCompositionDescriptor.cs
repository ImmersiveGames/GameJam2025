using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.InputModes.Bootstrap
{
    public static class InputModesCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                "InputModes",
                new[] { "RuntimePolicy" },
                Array.Empty<string>(),
                runtimeModeConfig => InputModesInstaller.Install(runtimeModeConfig),
                runtimeModeConfig => InputModesRuntimeComposer.ComposeRuntime(runtimeModeConfig),
                "InputModesInstaller.Install",
                "InputModesRuntimeComposer.ComposeRuntime",
                description: "Canonical operational input mode rail (request -> coordinator -> service -> changed event).");
    }
}
