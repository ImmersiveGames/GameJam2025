using System;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.SaveRuntime.Persistence.Bootstrap
{
    public static class SaveCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "Save",
                installerDependencies: new[] { "RuntimePolicy" },
                bootstrapDependencies: Array.Empty<string>(),
                installer: runtimeModeConfig => SaveInstaller.Install(runtimeModeConfig),
                bootstrap: null,
                installerEntry: "SaveInstaller.Install",
                runtimeComposerEntry: null,
                installerOnly: true,
                description: "Canonical save core composition with backend from SaveConfigAsset.");
    }
}

