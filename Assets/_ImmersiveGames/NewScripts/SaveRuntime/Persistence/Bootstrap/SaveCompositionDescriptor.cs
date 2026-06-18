using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Persistence.Bootstrap
{
    public static class SaveCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                "Save",
                new[] { "RuntimePolicy" },
                Array.Empty<string>(),
                runtimeModeConfig => SaveInstaller.Install(runtimeModeConfig),
                null,
                "SaveInstaller.Install",
                null,
                installerOnly: true,
                description: "Canonical save core composition with backend from SaveConfigAsset.");
    }
}
