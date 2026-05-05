using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;

namespace _ImmersiveGames.NewScripts.SessionFlow.Integration.SceneFlow
{
    /// <summary>
    /// Ponte temporária pendente de remoção.
    /// Observa eventos atuais do SceneFlow e só marca rota/transição.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SessionOperationalRouteTransitionBridge : IDisposable
    {
        private const string BridgeSource = "runtime_composition";
        private readonly SessionOperationalPipeline _pipeline;
        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;
        private readonly EventBinding<SceneTransitionFadeInCompletedEvent> _fadeInCompletedBinding;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private readonly EventBinding<SceneTransitionBeforeFadeOutEvent> _beforeFadeOutBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _completedBinding;
        private readonly string _source;

        private bool _disposed;
        private bool _hasActiveOperation;
        private bool _activeOperationCompleted;
        private string _activeTransitionSignature = string.Empty;
        private string _activeRouteOperationId = string.Empty;
        private string _activeRouteId = string.Empty;
        private string _activeRouteProfileId = string.Empty;
        private int _transitionSequence;

        public SessionOperationalRouteTransitionBridge(
            SessionOperationalPipeline pipeline = null,
            string source = "runtime_composition")
        {
            if (pipeline != null)
            {
                _pipeline = pipeline;
            }
            else if (DependencyManager.Provider.TryGetGlobal<SessionOperationalPipeline>(out var existingPipeline) && existingPipeline != null)
            {
                _pipeline = existingPipeline;
            }
            else
            {
                _pipeline = new SessionOperationalPipeline();
                DependencyManager.Provider.RegisterGlobal(_pipeline);
            }

            _source = Normalize(source);
            if (string.IsNullOrWhiteSpace(_source))
            {
                throw new ArgumentException("source is required.", nameof(source));
            }

            _startedBinding = new EventBinding<SceneTransitionStartedEvent>(OnStarted);
            _fadeInCompletedBinding = new EventBinding<SceneTransitionFadeInCompletedEvent>(OnFadeInCompleted);
            _scenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            _beforeFadeOutBinding = new EventBinding<SceneTransitionBeforeFadeOutEvent>(OnBeforeFadeOut);
            _completedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnCompleted);

            EventBus<SceneTransitionStartedEvent>.Register(_startedBinding);
            EventBus<SceneTransitionFadeInCompletedEvent>.Register(_fadeInCompletedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_scenesReadyBinding);
            EventBus<SceneTransitionBeforeFadeOutEvent>.Register(_beforeFadeOutBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_completedBinding);

            DebugUtility.Log(typeof(SessionOperationalRouteTransitionBridge),
                $"[OBS][SessionOperationalPipeline][Bridge] registered source='{_source}'",
                DebugUtility.Colors.Info);
        }

        public SessionOperationalPipeline Pipeline => _pipeline;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<SceneTransitionStartedEvent>.Unregister(_startedBinding);
            EventBus<SceneTransitionFadeInCompletedEvent>.Unregister(_fadeInCompletedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Unregister(_scenesReadyBinding);
            EventBus<SceneTransitionBeforeFadeOutEvent>.Unregister(_beforeFadeOutBinding);
            EventBus<SceneTransitionCompletedEvent>.Unregister(_completedBinding);
        }

        private void OnStarted(SceneTransitionStartedEvent evt)
        {
            LogObservedEvent("SceneTransitionStartedEvent", evt.context, string.Empty, 0);

            if (!TryAcceptRoute(evt.context, "SceneTransitionStartedEvent", out string transitionId, out string routeId, out string routeProfileId))
            {
                return;
            }

            if (!TryOpenNewOperation(evt.context, "SceneTransitionStartedEvent", transitionId, routeId, routeProfileId, out string routeOperationId, out int transitionSequence, out bool openedNewOperation))
            {
                return;
            }

            if (!openedNewOperation)
            {
                LogObservedEvent("SceneTransitionStartedEvent", evt.context, routeOperationId, transitionSequence);
                return;
            }

            if (!EmitStartedStages(evt.context, routeOperationId, transitionId, transitionSequence, routeId, routeProfileId))
            {
                DebugUtility.LogWarning<SessionOperationalRouteTransitionBridge>(
                    $"[OBS][SessionOperationalPipeline][Bridge] start stage sequence rejected routeId='{routeId}' signature='{transitionId}'.");
            }
        }

