using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
{
    public static class PhaseDefinitionCompositionDescriptor
    {
        public static ICompositionModuleDescriptor Descriptor { get; } =
            new CompositionModuleDescriptor(
                moduleId: "PhaseDefinition",
                installerDependencies: System.Array.Empty<string>(),
                bootstrapDependencies: System.Array.Empty<string>(),
                installer: bootstrapConfig => PhaseDefinitionInstaller.Install(bootstrapConfig),
                bootstrap: null,
                installerEntry: "PhaseDefinitionInstaller.Install",
                runtimeComposerEntry: string.Empty,
                installerOnly: true,
                description: "PhaseDefinition boundary: explicit catalog + global resolver.");
    }
}

