using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Ownership;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.Result;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset.Installers
{
    public static class RunEndBridgeRuntimeComposer
    {
        private static bool _composed;

        public static bool IsComposed => _composed;

        public static void ComposeOrFail()
        {
            if (_composed)
            {
                return;
            }

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
                    "[FATAL][Config][GameplaySessionFlow] IRunContinuationOperationalHandoffService ausente no DI global antes de compor IRunContinuationSelectionRoutingService."));

            DependencyManager.Provider.RegisterGlobal<IRunContinuationSelectionRoutingService>(service);
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
