#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.ContentContract;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.IntroStage.Eligibility;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.PhaseRuntime;

namespace _ImmersiveGames.NewScripts.SessionFlow.Host.IntroStage.PresenterExecution
{
    // Este coordinator controla a execução operacional da IntroStage.
    // O release final de gameplay continua acima, no GameLoop.
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageCoordinator : IIntroStageCoordinator
    {
        private const string SimulationGateToken = SimulationGateTokens.GameplaySimulation;

        private readonly object _sync = new();
        private string _activeSignature = string.Empty;
        private string _activeContextSignature = string.Empty;
        private int _activePhaseLocalEntrySequence;

        public async Task RunIntroStageAsync(IntroStageContext context)
        {
            if (!context.IsValid || !context.Session.IsValid)
            {
                HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                    "[FATAL][H1][GameLoop] Invalid intro context received by executor.");
            }

            IIntroStageControlService controlService = ResolveIntroStageControlServiceOrFail();
            IActorsGameplayOperationalReadinessService operationalReadinessService = ResolveOperationalReadinessServiceOrFail();
            IGameLoopService gameLoopService = ResolveGameLoopServiceOrFail();

            string signature = NormalizeSignature(context.ContextSignature);
            string executionSignature = NormalizeSignature(context.ExecutionSignature);
            int phaseLocalEntrySequence = context.Session.PhaseLocalEntrySequence;
            string entrySignature = NormalizeSignature(context.Session.EntrySignature);
            string routeLabel = FormatRouteKind(context.RouteKind);
            string targetScene = NormalizeValue(context.TargetScene);
            string reason = NormalizeReason(context.Reason);

            if (string.IsNullOrWhiteSpace(context.ExecutionSignature))
            {
                HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                    $"[FATAL][H1][GameLoop] IntroStage execution signature is required. contextSignature='{signature}' executionSignature='{executionSignature}' phaseLocalEntrySequence='{phaseLocalEntrySequence}' routeKind='{routeLabel}' target='{targetScene}' reason='{reason}'.");
            }

            IDisposable? gateLease = null;
            Exception? fatalIntroFailure = null;
            bool introResolved = false;
            GameplayStartReadyIntroStageStatus introStageStatus = GameplayStartReadyIntroStageStatus.Unknown;
            ActorsGameplayOperationalReadinessSnapshot currentReadinessSnapshot = default;
            bool hasCurrentReadinessSnapshot = false;
            bool gameplayOperationalReady = false;
            bool gameLoopStartRequested = false;
            bool releaseRequested = false;
            object coordinationSync = new object();
            TaskCompletionSource<bool> startReleaseSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Action<ActorsGameplayOperationalReadinessSnapshot>? readinessChangedHandler = null;

            bool MatchesGameplayStartContext(ActorsGameplayOperationalReadinessSnapshot readinessSnapshot, out string mismatchReason)
            {
                mismatchReason = string.Empty;

                if (!readinessSnapshot.HasCurrentContext)
                {
                    mismatchReason = "missing_current_context";
                    return false;
                }

                if (!readinessSnapshot.HasCanonicalPayload)
                {
                    mismatchReason = "missing_canonical_payload";
                    return false;
                }

                if (!readinessSnapshot.IsGameplayOperationalReady)
                {
                    mismatchReason = "actors_not_operational_ready";
                    return false;
                }

                if (!readinessSnapshot.RouteId.IsValid)
                {
                    mismatchReason = "missing_route_id";
                    return false;
                }

                if (readinessSnapshot.RouteKind != SceneRouteKind.Gameplay || readinessSnapshot.RouteKind != context.RouteKind)
                {
                    mismatchReason = "route_kind_mismatch";
                    return false;
                }

                if (!string.Equals(readinessSnapshot.SceneName, targetScene, StringComparison.Ordinal))
                {
                    mismatchReason = "scene_mismatch";
                    return false;
                }

                if (!string.Equals(readinessSnapshot.SessionSignature, signature, StringComparison.Ordinal))
                {
                    mismatchReason = "session_signature_mismatch";
                    return false;
                }

                if (!string.Equals(readinessSnapshot.PhaseSignature, context.Session.PhaseRuntimeSignature, StringComparison.Ordinal))
                {
                    mismatchReason = "phase_runtime_signature_mismatch";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(readinessSnapshot.ParticipationSignature))
                {
                    mismatchReason = "missing_participation_signature";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(readinessSnapshot.ActorSetRef))
                {
                    mismatchReason = "missing_actor_set_ref";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(readinessSnapshot.CycleSignature))
                {
                    mismatchReason = "missing_cycle_signature";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(context.Session.EntrySignature))
                {
                    mismatchReason = "missing_entry_signature";
                    return false;
                }

                return true;
            }