        private void OnFadeInCompleted(SceneTransitionFadeInCompletedEvent evt)
        {
            if (!TryResolveActiveOperation(evt.context, "SceneTransitionFadeInCompletedEvent", false, out string routeOperationId, out string transitionId, out int transitionSequence, out string routeId, out string routeProfileId))
            {
                return;
            }

            LogObservedEvent("SceneTransitionFadeInCompletedEvent", evt.context, routeOperationId, transitionSequence);
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            if (!TryResolveActiveOperation(evt.context, "SceneTransitionScenesReadyEvent", false, out string routeOperationId, out string transitionId, out int transitionSequence, out string routeId, out string routeProfileId))
            {
                return;
            }

            LogObservedEvent("SceneTransitionScenesReadyEvent", evt.context, routeOperationId, transitionSequence);
            string reason = Normalize(evt.context.Reason);

            if (!TryEmitStage(
                    "SceneTransitionScenesReadyEvent",
                    "CurtainClosed",
                    SessionOperationalStage.CurtainClosed,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryObserveCurtainClosed(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, reason)))
            {
                return;
            }

            if (!TryEmitStage(
                    "SceneTransitionScenesReadyEvent",
                    "PreviousRouteTeardownSkipped",
                    SessionOperationalStage.PreviousRouteTeardownSkipped,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryObservePreviousRouteTeardownSkipped(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, reason)))
            {
                return;
            }

            if (!TryEmitStage(
                    "SceneTransitionScenesReadyEvent",
                    "RoutePhysicalApplyObserved",
                    SessionOperationalStage.RoutePhysicalApplyObserved,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryObserveRoutePhysicalApply(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, reason)))
            {
                return;
            }

            if (!TryEmitStage(
                    "SceneTransitionScenesReadyEvent",
                    "ScenesReadyObserved",
                    SessionOperationalStage.ScenesReadyObserved,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryObserveScenesReady(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, reason)))
            {
                return;
            }

            if (!TryEmitStage(
                    "SceneTransitionScenesReadyEvent",
                    "SessionOperationalSetupNoOp",
                    SessionOperationalStage.SessionOperationalSetupNoOp,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryObserveSetupNoOp(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, reason)))
            {
                return;
            }

            if (!TryEmitStage(
                    "SceneTransitionScenesReadyEvent",
                    "InputCapabilityPrepared",
                    SessionOperationalStage.InputCapabilityPrepared,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryObserveInputCapabilityPrepared(
                        routeOperationId,
                        transitionId,
                        transitionSequence,
                        routeId,
                        routeProfileId,
                        ResolveRouteClass(evt.context.RouteKind),
                        ResolveInitialInputMode(evt.context.RouteKind),
                        _source,
                        reason)))
            {
                return;
            }

            if (!TryEmitStage(
                    "SceneTransitionScenesReadyEvent",
                    "InitialInputModePrepared",
                    SessionOperationalStage.InitialInputModePrepared,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryObserveInitialInputModePrepared(
                        routeOperationId,
                        transitionId,
                        transitionSequence,
                        routeId,
                        routeProfileId,
                        ResolveRouteClass(evt.context.RouteKind),
                        ResolveInitialInputMode(evt.context.RouteKind),
                        _source,
                        reason)))
            {
                return;
            }

            if (!TryEmitStage(
                    "SceneTransitionScenesReadyEvent",
                    "PauseCapabilityPrepared",
                    SessionOperationalStage.PauseCapabilityPrepared,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryObservePauseCapabilityPrepared(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, reason)))
            {
                return;
            }

            TryEmitStage(
                "SceneTransitionScenesReadyEvent",
                "ReadyToOpenCurtain",
                SessionOperationalStage.ReadyToOpenCurtain,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                () => _pipeline.TryObserveReadyToOpenCurtain(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, reason));
        }

        private void OnBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            if (!TryResolveActiveOperation(evt.context, "SceneTransitionBeforeFadeOutEvent", false, out string routeOperationId, out string transitionId, out int transitionSequence, out string routeId, out string routeProfileId))
            {
                return;
            }

