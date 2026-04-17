using ImmersiveGames.GameJam2025.Infrastructure.Composition;
namespace ImmersiveGames.GameJam2025.Experience.PostRun.Bootstrap
{
    public static class RunEndRailCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "RunEndRail",
                installerDependencies: new[] { "GameLoop" },
                bootstrapDependencies: System.Array.Empty<string>(),
                installer: _ => RunEndRailInstaller.Install(),
                bootstrap: null,
                installerEntry: "RunEndRailInstaller.Install",
                runtimeComposerEntry: null,
                installerOnly: true,
                description: "RunEndRail internal gameplay rail.");
    }
}


