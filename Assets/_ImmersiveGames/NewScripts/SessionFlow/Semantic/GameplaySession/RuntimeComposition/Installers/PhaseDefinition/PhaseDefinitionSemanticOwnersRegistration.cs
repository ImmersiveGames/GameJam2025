using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Context;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
{
    internal static class PhaseDefinitionSemanticOwnersRegistration
    {
        public static void RegisterAll()
        {
            EnsureGameplayParticipationFlowOwner();
            EnsureGameplayPhaseFlowOwner();
        }

        private static void EnsureGameplayPhaseFlowOwner()
        {
            if (DependencyManager.Provider.TryGetGlobal<GameplayPhaseFlowService>(out var existingOwner) && existingOwner != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticOwnersRegistration),
                    "[OBS][PhaseDefinition][PhaseFlow] GameplayPhaseFlowService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            new GameplayPhaseFlowService();

            DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticOwnersRegistration),
                "[OBS][PhaseDefinition][PhaseFlow] owner='GameplayPhaseFlowService' executor='GameplayPhaseFlowService' role='semantic-phase-flow-owner'.",
                DebugUtility.Colors.Info);
        }

        private static void EnsureGameplayParticipationFlowOwner()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out var existingOwner) && existingOwner != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticOwnersRegistration),
                    "[OBS][PhaseDefinition][Participation] GameplayParticipationFlowService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal<GameplayParticipationFlowService>(out var existingConcreteOwner) &&
                existingConcreteOwner != null)
            {
                DependencyManager.Provider.RegisterGlobal<IGameplayParticipationFlowService>(existingConcreteOwner);

                DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticOwnersRegistration),
                    "[OBS][PhaseDefinition][Participation] owner='GameplayParticipationFlowService' composition='external' role='semantic-roster-owner'.",
                    DebugUtility.Colors.Info);
                return;
            }

            var owner = new GameplayParticipationFlowService();
            DependencyManager.Provider.RegisterGlobal<GameplayParticipationFlowService>(owner);
            DependencyManager.Provider.RegisterGlobal<IGameplayParticipationFlowService>(owner);

            DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticOwnersRegistration),
                "[OBS][PhaseDefinition][Participation] owner='GameplayParticipationFlowService' composition='external' role='semantic-roster-owner'.",
                DebugUtility.Colors.Info);
        }
    }

    internal static class PhaseDefinitionSemanticPhaseSideHelpersRegistration
    {
        public static void RegisterAll()
        {
            RegisterRestartContextService();
            RegisterPhaseNextPhaseSelectionService();
            RegisterPhaseNextPhaseCompositionService();
        }

        private static void RegisterPhaseNextPhaseSelectionService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseSelectionService>(out var existingService) || existingService == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPhaseNextPhaseSelectionService>(new PhaseNextPhaseSelectionService());
                DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticPhaseSideHelpersRegistration),
                    "[OBS][PhaseDefinition][Core] NextPhase selection service registered in global DI.",
                    DebugUtility.Colors.Info);
            }
        }

        private static void RegisterPhaseNextPhaseCompositionService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseNextPhaseCompositionService>(out var existingService) || existingService == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPhaseNextPhaseCompositionService>(new PhaseNextPhaseCompositionService());
                DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticPhaseSideHelpersRegistration),
                    "[OBS][PhaseDefinition][Core] NextPhase composition service registered in global DI.",
                    DebugUtility.Colors.Info);
            }
        }

        private static void RegisterRestartContextService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRestartContextService>(out var existingService) && existingService != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticPhaseSideHelpersRegistration),
                    "[OBS][PhaseDefinition][Core] IRestartContextService ja registrado no DI global como owner canonical phase-side.",
                    DebugUtility.Colors.Info);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<IRestartContextService>(new RestartContextService());

            DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticPhaseSideHelpersRegistration),
                "[OBS][PhaseDefinition][Core] RestartContextService registrado no DI global como owner canonical phase-side.",
                DebugUtility.Colors.Info);
        }
    }
}
