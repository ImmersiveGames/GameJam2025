using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Semantic.Preparation;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
using _ImmersiveGames.NewScripts.SaveRuntime.Models;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using DebugUtility = _ImmersiveGames.NewScripts.Foundation.Core.Logging.DebugUtility;
namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum RouteRequestSubmissionKind
    {
        Unknown = 0,
        Accepted = 1,
        RejectedByPolicy = 2,
        IgnoredAlreadyInFlight = 3,
        FailedInvalidConfig = 4,
    }

    public readonly struct RouteRequestSubmissionResult
    {
        public RouteRequestSubmissionResult(RouteRequestSubmissionKind kind, string routeIdentity, string reason, string detail)
        {
            Kind = kind;
            RouteIdentity = Normalize(routeIdentity);
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public RouteRequestSubmissionKind Kind { get; }
        public string RouteIdentity { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsAccepted => Kind == RouteRequestSubmissionKind.Accepted;
        public bool IsRejectedByPolicy => Kind == RouteRequestSubmissionKind.RejectedByPolicy;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct RouteOperationCompletionSignal
    {
        public RouteOperationCompletionSignal(string routeIdentity, string routeOperationId, bool succeeded, string reason)
        {
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            Succeeded = succeeded;
            Reason = Normalize(reason);
        }

        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public bool Succeeded { get; }
        public string Reason { get; }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public sealed class SessionOperationalPipeline : IRouteActivityLoadedSnapshotPayloadProvider, IRouteActivityLoadedSnapshotPayloadStore
    {
        private const string DefaultPipelineId = "SessionOperationalPipeline.v0";
        private const string RouteActivitySnapshotSchemaId = "progression.route_activity.object_snapshot.v1";

        private readonly SessionOperationalRuntimeState _state = new();
        private readonly SessionOperationalStageOrderPolicy _stageOrderPolicy = new();
        private readonly SessionOperationalRoutePlanResolver _routePlanResolver = new();
        private readonly OperationalRouteSetupStage _routeSetupStage = new();
        private readonly OperationalTransitionBlackoutStage _transitionBlackoutStage = new();
        private readonly OperationalPreviousRouteExitStage _previousRouteExitStage = new();
        private readonly OperationalHandoffExitStage _handoffExitStage = new();
        private readonly OperationalRouteCameraPresentationStage _routeCameraPresentationStage = new();
        private readonly OperationalInputPreparationStage _inputPreparationStage = new();
        private readonly OperationalPlayerPreparationStage _playerPreparationStage = new();
        private readonly OperationalConsumerPresentationPreparationStage _consumerPresentationPreparationStage = new();
        private readonly OperationalConsumerPresentationReleaseStage _consumerPresentationReleaseStage = new();
        private readonly OperationalConsumerEntryAndReadinessStage _consumerEntryAndReadinessStage = new();
        private readonly OperationalRouteActivitySaveLoadOnEnterStage _routeActivitySaveLoadOnEnterStage = new();
        private readonly OperationalRouteActivitySaveSaveOnExitStage _routeActivitySaveSaveOnExitStage = new();
        private readonly OperationalLoadingStage _loadingStage = new();
        private readonly OperationalRouteRevealStage _routeRevealStage = new();
        private readonly OperationalRouteCompletionStage _routeCompletionStage = new();
        private readonly SessionOperationalPipelineDependencies _dependencies;
        private readonly string _sessionOperationalPipelineId;
        private readonly object _operationalRouteSync = new();
        private int _operationalRouteSequence;
        private SessionOperationalRouteSnapshot _lastCompletedRouteSnapshot;
        private bool _hasActiveOperationalRouteOperation;
        private string _activeOperationalRouteOperationId = string.Empty;
        private string _activeOperationalTransitionId = string.Empty;
        private string _activeOperationalRouteIdentity = string.Empty;
        private LoadedRouteActivitySnapshotPayloadContext _pendingLoadedRouteActivitySnapshotPayload;
        public event Action<RouteOperationCompletionSignal> RouteOperationCompleted;

        public SessionOperationalPipeline(
            SessionOperationalPipelineDependencies dependencies,
            string sessionOperationalPipelineId = DefaultPipelineId)
        {
            _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            _sessionOperationalPipelineId = Normalize(sessionOperationalPipelineId);

            if (string.IsNullOrWhiteSpace(_sessionOperationalPipelineId))
            {
                throw new ArgumentException("sessionOperationalPipelineId is required.", nameof(sessionOperationalPipelineId));
            }
        }

        public SessionOperationalRuntimeState State => _state;

        public RouteRequestSubmissionResult SubmitRouteRequest(
            OperationalRouteAsset route,
            string source,
            string reason)
        {
            string routeIdentity = route != null ? Normalize(route.RouteIdentity) : string.Empty;
            string sourceText = Normalize(source);
            string reasonText = Normalize(reason);

            if (route == null)
            {
                return new RouteRequestSubmissionResult(
                    RouteRequestSubmissionKind.FailedInvalidConfig,
                    routeIdentity,
                    "route_missing",
                    "OperationalRouteAsset is required.");
            }

            if (!route.TryValidate(out string routeValidationError))
            {
                return new RouteRequestSubmissionResult(
                    RouteRequestSubmissionKind.FailedInvalidConfig,
                    routeIdentity,
                    "route_invalid",
                    Normalize(routeValidationError));
            }

            RouteRequestSubmissionResult preflight = TryPreflightRouteRequest(routeIdentity, sourceText, reasonText);
            if (!preflight.IsAccepted)
            {
                return preflight;
            }

            try
            {
                _routePlanResolver.ValidateAuthoringOrFail(route, _dependencies.PersistentScenesPolicy);
            }
            catch (Exception ex)
            {
                return new RouteRequestSubmissionResult(
                    RouteRequestSubmissionKind.FailedInvalidConfig,
                    routeIdentity,
                    "invalid_runtime_config",
                    $"{ex.GetType().Name}:{Normalize(ex.Message)}");
            }

            _ = RequestOperationalRouteAsync(route, sourceText, reasonText);
            return new RouteRequestSubmissionResult(
                RouteRequestSubmissionKind.Accepted,
                routeIdentity,
                "accepted",
                "Route request accepted.");
        }

        public bool TryGetPendingLoadedSnapshotPayload(
            string activityIdentity,
            out LoadedSessionActivitySnapshotPayload payload,
            out string failureReason)
        {
            payload = default;
            string normalizedActivityIdentity = Normalize(activityIdentity);
            if (string.IsNullOrWhiteSpace(normalizedActivityIdentity))
            {
                failureReason = "activity_identity_missing";
                return false;
            }

            if (!_pendingLoadedRouteActivitySnapshotPayload.IsValid)
            {
                failureReason = "pending_payload_missing";
                return false;
            }

            if (!string.Equals(_pendingLoadedRouteActivitySnapshotPayload.ActivityIdentity, normalizedActivityIdentity, StringComparison.Ordinal))
            {
                failureReason = "pending_payload_activity_mismatch";
                return false;
            }

            payload = _pendingLoadedRouteActivitySnapshotPayload.Payload;
            failureReason = "resolved";
            return true;
        }

        public void ClearPendingLoadedSnapshotPayload()
        {
            _pendingLoadedRouteActivitySnapshotPayload = default;
        }

        public void SetPendingLoadedSnapshotPayload(
            string activityIdentity,
            LoadedSessionActivitySnapshotPayload payload,
            int payloadSize)
        {
            _pendingLoadedRouteActivitySnapshotPayload = new LoadedRouteActivitySnapshotPayloadContext(
                activityIdentity,
                payload,
                payloadSize);
        }

        public async Task<SessionOperationalRouteCompletedFact> RequestOperationalRouteAsync(
            OperationalRouteAsset route,
            string source,
            string reason)
        {
            if (route == null)
            {
                throw new InvalidOperationException("OperationalRouteAsset is required.");
            }

            ISceneCompositionAdapter routeExecutor = ResolveRouteExecutorOrFail();
            RuntimeModeConfig runtimeModeConfig = ResolveRuntimeModeConfigOrFail();
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = _dependencies.PersistentScenesPolicy;
            IFadeAdapter fadeAdapter = null;
            ILoadingAdapter loadingAdapter = null;

            string sourceText = Normalize(source);
            string reasonText = Normalize(reason);
            string routeIdentity = Normalize(route.RouteIdentity);
            string activeSceneName = string.Empty;
            SessionOperationalRouteSnapshot previousCompletedRoute = default;
            SessionOperationalRoutePlanResolution planResolution = default;

            string routeOperationId;
            string transitionId;
            int routeSequence;

            lock (_operationalRouteSync)
            {
                if (_hasActiveOperationalRouteOperation)
                {
                    DebugUtility.LogWarning<SessionOperationalPipeline>(
                        $"[OBS][SessionOperationalPipeline][Route] rejected reason='stale_or_foreign_route' routeIdentity='{routeIdentity}' activeRouteIdentity='{_activeOperationalRouteIdentity}' activeRouteOperationId='{_activeOperationalRouteOperationId}' activeTransitionId='{_activeOperationalTransitionId}' source='{sourceText}' reason='{reasonText}'.");
                    throw new InvalidOperationException("Operational route operation is already in flight.");
                }

                previousCompletedRoute = _lastCompletedRouteSnapshot;
                planResolution = _routePlanResolver.ResolveOrFail(
                    route,
                    persistentScenesPolicy,
                    previousCompletedRoute.IsValid,
                    previousCompletedRoute.RouteOwnedLoadedSceneKeys);

                routeIdentity = planResolution.Plan.RouteIdentity;
                activeSceneName = ResolveSceneName(planResolution.Plan.ActiveSceneKey, nameof(planResolution.Plan.ActiveSceneKey));
                _operationalRouteSequence += 1;
                routeSequence = _operationalRouteSequence;
                routeOperationId = BuildRouteOperationId(routeIdentity, activeSceneName, routeSequence);
                transitionId = BuildTransitionId(routeIdentity, activeSceneName, routeSequence);

                _hasActiveOperationalRouteOperation = true;
                _activeOperationalRouteOperationId = routeOperationId;
                _activeOperationalTransitionId = transitionId;
                _activeOperationalRouteIdentity = routeIdentity;
            }

            OperationalRouteSetupResult setupResult = _routeSetupStage.Execute(
                new OperationalRouteSetupCommand(
                    planResolution,
                    activeSceneName,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    runtimeModeConfig,
                    previousCompletedRoute.IsValid,
                    previousCompletedRoute.RouteIdentity,
                    previousCompletedRoute.RouteOperationId,
                    previousCompletedRoute.RouteSequence,
                    previousCompletedRoute.ActiveSceneKey,
                    previousCompletedRoute.SaveActivityOnExit,
                    previousCompletedRoute.ActivityIdentity,
                    BuildActivitySaveKey(previousCompletedRoute.ActivityIdentity),
                    previousCompletedRoute.RouteOwnedLoadedSceneKeys,
                    sourceText,
                    reasonText));

            if (!setupResult.IsCompleted)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][Route] OperationalRouteSetupStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' reason='{setupResult.Reason}' detail='{setupResult.Detail}'.");
            }

            SessionOperationalRouteCommand command = setupResult.RouteCommand;
            RouteActivitySavePlan routeActivitySavePlan = setupResult.RouteActivitySavePlan;
            SessionOperationalLoadingCommand loadingCommand = setupResult.LoadingCommand;
            routeIdentity = setupResult.RouteIdentity;
            activeSceneName = setupResult.ActiveSceneName;

            if (command.UsesTransition)
            {
                fadeAdapter = ResolveFadeAdapterOrFail();
            }

            if (loadingCommand.IsEnabled)
            {
                loadingAdapter = ResolveLoadingAdapterOrFail();
            }

            bool fadeInCompleted = false;
            bool fadeOutCompleted = false;
            bool loadingStarted = false;
            bool loadingCompleted = false;
            bool loadingHidden = false;
            bool routeOperationSucceeded = false;
            string completionReason = "failed";

            try
            {
                if (!TryBeginRouteOperation(
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        routeIdentity,
                        routeIdentity,
                        source,
                        reason))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][H1][SessionOperationalPipeline][Route] Failed to record RouteOperationStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{sourceText}' reason='{reasonText}'.");
                }

                OperationalRouteCompletionResult transitionPlanReadyResult = _routeCompletionStage.ExecuteTransitionPlanReady(
                    new OperationalRouteCompletionCommand(
                        _sessionOperationalPipelineId,
                        _state,
                        command,
                        sourceText,
                        reasonText));
                if (!transitionPlanReadyResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Transition] OperationalRouteCompletionStage TransitionPlanReady failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{transitionPlanReadyResult.Kind}' reason='{transitionPlanReadyResult.Reason}' detail='{transitionPlanReadyResult.Detail}'.");
                }

                OperationalLoadingResult loadingStartResult = await _loadingStage.ExecuteStartAsync(
                    new OperationalLoadingCommand(
                        loadingAdapter,
                        loadingCommand,
                        sourceText,
                        reasonText));
                if (!loadingStartResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Loading] OperationalLoadingStage start failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{loadingStartResult.Kind}' reason='{loadingStartResult.Reason}' detail='{loadingStartResult.Detail}'.");
                }

                loadingStarted = loadingStartResult.LoadingStarted;

                OperationalTransitionBlackoutResult blackoutResult = await _transitionBlackoutStage.ExecuteAsync(
                    new OperationalTransitionBlackoutCommand(
                        command,
                        fadeAdapter,
                        sourceText,
                        reasonText));

                if (!blackoutResult.IsCompleted && !blackoutResult.IsSkipped)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Transition] OperationalTransitionBlackoutStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{blackoutResult.Kind}' reason='{blackoutResult.Reason}' detail='{blackoutResult.Detail}'.");
                }

                fadeInCompleted = blackoutResult.FadeInCompleted;

                OperationalLoadingResult blackoutLoadingResult = await _loadingStage.ExecuteTransitionBlackoutProgressAsync(
                    new OperationalLoadingCommand(
                        loadingAdapter,
                        loadingCommand,
                        sourceText,
                        reasonText),
                    blackoutResult);
                if (!blackoutLoadingResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Loading] OperationalLoadingStage blackout progress failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{blackoutLoadingResult.Kind}' reason='{blackoutLoadingResult.Reason}' detail='{blackoutLoadingResult.Detail}'.");
                }

                _dependencies.TryResolveActivitySnapshotPayloadProvider(out ISessionActivitySnapshotPayloadProvider activitySnapshotPayloadProvider);

                OperationalPreviousRouteExitResult previousRouteExitResult = await _previousRouteExitStage.ExecuteAsync(
                    new OperationalPreviousRouteExitCommand(
                        command,
                        previousCompletedRoute.RouteIdentity,
                        previousCompletedRoute.ActivityIdentity,
                        routeActivitySavePlan,
                        sourceText,
                        reasonText,
                        _handoffExitStage,
                        new OperationalHandoffExitCommand(
                            _dependencies.ResolveRouteHandoffExitPort(),
                            command,
                            previousCompletedRoute.RouteIdentity,
                            previousCompletedRoute.ActivityIdentity,
                            previousCompletedRoute.ActiveSceneKey,
                            previousCompletedRoute.RouteOwnedLoadedSceneKeys,
                            command.FinalScenesToUnload,
                            sourceText,
                            reasonText),
                        _consumerPresentationReleaseStage,
                        new OperationalConsumerPresentationReleaseCommand(
                            _dependencies.ResolveRouteConsumerPresentationPort(),
                            command,
                            activeSceneName,
                            previousCompletedRoute.RouteIdentity,
                            previousCompletedRoute.ActivityIdentity,
                            sourceText,
                            reasonText),
                        _routeActivitySaveSaveOnExitStage,
                        new OperationalRouteActivitySaveSaveOnExitCommand(
                            runtimeModeConfig,
                            command,
                            routeActivitySavePlan,
                            _dependencies.ActivitySaveAdapter,
                            _dependencies.ProgressionSlotContextResolver,
                            activitySnapshotPayloadProvider,
                            RouteActivitySnapshotSchemaId,
                            sourceText,
                            reasonText)));

                if (!previousRouteExitResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][PreviousRouteExit] OperationalPreviousRouteExitStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{previousRouteExitResult.Kind}' reason='{previousRouteExitResult.Reason}' detail='{previousRouteExitResult.Detail}'.");
                }

                OperationalRouteMaterializationStage materializationStage = new();
                OperationalRouteMaterializationResult materializationResult = await materializationStage.ExecuteAsync(
                    BuildMaterializationCommand(
                        runtimeModeConfig,
                        command,
                        routeExecutor,
                        loadingAdapter,
                        loadingCommand,
                        routeActivitySavePlan,
                        previousCompletedRoute,
                        activeSceneName,
                        routeIdentity,
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        source,
                        reason,
                        sourceText,
                        reasonText));
                SessionOperationalRouteCompletedFact adapterFact = materializationResult.CompletionFact;
                loadingCompleted = materializationResult.LoadingCompleted;
                loadingHidden = materializationResult.LoadingHidden;

                IAudioAdapter audioAdapter = command.Audio.RouteAudioMode == SessionOperationalRouteAudioMode.None
                    ? null
                    : ResolveAudioAdapterOrFail();

                OperationalRouteRevealResult revealResult = await _routeRevealStage.ExecuteAsync(
                    new OperationalRouteRevealCommand(
                        command,
                        fadeAdapter,
                        audioAdapter,
                        sourceText,
                        reasonText));

                if (!revealResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Route] OperationalRouteRevealStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{revealResult.Kind}' reason='{revealResult.Reason}' detail='{revealResult.Detail}'.");
                }

                fadeOutCompleted = revealResult.FadeOutCompleted;

                OperationalRouteCompletionResult completionResult = _routeCompletionStage.ExecuteCompleted(
                    new OperationalRouteCompletionCommand(
                        _sessionOperationalPipelineId,
                        _state,
                        command,
                        sourceText,
                        reasonText),
                    adapterFact);
                if (!completionResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Route] OperationalRouteCompletionStage completed failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{completionResult.Kind}' reason='{completionResult.Reason}' detail='{completionResult.Detail}'.");
                }

                _lastCompletedRouteSnapshot = completionResult.RouteSnapshot;

                routeOperationSucceeded = true;
                completionReason = "completed";
                return adapterFact;
            }
            catch (RouteRequestBlockedByOperationalHandoffException blockedException)
            {
                completionReason = $"blocked:{blockedException.BlockedReason}";
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][Route] RouteRequestRejectedByPolicy routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' blockedReason='{blockedException.BlockedReason}' detail='{blockedException.BlockedDetail}' source='{sourceText}' reason='{reasonText}'.",
                    DebugUtility.Colors.Info);
                return new SessionOperationalRouteCompletedFact(
                    command,
                    routeOperationId,
                    $"route_request_blocked_by_operational_handoff:{blockedException.BlockedReason}");
            }
            catch (Exception ex)
            {
                completionReason = $"failed:{ex.GetType().Name}";
                if (loadingStarted && !loadingHidden)
                {
                    await _loadingStage.ExecuteFailureCleanupAsync(
                        new OperationalLoadingCommand(
                            loadingAdapter,
                            loadingCommand,
                            sourceText,
                            reasonText),
                        loadingCompleted);
                }

                if (command.UsesTransition && fadeInCompleted && !fadeOutCompleted)
                {
                    try
                    {
                        await fadeAdapter.FadeOutAsync(command);
                    }
                    catch (Exception cleanupEx)
                    {
                        DebugUtility.LogError<SessionOperationalPipeline>(
                            $"[OBS][SessionOperationalPipeline][Transition] fade_out_cleanup_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' transitionMode='{command.TransitionMode}' transitionProfile='{command.TransitionProfileLabel}' source='{sourceText}' reason='{reasonText}' exceptionType='{cleanupEx.GetType().Name}' exceptionMessage='{cleanupEx.Message}'.");
                    }
                }

                DebugUtility.LogError<SessionOperationalPipeline>(
                    $"[OBS][SessionOperationalPipeline][Transition] route_transition_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' transitionMode='{command.TransitionMode}' transitionProfile='{command.TransitionProfileLabel}' source='{sourceText}' reason='{reasonText}' exceptionType='{ex.GetType().Name}' exceptionMessage='{ex.Message}'.");
                throw;
            }
            finally
            {
                EmitRouteOperationCompleted(new RouteOperationCompletionSignal(
                    routeIdentity,
                    routeOperationId,
                    routeOperationSucceeded,
                    completionReason));
                lock (_operationalRouteSync)
                {
                    _hasActiveOperationalRouteOperation = false;
                    _activeOperationalRouteOperationId = string.Empty;
                    _activeOperationalTransitionId = string.Empty;
                    _activeOperationalRouteIdentity = string.Empty;
                }
            }
        }

        private RouteRequestSubmissionResult TryPreflightRouteRequest(
            string routeIdentity,
            string source,
            string reason)
        {
            routeIdentity = Normalize(routeIdentity);
            lock (_operationalRouteSync)
            {
                if (_hasActiveOperationalRouteOperation)
                {
                    return new RouteRequestSubmissionResult(
                        RouteRequestSubmissionKind.IgnoredAlreadyInFlight,
                        routeIdentity,
                        "already_in_flight",
                        $"activeRouteIdentity='{_activeOperationalRouteIdentity}' activeRouteOperationId='{_activeOperationalRouteOperationId}' activeTransitionId='{_activeOperationalTransitionId}'.");
                }
            }

            if (!_lastCompletedRouteSnapshot.IsValid ||
                string.IsNullOrWhiteSpace(_lastCompletedRouteSnapshot.ActivityIdentity))
            {
                return new RouteRequestSubmissionResult(RouteRequestSubmissionKind.Accepted, routeIdentity, "accepted", string.Empty);
            }

            OperationalRouteHandoffExitPreflightResult preflightResult = _handoffExitStage.EvaluatePreflight(
                _dependencies.ResolveRouteHandoffExitPort(),
                routeIdentity,
                _lastCompletedRouteSnapshot.RouteIdentity,
                _lastCompletedRouteSnapshot.ActivityIdentity,
                source,
                reason);

            if (!preflightResult.IsValid)
            {
                return new RouteRequestSubmissionResult(
                    RouteRequestSubmissionKind.FailedInvalidConfig,
                    routeIdentity,
                    "handoff_exit_preflight_invalid_result",
                    preflightResult.ToString());
            }

            if (preflightResult.IsRejected || preflightResult.IsFailed)
            {
                return RejectByPolicy(routeIdentity, preflightResult.Reason, source, reason, preflightResult.Detail);
            }

            return new RouteRequestSubmissionResult(RouteRequestSubmissionKind.Accepted, routeIdentity, "accepted", string.Empty);
        }

        private RouteRequestSubmissionResult RejectByPolicy(
            string routeIdentity,
            string reason,
            string source,
            string reasonDetail,
            string policyDetail)
        {
            string normalizedPolicyDetail = Normalize(policyDetail);
            DebugUtility.LogWarning<SessionOperationalPipeline>(
                $"[OBS][SessionOperationalPipeline][Route] RouteRequestBlockedByOperationalHandoff routeIdentity='{routeIdentity}' reason='{reason}' detail='{normalizedPolicyDetail}' source='{Normalize(source)}' reasonDetail='{Normalize(reasonDetail)}'.");
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Route] RouteRequestRejectedByPolicy routeIdentity='{routeIdentity}' reason='{reason}' detail='{normalizedPolicyDetail}' source='{Normalize(source)}' reasonDetail='{Normalize(reasonDetail)}'.",
                DebugUtility.Colors.Info);
            return new RouteRequestSubmissionResult(
                RouteRequestSubmissionKind.RejectedByPolicy,
                routeIdentity,
                reason,
                normalizedPolicyDetail);
        }

        private void EmitRouteOperationCompleted(RouteOperationCompletionSignal signal)
        {
            RouteOperationCompleted?.Invoke(signal);
        }

        public bool TryBeginRouteOperation(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            string normalizedRouteOperationId = Normalize(routeOperationId);
            string normalizedTransitionId = Normalize(transitionId);
            string normalizedRouteId = Normalize(routeId);
            string normalizedRouteProfileId = Normalize(routeProfileId);
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);

            if (string.IsNullOrWhiteSpace(normalizedRouteOperationId) ||
                string.IsNullOrWhiteSpace(normalizedTransitionId) ||
                transitionSequence <= 0 ||
                string.IsNullOrWhiteSpace(normalizedRouteId) ||
                string.IsNullOrWhiteSpace(normalizedRouteProfileId) ||
                string.IsNullOrWhiteSpace(normalizedSource) ||
                string.IsNullOrWhiteSpace(normalizedReason))
            {
                return Reject(
                    SessionOperationalFactKind.IgnoredForeignOrStale,
                    SessionOperationalStage.Unknown,
                    normalizedSource,
                    normalizedReason,
                    "route operation start ignored because the identity payload is incomplete.");
            }

            _state.Reset(
                _sessionOperationalPipelineId,
                normalizedRouteOperationId,
                normalizedTransitionId,
                transitionSequence,
                normalizedRouteId,
                normalizedRouteProfileId);

            return TryRecordStage(
                SessionOperationalStage.RouteOperationStarted,
                normalizedRouteOperationId,
                normalizedTransitionId,
                transitionSequence,
                normalizedRouteId,
                normalizedRouteProfileId,
                normalizedSource,
                normalizedReason,
                "Route operation started.");
        }

        public bool TryObserveNavigationIntent(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.NavigationIntentObserved,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Navigation intent observed.");
        }

        public bool TryObserveRouteResolved(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.RouteResolved,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Route resolved.");
        }

        public bool TryObserveTransitionRequested(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.TransitionRequested,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Transition requested.");
        }

        public bool TryObserveTransitionStarted(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.TransitionStarted,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Transition started.");
        }

        public bool TryObserveCurtainClosed(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.CurtainClosed,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Curtain closed.");
        }

        public bool TryObservePreviousRouteTeardownSkipped(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.PreviousRouteTeardownSkipped,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Previous route teardown skipped.");
        }

        public bool TryObserveRoutePhysicalApply(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.RoutePhysicalApplyObserved,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Route physical apply observed.");
        }

        public bool TryObserveScenesReady(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.ScenesReadyObserved,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Scenes ready observed.");
        }

        public bool TryObserveSetupNoOp(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.SessionOperationalSetupNoOp,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Session operational setup no-op.");
        }

        public bool TryObservePlayerPreparationObserved(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.PlayerPreparationObserved,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Player preparation observed no-op.");
        }

        public bool TryObserveInputCapabilityPrepared(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string routeClass,
            SessionOperationalInputPolicy inputPolicy,
            SessionOperationalInputModeKind initialInputMode,
            string source,
            string reason)
        {
            _state.SetInputModeContext(Normalize(routeClass), inputPolicy, initialInputMode);
            return TryRecordStage(
                SessionOperationalStage.InputCapabilityPrepared,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Input capability prepared.");
        }

        public bool TryObserveInitialInputModePrepared(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string routeClass,
            SessionOperationalInputPolicy inputPolicy,
            SessionOperationalInputModeKind initialInputMode,
            string source,
            string reason)
        {
            _state.SetInputModeContext(Normalize(routeClass), inputPolicy, initialInputMode);
            return TryRecordStage(
                SessionOperationalStage.InitialInputModePrepared,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Initial input mode prepared.");
        }

        private void DispatchInitialInputModeCommandOrFail(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string routeClass,
            SessionOperationalInputPolicy inputPolicy,
            SessionOperationalInputModeKind initialInputMode,
            string source,
            string reason)
        {
            string normalizedRouteOperationId = Normalize(routeOperationId);
            string normalizedTransitionId = Normalize(transitionId);
            string normalizedRouteId = Normalize(routeId);
            string normalizedRouteProfileId = Normalize(routeProfileId);
            string normalizedRouteClass = Normalize(routeClass);
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);

            if (_state.CurrentStage != SessionOperationalStage.InitialInputModePrepared ||
                _state.RouteOperationId != normalizedRouteOperationId ||
                _state.TransitionId != normalizedTransitionId ||
                _state.TransitionSequence != transitionSequence ||
                _state.RouteId != normalizedRouteId ||
                _state.RouteProfileId != normalizedRouteProfileId)
            {
                throw new InvalidOperationException(
                    $"[FATAL][H1][SessionOperationalPipeline][InputMode] Cannot dispatch SessionOperationalInputModeCommand before matching InitialInputModePrepared fact routeIdentity='{normalizedRouteId}' routeOperationId='{normalizedRouteOperationId}' transitionId='{normalizedTransitionId}' routeSequence='{transitionSequence}' currentStage='{_state.CurrentStage}' currentRouteOperationId='{_state.RouteOperationId}' currentTransitionId='{_state.TransitionId}' currentRouteSequence='{_state.TransitionSequence}' source='{normalizedSource}' reason='{normalizedReason}'.");
            }

            _state.SetInputModeContext(normalizedRouteClass, inputPolicy, initialInputMode);

            SessionOperationalIdentity identity = new(
                _sessionOperationalPipelineId,
                normalizedRouteOperationId,
                normalizedTransitionId,
                transitionSequence,
                normalizedRouteId,
                normalizedRouteProfileId,
                normalizedSource,
                normalizedReason,
                SessionOperationalStage.InitialInputModePrepared);

            SessionOperationalInputModeCommand inputModeCommand = new(
                identity,
                _state.CurrentInitialInputMode,
                _state.RouteClass);

            if (!inputModeCommand.IsValid)
            {
                throw new InvalidOperationException("Cannot emit invalid operational input mode command.");
            }

            _state.AppendTrace(
                $"[OBS][SessionOperationalPipeline][InputMode] command='SessionOperationalInputModeCommand' routeIdentity='{identity.RouteId}' routeOperationId='{identity.RouteOperationId}' transitionId='{identity.TransitionId}' routeSequence='{identity.TransitionSequence}' contextSignature='{inputModeCommand.ContextSignature}' operationalSurfaceKind='{Normalize(_state.RouteClass)}' inputPolicy='{_state.CurrentInputPolicy}' initialInputMode='{inputModeCommand.InitialInputMode}' source='{inputModeCommand.Source}' reason='{inputModeCommand.Reason}'.");

            EventBus<SessionOperationalInputModeCommand>.Raise(inputModeCommand);
        }

        public bool TryObservePauseCapabilityPrepared(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.PauseCapabilityPrepared,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Pause capability prepared.");
        }

        public bool TryObserveReadyToOpenCurtain(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.ReadyToOpenCurtain,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Ready to open curtain.");
        }

        public bool TryObserveTransitionCompleted(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            return TryRecordStage(
                SessionOperationalStage.TransitionCompletedObserved,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Transition completed observed.");
        }

        public bool TryCompleteRouteOperation(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            if (!TryRecordStage(
                    SessionOperationalStage.Completed,
                    routeOperationId,
                    transitionId,
                    transitionSequence,
                    routeId,
                    routeProfileId,
                    source,
                    reason,
                    "Completed."))
            {
                return false;
            }

            _state.MarkCompleted();
            return true;
        }

        public string DumpState()
        {
            return $"[OBS][SessionOperationalPipeline] pipelineId='{_state.SessionOperationalPipelineId}' routeOperationId='{_state.RouteOperationId}' transitionId='{_state.TransitionId}' transitionSequence='{_state.TransitionSequence}' routeId='{_state.RouteId}' routeProfileId='{_state.RouteProfileId}' routeClass='{_state.RouteClass}' inputPolicy='{_state.CurrentInputPolicy}' initialInputMode='{_state.CurrentInitialInputMode}' stage='{_state.CurrentStage}' started='{_state.HasStarted}' completed='{_state.HasCompleted}' factsCount='{_state.Facts.Count}'";
        }

        private bool TryRecordStage(
            SessionOperationalStage stage,
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason,
            string message)
        {
            string normalizedRouteOperationId = Normalize(routeOperationId);
            string normalizedTransitionId = Normalize(transitionId);
            string normalizedRouteId = Normalize(routeId);
            string normalizedRouteProfileId = Normalize(routeProfileId);
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);
            string normalizedMessage = Normalize(message);

            if (stage == SessionOperationalStage.Unknown ||
                string.IsNullOrWhiteSpace(normalizedRouteOperationId) ||
                string.IsNullOrWhiteSpace(normalizedTransitionId) ||
                transitionSequence <= 0 ||
                string.IsNullOrWhiteSpace(normalizedRouteId) ||
                string.IsNullOrWhiteSpace(normalizedRouteProfileId) ||
                string.IsNullOrWhiteSpace(normalizedSource) ||
                string.IsNullOrWhiteSpace(normalizedReason))
            {
                return Reject(
                    SessionOperationalFactKind.IgnoredForeignOrStale,
                    stage,
                    normalizedSource,
                    normalizedReason,
                    $"Stage '{stage}' ignored because the identity payload is incomplete.");
            }

            SessionOperationalTransitionKey incomingTransitionKey = BuildTransitionKey(
                normalizedRouteOperationId,
                normalizedTransitionId,
                transitionSequence,
                normalizedRouteId,
                normalizedRouteProfileId);
            SessionOperationalStageKey incomingStageKey = new(incomingTransitionKey, stage);

            if (!CanAcceptStage(incomingStageKey))
            {
                return Reject(
                    SessionOperationalFactKind.IgnoredForeignOrStale,
                    stage,
                    normalizedSource,
                    normalizedReason,
                    $"Stage '{stage}' ignored because it is foreign, stale, or out of order.");
            }

            SessionOperationalIdentity identity = new(
                _sessionOperationalPipelineId,
                normalizedRouteOperationId,
                normalizedTransitionId,
                transitionSequence,
                normalizedRouteId,
                normalizedRouteProfileId,
                normalizedSource,
                normalizedReason,
                stage);

            SessionOperationalFact fact = new(
                MapFactKind(stage),
                identity,
                normalizedSource,
                normalizedReason,
                normalizedMessage);

            if (!fact.IsValid)
            {
                throw new InvalidOperationException($"Cannot emit invalid operational fact for stage '{stage}'.");
            }

            _state.SetCurrentIdentity(identity);
            _state.MarkStarted();
            _state.AppendFact(fact);
            _state.AppendTrace(
                $"[OBS][SessionOperationalPipeline] fact='{fact.Kind}' stage='{fact.Identity.Stage}' routeOperationId='{fact.Identity.RouteOperationId}' transitionId='{fact.Identity.TransitionId}' transitionSequence='{fact.Identity.TransitionSequence}' routeId='{fact.Identity.RouteId}' routeProfileId='{fact.Identity.RouteProfileId}' source='{fact.Source}' reason='{fact.Reason}' message='{fact.Message}'");

            if (stage == SessionOperationalStage.InputCapabilityPrepared ||
                stage == SessionOperationalStage.InitialInputModePrepared)
            {
                _state.AppendTrace(
                    $"[OBS][SessionOperationalPipeline][InputMode] fact='{fact.Kind}' stage='{fact.Identity.Stage}' routeId='{fact.Identity.RouteId}' routeProfileId='{fact.Identity.RouteProfileId}' operationalSurfaceKind='{Normalize(_state.RouteClass)}' inputPolicy='{_state.CurrentInputPolicy}' inputMode='{_state.CurrentInitialInputMode}' source='{fact.Source}' reason='{fact.Reason}'");
            }

            if (stage == SessionOperationalStage.Completed)
            {
                _state.MarkCompleted();
            }

            return true;
        }

        private bool CanAcceptStage(SessionOperationalStageKey stageKey)
        {
            if (!stageKey.IsValid)
            {
                return false;
            }

            SessionOperationalTransitionKey activeTransitionKey = BuildTransitionKey(
                _state.RouteOperationId,
                _state.TransitionId,
                _state.TransitionSequence,
                _state.RouteId,
                _state.RouteProfileId);
            if (stageKey.TransitionKey != activeTransitionKey)
            {
                return false;
            }

            if (!_state.HasStarted)
            {
                return _stageOrderPolicy.CanStart(stageKey.Stage);
            }

            if (_state.HasCompleted)
            {
                return false;
            }

            // Aceita progresso monotônico/sparse:
            // - mesma identity já validada acima
            // - não aceita retrocesso
            // - não aceita duplicata do stage atual
            // - não depende do valor numérico do enum
            return _stageOrderPolicy.CanAdvance(_state.CurrentStage, stageKey.Stage);
        }

        private SessionOperationalTransitionKey BuildTransitionKey(
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string routeId,
            string routeProfileId)
        {
            SessionOperationalRouteKey routeKey = new(
                _sessionOperationalPipelineId,
                routeId,
                routeOperationId,
                routeId,
                routeProfileId,
                routeSequence);
            return new SessionOperationalTransitionKey(routeKey, transitionId);
        }

        private bool Reject(
            SessionOperationalFactKind factKind,
            SessionOperationalStage stage,
            string source,
            string reason,
            string message)
        {
            SessionOperationalIdentity identity = new(
                _sessionOperationalPipelineId,
                _state.RouteOperationId,
                _state.TransitionId,
                _state.TransitionSequence,
                _state.RouteId,
                _state.RouteProfileId,
                source,
                reason,
                stage);

            if (!identity.IsValid)
            {
                _state.AppendTrace(
                    $"[OBS][SessionOperationalPipeline] rejected_stage='{stage}' source='{source}' reason='{reason}' message='{message}'");
                return false;
            }

            SessionOperationalFact fact = new(factKind, identity, source, reason, message);
            _state.AppendFact(fact);
            _state.AppendTrace(
                $"[OBS][SessionOperationalPipeline] fact='{fact.Kind}' stage='{fact.Identity.Stage}' source='{fact.Source}' reason='{fact.Reason}' message='{fact.Message}'");
            return false;
        }

        private static SessionOperationalFactKind MapFactKind(SessionOperationalStage stage)
        {
            return stage switch
            {
                SessionOperationalStage.RouteOperationStarted => SessionOperationalFactKind.RouteOperationStarted,
                SessionOperationalStage.NavigationIntentObserved => SessionOperationalFactKind.NavigationIntentObserved,
                SessionOperationalStage.RouteResolved => SessionOperationalFactKind.RouteResolved,
                SessionOperationalStage.TransitionRequested => SessionOperationalFactKind.TransitionRequested,
                SessionOperationalStage.TransitionStarted => SessionOperationalFactKind.TransitionStarted,
                SessionOperationalStage.CurtainClosed => SessionOperationalFactKind.CurtainClosed,
                SessionOperationalStage.PreviousRouteTeardownSkipped => SessionOperationalFactKind.PreviousRouteTeardownSkipped,
                SessionOperationalStage.RoutePhysicalApplyObserved => SessionOperationalFactKind.RoutePhysicalApplyObserved,
                SessionOperationalStage.ScenesReadyObserved => SessionOperationalFactKind.ScenesReadyObserved,
                SessionOperationalStage.SessionOperationalSetupNoOp => SessionOperationalFactKind.SessionOperationalSetupNoOp,
                SessionOperationalStage.PlayerPreparationObserved => SessionOperationalFactKind.PlayerPreparationObserved,
                SessionOperationalStage.InputCapabilityPrepared => SessionOperationalFactKind.InputCapabilityPrepared,
                SessionOperationalStage.InitialInputModePrepared => SessionOperationalFactKind.InitialInputModePrepared,
                SessionOperationalStage.PauseCapabilityPrepared => SessionOperationalFactKind.PauseCapabilityPrepared,
                SessionOperationalStage.ReadyToOpenCurtain => SessionOperationalFactKind.ReadyToOpenCurtain,
                SessionOperationalStage.TransitionCompletedObserved => SessionOperationalFactKind.TransitionCompletedObserved,
                SessionOperationalStage.Completed => SessionOperationalFactKind.Completed,
                _ => SessionOperationalFactKind.Unknown
            };
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string ResolveSceneName(SceneKeyAsset sceneKey, string fieldName)
        {
            if (sceneKey == null)
            {
                HardFailFastH1.Trigger(typeof(SessionOperationalPipeline),
                    $"[FATAL][Config][SessionOperationalPipeline] {fieldName} is required.");
            }

            if (string.IsNullOrWhiteSpace(sceneKey.SceneName))
            {
                HardFailFastH1.Trigger(typeof(SessionOperationalPipeline),
                    $"[FATAL][Config][SessionOperationalPipeline] {fieldName} requires a SceneKeyAsset with a non-empty SceneName. asset='{sceneKey.name}'.");
            }

            return sceneKey.SceneName.Trim();
        }

        private static string BuildRouteOperationId(string routeIdentity, string activeScene, int sequence)
        {
            return SessionOperationalObservableIdFormatter.BuildRouteOperationId(routeIdentity, activeScene, sequence);
        }

        private static string BuildTransitionId(string routeIdentity, string activeScene, int sequence)
        {
            return SessionOperationalObservableIdFormatter.BuildTransitionId(routeIdentity, activeScene, sequence);
        }

        private ISceneCompositionAdapter ResolveRouteExecutorOrFail()
        {
            return _dependencies.SceneCompositionAdapter;
        }

        private IFadeAdapter ResolveFadeAdapterOrFail()
        {
            return _dependencies.FadeAdapter;
        }

        private ILoadingAdapter ResolveLoadingAdapterOrFail()
        {
            return _dependencies.LoadingAdapter;
        }

        private IAudioAdapter ResolveAudioAdapterOrFail()
        {
            return _dependencies.AudioAdapter;
        }

        private static string BuildActivitySaveKey(string activityIdentity)
        {
            string normalized = Normalize(activityIdentity);
            return string.IsNullOrWhiteSpace(normalized) ? string.Empty : $"activity:{normalized}";
        }

        private readonly struct LoadedRouteActivitySnapshotPayloadContext
        {
            public LoadedRouteActivitySnapshotPayloadContext(
                string activityIdentity,
                LoadedSessionActivitySnapshotPayload payload,
                int payloadSize)
            {
                ActivityIdentity = Normalize(activityIdentity);
                Payload = payload;
                PayloadSize = payloadSize < 0 ? 0 : payloadSize;
            }

            public string ActivityIdentity { get; }
            public LoadedSessionActivitySnapshotPayload Payload { get; }
            public int PayloadSize { get; }
            public bool IsValid =>
                !string.IsNullOrWhiteSpace(ActivityIdentity) &&
                Payload.IsValid &&
                PayloadSize > 0;
        }

        private RuntimeModeConfig ResolveRuntimeModeConfigOrFail()
        {
            return _dependencies.RuntimeModeConfig;
        }

        private OperationalRouteMaterializationCommand BuildMaterializationCommand(
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRouteCommand command,
            ISceneCompositionAdapter routeExecutor,
            ILoadingAdapter loadingAdapter,
            SessionOperationalLoadingCommand loadingCommand,
            RouteActivitySavePlan routeActivitySavePlan,
            SessionOperationalRouteSnapshot previousCompletedRoute,
            string activeSceneName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason,
            string sourceText,
            string reasonText)
        {
            SessionOperationalInputPolicy inputPolicy = command.InputPolicy;
            string routeClass = command.SurfaceKind.ToString();
            _dependencies.TryResolveSaveStateService(out ISaveStateService saveStateService);

            return new OperationalRouteMaterializationCommand
            {
                RouteIdentity = routeIdentity,
                RouteOperationId = routeOperationId,
                TransitionId = transitionId,
                RouteSequence = routeSequence,
                Source = sourceText,
                Reason = reasonText,
                ApplyOperationalRouteAsync = () => routeExecutor.ApplyOperationalRouteAsync(command),
                RouteCameraPresentationStage = _routeCameraPresentationStage,
                RouteCameraPresentationCommand = new OperationalRouteCameraPresentationCommand(
                    _dependencies.RouteCameraAdapter,
                    command,
                    previousCompletedRoute.RouteIdentity,
                    activeSceneName,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    sourceText,
                    reasonText),
                LoadingStage = _loadingStage,
                LoadingCommand = new OperationalLoadingCommand(
                    loadingAdapter,
                    loadingCommand,
                    sourceText,
                    reasonText),
                RouteActivitySaveLoadOnEnterStage = _routeActivitySaveLoadOnEnterStage,
                RouteActivitySaveLoadOnEnterCommand = new OperationalRouteActivitySaveLoadOnEnterCommand(
                    runtimeModeConfig,
                    command,
                    routeActivitySavePlan,
                    _dependencies.ActivitySaveAdapter,
                    _dependencies.ProgressionSlotContextResolver,
                    saveStateService,
                    this,
                    RouteActivitySnapshotSchemaId,
                    sourceText,
                    reasonText),
                InputPreparationStage = _inputPreparationStage,
                InputPreparationCommand = new OperationalInputPreparationCommand(
                    runtimeModeConfig,
                    command.Plan,
                    inputPolicy,
                    routeClass,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    source,
                    reason,
                    initialInputMode => TryObserveInputCapabilityPrepared(
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        routeIdentity,
                        routeIdentity,
                        routeClass,
                        inputPolicy,
                        initialInputMode,
                        source,
                        reason),
                    initialInputMode => TryObserveInitialInputModePrepared(
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        routeIdentity,
                        routeIdentity,
                        routeClass,
                        inputPolicy,
                        initialInputMode,
                        source,
                        reason),
                    initialInputMode => DispatchInitialInputModeCommandOrFail(
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        routeIdentity,
                        routeIdentity,
                        routeClass,
                        inputPolicy,
                        initialInputMode,
                        source,
                        reason)),
                PlayerPreparationStage = _playerPreparationStage,
                PlayerPreparationCommand = new OperationalPlayerPreparationCommand(
                    _sessionOperationalPipelineId,
                    command,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    sourceText,
                    reasonText),
                ConsumerPresentationPreparationStage = _consumerPresentationPreparationStage,
                ConsumerPresentationPreparationCommand = new OperationalConsumerPresentationPreparationCommand(
                    _dependencies.ResolveRouteConsumerPresentationPort(),
                    command,
                    activeSceneName,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    command.HandoffSessionStateId,
                    sourceText,
                    reasonText),

                ConsumerEntryAndReadinessStage = _consumerEntryAndReadinessStage,
                ConsumerEntryAndReadinessCommand = new OperationalConsumerEntryAndReadinessCommand(
                    _dependencies.ResolveRouteConsumerEntryPort(),
                    _dependencies.ResolveRouteConsumerReadinessPort(),
                    command,
                    loadingCommand,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    sourceText,
                    reasonText),
            };
        }


    }
}
