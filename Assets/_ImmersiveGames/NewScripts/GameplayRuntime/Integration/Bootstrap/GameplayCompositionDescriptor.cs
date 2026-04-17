using System;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.Bootstrap
{
    public static class GameplayCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "Gameplay",
                installerDependencies: new[] { "Gates" },
                bootstrapDependencies: Array.Empty<string>(),
                installer: _ => GameplayInstaller.Install(),
                bootstrap: null,
                installerEntry: "GameplayInstaller.Install",
                runtimeComposerEntry: null,
                installerOnly: true,
                description: "Gameplay state e camera resolver.");
    }
}

