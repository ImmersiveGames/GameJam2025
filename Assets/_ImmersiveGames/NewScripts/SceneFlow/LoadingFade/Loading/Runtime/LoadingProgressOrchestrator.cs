using System;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;
using ImmersiveGames.GameJam2025.Core.Events;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Orchestration.PhaseDefinition.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Navigation.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Transition.Runtime;
using ImmersiveGames.GameJam2025.Orchestration.WorldReset.Contracts;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneFlow.Loading.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LoadingProgressOrchestrator : IDisposable
    {
        private readonly EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private readonly EventBinding<SceneTransitionBeforeFadeOutEvent> _beforeFadeOutBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _completedBinding;
        private readonly EventBinding<SceneFlowRouteLoadingProgressEvent> _routeProgressBinding;
        private readonly EventBinding<WorldResetStartedEvent> _resetStartedBinding;
        private readonly EventBinding<WorldResetCompletedEvent> _resetCompletedBinding;
        private readonly EventBinding<PhaseDefinitionSelectedEvent> _phaseSelectedBinding;

        private ILoadingPresentationService _presentationService;
        private ActiveLoadingProgress _active;
        private bool _isRegistered;
        private bool _disposed;
        private string _lastPublishedSignature = string.Empty;
        private string _lastPublishedSnapshotKey = string.Empty;
        private int _lastPublishedFrame = -1;

        public LoadingProgressOrchestrator()
        {
            _transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            _beforeFadeOutBinding = new EventBinding<SceneTransitionBeforeFadeOutEvent>(OnBeforeFadeOut);
            _completedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnCompleted);
            _routeProgressBinding = new EventBinding<SceneFlowRouteLoadingProgressEvent>(OnRouteProgress);
            _resetStartedBinding = new EventBinding<WorldResetStartedEvent>(OnResetStarted);
            _resetCompletedBinding = new EventBinding<WorldResetCompletedEvent>(OnResetCompleted);
            _phaseSelectedBinding = new EventBinding<PhaseDefinitionSelectedEvent>(OnPhaseSelected);

            EnsureRegistered();
        }

        public void EnsureRegistered()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LoadingProgressOrchestrator));
            }

            if (_isRegistered)
            {
                return;
            }

            EventBus<SceneTransitionStartedEvent>.Register(_transitionStartedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);
            EventBus<SceneTransitionBeforeFadeOutEvent>.Register(_beforeFadeOutBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_completedBinding);
            EventBus<SceneFlowRouteLoadingProgressEvent>.Register(_routeProgressBinding);
            EventBus<WorldResetStartedEvent>.Register(_resetStartedBinding);
            EventBus<WorldResetCompletedEvent>.Register(_resetCompletedBinding);
            EventBus<PhaseDefinitionSelectedEvent>.Register(_phaseSelectedBinding);

            _isRegistered = true;

            DebugUtility.LogVerbose<LoadingProgressOrchestrator>(
                "[Loading] LoadingProgressOrchestrator registrado nos eventos de Scene Flow.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_isRegistered)
            {
                EventBus<SceneTransitionStartedEvent>.Unregister(_transitionStartedBinding);
                EventBus<SceneTransitionScenesReadyEvent>.Unregister(_scenesReadyBinding);
                EventBus<SceneTransitionBeforeFadeOutEvent>.Unregister(_beforeFadeOutBinding);
                EventBus<SceneTransitionCompletedEvent>.Unregister(_completedBinding);
                EventBus<SceneFlowRouteLoadingProgressEvent>.Unregister(_routeProgressBinding);
                EventBus<WorldResetStartedEvent>.Unregister(_resetStartedBinding);
                EventBus<WorldResetCompletedEvent>.Unregister(_resetCompletedBinding);
                EventBus<PhaseDefinitionSelectedEvent>.Unregister(_phaseSelectedBinding);
                _isRegistered = false;

                DebugUtility.LogVerbose<LoadingProgressOrchestrator>(
                    "[Loading] LoadingProgressOrchestrator desregistrado dos eventos de Scene Flow.",
                    DebugUtility.Colors.Info);
            }

            _disposed = true;
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.context);
            _active = new ActiveLoadingProgress(signature, evt.context.RouteKind, evt.context.RequiresWorldReset, evt.context.Reason);
            LogProgressEvent("SceneTransitionStartedEvent", signature, evt.context.RouteKind, evt.context.RequiresWorldReset, evt.context.Reason, "loading_start");
            PublishCurrent();
        }

        private void OnRouteProgress(SceneFlowRouteLoadingProgressEvent evt)
        {
            if (!HasActiveSignature(evt.ContextSignature))
            {
                LogIgnoredProgress("SceneFlowRouteLoadingProgressEvent", evt.ContextSignature, "signature_mismatch_or_inactive");
                return;
            }

            _active.RouteProgress = Mathf.Max(_active.RouteProgress, Mathf.Clamp01(evt.NormalizedProgress));
            if (!string.IsNullOrWhiteSpace(evt.StepLabel))
            {
                _active.StepLabel = evt.StepLabel;
            }

            LogProgressEvent("SceneFlowRouteLoadingProgressEvent", evt.ContextSignature, _active.RouteKind, _active.RequiresWorldReset, _active.Reason, evt.StepLabel);
            PublishCurrent();
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.context);
            if (!HasActiveSignature(signature))
            {
                LogIgnoredProgress("SceneTransitionScenesReadyEvent", signature, "signature_mismatch_or_inactive");
                return;
            }

            _active.RouteProgress = 1f;
            _active.StepLabel = _active.RouteKind == SceneRouteKind.Gameplay ? "Preparing gameplay" : "Finalizing route";
            LogProgressEvent("SceneTransitionScenesReadyEvent", signature, evt.context.RouteKind, evt.context.RequiresWorldReset, evt.context.Reason, _active.StepLabel);
            PublishCurrent();
        }

        private void OnPhaseSelected(PhaseDefinitionSelectedEvent evt)
        {
            if (_active == null || _active.RouteKind != SceneRouteKind.Gameplay)
            {
                LogIgnoredProgress("PhaseDefinitionSelectedEvent", _active?.Signature ?? string.Empty, "inactive_or_non_gameplay_route");
                return;
            }

            _active.PrepareProgress = 1f;
            string phaseName = evt.PhaseDefinitionRef != null ? evt.PhaseDefinitionRef.name : "current phase";
            _active.StepLabel = $"Preparing phase: {phaseName}";
            LogProgressEvent("PhaseDefinitionSelectedEvent", _active.Signature, _active.RouteKind, _active.RequiresWorldReset, _active.Reason, _active.StepLabel);
            PublishCurrent();
        }

        private void OnResetStarted(WorldResetStartedEvent evt)
        {
            if (!HasActiveSignature(evt.ContextSignature))
            {
                LogIgnoredProgress("WorldResetStartedEvent", evt.ContextSignature, "signature_mismatch_or_inactive");
                return;
            }

            _active.ResetProgress = Mathf.Max(_active.ResetProgress, 0.35f);
            _active.StepLabel = "Resetting world";
            LogProgressEvent("WorldResetStartedEvent", evt.ContextSignature, _active.RouteKind, _active.RequiresWorldReset, _active.Reason, _active.StepLabel);
            PublishCurrent();
        }

        private void OnResetCompleted(WorldResetCompletedEvent evt)
        {
            if (!HasActiveSignature(evt.ContextSignature))
            {
                LogIgnoredProgress("WorldResetCompletedEvent", evt.ContextSignature, "signature_mismatch_or_inactive");
                return;
            }

            _active.ResetProgress = 1f;
            _active.StepLabel = _active.RouteKind == SceneRouteKind.Gameplay ? "Finalizing gameplay" : "Finalizing route";
            LogProgressEvent("WorldResetCompletedEvent", evt.ContextSignature, _active.RouteKind, _active.RequiresWorldReset, _active.Reason, _active.StepLabel);
            PublishCurrent();
        }

        private void OnBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.context);
            if (!HasActiveSignature(signature))
            {
                LogIgnoredProgress("SceneTransitionBeforeFadeOutEvent", signature, "signature_mismatch_or_inactive");
                return;
            }

            _active.RouteProgress = 1f;
            _active.FinalizingProgress = 1f;
            _active.StepLabel = "Finalizing";
            LogProgressEvent("SceneTransitionBeforeFadeOutEvent", signature, evt.context.RouteKind, evt.context.RequiresWorldReset, evt.context.Reason, _active.StepLabel);
            PublishCurrent();
        }

        private void OnCompleted(SceneTransitionCompletedEvent evt)
        {
            string signature = SceneTransitionSignature.Compute(evt.context);
            if (!HasActiveSignature(signature))
            {
                LogIgnoredProgress("SceneTransitionCompletedEvent", signature, "signature_mismatch_or_inactive");
                return;
            }

            _active.Completed = true;
            _active.StepLabel = "Ready";
            LogProgressEvent("SceneTransitionCompletedEvent", signature, evt.context.RouteKind, evt.context.RequiresWorldReset, evt.context.Reason, _active.StepLabel);
            PublishCurrent();
            _active = null;
        }

        private void PublishCurrent()
        {
            if (_active == null || !TryResolvePresentationService())
            {
                return;
            }

            if (ShouldDedupePublishedSnapshot(_active))
            {
                LogIgnoredProgress(
                    "LoadingProgressPublish",
                    _active.Signature,
                    "duplicate_same_frame_same_snapshot");
                return;
            }

            _lastPublishedSignature = _active.Signature;
            _lastPublishedSnapshotKey = BuildSnapshotKey(_active);
            _lastPublishedFrame = Time.frameCount;

            _presentationService.SetProgress(_active.Signature, _active.ToSnapshot());
            DebugUtility.LogVerbose<LoadingProgressOrchestrator>(
                $"[Loading] Progress published signature='{_active.Signature}' routeKind='{_active.RouteKind}' step='{_active.StepLabel}' routeProgress={_active.RouteProgress:0.##} prepareProgress={_active.PrepareProgress:0.##} resetProgress={_active.ResetProgress:0.##} finalizingProgress={_active.FinalizingProgress:0.##} completed={_active.Completed}.",
                DebugUtility.Colors.Info);
        }

        private bool TryResolvePresentationService()
        {
            if (_presentationService != null)
            {
                return true;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ILoadingPresentationService>(out var service) || service == null)
            {
                DebugUtility.LogWarning<LoadingProgressOrchestrator>(
                    "[Loading] ILoadingPresentationService unavailable for progress orchestration.");
                return false;
            }

            _presentationService = service;
            return true;
        }

        private bool HasActiveSignature(string signature)
        {
            return _active != null &&
                   !string.IsNullOrWhiteSpace(signature) &&
                   string.Equals(_active.Signature, signature, StringComparison.Ordinal);
        }

        private bool ShouldDedupePublishedSnapshot(ActiveLoadingProgress active)
        {
            if (active == null)
            {
                return false;
            }

            int currentFrame = Time.frameCount;
            string snapshotKey = BuildSnapshotKey(active);
            bool sameFrame = currentFrame == _lastPublishedFrame;
            bool sameSignature = string.Equals(_lastPublishedSignature, active.Signature, StringComparison.Ordinal);
            bool sameSnapshot = string.Equals(_lastPublishedSnapshotKey, snapshotKey, StringComparison.Ordinal);
            return sameFrame && sameSignature && sameSnapshot;
        }

        private static string BuildSnapshotKey(ActiveLoadingProgress active)
        {
            LoadingProgressSnapshot snapshot = active.ToSnapshot();
            return string.Join("|",
                active.Signature ?? string.Empty,
                active.RouteKind,
                active.RequiresWorldReset ? "1" : "0",
                snapshot.NormalizedProgress.ToString("0.###"),
                snapshot.Percentage.ToString(),
                snapshot.StepLabel ?? string.Empty,
                snapshot.Reason ?? string.Empty,
                active.Completed ? "1" : "0");
        }

        private static void LogProgressEvent(string eventName, string signature, SceneRouteKind routeKind, bool requiresWorldReset, string reason, string stepLabel)
        {
            DebugUtility.LogVerbose<LoadingProgressOrchestrator>(
                $"[Loading] {eventName} signature='{signature}' routeKind='{routeKind}' requiresWorldReset={requiresWorldReset} reason='{reason}' step='{stepLabel}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogIgnoredProgress(string eventName, string signature, string reason)
        {
            DebugUtility.LogVerbose<LoadingProgressOrchestrator>(
                $"[Loading] {eventName} ignored signature='{signature}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private sealed class ActiveLoadingProgress
        {
            public ActiveLoadingProgress(string signature, SceneRouteKind routeKind, bool requiresWorldReset, string reason)
            {
                Signature = signature ?? string.Empty;
                RouteKind = routeKind;
                RequiresWorldReset = requiresWorldReset;
                Reason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
                StepLabel = routeKind == SceneRouteKind.Gameplay ? "Loading gameplay route" : "Loading route";
            }

            public string Signature { get; }
            public SceneRouteKind RouteKind { get; }
            public bool RequiresWorldReset { get; }
            public string Reason { get; }

            public float RouteProgress { get; set; }
            public float PrepareProgress { get; set; }
            public float ResetProgress { get; set; }
            public float FinalizingProgress { get; set; }
            public bool Completed { get; set; }
            public string StepLabel { get; set; }

            public LoadingProgressSnapshot ToSnapshot()
            {
                if (Completed)
                {
                    return new LoadingProgressSnapshot(1f, StepLabel, Reason);
                }

                float routeWeight = RouteKind == SceneRouteKind.Gameplay ? 0.55f : 0.80f;
                float prepareWeight = RouteKind == SceneRouteKind.Gameplay ? 0.15f : 0f;
                float resetWeight = RequiresWorldReset ? 0.20f : 0f;
                float finalizingWeight = RouteKind == SceneRouteKind.Gameplay ? 0.05f : 0.15f;
                float activeWeightTotal = routeWeight + prepareWeight + resetWeight + finalizingWeight;
                float weighted =
                    (routeWeight * Mathf.Clamp01(RouteProgress)) +
                    (prepareWeight * Mathf.Clamp01(PrepareProgress)) +
                    (resetWeight * Mathf.Clamp01(ResetProgress)) +
                    (finalizingWeight * Mathf.Clamp01(FinalizingProgress));

                float normalized = activeWeightTotal <= 0f
                    ? 0f
                    : Mathf.Clamp01((weighted / activeWeightTotal) * 0.95f);

                return new LoadingProgressSnapshot(normalized, StepLabel, Reason);
            }
        }
    }
}

