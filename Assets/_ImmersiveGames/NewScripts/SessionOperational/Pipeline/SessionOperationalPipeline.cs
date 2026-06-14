using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
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
        private readonly OperationalFactRecorder _factRecorder;
        private readonly SessionOperationalRoutePlanResolver _routePlanResolver = new();
        private readonly OperationalRouteSetupStage _routeSetupStage;
        private readonly OperationalTransitionBlackoutStage _transitionBlackoutStage;
        private readonly OperationalPreviousRouteExitBoundary _previousRouteExitBoundary;
        private readonly OperationalSceneCompositionStage _sceneCompositionStage;
        private readonly OperationalHandoffExitStage _handoffExitStage;
        private readonly OperationalRouteCameraReleasePreviousStage _routeCameraReleasePreviousStage;
        private readonly OperationalRouteCameraPresentationStage _routeCameraPresentationStage;
        private readonly OperationalInputPreparationStage _inputPreparationStage;
        private readonly OperationalPlayerParticipationStage _playerParticipationStage;
        private readonly OperationalActivityCameraPresentationStage _activityCameraPresentationStage;
        private readonly OperationalActivityCameraReleasePreviousStage _activityCameraReleasePreviousStage;
        private readonly OperationalConsumerEntryAndReadinessStage _consumerEntryAndReadinessStage;
        private readonly OperationalRouteActivitySaveLoadOnEnterStage _routeActivitySaveLoadOnEnterStage;
        private readonly OperationalRouteActivitySaveSaveOnExitStage _routeActivitySaveSaveOnExitStage;
        private readonly OperationalRouteMaterializationBoundary _routeMaterializationBoundary;
        private readonly OperationalLoadingStage _loadingStage;
        private readonly OperationalRouteAudioStage _routeAudioStage;
        private readonly OperationalRouteRevealStage _routeRevealStage;
        private readonly OperationalFadeStage _fadeStage;
        private readonly OperationalRouteCompletionStage _routeCompletionStage;
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

            _factRecorder = new OperationalFactRecorder(
                _state,
                _sessionOperationalPipelineId,
                _stageOrderPolicy);

            // Etapa 3: pass _factRecorder to all stages for centralized fact emission (canonization of Facts)
            _fadeStage = new OperationalFadeStage(_factRecorder, _dependencies.ResolveFadePort);
            _transitionBlackoutStage = new OperationalTransitionBlackoutStage(_factRecorder);
            _sceneCompositionStage = new OperationalSceneCompositionStage(_factRecorder, _dependencies.ResolveSceneCompositionPort);
            _handoffExitStage = new OperationalHandoffExitStage(
                _factRecorder,
                _dependencies.ResolveRouteHandoffExitPort,
                _dependencies.ResolveSessionActivityRouteExitTeardownBoundary);
            _routeCameraReleasePreviousStage = new OperationalRouteCameraReleasePreviousStage(_factRecorder, _dependencies.RouteCameraAdapter);
            _routeCameraPresentationStage = new OperationalRouteCameraPresentationStage(_factRecorder, _dependencies.RouteCameraAdapter);
            _inputPreparationStage = new OperationalInputPreparationStage(_factRecorder, _dependencies.ResolveInputModeRequestPort);
            _playerParticipationStage = new OperationalPlayerParticipationStage(_factRecorder, _dependencies.ResolveRoutePlayerParticipationEndpoint, _dependencies.ResolvePlayerParticipationRuntime);
            _routeCompletionStage = new OperationalRouteCompletionStage(_factRecorder);
            _activityCameraPresentationStage = new OperationalActivityCameraPresentationStage(_factRecorder, _dependencies.ActivityCameraAdapter);
            _activityCameraReleasePreviousStage = new OperationalActivityCameraReleasePreviousStage(_factRecorder, _dependencies.ActivityCameraAdapter);
            _consumerEntryAndReadinessStage = new OperationalConsumerEntryAndReadinessStage(_factRecorder, _dependencies.ResolveRouteConsumerEntryPort, _dependencies.ResolveRouteConsumerReadinessPort);
            _routeActivitySaveLoadOnEnterStage = new OperationalRouteActivitySaveLoadOnEnterStage(
                _factRecorder,
                _dependencies.ActivitySaveAdapter,
                _dependencies.ProgressionSlotContextResolver,
                this,
                RouteActivitySnapshotSchemaId);
            _routeActivitySaveSaveOnExitStage = new OperationalRouteActivitySaveSaveOnExitStage(
                _factRecorder,
                _dependencies.ActivitySaveAdapter,
                _dependencies.ProgressionSlotContextResolver,
                ResolveActivitySnapshotPayloadProviderOrNull,
                RouteActivitySnapshotSchemaId);
            _loadingStage = new OperationalLoadingStage(_factRecorder, _dependencies.LoadingAdapter);
            _routeAudioStage = new OperationalRouteAudioStage(_factRecorder, _dependencies.ResolveRouteAudioPort);
            _routeRevealStage = new OperationalRouteRevealStage(_factRecorder);
            _routeSetupStage = new OperationalRouteSetupStage(_factRecorder);

            // Etapa 3: boundaries also receive recorder for full fact canonization
            _previousRouteExitBoundary = new OperationalPreviousRouteExitBoundary(_factRecorder);
            _routeMaterializationBoundary = new OperationalRouteMaterializationBoundary(_factRecorder);
        }

        public SessionOperationalRuntimeState State => _state;

        public bool TryQaSaveCurrentActivitySnapshot(
            string sessionStateId,
            string activityIdentity,
            string source,
            string reason,
            out string outcomeReason)
        {
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);
            var command = new OperationalRouteActivitySaveQaSaveCommand(
                _dependencies.RuntimeModeConfig,
                sessionStateId,
                activityIdentity,
                normalizedSource,
                normalizedReason);

            OperationalRouteActivitySaveSaveOnExitResult result;
            try
            {
                result = _routeActivitySaveSaveOnExitStage.ExecuteQaSaveCurrentSnapshot(command);
            }
            catch (Exception exception)
            {
                outcomeReason = $"route_activity_save_qa_failed_exception:{exception.GetType().Name}";
                DebugUtility.LogVerbose(typeof(SessionOperationalPipeline),
                    $"checkpoint='RouteActivitySaveQaSave' checkpointStatus='Failed' activityIdentity='{Normalize(activityIdentity)}' payloadResolved='unknown' payloadKind='<none>' recordCount='0' contributorResolutionKind='{Normalize(outcomeReason)}' failureReason='{Normalize(exception.Message)}' source='{normalizedSource}' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                DebugUtility.LogVerbose(typeof(SessionOperationalPipeline),
                    $"event='RouteActivitySaveQaRequested' outcomeKind='Failed' reason='{Normalize(outcomeReason)}' sessionStateId='{Normalize(sessionStateId)}' saveOwnerActivityIdentity='{Normalize(sessionStateId)}' payloadActivityIdentity='{Normalize(activityIdentity)}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                    DebugUtility.Colors.Warning);
                return false;
            }

            outcomeReason = string.IsNullOrWhiteSpace(result.Reason) ? result.Detail : result.Reason;
            bool qaSkipped = result.IsCompleted && Normalize(outcomeReason).StartsWith("qa_save_skipped_", StringComparison.Ordinal);
            string outcomeKind = result.IsCompleted ? (qaSkipped ? "Skipped" : "Saved") : "Failed";
            DebugUtility.LogVerbose(typeof(SessionOperationalPipeline),
                $"event='RouteActivitySaveQaRequested' outcomeKind='{outcomeKind}' reason='{Normalize(outcomeReason)}' sessionStateId='{Normalize(sessionStateId)}' saveOwnerActivityIdentity='{Normalize(sessionStateId)}' payloadActivityIdentity='{Normalize(activityIdentity)}' source='{normalizedSource}' reasonDetail='{normalizedReason}'.",
                result.IsCompleted ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
            return result.IsCompleted;
        }

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

            var preflight = TryPreflightRouteRequest(routeIdentity, sourceText, reasonText);
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
            out LoadedRouteActivitySnapshotPayload payload,
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
            LoadedRouteActivitySnapshotPayload payload,
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

            var runtimeModeConfig = ResolveRuntimeModeConfigOrFail();
            var persistentScenesPolicy = _dependencies.PersistentScenesPolicy;
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
                        $"rejected reason='stale_or_foreign_route' routeIdentity='{routeIdentity}' activeRouteIdentity='{_activeOperationalRouteIdentity}' activeRouteOperationId='{_activeOperationalRouteOperationId}' activeTransitionId='{_activeOperationalTransitionId}' source='{sourceText}' reason='{reasonText}'.");
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

            var setupResult = _routeSetupStage.Execute(
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
                    previousCompletedRoute.ContributorScopePolicy,
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

            var command = setupResult.RouteCommand;
            var routeActivitySavePlan = setupResult.RouteActivitySavePlan;
            var loadingCommand = setupResult.LoadingCommand;
            routeIdentity = setupResult.RouteIdentity;
            activeSceneName = setupResult.ActiveSceneName;

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
                        routeIdentity,
                        source,
                        reason))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][H1][SessionOperationalPipeline][Route] Failed to record RouteOperationStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{sourceText}' reason='{reasonText}'.");
                }

                var transitionPlanReadyResult = _routeCompletionStage.ExecuteTransitionPlanReady(
                    new OperationalRouteCompletionCommand(
                        _sessionOperationalPipelineId,
                        command,
                        sourceText,
                        reasonText));
                if (!transitionPlanReadyResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Transition] OperationalRouteCompletionStage TransitionPlanReady failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{transitionPlanReadyResult.Kind}' reason='{transitionPlanReadyResult.Reason}' detail='{transitionPlanReadyResult.Detail}'.");
                }

                var loadingStartResult = await _loadingStage.ExecuteStartAsync(
                    new OperationalLoadingCommand(
                        loadingCommand,
                        sourceText,
                        reasonText));
                if (!loadingStartResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Loading] OperationalLoadingStage start failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{loadingStartResult.Kind}' reason='{loadingStartResult.Reason}' detail='{loadingStartResult.Detail}'.");
                }

                loadingStarted = loadingStartResult.LoadingStarted;

                var blackoutCommand = new OperationalTransitionBlackoutCommand(
                    command,
                    sourceText,
                    reasonText);
                _transitionBlackoutStage.Begin(blackoutCommand);

                bool blackoutFadeInCompleted = false;
                if (command.UsesTransition)
                {
                    var fadeResult = await _fadeStage.ExecuteAsync(
                        new OperationalFadeCommand(
                            command,
                            OperationalFadeOperationKind.CloseCurtain,
                            sourceText,
                            reasonText));
                    blackoutFadeInCompleted = fadeResult.IsCompleted;
                }

                var blackoutResult = _transitionBlackoutStage.Complete(
                    blackoutCommand,
                    blackoutFadeInCompleted);

                if (blackoutResult is { IsCompleted: false, IsSkipped: false })
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Transition] OperationalTransitionBlackoutStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{blackoutResult.Kind}' reason='{blackoutResult.Reason}' detail='{blackoutResult.Detail}'.");
                }

                fadeInCompleted = blackoutResult.FadeInCompleted;

                var blackoutLoadingResult = await _loadingStage.ExecuteTransitionBlackoutProgressAsync(
                    new OperationalLoadingCommand(
                        loadingCommand,
                        sourceText,
                        reasonText),
                    blackoutResult);
                if (!blackoutLoadingResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Loading] OperationalLoadingStage blackout progress failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{blackoutLoadingResult.Kind}' reason='{blackoutLoadingResult.Reason}' detail='{blackoutLoadingResult.Detail}'.");
                }

                var previousRouteExitBoundaryCommand = new OperationalPreviousRouteExitBoundaryCommand(
                    command,
                    previousCompletedRoute.RouteIdentity,
                    previousCompletedRoute.ActivityIdentity,
                    sourceText,
                    reasonText);

                var previousRouteExitBeginResult =
                    _previousRouteExitBoundary.Begin(previousRouteExitBoundaryCommand);
                if (!previousRouteExitBeginResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][PreviousRouteExit] OperationalPreviousRouteExitBoundary begin failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{previousRouteExitBeginResult.Kind}' reason='{previousRouteExitBeginResult.Reason}' detail='{previousRouteExitBeginResult.Detail}'.");
                }

                var handoffExitResult = await _handoffExitStage.ExecuteAsync(
                    new OperationalHandoffExitCommand(
                        command,
                        previousCompletedRoute.RouteIdentity,
                        previousCompletedRoute.ActivityIdentity,
                        previousCompletedRoute.ActiveSceneKey,
                        previousCompletedRoute.RouteOwnedLoadedSceneKeys,
                        command.FinalScenesToUnload,
                        sourceText,
                        reasonText));
                if (!handoffExitResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][PreviousRouteExit] OperationalHandoffExitStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{handoffExitResult.Kind}' reason='{handoffExitResult.Reason}' detail='{handoffExitResult.Detail}'.");
                }

                var activityCameraReleasePreviousResult =
                    _activityCameraReleasePreviousStage.Execute(
                        new OperationalActivityCameraReleasePreviousCommand(
                            command,
                            previousCompletedRoute.RouteIdentity,
                            previousCompletedRoute.ActivityIdentity,
                            sourceText,
                            reasonText));
                if (!activityCameraReleasePreviousResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][PreviousRouteExit] OperationalActivityCameraReleasePreviousStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{activityCameraReleasePreviousResult.Kind}' reason='{activityCameraReleasePreviousResult.Reason}' detail='{activityCameraReleasePreviousResult.Detail}'.");
                }

                var routeCameraReleasePreviousResult =
                    _routeCameraReleasePreviousStage.Execute(
                        new OperationalRouteCameraReleasePreviousCommand(
                            command,
                            previousCompletedRoute.RouteIdentity,
                            sourceText,
                            reasonText));
                if (!routeCameraReleasePreviousResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][PreviousRouteExit] OperationalRouteCameraReleasePreviousStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{routeCameraReleasePreviousResult.Kind}' reason='{routeCameraReleasePreviousResult.Reason}' detail='{routeCameraReleasePreviousResult.Detail}'.");
                }

                var saveOnExitResult =
                    _routeActivitySaveSaveOnExitStage.Execute(
                        new OperationalRouteActivitySaveSaveOnExitCommand(
                            runtimeModeConfig,
                            command,
                            routeActivitySavePlan,
                            sourceText,
                            reasonText));
                if (!saveOnExitResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][PreviousRouteExit] OperationalRouteActivitySaveSaveOnExitStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{saveOnExitResult.Kind}' reason='{saveOnExitResult.Reason}' detail='{saveOnExitResult.Detail}'.");
                }

                var sessionResetResult = ExecuteSessionResetAfterPreviousRouteExitIfRequired(
                    command,
                    previousCompletedRoute,
                    handoffExitResult,
                    sourceText,
                    reasonText);
                if (sessionResetResult is { IsValid: true, IsFailed: true })
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][PreviousRouteExit] Session reset after previous route exit failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' result='{sessionResetResult}'.");
                }

                var previousRouteExitCompleteResult =
                    _previousRouteExitBoundary.Complete(previousRouteExitBoundaryCommand);
                if (!previousRouteExitCompleteResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][PreviousRouteExit] OperationalPreviousRouteExitBoundary complete failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{previousRouteExitCompleteResult.Kind}' reason='{previousRouteExitCompleteResult.Reason}' detail='{previousRouteExitCompleteResult.Detail}'.");
                }

                var materializationBoundaryCommand = BuildMaterializationBoundaryCommand(
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    sourceText,
                    reasonText);
                _routeMaterializationBoundary.Begin(materializationBoundaryCommand);

                var sceneCompositionResult = await _sceneCompositionStage.ExecuteAsync(
                    BuildSceneCompositionCommand(
                        command,
                        activeSceneName,
                        sourceText,
                        reasonText));
                if (!sceneCompositionResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][SceneComposition] OperationalSceneCompositionStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{sceneCompositionResult.Kind}' reason='{sceneCompositionResult.Reason}' detail='{sceneCompositionResult.Detail}'.");
                }

                var adapterFact = sceneCompositionResult.CompletionFact;
                var materializationLoadingCommand = BuildOperationalLoadingCommand(
                    loadingCommand,
                    sourceText,
                    reasonText);

                var routeCameraPresentationResult = _routeCameraPresentationStage.Execute(
                    BuildRouteCameraPresentationCommand(
                        command,
                        activeSceneName,
                        routeIdentity,
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        sourceText,
                        reasonText));
                if (!routeCameraPresentationResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][RouteCamera] OperationalRouteCameraPresentationStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{routeCameraPresentationResult.Kind}' reason='{routeCameraPresentationResult.Reason}'.");
                }

                var sceneCompositionLoadingResult = await _loadingStage.ExecuteSceneCompositionCompletedAsync(materializationLoadingCommand);
                if (!sceneCompositionLoadingResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Loading] OperationalLoadingStage scene composition progress failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{sceneCompositionLoadingResult.Kind}' reason='{sceneCompositionLoadingResult.Reason}' detail='{sceneCompositionLoadingResult.Detail}'.");
                }

                var loadOnEnterResult = _routeActivitySaveLoadOnEnterStage.Execute(
                    BuildRouteActivitySaveLoadOnEnterCommand(
                        runtimeModeConfig,
                        command,
                        routeActivitySavePlan,
                        sourceText,
                        reasonText));
                if (!loadOnEnterResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][RouteActivitySave] OperationalRouteActivitySaveLoadOnEnterStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{loadOnEnterResult.Kind}' reason='{loadOnEnterResult.Reason}' detail='{loadOnEnterResult.Detail}'.");
                }

                var loadedSnapshotPayloadContext =
                    loadOnEnterResult.LoadedSnapshotPayloadContext;

                var inputPreparationResult = _inputPreparationStage.Execute(
                    BuildInputPreparationCommand(
                        runtimeModeConfig,
                        command,
                        routeIdentity,
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        sourceText,
                        reasonText));
                if (!inputPreparationResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][InputCapability] OperationalInputPreparationStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.");
                }

                var playerParticipationStageResult = _playerParticipationStage.Execute(
                    BuildPlayerParticipationCommand(
                        command,
                        routeIdentity,
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        sourceText,
                        reasonText));
                if (!playerParticipationStageResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][PlayerParticipation] OperationalPlayerParticipationStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.");
                }

                var playerParticipationResult = playerParticipationStageResult.PlayerParticipationResult;
                if (playerParticipationStageResult.IsCompleted)
                {
                    var activityCameraPresentationResult = _activityCameraPresentationStage.Execute(
                        BuildActivityCameraPresentationCommand(
                            command,
                            activeSceneName,
                            routeIdentity,
                            routeOperationId,
                            transitionId,
                            routeSequence,
                            sourceText,
                            reasonText));
                    if (!activityCameraPresentationResult.IsAccepted)
                    {
                        throw new InvalidOperationException(
                            $"[FATAL][SessionOperationalPipeline][ActivityCamera] OperationalActivityCameraPresentationStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{activityCameraPresentationResult.Kind}' reason='{activityCameraPresentationResult.Reason}' detail='{activityCameraPresentationResult.Detail}'.");
                    }
                }

                var loadingState = await _loadingStage.ExecuteClosedWindowCompletionAsync(materializationLoadingCommand);

                var consumerEntryAndReadinessResult = await _consumerEntryAndReadinessStage.ExecuteAsync(
                    BuildConsumerEntryAndReadinessCommand(
                        command,
                        loadingCommand,
                        loadedSnapshotPayloadContext,
                        routeIdentity,
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        sourceText,
                        reasonText),
                    playerParticipationStageResult);
                if (!consumerEntryAndReadinessResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][ConsumerEntry] OperationalConsumerEntryAndReadinessStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{consumerEntryAndReadinessResult.Kind}' reason='{consumerEntryAndReadinessResult.Reason}' detail='{consumerEntryAndReadinessResult.Detail}'.");
                }

                var materializationResult = _routeMaterializationBoundary.Complete(
                    materializationBoundaryCommand,
                    adapterFact,
                    loadingState);
                loadingCompleted = materializationResult.LoadingCompleted;
                loadingHidden = materializationResult.LoadingHidden;

                var revealCommand = new OperationalRouteRevealCommand(
                    command,
                    sourceText,
                    reasonText);
                _routeRevealStage.Begin(revealCommand);

                var routeAudioResult = _routeAudioStage.Execute(
                    new OperationalRouteAudioCommand(
                        command,
                        sourceText,
                        reasonText));

                bool revealFadeOutCompleted = false;
                if (command.UsesTransition)
                {
                    var fadeResult = await _fadeStage.ExecuteAsync(
                        new OperationalFadeCommand(
                            command,
                            OperationalFadeOperationKind.OpenCurtain,
                            sourceText,
                            reasonText));
                    revealFadeOutCompleted = fadeResult.IsCompleted;
                }

                var revealResult = _routeRevealStage.Complete(
                    revealCommand,
                    routeAudioResult.IsCompleted,
                    revealFadeOutCompleted);

                if (!revealResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Route] OperationalRouteRevealStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{revealResult.Kind}' reason='{revealResult.Reason}' detail='{revealResult.Detail}'.");
                }

                fadeOutCompleted = revealResult.FadeOutCompleted;

                if (!TryCompleteRouteOperation(
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        routeIdentity,
                        routeIdentity,
                        sourceText,
                        reasonText))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][H1][SessionOperationalPipeline][Transition] Failed to record OperationalRouteCompleted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{sourceText}' reason='{reasonText}'.");
                }

                var completionResult = _routeCompletionStage.ExecuteCompleted(
                    new OperationalRouteCompletionCommand(
                        _sessionOperationalPipelineId,
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
                DebugUtility.LogVerbose(typeof(SessionOperationalPipeline),
                    $"RouteRequestRejectedByPolicy routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' blockedReason='{blockedException.BlockedReason}' detail='{blockedException.BlockedDetail}' source='{sourceText}' reason='{reasonText}'.",
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
                            loadingCommand,
                            sourceText,
                            reasonText),
                        loadingCompleted);
                }

                if (command.UsesTransition && fadeInCompleted && !fadeOutCompleted)
                {
                    try
                    {
                        await _fadeStage.ExecuteAsync(
                            new OperationalFadeCommand(
                                command,
                                OperationalFadeOperationKind.CleanupOpenCurtain,
                                sourceText,
                                reasonText));
                    }
                    catch (Exception cleanupEx)
                    {
                        DebugUtility.LogError<SessionOperationalPipeline>(
                            $"fade_out_cleanup_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' transitionMode='{command.TransitionMode}' transitionProfile='{command.TransitionProfileLabel}' source='{sourceText}' reason='{reasonText}' exceptionType='{cleanupEx.GetType().Name}' exceptionMessage='{cleanupEx.Message}'.");
                    }
                }

                DebugUtility.LogError<SessionOperationalPipeline>(
                    $"route_transition_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' transitionMode='{command.TransitionMode}' transitionProfile='{command.TransitionProfileLabel}' source='{sourceText}' reason='{reasonText}' exceptionType='{ex.GetType().Name}' exceptionMessage='{ex.Message}'.");
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

            var preflightResult = _handoffExitStage.EvaluatePreflight(
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
                $"RouteRequestBlockedByOperationalHandoff routeIdentity='{routeIdentity}' reason='{reason}' detail='{normalizedPolicyDetail}' source='{Normalize(source)}' reasonDetail='{Normalize(reasonDetail)}'.");
            DebugUtility.LogVerbose(typeof(SessionOperationalPipeline),
                $"RouteRequestRejectedByPolicy routeIdentity='{routeIdentity}' reason='{reason}' detail='{normalizedPolicyDetail}' source='{Normalize(source)}' reasonDetail='{Normalize(reasonDetail)}'.",
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
            string routeIdentity,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            string normalizedRouteOperationId = Normalize(routeOperationId);
            string normalizedTransitionId = Normalize(transitionId);
            string normalizedRouteIdentity = Normalize(routeIdentity);
            string normalizedRouteId = Normalize(routeId);
            string normalizedRouteProfileId = Normalize(routeProfileId);
            string normalizedSource = Normalize(source);
            string normalizedReason = Normalize(reason);

            if (string.IsNullOrWhiteSpace(normalizedRouteOperationId) ||
                string.IsNullOrWhiteSpace(normalizedTransitionId) ||
                transitionSequence <= 0 ||
                string.IsNullOrWhiteSpace(normalizedRouteIdentity) ||
                string.IsNullOrWhiteSpace(normalizedRouteId) ||
                string.IsNullOrWhiteSpace(normalizedRouteProfileId) ||
                string.IsNullOrWhiteSpace(normalizedSource) ||
                string.IsNullOrWhiteSpace(normalizedReason))
            {
                return _factRecorder.Reject(
                    SessionOperationalFactKind.IgnoredForeignOrStale,
                    SessionOperationalStage.Unknown,
                    normalizedSource,
                    normalizedReason,
                    "route operation start ignored because the identity payload is incomplete.");
            }

            _state.Reset(
                _sessionOperationalPipelineId,
                normalizedRouteIdentity,
                normalizedRouteOperationId,
                normalizedTransitionId,
                transitionSequence,
                normalizedRouteId,
                normalizedRouteProfileId);

            // Etapa 4: record directly via recorder after state setup (consolidation of fact entry point).
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.RouteOperationStarted,
                normalizedRouteIdentity,
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
            // Etapa 4: high-level macro fact now recorded directly via the recorder (order/consistency enforced there).
            // This is part of consolidating fact orchestration so the recorder is the primary/only entry point.
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.NavigationIntentObserved,
                _activeOperationalRouteIdentity,
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
            // Etapa 4: high-level macro fact recorded directly via the recorder (order/consistency now enforced centrally).
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.RouteResolved,
                _activeOperationalRouteIdentity,
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
            // Etapa 4: high-level macro fact recorded directly via the recorder.
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.TransitionRequested,
                _activeOperationalRouteIdentity,
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
            // Etapa 4: high-level macro fact recorded directly via the recorder.
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.TransitionStarted,
                _activeOperationalRouteIdentity,
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
            // Etapa 4: high-level macro fact recorded directly via the recorder.
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.CurtainClosed,
                _activeOperationalRouteIdentity,
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
            // Etapa 4: high-level macro fact recorded directly via the recorder.
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.PreviousRouteTeardownSkipped,
                _activeOperationalRouteIdentity,
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
            // Etapa 4: high-level macro fact recorded directly via the recorder.
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.RoutePhysicalApplyObserved,
                _activeOperationalRouteIdentity,
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
            // Etapa 4: high-level macro fact recorded directly via the recorder.
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.ScenesReadyObserved,
                _activeOperationalRouteIdentity,
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
            // Etapa 4: high-level macro fact recorded directly via the recorder.
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.SessionOperationalSetupNoOp,
                _activeOperationalRouteIdentity,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Session operational setup no-op.");
        }

        public bool TryObservePlayerParticipationSeedObserved(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            // Etapa 4: high-level macro fact recorded directly via the recorder.
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.PlayerParticipationSeedObserved,
                _activeOperationalRouteIdentity,
                routeOperationId,
                transitionId,
                transitionSequence,
                routeId,
                routeProfileId,
                source,
                reason,
                "Player participation seed observed no-op.");
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
            // Etapa 4: high-level macro fact recorded directly via the recorder.
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.PauseCapabilityPrepared,
                _activeOperationalRouteIdentity,
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
            // Etapa 4: high-level macro fact recorded directly via the recorder.
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.ReadyToOpenCurtain,
                _activeOperationalRouteIdentity,
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
            // Etapa 4: high-level macro fact recorded directly via the recorder.
            return _factRecorder.TryRecordStage(
                SessionOperationalStage.TransitionCompletedObserved,
                _activeOperationalRouteIdentity,
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
            // Etapa 4: direct to recorder (consolidation; order enforced in recorder for primary stage Completed).
            if (!_factRecorder.TryRecordStage(
                    SessionOperationalStage.Completed,
                    _activeOperationalRouteIdentity,
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

            return true;
        }

        // Etapa 4: private TryRecordStage wrapper fully removed.
        // All macro fact recordings now go directly through _factRecorder.TryRecordStage (which owns validation + order for primaries).
        // This completes the consolidation of high-level fact orchestration.

        public string DumpState()
        {
            return _factRecorder.DumpState();
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

        private ISessionActivitySnapshotPayloadProvider ResolveActivitySnapshotPayloadProviderOrNull()
        {
            return _dependencies.TryResolveActivitySnapshotPayloadProvider(out var provider)
                ? provider
                : null;
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
                LoadedRouteActivitySnapshotPayload payload,
                int payloadSize)
            {
                ActivityIdentity = Normalize(activityIdentity);
                Payload = payload;
                PayloadSize = payloadSize < 0 ? 0 : payloadSize;
            }

            public string ActivityIdentity { get; }
            public LoadedRouteActivitySnapshotPayload Payload { get; }
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

        private OperationalRouteCameraPresentationCommand BuildRouteCameraPresentationCommand(
            SessionOperationalRouteCommand command,
            string activeSceneName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string sourceText,
            string reasonText)
        {
            return new OperationalRouteCameraPresentationCommand(
                command,
                activeSceneName,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                sourceText,
                reasonText);
        }

        private OperationalRouteActivitySaveLoadOnEnterCommand BuildRouteActivitySaveLoadOnEnterCommand(
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRouteCommand command,
            RouteActivitySavePlan routeActivitySavePlan,
            string sourceText,
            string reasonText)
        {
            return new OperationalRouteActivitySaveLoadOnEnterCommand(
                runtimeModeConfig,
                command,
                routeActivitySavePlan,
                sourceText,
                reasonText);
        }

        private OperationalInputPreparationCommand BuildInputPreparationCommand(
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRouteCommand command,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string sourceText,
            string reasonText)
        {
            var inputPolicy = command.InputPolicy;
            string routeClass = command.SurfaceKind.ToString();
            return new OperationalInputPreparationCommand(
                _sessionOperationalPipelineId,
                runtimeModeConfig,
                command.Plan,
                inputPolicy,
                routeClass,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                sourceText,
                reasonText);
        }

        private OperationalPlayerParticipationCommand BuildPlayerParticipationCommand(
            SessionOperationalRouteCommand command,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string sourceText,
            string reasonText)
        {
            return new OperationalPlayerParticipationCommand(
                _sessionOperationalPipelineId,
                command,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                sourceText,
                reasonText);
        }

        private OperationalActivityCameraPresentationCommand BuildActivityCameraPresentationCommand(
            SessionOperationalRouteCommand command,
            string activeSceneName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string sourceText,
            string reasonText)
        {
            return new OperationalActivityCameraPresentationCommand(
                command,
                activeSceneName,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                command.HandoffSessionStateId,
                sourceText,
                reasonText);
        }

        private OperationalConsumerEntryAndReadinessCommand BuildConsumerEntryAndReadinessCommand(
            SessionOperationalRouteCommand command,
            SessionOperationalLoadingCommand loadingCommand,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string sourceText,
            string reasonText)
        {
            return new OperationalConsumerEntryAndReadinessCommand(
                command,
                loadingCommand,
                loadedSnapshotPayloadContext,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                sourceText,
                reasonText);
        }

        private OperationalSceneCompositionCommand BuildSceneCompositionCommand(
            SessionOperationalRouteCommand command,
            string activeSceneName,
            string sourceText,
            string reasonText)
        {
            return new OperationalSceneCompositionCommand(
                command,
                activeSceneName,
                sourceText,
                reasonText);
        }

        private static OperationalLoadingCommand BuildOperationalLoadingCommand(
            SessionOperationalLoadingCommand loadingCommand,
            string sourceText,
            string reasonText)
        {
            return new OperationalLoadingCommand(
                loadingCommand,
                sourceText,
                reasonText);
        }

        private SessionActivitySessionResetResult ExecuteSessionResetAfterPreviousRouteExitIfRequired(
            SessionOperationalRouteCommand command,
            SessionOperationalRouteSnapshot previousCompletedRoute,
            OperationalHandoffExitResult handoffExitResult,
            string source,
            string reason)
        {
            if (!ShouldResetSessionAfterPreviousRouteExit(command, previousCompletedRoute, handoffExitResult))
            {
                return new SessionActivitySessionResetResult(
                    SessionActivitySessionResetKind.NotRequired,
                    previousCompletedRoute.ActivityIdentity,
                    SessionActivityStage.Unknown,
                    string.Empty,
                    0,
                    0,
                    "session_reset_not_required",
                    "route_does_not_end_session");
            }

            var boundary = _dependencies.ResolveSessionActivityRouteExitTeardownBoundary();
            if (boundary == null)
            {
                throw new InvalidOperationException("[FATAL][SessionOperationalPipeline][PreviousRouteExit] SessionActivity route-exit boundary ausente para session reset after route exit.");
            }

            DebugUtility.LogVerbose(typeof(SessionOperationalPipeline),
                $"OperationalSessionResetAfterRouteExitStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' previousActivityIdentity='{previousCompletedRoute.ActivityIdentity}' destinationSurfaceKind='{command.SurfaceKind}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            var result = boundary.ResetSessionAfterRouteExit(
                previousCompletedRoute.ActivityIdentity,
                source,
                reason);

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"OperationalSessionResetAfterRouteExitCompleted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' previousActivityIdentity='{previousCompletedRoute.ActivityIdentity}' result='{result}' source='{source}' reason='{reason}'.",
                result.IsFailed ? DebugUtility.Colors.Warning : DebugUtility.Colors.Success);

            return result;
        }

        private static bool ShouldResetSessionAfterPreviousRouteExit(
            SessionOperationalRouteCommand command,
            SessionOperationalRouteSnapshot previousCompletedRoute,
            OperationalHandoffExitResult handoffExitResult)
        {
            if (!previousCompletedRoute.IsValid || string.IsNullOrWhiteSpace(previousCompletedRoute.ActivityIdentity))
            {
                return false;
            }

            if (!handoffExitResult.IsCompleted)
            {
                return false;
            }

            if (command.CompletionHandoff != SessionOperationalRouteCompletionHandoffKind.NoHandoff)
            {
                return false;
            }

            return command.SurfaceKind == OperationalSurfaceKind.FrontendMenu;
        }

        private static OperationalRouteMaterializationBoundaryCommand BuildMaterializationBoundaryCommand(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string sourceText,
            string reasonText)
        {
            return new OperationalRouteMaterializationBoundaryCommand(
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                sourceText,
                reasonText);
        }



    }
}
