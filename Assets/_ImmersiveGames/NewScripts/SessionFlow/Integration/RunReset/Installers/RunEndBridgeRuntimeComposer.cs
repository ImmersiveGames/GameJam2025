using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Ownership;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Result;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset.Installers
{
    public static class RunEndBridgeRuntimeComposer
    {
        private static bool _composed;

        public static void ComposeOrFail()
        {
            if (_composed)
            {
                return;
            }

            RegisterRunResetTargetPhaseResolver();
            RegisterRunContinuationSelectionRoutingService();
            RegisterRunEndPostMaterializationDispatchService();
            RegisterRunEndMaterializationService();

            _composed = true;

            DebugUtility.LogVerbose(typeof(RunEndBridgeRuntimeComposer),
                "[OBS][GameplaySessionFlow][RunEndBridge] Runtime services composed for GameRunEndedEventBridge seam.",
                DebugUtility.Colors.Info);
        }

        private static void RegisterRunContinuationSelectionRoutingService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRunContinuationSelectionRoutingService>(out var existing) && existing != null)
            {
                return;
            }

            var service = new RunContinuationSelectionRoutingService(
                ResolveRequired<IRunContinuationOperationalHandoffService>(
                    "[FATAL][Config][GameplaySessionFlow] IRunContinuationOperationalHandoffService ausente no DI global antes de compor IRunContinuationSelectionRoutingService."),
                ResolveRequired<IGameplaySessionRunResetService>(
                    "[FATAL][Config][GameplaySessionFlow] IGameplaySessionRunResetService ausente no DI global antes de compor IRunContinuationSelectionRoutingService."),
                ResolveRequired<IRunResetTargetPhaseResolver>(
                    "[FATAL][Config][GameplaySessionFlow] IRunResetTargetPhaseResolver ausente no DI global antes de compor IRunContinuationSelectionRoutingService."));

            DependencyManager.Provider.RegisterGlobal<IRunContinuationSelectionRoutingService>(service);
        }

        private static void RegisterRunResetTargetPhaseResolver()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRunResetTargetPhaseResolver>(out var existing) && existing != null)
            {
                return;
            }

            var service = new RunResetTargetPhaseResolver(
                ResolveRequired<IPhaseDefinitionCatalog>(
                    "[FATAL][Config][GameplaySessionFlow] IPhaseDefinitionCatalog ausente no DI global antes de compor IRunResetTargetPhaseResolver."),
                ResolveRequired<IPhaseCatalogRuntimeStateService>(
                    "[FATAL][Config][GameplaySessionFlow] IPhaseCatalogRuntimeStateService ausente no DI global antes de compor IRunResetTargetPhaseResolver."));

            DependencyManager.Provider.RegisterGlobal<IRunResetTargetPhaseResolver>(service);
        }

        private static void RegisterRunEndMaterializationService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRunEndMaterializationService>(out var existing) && existing != null)
            {
                return;
            }

            DependencyManager.Provider.TryGetGlobal<IGameplaySceneClassifier>(out var gameplaySceneClassifier);

            var service = new RunEndMaterializationService(
                ResolveRequired<ISessionIntegrationContextService>(
                    "[FATAL][Config][GameplaySessionFlow] ISessionIntegrationContextService ausente no DI global antes de compor IRunEndMaterializationService."),
                ResolveRequired<IPostRunResultService>(
                    "[FATAL][Config][GameplaySessionFlow] IPostRunResultService ausente no DI global antes de compor IRunEndMaterializationService."),
                ResolveRequired<IRunEndIntentOwnershipService>(
                    "[FATAL][Config][GameplaySessionFlow] IRunEndIntentOwnershipService ausente no DI global antes de compor IRunEndMaterializationService."),
                ResolveRequired<IRunContinuationOwnershipService>(
                    "[FATAL][Config][GameplaySessionFlow] IRunContinuationOwnershipService ausente no DI global antes de compor IRunEndMaterializationService."),
                ResolveRequired<IRunEndPostMaterializationDispatchService>(
                    "[FATAL][Config][GameplaySessionFlow] IRunEndPostMaterializationDispatchService ausente no DI global antes de compor IRunEndMaterializationService."),
                gameplaySceneClassifier);

            DependencyManager.Provider.RegisterGlobal<IRunEndMaterializationService>(service);
        }

        private static void RegisterRunEndPostMaterializationDispatchService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IRunEndPostMaterializationDispatchService>(out var existing) && existing != null)
            {
                return;
            }

            var service = new RunEndPostMaterializationDispatchService(
                ResolveRequired<IRunResultStageOwnershipService>(
                    "[FATAL][Config][GameplaySessionFlow] IRunResultStageOwnershipService ausente no DI global antes de compor IRunEndPostMaterializationDispatchService."),
                ResolveRequired<IRunContinuationOwnershipService>(
                    "[FATAL][Config][GameplaySessionFlow] IRunContinuationOwnershipService ausente no DI global antes de compor IRunEndPostMaterializationDispatchService."));

            DependencyManager.Provider.RegisterGlobal<IRunEndPostMaterializationDispatchService>(service);
        }

        private static T ResolveRequired<T>(string errorMessage)
            where T : class
        {
            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                throw new InvalidOperationException(errorMessage);
            }

            return service;
        }
    }
}
