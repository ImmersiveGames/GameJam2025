using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Integration.Bootstrap
{
    public static class ActorsSystemCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "ActorsSystem",
                installerDependencies: new[] { "PhaseDefinition", "Gameplay" },
                bootstrapDependencies: new[] { "SessionIntegration" },
                installer: _ => ActorsSystemBootstrap.ComposeInstallerPhase(),
                bootstrap: _ => ActorsSystemBootstrap.ComposeRuntime(),
                installerEntry: "ActorsSystemBootstrap.ComposeInstallerPhase",
                runtimeComposerEntry: "ActorsSystemBootstrap.ComposeRuntime",
                description: "ActorsSystem semantic owner (definitions + identity policy + ensemble + presence + registry boundary + participant/runtime mapping boundary + operational binding boundary + materialization plan/spec boundary + materialization execution policy boundary).");
    }
}
