using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.SceneComposition;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Continuity;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.RunReset;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Installers.Bootstrap
{
    public static class SessionTransitionBootstrap
    {
        private static bool _runtimeComposed;

        public static void ComposeRuntime()
        {
            if (_runtimeComposed)
            {
                return;
            }

            IGameplaySessionFlowContinuityService continuityService = ResolveGlobalOrFail<IGameplaySessionFlowContinuityService>(
                "IGameplaySessionFlowContinuityService missing from global DI before session transition composition.");

            if (!DependencyManager.Provider.TryGetGlobal<SessionTransitionPlanResolver>(out var existingResolver) || existingResolver == null)
            {
                DependencyManager.Provider.RegisterGlobal(new SessionTransitionPlanResolver());
                DebugUtility.LogVerbose(typeof(SessionTransitionBootstrap),
                    "[OBS][GameplaySessionFlow][SessionTransition] SessionTransitionPlanResolver registered in global DI.",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISessionTransitionAdvancePhaseExecutionService>(out var existingAdvancePhaseExecutionService) || existingAdvancePhaseExecutionService == null)
            {
                DependencyManager.Provider.RegisterGlobal<ISessionTransitionAdvancePhaseExecutionService>(
                    new SessionTransitionAdvancePhaseExecutionService(
                        ResolveGlobalOrFail<IRestartContextService>("IRestartContextService missing from global DI before advance phase execution service composition."),
                        ResolveGlobalOrFail<IPhaseCatalogNavigationService>("IPhaseCatalogNavigationService missing from global DI before advance phase execution service composition."),
                        ResolveGlobalOrFail<GameplayPhaseFlowService>("GameplayPhaseFlowService missing from global DI before advance phase execution service composition."),
                        ResolveGlobalOrFail<ISceneCompositionExecutor>("ISceneCompositionExecutor missing from global DI before advance phase execution service composition.")));
                DebugUtility.LogVerbose(typeof(SessionTransitionBootstrap),
                    "[OBS][GameplaySessionFlow][SessionTransition] ISessionTransitionAdvancePhaseExecutionService registered in global DI.",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISessionTransitionPhaseOrdinalNavigationExecutionService>(out var existingPhaseOrdinalNavigationExecutionService) || existingPhaseOrdinalNavigationExecutionService == null)
            {
                DependencyManager.Provider.RegisterGlobal<ISessionTransitionPhaseOrdinalNavigationExecutionService>(
                    new SessionTransitionPhaseOrdinalNavigationExecutionService(
                        ResolveGlobalOrFail<IRestartContextService>("IRestartContextService missing from global DI before phase ordinal navigation execution service composition."),
                        ResolveGlobalOrFail<IPhaseCatalogNavigationService>("IPhaseCatalogNavigationService missing from global DI before phase ordinal navigation execution service composition."),
                        ResolveGlobalOrFail<GameplayPhaseFlowService>("GameplayPhaseFlowService missing from global DI before phase ordinal navigation execution service composition."),
                        ResolveGlobalOrFail<ISceneCompositionExecutor>("ISceneCompositionExecutor missing from global DI before phase ordinal navigation execution service composition.")));
                DebugUtility.LogVerbose(typeof(SessionTransitionBootstrap),
                    "[OBS][GameplaySessionFlow][SessionTransition] ISessionTransitionPhaseOrdinalNavigationExecutionService registered in global DI.",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISessionTransitionExecutionPort>(out var existingExecutionPort) || existingExecutionPort == null)
            {
                DependencyManager.Provider.RegisterGlobal<ISessionTransitionExecutionPort>(
                    new SessionTransitionExecutionPort(
                        continuityService,
                        ResolveGlobalOrFail<ISessionTransitionAdvancePhaseExecutionService>("ISessionTransitionAdvancePhaseExecutionService missing from global DI before execution port composition."),
                        ResolveGlobalOrFail<ISessionTransitionPhaseOrdinalNavigationExecutionService>("ISessionTransitionPhaseOrdinalNavigationExecutionService missing from global DI before execution port composition.")));
                DebugUtility.LogVerbose(typeof(SessionTransitionBootstrap),
                    "[OBS][GameplaySessionFlow][SessionTransition] ISessionTransitionExecutionPort registered in global DI.",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISessionTransitionGameplayPrepareExecutionPort>(out var existingGameplayPrepareExecutionPort) || existingGameplayPrepareExecutionPort == null)
            {
                DependencyManager.Provider.RegisterGlobal<ISessionTransitionGameplayPrepareExecutionPort>(
                    new SessionTransitionGameplayPrepareExecutionPort(
                        ResolveGlobalOrFail<IPhaseDefinitionSelectionService>("IPhaseDefinitionSelectionService missing from global DI before gameplay prepare execution port composition."),
                        ResolveGlobalOrFail<GameplayPhaseFlowService>("GameplayPhaseFlowService missing from global DI before gameplay prepare execution port composition."),
                        ResolveGlobalOrFail<ISceneCompositionExecutor>("ISceneCompositionExecutor missing from global DI before gameplay prepare execution port composition."),
                        ResolveGlobalOrFail<ISceneFlowRouteActorSetRefContext>("ISceneFlowRouteActorSetRefContext missing from global DI before gameplay prepare execution port composition."),
                        ResolveGlobalOrFail<IGameplayPhaseRuntimeService>("IGameplayPhaseRuntimeService missing from global DI before gameplay prepare execution port composition."),
                        ResolveGlobalOrFail<IGameplayParticipationFlowService>("IGameplayParticipationFlowService missing from global DI before gameplay prepare execution port composition."),
                        new WorldSpawnServiceRegistryReadPortProvider(DependencyManager.Provider)));

                DebugUtility.LogVerbose(typeof(SessionTransitionBootstrap),
                    "[OBS][GameplaySessionFlow][SessionTransition] ISessionTransitionGameplayPrepareExecutionPort registered in global DI.",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<SessionTransitionOrchestrator>(out var existingOrchestrator) || existingOrchestrator == null)
            {
                ISessionTransitionExecutionPort executionPort = ResolveGlobalOrFail<ISessionTransitionExecutionPort>(
                    "ISessionTransitionExecutionPort missing from global DI before SessionTransitionOrchestrator composition.");
                ISessionTransitionGameplayPrepareExecutionPort gameplayPrepareExecutionPort = ResolveGlobalOrFail<ISessionTransitionGameplayPrepareExecutionPort>(
                    "ISessionTransitionGameplayPrepareExecutionPort missing from global DI before SessionTransitionOrchestrator composition.");
                SessionTransitionPlanResolver planResolver = ResolveGlobalOrFail<SessionTransitionPlanResolver>(
                    "SessionTransitionPlanResolver missing from global DI before SessionTransitionOrchestrator composition.");
                ISceneFlowRouteActorSetRefContext routeActorSetContext = ResolveGlobalOrFail<ISceneFlowRouteActorSetRefContext>(
                    "ISceneFlowRouteActorSetRefContext missing from global DI before SessionTransitionOrchestrator composition.");
                IGameplayPhaseRuntimeService phaseRuntimeService = ResolveGlobalOrFail<IGameplayPhaseRuntimeService>(
                    "IGameplayPhaseRuntimeService missing from global DI before SessionTransitionOrchestrator composition.");
                IGameplayParticipationFlowService participationFlowService = ResolveGlobalOrFail<IGameplayParticipationFlowService>(
                    "IGameplayParticipationFlowService missing from global DI before SessionTransitionOrchestrator composition.");

                DependencyManager.Provider.RegisterGlobal(new SessionTransitionOrchestrator(
                    planResolver,
                    executionPort,
                    gameplayPrepareExecutionPort,
                    routeActorSetContext,
                    phaseRuntimeService,
                    participationFlowService));
                DebugUtility.LogVerbose(typeof(SessionTransitionBootstrap),
                    "[OBS][GameplaySessionFlow][SessionTransition] SessionTransitionOrchestrator registered in global DI.",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseOrdinalNavigationRequestService>(out var existingOrdinalNavigationRequestService) || existingOrdinalNavigationRequestService == null)
            {
                DependencyManager.Provider.RegisterGlobal<IPhaseOrdinalNavigationRequestService>(
                    new PhaseOrdinalNavigationRequestService(
                        ResolveGlobalOrFail<IRestartContextService>("IRestartContextService missing from global DI before phase ordinal navigation request service composition."),
                        ResolveGlobalOrFail<IPhaseCatalogNavigationService>("IPhaseCatalogNavigationService missing from global DI before phase ordinal navigation request service composition."),
                        ResolveGlobalOrFail<SessionTransitionOrchestrator>("SessionTransitionOrchestrator missing from global DI before phase ordinal navigation request service composition.")));
                DebugUtility.LogVerbose(typeof(SessionTransitionBootstrap),
                    "[OBS][QA][PhaseNavigation] IPhaseOrdinalNavigationRequestService registered in global DI.",
                    DebugUtility.Colors.Info);
            }

            if (!DependencyManager.Provider.TryGetGlobal<IRunContinuationOperationalHandoffService>(out var existingHandoff) || existingHandoff == null)
            {
                SessionTransitionOrchestrator orchestrator = ResolveGlobalOrFail<SessionTransitionOrchestrator>(
                    "SessionTransitionOrchestrator missing from global DI before run continuation handoff composition.");

                DependencyManager.Provider.RegisterGlobal<IRunContinuationOperationalHandoffService>(
                    new RunContinuationOperationalHandoffService(orchestrator));

                DebugUtility.LogVerbose(typeof(SessionTransitionBootstrap),
                    "[OBS][GameplaySessionFlow][SessionTransition] IRunContinuationOperationalHandoffService registered in global DI.",
                    DebugUtility.Colors.Info);
            }

            _runtimeComposed = true;
        }

        private static T ResolveGlobalOrFail<T>(string message) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionBootstrap),
                    "[FATAL][Config][SessionTransition] DependencyManager.Provider missing before session transition composition.");
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var value) || value == null)
            {
                HardFailFastH1.Trigger(typeof(SessionTransitionBootstrap),
                    $"[FATAL][Config][SessionTransition] {message}");
            }

            return value;
        }

    }
}
