using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;
using _ImmersiveGames.NewScripts.ActorsSystem.Integration.Bootstrap;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SceneFlow.Installers;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Context;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
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

            SceneFlowInstaller.EnsureRouteActorSetRefContext();
            ActorsSystemBootstrap.EnsureActorSetSelectionInfrastructure();

            if (!DependencyManager.Provider.TryGetGlobal<ISceneFlowRouteActorSetRefContext>(out var routeActorSetContext) ||
                routeActorSetContext == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameplaySessionFlow] Missing ISceneFlowRouteActorSetRefContext for GameplayParticipationFlowService.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IActorSetSelectionService>(out var actorSetSelectionService) ||
                actorSetSelectionService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][GameplaySessionFlow] Missing IActorSetSelectionService for GameplayParticipationFlowService.");
            }

            var owner = new GameplayParticipationFlowService(routeActorSetContext, actorSetSelectionService);
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
