using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
{
    internal static class PhaseDefinitionOperationalAuxRegistration
    {
        public static void RegisterAll()
        {
            RegisterPhaseContentUnloadSupplementProvider();
            RegisterPhaseContentCompletionCleaner();
        }

        private static void RegisterPhaseContentUnloadSupplementProvider()
        {
            if (DependencyManager.Provider.TryGetGlobal<PhaseContentSceneTransitionUnloadSupplementProvider>(out var existingProvider) && existingProvider != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionOperationalAuxRegistration),
                    "[OBS][PhaseDefinition][PhaseFlow] PhaseContentSceneTransitionUnloadSupplementProvider ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var provider = new PhaseContentSceneTransitionUnloadSupplementProvider();
            DependencyManager.Provider.RegisterGlobal(provider);

            DebugUtility.LogVerbose(typeof(PhaseDefinitionOperationalAuxRegistration),
                "[OBS][PhaseDefinition][PhaseFlow] executor='PhaseContentSceneTransitionUnloadSupplementProvider' role='phase-content-unload-supplement'.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterPhaseContentCompletionCleaner()
        {
            if (DependencyManager.Provider.TryGetGlobal<PhaseContentSceneTransitionCompletionCleaner>(out var existingCleaner) && existingCleaner != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionOperationalAuxRegistration),
                    "[OBS][PhaseDefinition][PhaseFlow] PhaseContentSceneTransitionCompletionCleaner ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var cleaner = new PhaseContentSceneTransitionCompletionCleaner();
            DependencyManager.Provider.RegisterGlobal(cleaner);

            DebugUtility.LogVerbose(typeof(PhaseDefinitionOperationalAuxRegistration),
                "[OBS][PhaseDefinition][PhaseFlow] executor='PhaseContentSceneTransitionCompletionCleaner' role='phase-content-cleanup'.",
                DebugUtility.Colors.Info);
        }
    }
}
