using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;

namespace _ImmersiveGames.NewScripts.ActorSystem.Integration.Bootstrap
{
    public static class ActorSystemCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "ActorSystem",
                installerDependencies: new[] { "PhaseDefinition", "Gameplay" },
                bootstrapDependencies: new[] { "SessionIntegration" },
                installer: _ => ActorSystemBootstrap.ComposeInstallerPhase(),
                bootstrap: _ => ActorSystemBootstrap.ComposeRuntime(),
                installerEntry: "ActorSystemBootstrap.ComposeInstallerPhase",
                runtimeComposerEntry: "ActorSystemBootstrap.ComposeRuntime",
                description: "ActorSystem thin semantic module with SessionFlow inbound and GameplayRuntime read-only presence.");
    }
}
