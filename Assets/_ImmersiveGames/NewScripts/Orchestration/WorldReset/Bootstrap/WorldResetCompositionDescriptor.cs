using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;

namespace _ImmersiveGames.NewScripts.Modules.WorldReset.Bootstrap
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
                description: "Reset macro installer-only.");
    }
}
