using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Semantic.Preparation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SaveRuntime.Contracts;
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
        private readonly OperationalFactRecorder _factRecorder;
        private readonly SessionOperationalRoutePlanResolver _routePlanResolver = new();
        private readonly OperationalRouteSetupStage _routeSetupStage = new();
        private readonly OperationalTransitionBlackoutStage _transitionBlackoutStage;
        private readonly OperationalPreviousRouteExitBoundary _previousRouteExitBoundary = new();
        private readonly OperationalSceneCompositionStage _sceneCompositionStage;
        private readonly OperationalHandoffExitStage _handoffExitStage;
        private readonly OperationalRouteCameraReleasePreviousStage _routeCameraReleasePreviousStage;
        private readonly OperationalRouteCameraPresentationStage _routeCameraPresentationStage;
        private readonly OperationalInputPreparationStage _inputPreparationStage;
        private readonly OperationalPlayerPreparationStage _playerPreparationStage = new();
        private readonly OperationalActivityCameraPresentationStage _activityCameraPresentationStage;
        private readonly OperationalActivityCameraReleasePreviousStage _activityCameraReleasePreviousStage;
        private readonly OperationalConsumerEntryAndReadinessStage _consumerEntryAndReadinessStage;
        private readonly OperationalRouteActivitySaveLoadOnEnterStage _routeActivitySaveLoadOnEnterStage;
        private readonly OperationalRouteActivitySaveSaveOnExitStage _routeActivitySaveSaveOnExitStage;
        private readonly OperationalRouteMaterializationBoundary _routeMaterializationBoundary = new();
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
                _sessionOperationalPipelineId);

            _fadeStage = new OperationalFadeStage(_dependencies.ResolveFadePort);
            _transitionBlackoutStage = new OperationalTransitionBlackoutStage();
            _sceneCompositionStage = new OperationalSceneCompositionStage(_dependencies.ResolveSceneCompositionPort);
            _handoffExitStage = new OperationalHandoffExitStage(
                _dependencies.ResolveRouteHandoffExitPort,
                _dependencies.ResolveSessionActivityRouteExitTeardownBoundary);
            _routeCameraReleasePreviousStage = new OperationalRouteCameraReleasePreviousStage(_dependencies.RouteCameraAdapter);
            _routeCameraPresentationStage = new OperationalRouteCameraPresentationStage(_dependencies.RouteCameraAdapter);
            _inputPreparationStage = new OperationalInputPreparationStage(_factRecorder, _dependencies.ResolveInputModeRequestPort);
            _routeCompletionStage = new OperationalRouteCompletionStage(_factRecorder);
            _activityCameraPresentationStage = new OperationalActivityCameraPresentationStage(_dependencies.ActivityCameraAdapter);
            _activityCameraReleasePreviousStage = new OperationalActivityCameraReleasePreviousStage(_dependencies.ActivityCameraAdapter);
            _consumerEntryAndReadinessStage = new OperationalConsumerEntryAndReadinessStage(_dependencies.ResolveRouteConsumerEntryPort, _dependencies.ResolveRouteConsumerReadinessPort);
            _routeActivitySaveLoadOnEnterStage = new OperationalRouteActivitySaveLoadOnEnterStage(
                _dependencies.ActivitySaveAdapter,
                _dependencies.ProgressionSlotContextResolver,
                ResolveSaveStateServiceOrNull,
                this,
                RouteActivitySnapshotSchemaId);
            _routeActivitySaveSaveOnExitStage = new OperationalRouteActivitySaveSaveOnExitStage(
                _dependencies.ActivitySaveAdapter,
                _dependencies.ProgressionSlotContextResolver,
                ResolveActivitySnapshotPayloadProviderOrNull,
                RouteActivitySnapshotSchemaId);
            _loadingStage = new OperationalLoadingStage(_dependencies.LoadingAdapter);
            _routeAudioStage = new OperationalRouteAudioStage(_dependencies.ResolveRouteAudioPort);
            _routeRevealStage = new OperationalRouteRevealStage();
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

            RuntimeModeConfig runtimeModeConfig = ResolveRuntimeModeConfigOrFail();
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = _dependencies.PersistentScenesPolicy;
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
                        loadingCommand,
                        sourceText,
                        reasonText));
                if (!loadingStartResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Loading] OperationalLoadingStage start failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{loadingStartResult.Kind}' reason='{loadingStartResult.Reason}' detail='{loadingStartResult.Detail}'.");
                }

                loadingStarted = loadingStartResult.LoadingStarted;

                OperationalTransitionBlackoutCommand blackoutCommand = new OperationalTransitionBlackoutCommand(
                    command,
                    sourceText,
                    reasonText);
                _transitionBlackoutStage.Begin(blackoutCommand);

                bool blackoutFadeInCompleted = false;
                if (command.UsesTransition)
                {
                    OperationalFadeStageResult fadeResult = await _fadeStage.ExecuteAsync(
                        new OperationalFadeCommand(
                            command,
                            OperationalFadeOperationKind.CloseCurtain,
                            sourceText,
                            reasonText));
                    blackoutFadeInCompleted = fadeResult.FadeCompleted;
                }

                OperationalTransitionBlackoutResult blackoutResult = _transitionBlackoutStage.Complete(
                    blackoutCommand,
                    blackoutFadeInCompleted);

                if (!blackoutResult.IsCompleted && !blackoutResult.IsSkipped)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Transition] OperationalTransitionBlackoutStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{blackoutResult.Kind}' reason='{blackoutResult.Reason}' detail='{blackoutResult.Detail}'.");
                }

                fadeInCompleted = blackoutResult.FadeInCompleted;

                OperationalLoadingResult blackoutLoadingResult = await _loadingStage.ExecuteTransitionBlackoutProgressAsync(
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

                OperationalPreviousRouteExitBoundaryCommand previousRouteExitBoundaryCommand = new OperationalPreviousRouteExitBoundaryCommand(
                    command,
                    previousCompletedRoute.RouteIdentity,
                    previousCompletedRoute.ActivityIdentity,
                    sourceText,
                    reasonText);

                OperationalPreviousRouteExitBoundaryResult previousRouteExitBeginResult =
                    _previousRouteExitBoundary.Begin(previousRouteExitBoundaryCommand);
                if (!previousRouteExitBeginResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][PreviousRouteExit] OperationalPreviousRouteExitBoundary begin failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{previousRouteExitBeginResult.Kind}' reason='{previousRouteExitBeginResult.Reason}' detail='{previousRouteExitBeginResult.Detail}'.");
                }

                OperationalHandoffExitResult handoffExitResult = await _handoffExitStage.ExecuteAsync(
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

                OperationalActivityCameraReleasePreviousResult activityCameraReleasePreviousResult =
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

                OperationalRouteCameraReleasePreviousResult routeCameraReleasePreviousResult =
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

                OperationalRouteActivitySaveSaveOnExitResult saveOnExitResult =
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

                OperationalPreviousRouteExitBoundaryResult previousRouteExitCompleteResult =
                    _previousRouteExitBoundary.Complete(previousRouteExitBoundaryCommand);
                if (!previousRouteExitCompleteResult.IsCompleted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][PreviousRouteExit] OperationalPreviousRouteExitBoundary complete failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{previousRouteExitCompleteResult.Kind}' reason='{previousRouteExitCompleteResult.Reason}' detail='{previousRouteExitCompleteResult.Detail}'.");
                }

                OperationalRouteMaterializationBoundaryCommand materializationBoundaryCommand = BuildMaterializationBoundaryCommand(
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    sourceText,
                    reasonText);
                _routeMaterializationBoundary.Begin(materializationBoundaryCommand);

                OperationalSceneCompositionStageResult sceneCompositionResult = await _sceneCompositionStage.ExecuteAsync(
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

                SessionOperationalRouteCompletedFact adapterFact = sceneCompositionResult.CompletionFact;
                OperationalLoadingCommand materializationLoadingCommand = BuildOperationalLoadingCommand(
                    loadingCommand,
                    sourceText,
                    reasonText);

                OperationalRouteCameraPresentationResult routeCameraPresentationResult = _routeCameraPresentationStage.Execute(
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

                OperationalLoadingResult sceneCompositionLoadingResult = await _loadingStage.ExecuteSceneCompositionCompletedAsync(materializationLoadingCommand);
                if (!sceneCompositionLoadingResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][Loading] OperationalLoadingStage scene composition progress failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{sceneCompositionLoadingResult.Kind}' reason='{sceneCompositionLoadingResult.Reason}' detail='{sceneCompositionLoadingResult.Detail}'.");
                }

                OperationalRouteActivitySaveLoadOnEnterResult loadOnEnterResult = _routeActivitySaveLoadOnEnterStage.Execute(
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

                OperationalInputPreparationResult inputPreparationResult = _inputPreparationStage.Execute(
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

                OperationalPlayerPreparationResult playerPreparationStageResult = _playerPreparationStage.Execute(
                    BuildPlayerPreparationCommand(
                        command,
                        routeIdentity,
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        sourceText,
                        reasonText));
                if (!playerPreparationStageResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][PlayerPreparation] OperationalPlayerPreparationStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.");
                }

                PlayerPreparationResult playerPreparationResult = playerPreparationStageResult.PlayerPreparationResult;
                if (playerPreparationStageResult.IsCompleted)
                {
                    OperationalActivityCameraPresentationResult activityCameraPresentationResult = _activityCameraPresentationStage.Execute(
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

                OperationalLoadingCompletionState loadingState = await _loadingStage.ExecuteClosedWindowCompletionAsync(materializationLoadingCommand);

                OperationalConsumerEntryAndReadinessResult consumerEntryAndReadinessResult = await _consumerEntryAndReadinessStage.ExecuteAsync(
                    BuildConsumerEntryAndReadinessCommand(
                        command,
                        loadingCommand,
                        routeIdentity,
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        sourceText,
                        reasonText),
                    playerPreparationResult);
                if (!consumerEntryAndReadinessResult.IsAccepted)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][SessionOperationalPipeline][ConsumerEntry] OperationalConsumerEntryAndReadinessStage failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' resultKind='{consumerEntryAndReadinessResult.Kind}' reason='{consumerEntryAndReadinessResult.Reason}' detail='{consumerEntryAndReadinessResult.Detail}'.");
                }

                OperationalRouteMaterializationBoundaryResult materializationResult = _routeMaterializationBoundary.Complete(
                    materializationBoundaryCommand,
                    adapterFact,
                    loadingState);
                loadingCompleted = materializationResult.LoadingCompleted;
                loadingHidden = materializationResult.LoadingHidden;

                OperationalRouteRevealCommand revealCommand = new OperationalRouteRevealCommand(
                    command,
                    sourceText,
                    reasonText);
                _routeRevealStage.Begin(revealCommand);

                OperationalRouteAudioStageResult routeAudioResult = _routeAudioStage.Execute(
                    new OperationalRouteAudioCommand(
                        command,
                        sourceText,
                        reasonText));

                bool revealFadeOutCompleted = false;
                if (command.UsesTransition)
                {
                    OperationalFadeStageResult fadeResult = await _fadeStage.ExecuteAsync(
                        new OperationalFadeCommand(
                            command,
                            OperationalFadeOperationKind.OpenCurtain,
                            sourceText,
                            reasonText));
                    revealFadeOutCompleted = fadeResult.FadeCompleted;
                }

                OperationalRouteRevealResult revealResult = _routeRevealStage.Complete(
                    revealCommand,
                    routeAudioResult.AudioSubmitted,
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

                OperationalRouteCompletionResult completionResult = _routeCompletionStage.ExecuteCompleted(
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
                return _factRecorder.Reject(
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

            return true;
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
                return _factRecorder.Reject(
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
                return _factRecorder.Reject(
                    SessionOperationalFactKind.IgnoredForeignOrStale,
                    stage,
                    normalizedSource,
                    normalizedReason,
                    $"Stage '{stage}' ignored because it is foreign, stale, or out of order.");
            }

            return _factRecorder.TryRecordStage(
                stage,
                normalizedRouteOperationId,
                normalizedTransitionId,
                transitionSequence,
                normalizedRouteId,
                normalizedRouteProfileId,
                normalizedSource,
                normalizedReason,
                normalizedMessage);
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

        private ISaveStateService ResolveSaveStateServiceOrNull()
        {
            return _dependencies.TryResolveSaveStateService(out ISaveStateService saveStateService)
                ? saveStateService
                : null;
        }

        private ISessionActivitySnapshotPayloadProvider ResolveActivitySnapshotPayloadProviderOrNull()
        {
            return _dependencies.TryResolveActivitySnapshotPayloadProvider(out ISessionActivitySnapshotPayloadProvider provider)
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
            SessionOperationalInputPolicy inputPolicy = command.InputPolicy;
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

        private OperationalPlayerPreparationCommand BuildPlayerPreparationCommand(
            SessionOperationalRouteCommand command,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string sourceText,
            string reasonText)
        {
            return new OperationalPlayerPreparationCommand(
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
