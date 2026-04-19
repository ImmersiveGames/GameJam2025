using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Context;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
{
    internal static class PhaseDefinitionSeamRegistration
    {
        public static void RegisterAll()
        {
            RegisterSessionIntegrationContextService();
            RegisterGameplaySessionFlowPrepareOperationalHandoffService();
        }

        private static void RegisterSessionIntegrationContextService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGameplaySessionContextService>(out var sessionContextService) || sessionContextService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IGameplaySessionContextService missing from global DI before session integration registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayPhaseRuntimeService>(out var phaseRuntimeService) || phaseRuntimeService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IGameplayPhaseRuntimeService missing from global DI before session integration registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameplayParticipationFlowService>(out var participationService) || participationService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionIntegration] IGameplayParticipationFlowService missing from global DI before session integration registration.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISessionIntegrationContextService>(out var existingService) || existingService == null)
            {
                DependencyManager.Provider.RegisterGlobal<ISessionIntegrationContextService>(
                    new SessionIntegrationContextService(sessionContextService, phaseRuntimeService, participationService));

                DebugUtility.LogVerbose(typeof(PhaseDefinitionSeamRegistration),
                    "[OBS][SessionIntegration][Core] seam='SessionIntegration' executor='SessionIntegrationContextService' role='canonical-session-integration-seam'.",
                    DebugUtility.Colors.Info);
                GameplaySessionFlowCompletionGateComposer.ComposeOrValidate();
                return;
            }

            if (!ReferenceEquals(existingService.SessionContextService, sessionContextService) ||
                !ReferenceEquals(existingService.PhaseRuntimeService, phaseRuntimeService) ||
                !ReferenceEquals(existingService.ParticipationService, participationService))
            {
                throw new InvalidOperationException(
                    "[FATAL][Config][SessionIntegration] SessionIntegrationContextService mismatch between DI binding and phase-side owners.");
            }

            GameplaySessionFlowCompletionGateComposer.ComposeOrValidate();
        }

        private static void RegisterGameplaySessionFlowPrepareOperationalHandoffService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplaySessionFlowPrepareOperationalHandoffService>(out var existing) && existing != null)
            {
                DebugUtility.LogVerbose(typeof(PhaseDefinitionSeamRegistration),
                    "[OBS][SessionIntegration][SceneFlow] IGameplaySessionFlowPrepareOperationalHandoffService ja registrado no DI global.",
                    DebugUtility.Colors.Info);
                return;
            }

            var handoffService = new GameplaySessionFlowPrepareOperationalHandoffService();
            DependencyManager.Provider.RegisterGlobal<IGameplaySessionFlowPrepareOperationalHandoffService>(handoffService);

            DebugUtility.LogVerbose(typeof(PhaseDefinitionSeamRegistration),
                "[OBS][SessionIntegration][SceneFlow] IGameplaySessionFlowPrepareOperationalHandoffService registrado no DI global.",
                DebugUtility.Colors.Info);
        }
    }
}
