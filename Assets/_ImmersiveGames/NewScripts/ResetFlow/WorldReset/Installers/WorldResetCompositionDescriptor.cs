using System;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;
namespace ImmersiveGames.GameJam2025.Orchestration.WorldReset.Bootstrap
{
    public static class WorldResetCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "WorldReset",
                installerDependencies: Array.Empty<string>(),
                bootstrapDependencies: Array.Empty<string>(),
                installer: bootstrapConfig => WorldResetInstaller.Install(bootstrapConfig),
                bootstrap: null,
                installerEntry: "WorldResetInstaller.Install",
                runtimeComposerEntry: null,
                installerOnly: true,
                description: "WorldReset boundary: reset lifecycle, dispatch, skip, dedupe, completion.");
    }
}