            GameplayStartReadySnapshot BuildGameplayStartReadySnapshot(
                ActorsGameplayOperationalReadinessSnapshot readinessSnapshot,
                GameplayStartReadyIntroStageStatus status)
            {
                return new GameplayStartReadySnapshot(
                    readinessSnapshot.SessionSignature,
                    readinessSnapshot.RouteId,
                    readinessSnapshot.RouteKind,
                    readinessSnapshot.SceneName,
                    readinessSnapshot.ActorSetRef,
                    readinessSnapshot.CycleSignature,
                    readinessSnapshot.PhaseSignature,
                    readinessSnapshot.ParticipationSignature,
                    status,
                    readinessSnapshot.IsGameplayOperationalReady,
                    GameplayStartReadyReasonKind.Ready,
                    "IntroStageDone+ActorsOperationalReady");
            }

            if (!TryEnterContext(signature, executionSignature, phaseLocalEntrySequence, reason))
            {
                return;
            }

            try
            {
                readinessChangedHandler = snapshot =>
                {
                    if (!snapshot.HasCurrentContext)
                    {
                        return;
                    }

                    bool shouldRequestStart = false;
                    GameplayStartReadySnapshot readinessGameplayStartReadySnapshot = GameplayStartReadySnapshot.Empty;
                    lock (coordinationSync)
                    {
                        currentReadinessSnapshot = snapshot;
                        hasCurrentReadinessSnapshot = true;
                        gameplayOperationalReady = snapshot.IsGameplayOperationalReady;
                        if (!gameplayOperationalReady)
                        {
                            return;
                        }

                        if (!MatchesGameplayStartContext(snapshot, out string readinessMismatchReason))
                        {
                            LogPendingStart(signature, routeLabel, targetScene, readinessMismatchReason);
                            return;
                        }

                        if (!introResolved)
                        {
                            LogPendingStart(signature, routeLabel, targetScene, "waiting_for_intro_stage");
                            return;
                        }

                        if (gameLoopStartRequested)
                        {
                            return;
                        }

                        gameLoopStartRequested = true;
                        readinessGameplayStartReadySnapshot = BuildGameplayStartReadySnapshot(
                            snapshot,
                            introStageStatus);
                        shouldRequestStart = true;
                    }

                    if (shouldRequestStart)
                    {
                        LogGameplayStartReady(
                            readinessGameplayStartReadySnapshot,
                            signature,
                            executionSignature,
                            phaseLocalEntrySequence,
                            entrySignature,
                            reason,
                            "actors_readiness_callback",
                            "matched");
                        ReleaseGameLoopStart(
                            signature,
                            executionSignature,
                            phaseLocalEntrySequence,
                            entrySignature,
                            readinessGameplayStartReadySnapshot.CycleSignature,
                            routeLabel,
                            targetScene,
                            reason,
                            "actors_readiness_callback",
                            "GameplayStartReady",
                            gameLoopService);
                        releaseRequested = true;
                        startReleaseSource.TrySetResult(true);
                    }
                };

                operationalReadinessService.Changed += readinessChangedHandler;

                if (operationalReadinessService.TryGetCurrent(out ActorsGameplayOperationalReadinessSnapshot currentReadiness))
                {
                    lock (coordinationSync)
                    {
                        currentReadinessSnapshot = currentReadiness;
                        hasCurrentReadinessSnapshot = currentReadiness.HasCurrentContext;
                        gameplayOperationalReady = currentReadiness.IsGameplayOperationalReady;
                        if (!introResolved)
                        {
                            if (gameplayOperationalReady)
                            {
                                LogPendingStart(signature, routeLabel, targetScene, "waiting_for_intro_stage");
                            }
                        }
                    }
                }

                DebugUtility.Log<IntroStageCoordinator>(
                    $"[OBS][IntroStageCoordinator] IntroStageStarted contextSignature='{signature}' executionSignature='{executionSignature}' phaseLocalEntrySequence='{phaseLocalEntrySequence}' routeKind='{routeLabel}' target='{targetScene}' reason='{reason}' hasIntroStage='{context.Session.HasIntroStage}'.",
                    DebugUtility.Colors.Info);

                if (!context.HasIntroStage)
                {
                    LogSkipped("no_content", context);
                    LogCompletion(signature, targetScene, routeLabel, IntroStageRunResult.Skipped);
                    PublishNoContentCompletion(context);

                    bool shouldRequestStart = false;
                    GameplayStartReadySnapshot noContentGameplayStartReadySnapshot = GameplayStartReadySnapshot.Empty;
                    lock (coordinationSync)
                    {
                        introResolved = true;
                        introStageStatus = GameplayStartReadyIntroStageStatus.NoContent;
                        string noContentMismatchReason = string.Empty;
                        bool noContentMatchesContext = hasCurrentReadinessSnapshot &&
                            MatchesGameplayStartContext(currentReadinessSnapshot, out noContentMismatchReason);

                        if (gameplayOperationalReady &&
                            noContentMatchesContext &&
                            !gameLoopStartRequested)
                        {
                            gameLoopStartRequested = true;
                            noContentGameplayStartReadySnapshot = BuildGameplayStartReadySnapshot(
                                currentReadinessSnapshot,
                                introStageStatus);
                            shouldRequestStart = true;
                        }
                        else if (!gameplayOperationalReady)
                        {
                            LogPendingStart(signature, routeLabel, targetScene, "waiting_for_actors_operational_ready");
                        }
                        else if (hasCurrentReadinessSnapshot && !noContentMatchesContext)
                        {
                            LogPendingStart(signature, routeLabel, targetScene, noContentMismatchReason);
                        }
                    }

                    if (shouldRequestStart)
                    {
                        LogGameplayStartReady(
                            noContentGameplayStartReadySnapshot,
                            signature,
                            executionSignature,
                            phaseLocalEntrySequence,
                            entrySignature,
                            reason,
                            "no_content_immediate",
                            "matched");
                        ReleaseGameLoopStart(
                            signature,
                            executionSignature,
                            phaseLocalEntrySequence,
                            entrySignature,
                            noContentGameplayStartReadySnapshot.CycleSignature,
                            routeLabel,
                            targetScene,
                            reason,
                            "no_content_immediate",
                            "GameplayStartReady",
                            gameLoopService);
                        releaseRequested = true;
                        startReleaseSource.TrySetResult(true);
                    }

                    await startReleaseSource.Task.ConfigureAwait(false);
                    return;
                }

                gateLease = AcquireSimulationGateOrFail(signature, routeLabel, targetScene, reason);

                controlService.BeginIntroStage(context);
                Task<IntroStageCompletionResult> completionTask = WaitForCompletionAsync(context, CancellationToken.None);

                DebugUtility.Log<IntroStageCoordinator>(
                    "[OBS][IntroStageCoordinator] IntroStage active: gameplay simulation blocked; operational intro gate held until confirmation.",
                    DebugUtility.Colors.Info);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                DebugUtility.LogVerbose<IntroStageCoordinator>(
                    "[QA][IntroStageCoordinator] EditorQAActions available for Complete/Skip in Editor/Dev.",
                    DebugUtility.Colors.Info);
#endif

                var completion = await completionTask.ConfigureAwait(false);

                if (IsSupersededCompletion(completion))
                {
                    DebugUtility.LogVerbose<IntroStageCoordinator>(
                        $"[OBS][IntroStageCoordinator] IntroStageSuperseded signature='{signature}' routeKind='{routeLabel}' target='{targetScene}'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                if (completion.WasSkipped)
                {
                    string skipReason = NormalizeValue(completion.Reason);
                    LogSkipped(skipReason, context);
                    LogCompletion(signature, targetScene, routeLabel, IntroStageRunResult.Skipped);
                }
                else
                {
                    LogCompletion(signature, targetScene, routeLabel, IntroStageRunResult.Completed);
                }

                bool shouldReleaseNow = false;
                GameplayStartReadySnapshot completedGameplayStartReadySnapshot = GameplayStartReadySnapshot.Empty;
                lock (coordinationSync)
                {
                    introResolved = true;
                    introStageStatus = ResolveIntroStageStatus(completion);
                    string completedMismatchReason = string.Empty;
                    bool completedMatchesContext = hasCurrentReadinessSnapshot &&
                        MatchesGameplayStartContext(currentReadinessSnapshot, out completedMismatchReason);

                    if (gameplayOperationalReady &&
                        completedMatchesContext &&
                        !gameLoopStartRequested)
                    {
                        gameLoopStartRequested = true;
                        completedGameplayStartReadySnapshot = BuildGameplayStartReadySnapshot(
                            currentReadinessSnapshot,
                            introStageStatus);
                        shouldReleaseNow = true;
                    }
                    else if (!gameplayOperationalReady)
                    {
                        LogPendingStart(signature, routeLabel, targetScene, "waiting_for_actors_operational_ready");
                    }
                    else if (hasCurrentReadinessSnapshot && !completedMatchesContext)
                    {
                        LogPendingStart(signature, routeLabel, targetScene, completedMismatchReason);
                    }
                }

                if (shouldReleaseNow)
                {
                    LogGameplayStartReady(
                        completedGameplayStartReadySnapshot,
                        signature,
                        executionSignature,
                        phaseLocalEntrySequence,
                        entrySignature,
                        reason,
                        "intro_completion_immediate",
                        "matched");
                    ReleaseGameLoopStart(
                        signature,
                        executionSignature,
                        phaseLocalEntrySequence,
                        entrySignature,
                        completedGameplayStartReadySnapshot.CycleSignature,
                        routeLabel,
                        targetScene,
                        reason,
                        "intro_completion_immediate",
                        "GameplayStartReady",
                        gameLoopService);
                    releaseRequested = true;
                    startReleaseSource.TrySetResult(true);
                }

                await startReleaseSource.Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                fatalIntroFailure = ex;
                DebugUtility.LogWarning<IntroStageCoordinator>(
                    $"[IntroStageCoordinator] Falha ao executar IntroStage. contextSignature='{signature}' executionSignature='{executionSignature}' phaseLocalEntrySequence='{phaseLocalEntrySequence}' ex='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                if (readinessChangedHandler != null)
                {
                    operationalReadinessService.Changed -= readinessChangedHandler;
                }

                if (gateLease != null)
                {
                    gateLease.Dispose();
                    DebugUtility.Log<IntroStageCoordinator>(
                        $"[OBS][IntroStageCoordinator] GameplaySimulationUnblocked token='{SimulationGateTokens.GameplaySimulation}' contextSignature='{signature}' executionSignature='{executionSignature}' phaseLocalEntrySequence='{phaseLocalEntrySequence}' routeKind='{routeLabel}' target='{targetScene}' reason='{reason}' (intro gate released).",
                        DebugUtility.Colors.Info);
                }

                if (!releaseRequested && fatalIntroFailure == null)
                {
                    // Não libera start sozinho; apenas registra o estado para debug.
                    lock (coordinationSync)
                    {
                        if (introResolved && !gameplayOperationalReady)
                        {
                            LogPendingStart(signature, routeLabel, targetScene, "waiting_for_actors_operational_ready");
                        }
                    }
                }

                controlService.MarkSessionClosed();
                ReleaseContext(executionSignature);
            }

            if (fatalIntroFailure != null)
            {
                HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                    $"[FATAL][H1][GameLoop] IntroStage execution failed. contextSignature='{signature}' executionSignature='{executionSignature}' phaseLocalEntrySequence='{phaseLocalEntrySequence}' routeKind='{routeLabel}' target='{targetScene}' reason='{reason}' ex='{fatalIntroFailure.GetType().Name}: {fatalIntroFailure.Message}'.",
                    fatalIntroFailure);
            }
        }

        private static IIntroStageControlService ResolveIntroStageControlServiceOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) && service != null)
            {
                return service;
            }

            HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                "[FATAL][H1][GameLoop] IIntroStageControlService obrigatorio ausente para executar IntroStage.");

            throw new InvalidOperationException("IIntroStageControlService is required.");
        }

        private static IActorsGameplayOperationalReadinessService ResolveOperationalReadinessServiceOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<IActorsGameplayOperationalReadinessService>(out var service) && service != null)
            {
                return service;
            }

            HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                "[FATAL][H1][GameLoop] IActorsGameplayOperationalReadinessService obrigatorio ausente para coordenar o release do GameLoop.");

            throw new InvalidOperationException("IActorsGameplayOperationalReadinessService is required.");
        }

        private static IGameLoopService ResolveGameLoopServiceOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoopService) && gameLoopService != null)
            {
                return gameLoopService;
            }

            HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                "[FATAL][H1][GameLoop] IGameLoopService obrigatorio ausente para liberar o start operacional.");

            throw new InvalidOperationException("IGameLoopService is required.");
        }

        private static IDisposable AcquireSimulationGateOrFail(
            string signature,
            string routeKind,
            string targetScene,
            string reason)
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISimulationGateService>(out var gateService) || gateService == null)
            {
                HardFailFastH1.Trigger(typeof(IntroStageCoordinator),
                    $"[FATAL][H1][GameLoop] ISimulationGateService obrigatorio ausente para bloquear IntroStage. signature='{signature}' routeKind='{routeKind}' target='{targetScene}' reason='{reason}'.");
            }

            IDisposable lease = gateService!.Acquire(SimulationGateTokens.GameplaySimulation);

            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageCoordinator] GameplaySimulationBlocked token='{SimulationGateTokens.GameplaySimulation}' contextSignature='{signature}' routeKind='{routeKind}' target='{targetScene}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return lease;
        }

        private bool TryEnterContext(
            string contextSignature,
            string executionSignature,
            int phaseLocalEntrySequence,
            string reason)
        {
            lock (_sync)
            {
                if (string.Equals(_activeSignature, executionSignature, StringComparison.Ordinal))
                {
                    DebugUtility.LogWarning<IntroStageCoordinator>(
                        $"[OBS][IntroStageCoordinator] IntroStageSkipped reason='duplicate_same_execution_in_progress' contextSignature='{contextSignature}' executionSignature='{executionSignature}' phaseLocalEntrySequence='{phaseLocalEntrySequence}' activeContextSignature='{_activeContextSignature}' activeExecutionSignature='{_activeSignature}' activePhaseLocalEntrySequence='{_activePhaseLocalEntrySequence}' reasonInput='{reason}'.");
                    return false;
                }

                if (string.Equals(_activeContextSignature, contextSignature, StringComparison.Ordinal))
                {
                    DebugUtility.Log<IntroStageCoordinator>(
                        $"[OBS][IntroStageCoordinator] IntroStageExecutionAccepted reason='new_execution_same_session' contextSignature='{contextSignature}' executionSignature='{executionSignature}' phaseLocalEntrySequence='{phaseLocalEntrySequence}' previousExecutionSignature='{_activeSignature}' previousPhaseLocalEntrySequence='{_activePhaseLocalEntrySequence}' reasonInput='{reason}'.",
                        DebugUtility.Colors.Info);
                }
                else
                {
                    DebugUtility.Log<IntroStageCoordinator>(
                        $"[OBS][IntroStageCoordinator] IntroStageExecutionAccepted reason='new_execution' contextSignature='{contextSignature}' executionSignature='{executionSignature}' phaseLocalEntrySequence='{phaseLocalEntrySequence}' previousContextSignature='{_activeContextSignature}' previousExecutionSignature='{_activeSignature}' previousPhaseLocalEntrySequence='{_activePhaseLocalEntrySequence}' reasonInput='{reason}'.",
                        DebugUtility.Colors.Info);
                }

                _activeContextSignature = contextSignature;
                _activeSignature = executionSignature;
                _activePhaseLocalEntrySequence = phaseLocalEntrySequence;
                return true;
            }
        }

        private void ReleaseContext(string signature)
        {
            lock (_sync)
            {
                if (string.Equals(_activeSignature, signature, StringComparison.Ordinal))
                {
                    _activeSignature = string.Empty;
                    _activeContextSignature = string.Empty;
                    _activePhaseLocalEntrySequence = 0;
                }
            }
        }

        private static async Task<IntroStageCompletionResult> WaitForCompletionAsync(
            IntroStageContext context,
            CancellationToken cancellationToken)
        {
            var completionSource = new TaskCompletionSource<IntroStageCompletionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            EventBinding<IntroStageCompletedEvent> binding = null!;
            binding = new EventBinding<IntroStageCompletedEvent>(evt =>
            {
                if (!evt.Session.IsValid)
                {
                    return;
                }

                if (!string.Equals(evt.Session.SessionSignature, context.ContextSignature, StringComparison.Ordinal))
                {
                    return;
                }

                if (!string.Equals(evt.Session.EntrySignature, context.ExecutionSignature, StringComparison.Ordinal))
                {
                    return;
                }

                completionSource.TrySetResult(new IntroStageCompletionResult(evt.Reason, evt.WasSkipped));
            });

            EventBus<IntroStageCompletedEvent>.Register(binding);

            try
            {
                if (cancellationToken.CanBeCanceled)
                {
                    using var registration = cancellationToken.Register(() => completionSource.TrySetCanceled(cancellationToken));
                    return await completionSource.Task.ConfigureAwait(false);
                }

                return await completionSource.Task.ConfigureAwait(false);
            }
            finally
            {
                EventBus<IntroStageCompletedEvent>.Unregister(binding);
            }
        }

        private static bool IsSupersededCompletion(IntroStageCompletionResult completion)
            => completion.WasSkipped &&
               string.Equals(NormalizeValue(completion.Reason), "superseded", StringComparison.OrdinalIgnoreCase);

        private static string FormatRouteKind(SceneRouteKind routeKind)
            => routeKind.ToString();

        private static string NormalizeSignature(string signature)
            => string.IsNullOrWhiteSpace(signature) ? "<none>" : signature.Trim();

        private static string NormalizeReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "n/a" : reason.Trim();

        private static string NormalizeValue(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private static void LogCompletion(string signature, string targetScene, string routeKind, IntroStageRunResult result)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageCoordinator] IntroStageCompleted signature='{signature}' result='{FormatResult(result)}' routeKind='{routeKind}' target='{targetScene}'.",
                DebugUtility.Colors.Info);
        }

        private static void PublishNoContentCompletion(IntroStageContext context)
        {
            string canonicalSource = PhaseFlowSignalVocabulary.CanonicalizeCompletionSource(PhaseFlowSignalVocabulary.GameplaySessionFlowSource);
            string canonicalReason = PhaseFlowSignalVocabulary.CanonicalizeCompletionReason(PhaseFlowSignalVocabulary.NoContentReason, wasSkipped: true);

            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageCoordinator] IntroStageCompletedPublished owner='IntroStageCoordinator' source='{canonicalSource}' sessionSignature='{NormalizeSignature(context.Session.SessionSignature)}' phaseRuntimeSignature='{NormalizeSignature(context.Session.PhaseRuntimeSignature)}' entrySignature='{NormalizeSignature(context.Session.EntrySignature)}' skipped='true' reason='{canonicalReason}'.",
                DebugUtility.Colors.Info);

            EventBus<IntroStageCompletedEvent>.Raise(new IntroStageCompletedEvent(
                context.Session,
                canonicalSource,
                wasSkipped: true,
                canonicalReason));
        }

        private static void LogSkipped(string reason, IntroStageContext context)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageCoordinator] IntroStageSkipped reason='{reason}' contextSignature='{NormalizeSignature(context.ContextSignature)}' executionSignature='{NormalizeSignature(context.ExecutionSignature)}' phaseLocalEntrySequence='{context.Session.PhaseLocalEntrySequence}' routeKind='{FormatRouteKind(context.RouteKind)}' target='{NormalizeValue(context.TargetScene)}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogPendingStart(string signature, string routeKind, string targetScene, string pendingReason)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageCoordinator] GameLoopStartPending signature='{signature}' routeKind='{routeKind}' target='{targetScene}' pendingStartReason='{pendingReason}'.",
                DebugUtility.Colors.Info);
        }

        private static void ReleaseGameLoopStart(
            string contextSignature,
            string executionSignature,
            int phaseLocalEntrySequence,
            string entrySignature,
            string cycleSignature,
            string routeKind,
            string targetScene,
            string reason,
            string sourcePath,
            string releaseReason,
            IGameLoopService gameLoopService)
        {
            bool alreadyPlaying = string.Equals(gameLoopService.CurrentStateIdName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal);

            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageCoordinator] GameLoopStartReleased signature='{contextSignature}' contextSignature='{contextSignature}' executionSignature='{executionSignature}' phaseLocalEntrySequence='{phaseLocalEntrySequence}' entrySignature='{entrySignature}' cycleSignature='{NormalizeSignature(cycleSignature)}' routeKind='{routeKind}' target='{targetScene}' reason='{reason}' releaseReason='{releaseReason}' sourcePath='{sourcePath}' alreadyPlaying='{alreadyPlaying.ToString().ToLowerInvariant()}'.",
                DebugUtility.Colors.Info);

            if (alreadyPlaying)
            {
                DebugUtility.Log<IntroStageCoordinator>(
                    $"[OBS][IntroStageCoordinator] GameLoopStartRequestSkipped reason='already_playing' contextSignature='{contextSignature}' executionSignature='{executionSignature}' phaseLocalEntrySequence='{phaseLocalEntrySequence}' routeKind='{routeKind}' target='{targetScene}' sourcePath='{sourcePath}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            gameLoopService.RequestStart();
        }

        private static void LogGameplayStartReady(
            GameplayStartReadySnapshot snapshot,
            string contextSignature,
            string executionSignature,
            int phaseLocalEntrySequence,
            string entrySignature,
            string reason,
            string sourcePath,
            string actorsReadinessMatchStatus)
        {
            DebugUtility.Log<IntroStageCoordinator>(
                $"[OBS][IntroStageCoordinator] GameplayStartReady isReady='{snapshot.IsReady.ToString().ToLowerInvariant()}' reason='{reason}' readinessReason='{snapshot.ReadinessReason}' readinessReasonKind='{snapshot.ReadinessReasonKind}' contextSignature='{contextSignature}' executionSignature='{executionSignature}' phaseLocalEntrySequence='{phaseLocalEntrySequence}' entrySignature='{entrySignature}' sessionSignature='{snapshot.SessionSignature}' routeId='{snapshot.RouteId}' routeKind='{snapshot.RouteKind}' scene='{snapshot.SceneName}' actorSetRef='{snapshot.ActorSetRef}' cycleSignature='{snapshot.CycleSignature}' phaseRuntimeSignature='{snapshot.PhaseRuntimeSignature}' participationSignature='{snapshot.ParticipationSignature}' introStageStatus='{snapshot.IntroStageStatus}' actorsOperationalReady='{snapshot.ActorsOperationalReady.ToString().ToLowerInvariant()}' actorsReadinessMatchStatus='{actorsReadinessMatchStatus}' hasCanonicalPayload='{snapshot.HasCanonicalPayload.ToString().ToLowerInvariant()}' sourcePath='{sourcePath}'.",
                DebugUtility.Colors.Info);
        }

        private static string FormatResult(IntroStageRunResult result)
        {
            return result switch
            {
                IntroStageRunResult.Completed => "completed",
                IntroStageRunResult.Skipped => "skipped",
                _ => "unknown"
            };
        }

        private enum IntroStageRunResult
        {
            Completed,
            Skipped
        }

        private static GameplayStartReadyIntroStageStatus ResolveIntroStageStatus(IntroStageCompletionResult completion)
        {
            if (!completion.WasSkipped)
            {
                return GameplayStartReadyIntroStageStatus.Completed;
            }

            return string.Equals(NormalizeValue(completion.Reason), PhaseFlowSignalVocabulary.NoContentReason, StringComparison.OrdinalIgnoreCase)
                ? GameplayStartReadyIntroStageStatus.NoContent
                : GameplayStartReadyIntroStageStatus.Skipped;
        }
    }
}