            LogObservedEvent("SceneTransitionBeforeFadeOutEvent", evt.context, routeOperationId, transitionSequence);
        }

        private void OnCompleted(SceneTransitionCompletedEvent evt)
        {
            if (!TryResolveActiveOperation(evt.context, "SceneTransitionCompletedEvent", true, out string routeOperationId, out string transitionId, out int transitionSequence, out string routeId, out string routeProfileId))
            {
                return;
            }

            LogObservedEvent("SceneTransitionCompletedEvent", evt.context, routeOperationId, transitionSequence);
            string reason = Normalize(evt.context.Reason);

            if (!TryEmitStage(
                    "SceneTransitionCompletedEvent",
                    "TransitionCompletedObserved",
                    SessionOperationalStage.TransitionCompletedObserved,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryObserveTransitionCompleted(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, reason)))
            {
                return;
            }

            bool completedObserved = TryEmitStage(
                "SceneTransitionCompletedEvent",
                "Completed",
                SessionOperationalStage.Completed,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                () => _pipeline.TryCompleteRouteOperation(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, reason));

            if (completedObserved)
            {
                _hasActiveOperation = false;
                _activeOperationCompleted = true;
                _activeTransitionSignature = string.Empty;
                _activeRouteOperationId = string.Empty;
                _activeRouteId = string.Empty;
                _activeRouteProfileId = string.Empty;
            }
        }

        private bool EmitStartedStages(
            SceneTransitionContext context,
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId)
        {
            if (!TryEmitStage(
                    "SceneTransitionStartedEvent",
                    "RouteOperationStarted",
                    SessionOperationalStage.RouteOperationStarted,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryBeginRouteOperation(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, Normalize(context.Reason))))
            {
                return false;
            }

            if (!TryEmitStage(
                    "SceneTransitionStartedEvent",
                    "NavigationIntentObserved",
                    SessionOperationalStage.NavigationIntentObserved,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryObserveNavigationIntent(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, Normalize(context.Reason))))
            {
                return false;
            }

            if (!TryEmitStage(
                    "SceneTransitionStartedEvent",
                    "RouteResolved",
                    SessionOperationalStage.RouteResolved,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryObserveRouteResolved(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, Normalize(context.Reason))))
            {
                return false;
            }

            if (!TryEmitStage(
                    "SceneTransitionStartedEvent",
                    "TransitionRequested",
                    SessionOperationalStage.TransitionRequested,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    () => _pipeline.TryObserveTransitionRequested(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, Normalize(context.Reason))))
            {
                return false;
            }

            return TryEmitStage(
                "SceneTransitionStartedEvent",
                "TransitionStarted",
                SessionOperationalStage.TransitionStarted,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                () => _pipeline.TryObserveTransitionStarted(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, Normalize(context.Reason)));
        }

        private bool TryOpenNewOperation(
            SceneTransitionContext context,
            string eventName,
            string transitionId,
            string routeId,
            string routeProfileId,
            out string routeOperationId,
            out int transitionSequence,
            out bool openedNewOperation)
        {
            routeOperationId = string.Empty;
            transitionSequence = 0;
            openedNewOperation = false;

            if (_hasActiveOperation && !_activeOperationCompleted)
            {
                if (string.Equals(_activeTransitionSignature, transitionId, StringComparison.Ordinal))
                {
                    if (!string.Equals(_activeRouteId, routeId, StringComparison.Ordinal) ||
                        !string.Equals(_activeRouteProfileId, routeProfileId, StringComparison.Ordinal))
                    {
                        LogRejected(eventName, _activeRouteOperationId, transitionId, _transitionSequence, routeId);
                        return false;
                    }

                    routeOperationId = _activeRouteOperationId;
                    transitionSequence = _transitionSequence;
                    openedNewOperation = false;
                    return true;
                }

                LogRejected(eventName, _activeRouteOperationId, transitionId, _transitionSequence, routeId);
                return false;
            }

            int nextTransitionSequence = _transitionSequence + 1;
            routeOperationId = BuildRouteOperationId(routeId, routeProfileId, transitionId, nextTransitionSequence, context);

            bool started = _pipeline.TryBeginRouteOperation(
                routeOperationId,
                transitionId,
                nextTransitionSequence,
                routeId,
                routeProfileId,
                BridgeSource,
                Normalize(context.Reason));

            if (!started)
            {
                LogRejected(eventName, routeOperationId, transitionId, nextTransitionSequence, routeId);
                return false;
            }

            _transitionSequence = nextTransitionSequence;
            transitionSequence = nextTransitionSequence;
            _hasActiveOperation = true;
            _activeOperationCompleted = false;
            _activeTransitionSignature = transitionId;
            _activeRouteOperationId = routeOperationId;
            _activeRouteId = routeId;
            _activeRouteProfileId = routeProfileId;
            openedNewOperation = true;
            return started;
        }

        private bool TryResolveActiveOperation(
            SceneTransitionContext context,
            string eventName,
            bool acceptCompletedOperation,
            out string routeOperationId,
            out string transitionId,
            out int transitionSequence,
            out string routeId,
            out string routeProfileId)
        {
            routeOperationId = string.Empty;
            transitionId = SceneTransitionSignature.Compute(context);
            transitionSequence = _transitionSequence;
            routeId = Normalize(context.RouteId.Value);
            routeProfileId = ResolveRouteProfileId(context);

            if (!_hasActiveOperation || string.IsNullOrWhiteSpace(_activeTransitionSignature))
            {
                LogRejected(eventName, _activeRouteOperationId, transitionId, transitionSequence, routeId);
                return false;
            }

            if (!string.Equals(transitionId, _activeTransitionSignature, StringComparison.Ordinal))
            {
                LogRejected(eventName, _activeRouteOperationId, transitionId, transitionSequence, routeId);
                return false;
            }

            if (!string.Equals(routeId, _activeRouteId, StringComparison.Ordinal) ||
                !string.Equals(routeProfileId, _activeRouteProfileId, StringComparison.Ordinal))
            {
                LogRejected(eventName, _activeRouteOperationId, transitionId, transitionSequence, routeId);
                return false;
            }

            if (_activeOperationCompleted && !acceptCompletedOperation)
            {
                LogRejected(eventName, _activeRouteOperationId, transitionId, transitionSequence, routeId);
                return false;
            }

            routeOperationId = _activeRouteOperationId;
            return true;
        }

        private bool TryAcceptRoute(
            SceneTransitionContext context,
            string eventName,
            out string transitionId,
            out string routeId,
            out string routeProfileId)
        {
            routeId = Normalize(context.RouteId.Value);
            transitionId = SceneTransitionSignature.Compute(context);
            routeProfileId = ResolveRouteProfileId(context);

            if (!context.RouteId.IsValid)
            {
                LogRejected(eventName, string.Empty, transitionId, 0, routeId);
                return false;
            }

            return true;
        }


        private static string BuildRouteOperationId(
            string routeId,
            string routeProfileId,
            string transitionId,
            int transitionSequence,
            SceneTransitionContext context)
        {
            string targetActiveScene = Normalize(context.TargetActiveScene);
            return $"{routeId}|{routeProfileId}|{targetActiveScene}|{transitionSequence}|{transitionId}";
        }

        private bool TryEmitStage(
            string eventName,
            string factName,
            SessionOperationalStage stage,
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            Func<bool> emit)
        {
            if (!emit())
            {
                LogRejected(eventName, routeOperationId, transitionId, transitionSequence, routeId);
                return false;
            }

            LogPipelineAdvance(factName, stage, routeOperationId, transitionId, transitionSequence, routeId);
            return true;
        }

        private static void LogObservedEvent(
            string eventName,
            SceneTransitionContext context,
            string routeOperationId,
            int transitionSequence)
        {
            string routeId = Normalize(context.RouteId.Value);
            string transitionId = SceneTransitionSignature.Compute(context);

            DebugUtility.Log(typeof(SessionOperationalRouteTransitionBridge),
                $"[OBS][SessionOperationalPipeline][Bridge] observed event='{eventName}' routeId='{routeId}' transitionId='{transitionId}' transitionSequence='{transitionSequence}' routeOperationId='{Normalize(routeOperationId)}'",
                DebugUtility.Colors.Info);
        }

        private static void LogPipelineAdvance(
            string factName,
            SessionOperationalStage stage,
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId)
        {
            DebugUtility.Log(typeof(SessionOperationalRouteTransitionBridge),
                $"[OBS][SessionOperationalPipeline] fact='{factName}' stage='{stage}' routeId='{Normalize(routeId)}' transitionId='{Normalize(transitionId)}' transitionSequence='{transitionSequence}' routeOperationId='{Normalize(routeOperationId)}'",
                DebugUtility.Colors.Info);
        }

        private static void LogRejected(
            string eventName,
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId)
        {
            DebugUtility.LogWarning<SessionOperationalRouteTransitionBridge>(
                $"[OBS][SessionOperationalPipeline] rejected reason='stale_or_foreign_event' event='{eventName}' routeId='{Normalize(routeId)}' transitionId='{Normalize(transitionId)}' transitionSequence='{transitionSequence}' routeOperationId='{Normalize(routeOperationId)}'.");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string ResolveRouteProfileId(SceneTransitionContext context)
        {
            if (context.RouteRef != null &&
                context.RouteRef.RouteProfile != null &&
                context.RouteRef.RouteProfile.ProfileId.IsValid)
            {
                return Normalize(context.RouteRef.RouteProfile.ProfileId.Value);
            }

            return Normalize(context.TransitionProfileName);
        }

        private static string ResolveRouteClass(SceneRouteKind routeKind)
        {
            return Normalize(routeKind.ToString());
        }

        private static SessionOperationalInputModeKind ResolveInitialInputMode(SceneRouteKind routeKind)
        {
            return routeKind switch
            {
                SceneRouteKind.Frontend => SessionOperationalInputModeKind.FrontendMenu,
                SceneRouteKind.Sandbox => SessionOperationalInputModeKind.ActivityDefault,
                SceneRouteKind.Gameplay => SessionOperationalInputModeKind.ActivityDefault,
                _ => SessionOperationalInputModeKind.Unknown,
            };
        }
    }
}
