using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
{
    internal static class PhaseDefinitionSemanticRegistration
    {
        public static void RegisterAll(BootstrapConfigAsset bootstrapConfig)
        {
            PhaseDefinitionSemanticCatalogRuntimeRegistration.RegisterAll(bootstrapConfig);
            PhaseDefinitionSemanticSelectionRegistration.RegisterAll();
            PhaseDefinitionSemanticOwnersRegistration.RegisterAll();
            PhaseDefinitionSemanticPhaseSideHelpersRegistration.RegisterAll();
        }
    }
}
