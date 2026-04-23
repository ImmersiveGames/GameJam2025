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
            RegisterGameplaySessionFlowPrepareOperationalHandoffService();
            RegisterSessionIntegrationContextService();
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
                var sessionIntegrationService = new SessionIntegrationContextService(sessionContextService, phaseRuntimeService, participationService);
                DependencyManager.Provider.RegisterGlobal<ISessionIntegrationContextService>(sessionIntegrationService);
                DependencyManager.Provider.RegisterGlobal<ISessionIntegrationInputModeEmitter>(sessionIntegrationService);
                DependencyManager.Provider.RegisterGlobal<ISpawnResetParticipationReadPort>(
                    new SpawnResetParticipationReadPortAdapter(sessionIntegrationService));

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

            if (existingService is not ISessionIntegrationInputModeEmitter contextEmitter)
            {
                throw new InvalidOperationException(
                    "[FATAL][Config][SessionIntegration] ISessionIntegrationContextService binding does not implement ISessionIntegrationInputModeEmitter.");
            }

            if (DependencyManager.Provider.TryGetGlobal<ISessionIntegrationInputModeEmitter>(out var existingEmitter) && existingEmitter != null)
            {
                if (!ReferenceEquals(existingEmitter, contextEmitter))
                {
                    throw new InvalidOperationException(
                        "[FATAL][Config][SessionIntegration] ISessionIntegrationInputModeEmitter mismatch against canonical SessionIntegrationContextService binding.");
                }
            }
            else
            {
                DependencyManager.Provider.RegisterGlobal<ISessionIntegrationInputModeEmitter>(contextEmitter);
            }

            if (DependencyManager.Provider.TryGetGlobal<ISpawnResetParticipationReadPort>(out var existingReadPort) && existingReadPort != null)
            {
                if (existingReadPort is not SpawnResetParticipationReadPortAdapter)
                {
                    throw new InvalidOperationException(
                        "[FATAL][Config][SessionIntegration] ISpawnResetParticipationReadPort mismatch against canonical SessionIntegrationContextService binding.");
                }
            }
            else
            {
                DependencyManager.Provider.RegisterGlobal<ISpawnResetParticipationReadPort>(
                    new SpawnResetParticipationReadPortAdapter(existingService));
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
