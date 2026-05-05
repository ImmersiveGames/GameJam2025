using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
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
        private const string BootToMenuTargetScene = "MenuScene";
        private const string MenuToSandboxTargetScene = "SessionActivitySandboxScene";

        private readonly SessionOperationalPipeline _pipeline;
        private readonly EventBinding<SceneTransitionStartedEvent> _startedBinding;
        private readonly EventBinding<SceneTransitionFadeInCompletedEvent> _fadeInCompletedBinding;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _scenesReadyBinding;
        private readonly EventBinding<SceneTransitionBeforeFadeOutEvent> _beforeFadeOutBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _completedBinding;
        private readonly string _source;

        private bool _disposed;
        private string _activeTransitionSignature = string.Empty;
        private string _activeRouteOperationId = string.Empty;
        private int _transitionSequence;

        public SessionOperationalRouteTransitionBridge(
            SessionOperationalPipeline pipeline = null,
            string source = "runtime_composition")
        {
            _pipeline = pipeline ?? new SessionOperationalPipeline();
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

            if (!TryStartNewCycle(evt.context, "SceneTransitionStartedEvent", transitionId, routeId, routeProfileId, out string routeOperationId))
            {
                return;
            }

            if (!EmitStartedStages(evt.context, routeOperationId, transitionId, routeId, routeProfileId))
            {
                DebugUtility.LogWarning<SessionOperationalRouteTransitionBridge>(
                    $"[OBS][SessionOperationalPipeline][Bridge] start stage sequence rejected routeId='{routeId}' signature='{transitionId}'.");
            }
        }

        private void OnFadeInCompleted(SceneTransitionFadeInCompletedEvent evt)
        {
            LogObservedEvent("SceneTransitionFadeInCompletedEvent", evt.context, _activeRouteOperationId, _transitionSequence);

            if (!MatchesActiveTransition(evt.context, "SceneTransitionFadeInCompletedEvent"))
            {
                return;
            }
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            LogObservedEvent("SceneTransitionScenesReadyEvent", evt.context, _activeRouteOperationId, _transitionSequence);

            if (!MatchesActiveTransition(evt.context, "SceneTransitionScenesReadyEvent"))
            {
                return;
            }

            string routeOperationId = _activeRouteOperationId;
            string transitionId = SceneTransitionSignature.Compute(evt.context);
            string routeId = Normalize(evt.context.RouteId.Value);
            string routeProfileId = ResolveRouteProfileId(evt.context);
            int transitionSequence = _transitionSequence;
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
            LogObservedEvent("SceneTransitionBeforeFadeOutEvent", evt.context, _activeRouteOperationId, _transitionSequence);

            if (!MatchesActiveTransition(evt.context, "SceneTransitionBeforeFadeOutEvent"))
            {
                return;
            }
        }

        private void OnCompleted(SceneTransitionCompletedEvent evt)
        {
            LogObservedEvent("SceneTransitionCompletedEvent", evt.context, _activeRouteOperationId, _transitionSequence);

            if (!MatchesActiveTransition(evt.context, "SceneTransitionCompletedEvent"))
            {
                return;
            }

            string routeOperationId = _activeRouteOperationId;
            string transitionId = SceneTransitionSignature.Compute(evt.context);
            string routeId = Normalize(evt.context.RouteId.Value);
            string routeProfileId = ResolveRouteProfileId(evt.context);
            int transitionSequence = _transitionSequence;
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

            TryEmitStage(
                "SceneTransitionCompletedEvent",
                "Completed",
                SessionOperationalStage.Completed,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                () => _pipeline.TryCompleteRouteOperation(routeOperationId, transitionId, transitionSequence, routeId, routeProfileId, _source, reason));
        }

        private bool EmitStartedStages(
            SceneTransitionContext context,
            string routeOperationId,
            string transitionId,
            string routeId,
            string routeProfileId)
        {
            int transitionSequence = _transitionSequence;

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

        private bool TryStartNewCycle(
            SceneTransitionContext context,
            string eventName,
            string transitionId,
            string routeId,
            string routeProfileId,
            out string routeOperationId)
        {
            routeOperationId = string.Empty;

            if (string.Equals(_activeTransitionSignature, transitionId, StringComparison.Ordinal) && !_pipeline.State.HasCompleted)
            {
                LogRejected(eventName, routeOperationId, transitionId, _transitionSequence, routeId);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_activeTransitionSignature) && !_pipeline.State.HasCompleted)
            {
                LogRejected(eventName, routeOperationId, transitionId, _transitionSequence, routeId);
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
            _activeTransitionSignature = transitionId;
            _activeRouteOperationId = routeOperationId;
            return started;
        }

        private bool MatchesActiveTransition(SceneTransitionContext context, string eventName)
        {
            if (!_pipeline.State.HasStarted || string.IsNullOrWhiteSpace(_activeTransitionSignature))
            {
                LogRejected(eventName, _activeRouteOperationId, SceneTransitionSignature.Compute(context), _transitionSequence, Normalize(context.RouteId.Value));
                return false;
            }

            string signature = SceneTransitionSignature.Compute(context);
            if (!string.Equals(signature, _activeTransitionSignature, StringComparison.Ordinal))
            {
                LogRejected(eventName, _activeRouteOperationId, signature, _transitionSequence, Normalize(context.RouteId.Value));
                return false;
            }

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

            if (!IsObservedRoute(context))
            {
                LogRejected(eventName, string.Empty, transitionId, 0, routeId);
                return false;
            }

            return true;
        }

        private static bool IsObservedRoute(SceneTransitionContext context)
        {
            string targetActiveScene = Normalize(context.TargetActiveScene);

            if (string.Equals(targetActiveScene, BootToMenuTargetScene, StringComparison.Ordinal) &&
                context.RouteKind == SceneRouteKind.Frontend)
            {
                return true;
            }

            if (string.Equals(targetActiveScene, MenuToSandboxTargetScene, StringComparison.Ordinal) &&
                context.RouteKind == SceneRouteKind.Sandbox)
            {
                return true;
            }

            return false;
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
