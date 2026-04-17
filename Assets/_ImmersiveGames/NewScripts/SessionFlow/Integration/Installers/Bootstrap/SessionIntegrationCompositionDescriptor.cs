using ImmersiveGames.GameJam2025.Infrastructure.Composition;

namespace ImmersiveGames.GameJam2025.Orchestration.SessionIntegration.Bootstrap
{
    public static class SessionIntegrationCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "SessionIntegration",
                installerDependencies: new[] { "PhaseDefinition" },
                bootstrapDependencies: new[] { "Navigation" },
                installer: _ => SessionIntegrationBootstrap.ComposeInstallerPhase(),
                bootstrap: bootstrapConfig => SessionIntegrationBootstrap.ComposeRuntime(bootstrapConfig),
                installerEntry: "SessionIntegrationBootstrap.ComposeInstallerPhase",
                runtimeComposerEntry: "SessionIntegrationBootstrap.ComposeRuntime",
                description: "SessionIntegration boundary: session continuity, run reset and session-side completion gate composition.");
    }
}

