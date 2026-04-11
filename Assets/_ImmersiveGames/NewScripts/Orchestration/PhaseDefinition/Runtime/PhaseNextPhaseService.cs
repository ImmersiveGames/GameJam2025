using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.IntroStage.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.LevelLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneComposition;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Transition.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PhaseNextPhaseService : IPhaseNextPhaseService
    {
        private readonly SemaphoreSlim _gate = new(1, 1);

        public async Task NextPhaseAsync(string reason = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string normalizedReason = NormalizeReason(reason);

            await _gate.WaitAsync(ct);
            try
            {
                GameplayStartSnapshot currentSnapshot = ResolveCurrentGameplaySnapshotOrFail(normalizedReason);
                IPhaseDefinitionCatalog catalog = ResolveRequiredGlobal<IPhaseDefinitionCatalog>("IPhaseDefinitionCatalog");
                if (!catalog.TryGetNext(currentSnapshot.PhaseDefinitionRef.PhaseId.Value, out PhaseDefinitionAsset nextPhaseRef) ||
                    nextPhaseRef == null)
                {
                    DebugUtility.LogWarning(typeof(PhaseNextPhaseService),
                        $"[WARN][GameplaySessionFlow][PhaseDefinition] PhaseNextUnavailable current='{currentSnapshot.PhaseDefinitionRef.PhaseId}' reason='end_of_catalog'.");
                    return;
                }

                string currentPhaseId = currentSnapshot.PhaseDefinitionRef.PhaseId.Value;
                string currentPhaseName = currentSnapshot.PhaseDefinitionRef.name;
                string nextPhaseId = nextPhaseRef.PhaseId.Value;
                string nextPhaseName = nextPhaseRef.name;
                string targetSceneName = SceneManager.GetActiveScene().name;
                string nextIntroContentId = nextPhaseRef.BuildCanonicalIntroContentId();

                int nextSelectionVersion = Math.Max(currentSnapshot.SelectionVersion + 1, 1);
                PhaseDefinitionSelectedEvent phaseSelectedEvent = new PhaseDefinitionSelectedEvent(
                    nextPhaseRef,
                    currentSnapshot.MacroRouteId,
                    currentSnapshot.MacroRouteRef,
                    nextSelectionVersion,
                    normalizedReason);

                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseNextPhaseOwnerConfirmed owner='PhaseNextPhaseService' currentPhaseId='{currentPhaseId}' nextPhaseId='{nextPhaseId}' routeId='{currentSnapshot.MacroRouteId}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseNextPhaseSelected currentPhaseRef='{currentPhaseName}' nextPhaseRef='{nextPhaseName}' routeId='{currentSnapshot.MacroRouteId}' v='{nextSelectionVersion}' reason='{normalizedReason}' signature='{phaseSelectedEvent.SelectionSignature}'.",
                    DebugUtility.Colors.Info);

                SyncRestartContextFromPhaseSelection(phaseSelectedEvent);
                EventBus<PhaseDefinitionSelectedEvent>.Raise(phaseSelectedEvent);

                ISceneCompositionExecutor sceneCompositionExecutor = ResolveRequiredGlobal<ISceneCompositionExecutor>("ISceneCompositionExecutor");
                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentAppliedExecutorConfirmed executor='SceneCompositionExecutor' owner='PhaseNextPhaseService' routeId='{currentSnapshot.MacroRouteId}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                SceneCompositionRequest applyRequest = PhaseDefinitionSceneCompositionRequestFactory.CreateApplyRequest(
                    nextPhaseRef,
                    normalizedReason,
                    phaseSelectedEvent.SelectionSignature);

                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentAppliedStart from='{currentPhaseId}' to='{nextPhaseId}' scenesToLoad=[{string.Join(",", applyRequest.ScenesToLoad)}] scenesToUnload=[{string.Join(",", applyRequest.ScenesToUnload)}] correlationId='{applyRequest.CorrelationId}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                await sceneCompositionExecutor.ApplyAsync(applyRequest, ct);

                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentCleared phaseId='{currentPhaseId}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseLegacyBridgeBypassed path='phase_enabled_next_phase' routeId='{currentSnapshot.MacroRouteId}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                PhaseContentSceneRuntimeApplier.RecordAppliedPhaseDefinition(
                    nextPhaseRef,
                    applyRequest.ScenesToLoad,
                    applyRequest.ActiveScene,
                    source: "PhaseDefinitionNextPhase");

                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentReadModelCommitted owner='PhaseContentSceneRuntimeApplier' phaseId='{nextPhaseId}' routeId='{currentSnapshot.MacroRouteId}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentApplied phaseId='{nextPhaseId}' routeId='{currentSnapshot.MacroRouteId}' v='{nextSelectionVersion}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Success);

                GameplayPhaseRuntimeSnapshot nextPhaseRuntime = GameplayPhaseRuntimeSnapshot.FromPhaseDefinitionSelectedEvent(phaseSelectedEvent);
                IntroStageSession introSession = nextPhaseRuntime.CreateIntroStageSession(
                    nextIntroContentId,
                    normalizedReason,
                    nextSelectionVersion,
                    phaseSelectedEvent.SelectionSignature);
                using var introWaitCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                Task<IntroStageCompletedEvent> introCompletionTask = WaitForIntroCompletionAsync(
                    introSession.SessionSignature,
                    introWaitCts.Token);

                IntroStageEntryEvent introStageEntryEvent = new IntroStageEntryEvent(
                    introSession,
                    "GameplaySessionFlow",
                    currentSnapshot.MacroRouteRef.RouteKind);

                if (introSession.HasIntroStage)
                {
                    DebugUtility.Log<PhaseNextPhaseService>(
                        $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseNextPhaseIntroQueued nextPhaseRef='{nextPhaseName}' routeId='{currentSnapshot.MacroRouteId}' v='{nextSelectionVersion}' reason='{normalizedReason}' signature='{introSession.SessionSignature}'.",
                        DebugUtility.Colors.Info);
                }
                else
                {
                    DebugUtility.Log<PhaseNextPhaseService>(
                        $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseNextPhaseIntroSkipped nextPhaseRef='{nextPhaseName}' routeId='{currentSnapshot.MacroRouteId}' v='{nextSelectionVersion}' reason='{normalizedReason}' signature='{introSession.SessionSignature}' skipReason='no_concrete_intro'.",
                        DebugUtility.Colors.Info);
                }

                EventBus<IntroStageEntryEvent>.Raise(introStageEntryEvent);

                EventBus<SceneTransitionCompletedEvent>.Raise(new SceneTransitionCompletedEvent(
                    BuildSyntheticRevealContext(
                        currentSnapshot,
                        applyRequest,
                        normalizedReason,
                        phaseSelectedEvent.SelectionSignature,
                        targetSceneName)));

                IntroStageCompletedEvent introCompletedEvent = await introCompletionTask;

                DebugUtility.Log<PhaseNextPhaseService>(
                    $"[OBS][GameplaySessionFlow][PhaseDefinition] PhaseContentAppliedCompleted current='{nextPhaseId}' routeId='{currentSnapshot.MacroRouteId}' v='{nextSelectionVersion}' reason='{normalizedReason}' introSkipped='{introCompletedEvent.WasSkipped.ToString().ToLowerInvariant()}' introReason='{NormalizeReason(introCompletedEvent.Reason)}' source='{introCompletedEvent.Source}'.",
                    DebugUtility.Colors.Info);
            }
            finally
            {
                _gate.Release();
            }
        }

        private static GameplayStartSnapshot ResolveCurrentGameplaySnapshotOrFail(string reason)
        {
            if (!TryResolveGlobal<IRestartContextService>(out var restartContextService) ||
                restartContextService == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] IRestartContextService ausente ao executar NextPhase. reason='{reason}'.");
            }

            if (!restartContextService.TryGetCurrent(out GameplayStartSnapshot currentSnapshot) ||
                !currentSnapshot.IsValid ||
                !currentSnapshot.HasPhaseDefinitionRef ||
                currentSnapshot.PhaseDefinitionRef == null ||
                !currentSnapshot.PhaseDefinitionRef.PhaseId.IsValid ||
                currentSnapshot.MacroRouteRef == null ||
                !currentSnapshot.MacroRouteId.IsValid ||
                currentSnapshot.MacroRouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] NextPhase requer um gameplay snapshot valido. reason='{reason}'.");
            }

            return currentSnapshot;
        }

        private static SceneTransitionContext BuildSyntheticRevealContext(
            GameplayStartSnapshot currentSnapshot,
            SceneCompositionRequest applyRequest,
            string reason,
            string selectionSignature,
            string targetSceneName)
        {
            return new SceneTransitionContext(
                applyRequest.ScenesToLoad,
                applyRequest.ScenesToUnload,
                targetSceneName,
                useFade: false,
                currentSnapshot.MacroRouteId,
                currentSnapshot.MacroRouteRef.RouteKind,
                transitionStyle: null,
                reason,
                transitionProfile: null,
                routeRef: currentSnapshot.MacroRouteRef,
                requiresWorldReset: false,
                resetDecisionSource: "phase-local-next-phase",
                resetDecisionReason: "phase-local-reveal-completed",
                contextSignature: selectionSignature);
        }

        private static bool TryResolveGlobal<T>(out T service)
            where T : class
        {
            service = null;

            if (DependencyManager.Provider == null)
            {
                return false;
            }

            return DependencyManager.Provider.TryGetGlobal<T>(out service) && service != null;
        }

        private static T ResolveRequiredGlobal<T>(string label)
            where T : class
        {
            if (!TryResolveGlobal<T>(out var service) || service == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseService),
                    $"[FATAL][H1][GameplaySessionFlow][PhaseDefinition] Missing required global service: {label}.");
            }

            return service;
        }

        private static void SyncRestartContextFromPhaseSelection(PhaseDefinitionSelectedEvent phaseSelectedEvent)
        {
            if (!TryResolveGlobal<IRestartContextService>(out var restartContextService) || restartContextService == null)
            {
                HardFailFastH1.Trigger(typeof(PhaseNextPhaseService),
                    "[FATAL][H1][GameplaySessionFlow][PhaseDefinition] RestartContextService ausente durante publicacao phase-owned de PhaseDefinitionSelectedEvent.");
            }

            GameplayStartSnapshot gameplayStartSnapshot = new GameplayStartSnapshot(
                phaseSelectedEvent.PhaseDefinitionRef,
                phaseSelectedEvent.MacroRouteId,
                phaseSelectedEvent.MacroRouteRef,
                phaseSelectedEvent.PhaseDefinitionRef.BuildCanonicalIntroContentId(),
                phaseSelectedEvent.Reason,
                phaseSelectedEvent.SelectionVersion,
                phaseSelectedEvent.SelectionSignature);

            restartContextService.RegisterGameplayStart(gameplayStartSnapshot);

            DebugUtility.Log<PhaseNextPhaseService>(
                $"[OBS][GameplaySessionFlow][PhaseDefinition] GameplayStartSnapshotLinked owner='PhaseNextPhaseService' phaseId='{phaseSelectedEvent.PhaseId}' routeId='{phaseSelectedEvent.MacroRouteId}' v='{phaseSelectedEvent.SelectionVersion}' reason='{phaseSelectedEvent.Reason}' signature='{gameplayStartSnapshot.LevelSignature}'.",
                DebugUtility.Colors.Info);
        }

        private static async Task<IntroStageCompletedEvent> WaitForIntroCompletionAsync(
            string expectedSignature,
            CancellationToken ct)
        {
            var completionSource = new TaskCompletionSource<IntroStageCompletedEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
            EventBinding<IntroStageCompletedEvent> binding = null!;

            binding = new EventBinding<IntroStageCompletedEvent>(evt =>
            {
                if (!evt.Session.IsValid)
                {
                    return;
                }

                if (!string.Equals(evt.Source, "GameplaySessionFlow", StringComparison.Ordinal))
                {
                    return;
                }

                if (!string.Equals(evt.Session.SessionSignature, expectedSignature, StringComparison.Ordinal))
                {
                    return;
                }

                completionSource.TrySetResult(evt);
            });

            EventBus<IntroStageCompletedEvent>.Register(binding);

            CancellationTokenRegistration cancellationRegistration = default;

            try
            {
                if (ct.CanBeCanceled)
                {
                    cancellationRegistration = ct.Register(() => completionSource.TrySetCanceled(ct));
                }

                return await completionSource.Task;
            }
            finally
            {
                cancellationRegistration.Dispose();
                EventBus<IntroStageCompletedEvent>.Unregister(binding);
            }
        }

        private static string NormalizeReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "PhaseDefinition/NextPhase" : reason.Trim();
    }
}
