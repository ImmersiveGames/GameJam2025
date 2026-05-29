using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Semantic.Preparation;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
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

    public sealed class SessionOperationalPipeline : IRouteActivityLoadedSnapshotPayloadProvider
    {
        private const string DefaultPipelineId = "SessionOperationalPipeline.v0";
        private const string RouteActivitySnapshotSchemaId = "progression.route_activity.object_snapshot.v1";

        private readonly SessionOperationalRuntimeState _state = new();
        private readonly SessionOperationalStageOrderPolicy _stageOrderPolicy = new();
        private readonly SessionOperationalRoutePlanResolver _routePlanResolver = new();
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

                if (planResolution.UnloadPreviousRouteOwnedScenes)
                {
                    LogPreviousRouteUnloadPlan(
                        previousCompletedRoute,
                        routeIdentity,
                        planResolution.ExplicitScenesToUnload,
                        planResolution.Plan.AutoScenesToUnload,
                        planResolution.Plan.FinalScenesToUnload);
                }

                _hasActiveOperationalRouteOperation = true;
                _activeOperationalRouteOperationId = routeOperationId;
                _activeOperationalTransitionId = transitionId;
                _activeOperationalRouteIdentity = routeIdentity;
            }

            SessionOperationalRouteCommand command = new(
                planResolution.Plan,
                routeOperationId,
                transitionId,
                routeSequence,
                sourceText,
                reasonText);

            if (!command.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalRoute] runtime command invalid routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.");
            }

            RouteActivitySavePlan routeActivitySavePlan = RouteActivitySavePlanResolver.Resolve(
                command,
                previousCompletedRoute.IsValid,
                previousCompletedRoute.RouteIdentity,
                previousCompletedRoute.RouteOperationId,
                previousCompletedRoute.RouteSequence,
                previousCompletedRoute.SaveActivityOnExit,
                previousCompletedRoute.ActivityIdentity,
                BuildActivitySaveKey(previousCompletedRoute.ActivityIdentity));
            if (!routeActivitySavePlan.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] RouteActivitySavePlan invalido routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.");
            }

            if (command.UsesTransition)
            {
                fadeAdapter = ResolveFadeAdapterOrFail();
            }

            SessionOperationalLoadingCommand loadingCommand = ResolveLoadingCommandOrFail(
                command.Plan,
                runtimeModeConfig,
                routeOperationId,
                transitionId,
                routeSequence,
                sourceText,
                reasonText);

            if (loadingCommand.IsEnabled)
            {
                loadingAdapter = ResolveLoadingAdapterOrFail();

                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][Loading] LoadingPlanReady routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' loadingScene='{loadingCommand.LoadingSceneName}' showImmediately='{loadingCommand.ShowImmediately}' hideAfterCompletion='{loadingCommand.HideAfterCompletion}' minimumVisibleSeconds='{loadingCommand.MinimumVisibleSeconds:0.###}' finalProgressHoldSeconds='{loadingCommand.FinalProgressHoldSeconds:0.###}' source='{sourceText}' reason='{reasonText}'.",
                    DebugUtility.Colors.Info);
            }

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Audio] RouteAudioPlanReady routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' routeAudioMode='{command.Audio.RouteAudioMode}' routeAudioTiming='{command.Audio.RouteAudioTiming}' routeAudioCue='{command.Audio.RouteAudioCueName}' stopPreviousRouteAudio='{command.Audio.StopPreviousRouteAudio}' source='{sourceText}' reason='{reasonText}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Route] command='OperationalRouteCommand' routeIdentity='{routeIdentity}' activeScene='{activeSceneName}' activeSceneKey='{command.ActiveSceneKey?.name ?? string.Empty}' activeSceneImplicitLoad='{planResolution.ActiveSceneImplicitLoad}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{command.CompletionHandoff}' finalScenesToLoad=[{FormatSceneNames(command.FinalScenesToLoad)}] autoScenesToUnload=[{FormatSceneNames(command.AutoScenesToUnload)}] explicitScenesToUnload=[{FormatSceneNames(planResolution.ExplicitScenesToUnload)}] finalScenesToUnload=[{FormatSceneNames(command.FinalScenesToUnload)}] source='{sourceText}' reason='{reasonText}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySavePlanReady routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' routeSequence='{routeSequence}' loadActivitySaveOnEnter='{routeActivitySavePlan.CurrentPolicy.LoadActivitySaveOnEnter}' saveActivityOnExit='{routeActivitySavePlan.CurrentPolicy.SaveActivityOnExit}' loadShouldRun='{routeActivitySavePlan.LoadOnEnter.ShouldLoad}' saveOnExitShouldRun='{routeActivitySavePlan.SaveOnExit.ShouldSave}' saveOnExitSkipKind='{routeActivitySavePlan.SaveOnExit.SkipKind}' source='{sourceText}' reason='{reasonText}'.",
                DebugUtility.Colors.Info);

            bool fadeInCompleted = false;
            bool fadeOutCompleted = false;
            bool loadingStarted = false;
            bool loadingCompleted = false;
            bool loadingHidden = false;
            PlayerPreparationResult playerPreparationResult = default;
            bool hasPlayerPreparationResult = false;
            bool routeOperationSucceeded = false;
            string completionReason = "failed";

            try
            {
                await EnsureOperationalRouteHandoffExitOrFailAsync(
                    previousCompletedRoute,
                    command.FinalScenesToUnload,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    source,
                    reason);

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

                ExecuteReleasePreviousActivityCameraStageOrFail(
                    previousCompletedRoute,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    source,
                    reason);

                ExecuteRouteActivitySaveSaveOnExitOrFail(
                    runtimeModeConfig,
                    previousCompletedRoute,
                    routeActivitySavePlan,
                    source,
                    reason);

                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][Transition] command='TransitionPlanReady' transitionMode='{command.TransitionMode}' transitionProfile='{command.TransitionProfileLabel}' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{sourceText}' reason='{reasonText}'.",
                    DebugUtility.Colors.Info);

                if (loadingCommand.IsEnabled)
                {
                    await loadingAdapter.ShowLoadingAsync(
                        loadingCommand,
                        CreateLoadingFact(
                            loadingCommand,
                            SessionOperationalLoadingStage.LoadingStarted,
                            SessionOperationalLoadingOutcomeKind.Started,
                            0f,
                            "Loading started",
                            "LoadingStarted"));
                    loadingStarted = true;

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][Loading] LoadingStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' source='{sourceText}' reason='{reasonText}' showImmediately='{loadingCommand.ShowImmediately}'.",
                        DebugUtility.Colors.Info);

                    LogLoadingProgress(
                        loadingCommand,
                        SessionOperationalLoadingStage.LoadingStarted,
                        0f,
                        "Loading started",
                        sourceText,
                        reasonText);

                    await loadingAdapter.UpdateLoadingAsync(
                        loadingCommand,
                        CreateLoadingFact(
                            loadingCommand,
                            SessionOperationalLoadingStage.RoutePlanReady,
                            SessionOperationalLoadingOutcomeKind.ProgressApplied,
                            0.10f,
                            "Route plan ready",
                            "RoutePlanReady"));

                    LogLoadingProgress(
                        loadingCommand,
                        SessionOperationalLoadingStage.RoutePlanReady,
                        0.10f,
                        "Route plan ready",
                        sourceText,
                        reasonText);
                }

                if (command.UsesTransition)
                {
                    await fadeAdapter.FadeInAsync(command);
                    fadeInCompleted = true;

                    if (loadingCommand.IsEnabled)
                    {
                        await loadingAdapter.UpdateLoadingAsync(
                            loadingCommand,
                            CreateLoadingFact(
                                loadingCommand,
                                SessionOperationalLoadingStage.FadeInCompleted,
                                SessionOperationalLoadingOutcomeKind.ProgressApplied,
                                0.20f,
                                "Fade in completed",
                                "FadeInCompleted"));

                        LogLoadingProgress(
                            loadingCommand,
                            SessionOperationalLoadingStage.FadeInCompleted,
                            0.20f,
                            "Fade in completed",
                            sourceText,
                            reasonText);
                    }
                }
                else if (loadingCommand.IsEnabled)
                {
                    await loadingAdapter.UpdateLoadingAsync(
                        loadingCommand,
                        CreateLoadingFact(
                            loadingCommand,
                            SessionOperationalLoadingStage.TransitionSkipped,
                            SessionOperationalLoadingOutcomeKind.ProgressApplied,
                            0.20f,
                            "Transition skipped",
                            "TransitionSkipped"));

                    LogLoadingProgress(
                        loadingCommand,
                        SessionOperationalLoadingStage.TransitionSkipped,
                        0.20f,
                        "Transition skipped",
                        sourceText,
                        reasonText);
                }

                SessionOperationalRouteCompletedFact adapterFact = await routeExecutor.ApplyOperationalRouteAsync(command);
                if (!adapterFact.IsValid)
                {
                    throw new InvalidOperationException("Operational route executor returned an invalid completion fact.");
                }

                ExecuteRouteCameraPresentationStageOrFail(
                    command,
                    previousCompletedRoute,
                    activeSceneName,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    source,
                    reason);

                if (loadingCommand.IsEnabled)
                {
                    await loadingAdapter.UpdateLoadingAsync(
                        loadingCommand,
                        CreateLoadingFact(
                            loadingCommand,
                            SessionOperationalLoadingStage.SceneCompositionCompleted,
                            SessionOperationalLoadingOutcomeKind.ProgressApplied,
                            0.60f,
                            "Scene composition completed",
                            "SceneCompositionCompleted"));

                    LogLoadingProgress(
                        loadingCommand,
                        SessionOperationalLoadingStage.SceneCompositionCompleted,
                        0.60f,
                        "Scene composition completed",
                        sourceText,
                        reasonText);

                }

                ExecuteRouteActivitySaveLoadOnEnterOrFail(
                    runtimeModeConfig,
                    command,
                    routeActivitySavePlan,
                    source,
                    reason);

                SessionOperationalInputPolicy inputPolicy = command.InputPolicy;
                SessionOperationalInputModeKind initialInputMode = PrepareInputCapabilityOrFail(
                    runtimeModeConfig,
                    command.Plan,
                    inputPolicy,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    source,
                    reason);

                string routeClass = command.SurfaceKind.ToString();
                if (!TryObserveInputCapabilityPrepared(
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        routeIdentity,
                        routeIdentity,
                        routeClass,
                        inputPolicy,
                        initialInputMode,
                        source,
                        reason))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][H1][SessionOperationalPipeline][InputMode] Failed to record InputCapabilityPrepared routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{command.SurfaceKind}' inputPolicy='{inputPolicy}' resolvedInputMode='{initialInputMode}' source='{sourceText}' reason='{reasonText}'.");
                }

                if (!TryObserveInitialInputModePrepared(
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        routeIdentity,
                        routeIdentity,
                        routeClass,
                        inputPolicy,
                        initialInputMode,
                        source,
                        reason))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][H1][SessionOperationalPipeline][InputMode] Failed to record InitialInputModePrepared routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{command.SurfaceKind}' inputPolicy='{inputPolicy}' resolvedInputMode='{initialInputMode}' source='{sourceText}' reason='{reasonText}'.");
                }

                DispatchInitialInputModeCommandOrFail(
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    routeIdentity,
                    routeIdentity,
                    routeClass,
                    inputPolicy,
                    initialInputMode,
                    source,
                    reason);

                if (command.CompletionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
                {
                    if (string.IsNullOrWhiteSpace(command.HandoffSessionStateId))
                    {
                        throw new InvalidOperationException("handoffSessionStateId is required when completionHandoff=SessionActivityEntry.");
                    }

                    PlayerPreparationIdentity playerPreparationIdentity = new(
                        _sessionOperationalPipelineId,
                        command.HandoffSessionStateId,
                        routeIdentity,
                        routeOperationId,
                        routeSequence,
                        transitionId);
                    PlayerPreparationPlan playerPreparationPlan = new(
                        playerPreparationIdentity,
                        command.CompletionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry,
                        new PlayerSet(ResolvePlayerSetFromPlan(command.Plan)),
                        sourceText,
                        reasonText);
                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerPreparationStarted' pipelineId='{playerPreparationIdentity.PipelineId}' sessionId='{playerPreparationIdentity.SessionId}' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' routeSessionParticipantPreparation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(command.Plan)}' source='{sourceText}' reason='{reasonText}'.",
                        DebugUtility.Colors.Info);

                    playerPreparationResult = PlayerPreparationStage.Execute(playerPreparationPlan);
                    if (!playerPreparationResult.IsValid)
                    {
                        throw new InvalidOperationException("PlayerPreparationStage returned an invalid result.");
                    }
                    hasPlayerPreparationResult = true;

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerPreparationIntentPrepared' pipelineId='{playerPreparationIdentity.PipelineId}' sessionId='{playerPreparationIdentity.SessionId}' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' routeSessionParticipantPreparation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(command.Plan)}' source='{sourceText}' reason='{reasonText}' playerIds='{FormatPlayerIdsForHandoff(playerPreparationResult.Snapshot.PlannedEntries)}'.",
                        DebugUtility.Colors.Info);

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerPreparationCompleted' pipelineId='{playerPreparationIdentity.PipelineId}' sessionId='{playerPreparationIdentity.SessionId}' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' routeSessionParticipantPreparation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(command.Plan)}' source='{sourceText}' reason='{reasonText}' outcome='{(playerPreparationResult.IsObservedNoOp ? "observed_noop" : (playerPreparationResult.IsPlannedOnly ? "planned_only" : "materialized"))}'.",
                        DebugUtility.Colors.Info);

                    ExecuteActivityCameraPreparationStageOrFail(
                        command,
                        activeSceneName,
                        routeIdentity,
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        command.HandoffSessionStateId,
                        source,
                        reason);
                }

                if (loadingCommand.IsEnabled)
                {
                    await loadingAdapter.UpdateLoadingAsync(
                        loadingCommand,
                        CreateLoadingFact(
                            loadingCommand,
                            SessionOperationalLoadingStage.ConsumerEntryPreparationCompleted,
                            SessionOperationalLoadingOutcomeKind.ProgressApplied,
                            0.80f,
                            "Consumer entry preparation completed",
                            "ConsumerEntryPreparationCompleted"));

                    LogLoadingProgress(
                        loadingCommand,
                        SessionOperationalLoadingStage.ConsumerEntryPreparationCompleted,
                        0.80f,
                        "Consumer entry preparation completed",
                        sourceText,
                        reasonText);

                    await loadingAdapter.UpdateLoadingAsync(
                        loadingCommand,
                        CreateLoadingFact(
                            loadingCommand,
                            SessionOperationalLoadingStage.OperationalRouteCompleted,
                            SessionOperationalLoadingOutcomeKind.Completed,
                            1.0f,
                            "Operational route completed",
                            "OperationalRouteCompleted"));
                    loadingCompleted = true;

                    LogLoadingProgress(
                        loadingCommand,
                        SessionOperationalLoadingStage.OperationalRouteCompleted,
                        1.0f,
                        "Operational route completed",
                        sourceText,
                        reasonText);

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][Loading] LoadingCompleted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' source='{sourceText}' reason='{reasonText}' hideAfterCompletion='{loadingCommand.HideAfterCompletion}'.",
                        DebugUtility.Colors.Success);

                    if (loadingCommand.HideAfterCompletion)
                    {
                        await loadingAdapter.HideLoadingAsync(
                            loadingCommand,
                            CreateLoadingFact(
                                loadingCommand,
                                SessionOperationalLoadingStage.LoadingHidden,
                                SessionOperationalLoadingOutcomeKind.Hidden,
                                1.0f,
                                "Loading hidden",
                                "LoadingHidden"));
                        loadingHidden = true;

                        DebugUtility.Log(typeof(SessionOperationalPipeline),
                            $"[OBS][SessionOperationalPipeline][Loading] LoadingHidden routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' source='{sourceText}' reason='{reasonText}'.",
                            DebugUtility.Colors.Success);
                    }
                    else
                    {
                        DebugUtility.Log(typeof(SessionOperationalPipeline),
                            $"[OBS][SessionOperationalPipeline][Loading] LoadingHiddenSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' source='{sourceText}' reason='{reasonText}' hideAfterCompletion='false'.",
                            DebugUtility.Colors.Info);
                    }
                }

                if (command.CompletionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
                {
                    if (string.IsNullOrWhiteSpace(command.HandoffSessionStateId))
                    {
                        throw new InvalidOperationException("handoffSessionStateId is required when completionHandoff=SessionActivityEntry.");
                    }

                    if (!hasPlayerPreparationResult || !playerPreparationResult.IsValid)
                    {
                        throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][PlayerPreparation] Missing valid PlayerPreparationResult for operational consumer entry routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.");
                    }

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][Route] handoff='OperationalRouteConsumerEntryStarted' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{sourceText}' reason='{reasonText}' pendingHandoff='SessionActivityEntry' routeSessionParticipantPreparation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(command.Plan)}' playerPreparationOutcome='{FormatPlayerPreparationOutcome(playerPreparationResult.Snapshot.Outcome)}' plannedPlayers='{playerPreparationResult.Snapshot.PlannedPlayersCount}' materializedPlayers='{playerPreparationResult.Snapshot.MaterializedPlayersCount}' pendingRequiredPlayers='{playerPreparationResult.Snapshot.PendingRequiredPlayersCount}'.",
                        DebugUtility.Colors.Info);

                    OperationalRouteConsumerEntryRequest consumerEntryRequest = new(
                        command.HandoffSessionStateId,
                        playerPreparationResult.Snapshot,
                        ResolvePlayerTechnicalEntriesFromPlan(command.Plan),
                        command.UsesTransition,
                        command.TransitionProfile,
                        loadingCommand.LoadingMode == SessionOperationalRouteLoadingMode.Profile,
                        loadingCommand.LoadingProfile,
                        sourceText,
                        reasonText);

                    OperationalRouteConsumerEntryResult consumerEntryResult = await ResolveRouteConsumerEntryPortOrFail()
                        .RequestEntryAsync(consumerEntryRequest, CancellationToken.None);
                    if (!consumerEntryResult.IsValid || !consumerEntryResult.IsCompleted)
                    {
                        throw new InvalidOperationException($"Operational route consumer entry failed/rejected. kind='{consumerEntryResult.Kind}' reason='{consumerEntryResult.Reason}' detail='{consumerEntryResult.Detail}'.");
                    }

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][Route] handoff='OperationalRouteConsumerEntryCompleted' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{sourceText}' reason='{reasonText}' resultKind='{consumerEntryResult.Kind}' resultReason='{consumerEntryResult.Reason}'.",
                        DebugUtility.Colors.Success);

                    await AwaitOperationalRouteConsumerReadinessOrFailAsync(
                        command.HandoffSessionStateId,
                        playerPreparationResult.Snapshot.Identity.RouteOperationId,
                        routeIdentity,
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        sourceText,
                        reasonText);
                }

                if (command.Audio.RouteAudioMode == SessionOperationalRouteAudioMode.None)
                {
                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        BuildAudioPipelineLog(
                            "[OBS][SessionOperationalPipeline][Audio] RouteRevealAudioSkipped",
                            command,
                            sourceText,
                            reasonText,
                            "skipReason='route_audio_disabled'"),
                        DebugUtility.Colors.Info);
                }
                else
                {
                    IAudioAdapter audioAdapter = ResolveAudioAdapterOrFail();

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        BuildAudioPipelineLog(
                            "[OBS][SessionOperationalPipeline][Audio] RouteRevealAudioStarted",
                            command,
                            sourceText,
                            reasonText,
                            $"cueType='{ResolveRouteAudioCueTypeOrFail(command.Audio.RouteAudioCue)}'"),
                        DebugUtility.Colors.Info);

                    audioAdapter.PlayRouteRevealAudio(command);

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        BuildAudioPipelineLog(
                            "[OBS][SessionOperationalPipeline][Audio] RouteRevealAudioSubmitted",
                            command,
                            sourceText,
                            reasonText,
                            $"cueType='{ResolveRouteAudioCueTypeOrFail(command.Audio.RouteAudioCue)}'"),
                        DebugUtility.Colors.Success);
                }

                if (command.UsesTransition)
                {
                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][Fade] fadeOutStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{sourceText}' reason='{reasonText}'.",
                        DebugUtility.Colors.Info);
                    await fadeAdapter.FadeOutAsync(command);
                    fadeOutCompleted = true;

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][Fade] fadeOutCompleted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{sourceText}' reason='{reasonText}'.",
                        DebugUtility.Colors.Success);
                }

                CompleteOperationalRouteOperation(routeOperationId, transitionId, routeSequence, routeIdentity, sourceText, reasonText);
                RecordLastCompletedRouteSnapshot(command);

                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][Transition] fact='OperationalRouteCompleted' routeIdentity='{adapterFact.RouteIdentity}' routeOperationId='{adapterFact.RouteOperationId}' transitionId='{adapterFact.TransitionId}' routeSequence='{adapterFact.RouteSequence}' correlationId='{adapterFact.CorrelationId}' message='{adapterFact.Message}' transitionMode='{command.TransitionMode}' transitionProfile='{command.TransitionProfileLabel}' source='{sourceText}' reason='{reasonText}'.",
                    DebugUtility.Colors.Success);

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
                if (loadingCommand.IsEnabled && loadingStarted && !loadingHidden)
                {
                    try
                    {
                        await loadingAdapter.HideLoadingAsync(
                            loadingCommand,
                            CreateLoadingFact(
                                loadingCommand,
                                SessionOperationalLoadingStage.LoadingHidden,
                                loadingCompleted ? SessionOperationalLoadingOutcomeKind.Hidden : SessionOperationalLoadingOutcomeKind.Failed,
                                loadingCompleted ? 1.0f : 0.95f,
                                loadingCompleted ? "Loading hidden" : "Loading hidden after failure",
                                loadingCompleted ? "LoadingHidden" : "LoadingHiddenAfterFailure"));
                    }
                    catch (Exception cleanupEx)
                    {
                        DebugUtility.LogError<SessionOperationalPipeline>(
                            $"[OBS][SessionOperationalPipeline][Loading] hide_cleanup_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' source='{sourceText}' reason='{reasonText}' exceptionType='{cleanupEx.GetType().Name}' exceptionMessage='{cleanupEx.Message}'.");
                    }
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

            IOperationalRouteHandoffExitPort handoffExitPort = ResolveRouteHandoffExitPortOrFail();
            OperationalRouteHandoffExitPreflightResult preflightResult = handoffExitPort.EvaluatePreflight(
                new OperationalRouteHandoffExitPreflightRequest(
                    routeIdentity,
                    _lastCompletedRouteSnapshot.RouteIdentity,
                    _lastCompletedRouteSnapshot.ActivityIdentity,
                    source,
                    reason));

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

        private ISessionOperationalRouteCameraAdapter ResolveSessionOperationalRouteCameraAdapterOrFail()
        {
            return _dependencies.RouteCameraAdapter;
        }

        private ISessionOperationalActivityCameraAdapter ResolveSessionOperationalActivityCameraAdapterOrFail()
        {
            return _dependencies.ActivityCameraAdapter;
        }

        private IOperationalRouteConsumerEntryPort ResolveRouteConsumerEntryPortOrFail()
        {
            IOperationalRouteConsumerEntryPort port = _dependencies.ResolveRouteConsumerEntryPort();
            if (port != null)
            {
                return port;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline] IOperationalRouteConsumerEntryPort obrigatorio ausente para o trilho operacional.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private IOperationalRouteConsumerReadinessPort ResolveRouteConsumerReadinessPortOrFail()
        {
            IOperationalRouteConsumerReadinessPort port = _dependencies.ResolveRouteConsumerReadinessPort();
            if (port != null)
            {
                return port;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline] IOperationalRouteConsumerReadinessPort obrigatorio ausente para readiness visual do route consumer.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private IOperationalRouteHandoffExitPort ResolveRouteHandoffExitPortOrFail()
        {
            IOperationalRouteHandoffExitPort port = _dependencies.ResolveRouteHandoffExitPort();
            if (port != null)
            {
                return port;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline] IOperationalRouteHandoffExitPort obrigatorio ausente para handoff exit pre-unload.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private async Task AwaitOperationalRouteConsumerReadinessOrFailAsync(
            string consumerIdentity,
            string expectedRouteOperationId,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            string normalizedConsumerIdentity = Normalize(consumerIdentity);
            string normalizedExpectedRouteOperationId = Normalize(expectedRouteOperationId);
            if (string.IsNullOrWhiteSpace(normalizedConsumerIdentity))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] consumerIdentity ausente para readiness visual routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.");
            }

            if (string.IsNullOrWhiteSpace(normalizedExpectedRouteOperationId))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] expectedRouteOperationId ausente para readiness visual routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.");
            }

            IOperationalRouteConsumerReadinessPort readinessPort = ResolveRouteConsumerReadinessPortOrFail();
            OperationalRouteConsumerReadinessRequest request = new(
                normalizedConsumerIdentity,
                normalizedExpectedRouteOperationId,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][ConsumerReadiness] OperationalRouteConsumerReadinessAwaitStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' consumerIdentity='{normalizedConsumerIdentity}' expectedRouteOperationId='{normalizedExpectedRouteOperationId}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            OperationalRouteConsumerReadinessResult readinessResult = await readinessPort.AwaitReadinessAsync(
                request,
                CancellationToken.None);

            if (!readinessResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] readiness_result_invalid routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' readinessResult='{readinessResult}'.");
            }

            if (readinessResult.IsReady)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][ConsumerReadiness] OperationalRouteConsumerReadinessCompleted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' consumerIdentity='{normalizedConsumerIdentity}' expectedRouteOperationId='{normalizedExpectedRouteOperationId}' readinessResult='{readinessResult}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            if (readinessResult.IsNotRequired)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] readiness_not_required_invalid_for_handoff routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' consumerIdentity='{normalizedConsumerIdentity}' expectedRouteOperationId='{normalizedExpectedRouteOperationId}' readinessResult='{readinessResult}'.");
            }

            if (readinessResult.IsRejectedForeignOrStale || readinessResult.IsFailed)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] readiness_rejected_or_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' consumerIdentity='{normalizedConsumerIdentity}' expectedRouteOperationId='{normalizedExpectedRouteOperationId}' readinessResult='{readinessResult}'.");
            }

            throw new InvalidOperationException(
                $"[FATAL][Config][SessionOperationalPipeline][ConsumerReadiness] readiness_unhandled_kind routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' consumerIdentity='{normalizedConsumerIdentity}' expectedRouteOperationId='{normalizedExpectedRouteOperationId}' readinessResult='{readinessResult}'.");
        }

        private async Task EnsureOperationalRouteHandoffExitOrFailAsync(
            SessionOperationalRouteSnapshot previousCompletedRoute,
            IReadOnlyList<SceneKeyAsset> finalScenesToUnload,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (!ShouldRequireOperationalRouteHandoffExit(previousCompletedRoute, finalScenesToUnload))
            {
                return;
            }

            IOperationalRouteHandoffExitPort handoffExitPort = ResolveRouteHandoffExitPortOrFail();
            OperationalRouteHandoffExitRequest request = new(
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                previousCompletedRoute.RouteIdentity,
                previousCompletedRoute.ActivityIdentity,
                source,
                reason);

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Route] OperationalRouteRequestDeferredForHandoffExit routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' handoffIdentity='{previousCompletedRoute.ActivityIdentity}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Route] OperationalHandoffExitStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' handoffIdentity='{previousCompletedRoute.ActivityIdentity}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            OperationalRouteHandoffExitResult exitResult = await handoffExitPort.RequestExitAsync(
                request,
                CancellationToken.None);

            if (!exitResult.IsValid)
            {
                FailOperationalRouteHandoffExitOrThrow(
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    previousCompletedRoute,
                    source,
                    reason,
                    "handoff_exit_invalid_result",
                    "Operational handoff exit port returned invalid result.",
                    exitResult);
            }

            if (exitResult.IsCompleted)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][Route] OperationalHandoffExitCompleted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' handoffIdentity='{previousCompletedRoute.ActivityIdentity}' exitResult='{exitResult}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            FailOperationalRouteHandoffExitOrThrow(
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                previousCompletedRoute,
                source,
                reason,
                string.IsNullOrWhiteSpace(exitResult.Reason) ? "handoff_exit_failed" : exitResult.Reason,
                string.IsNullOrWhiteSpace(exitResult.Detail) ? "Operational handoff exit failed." : exitResult.Detail,
                exitResult);
        }

        private static void FailOperationalRouteHandoffExitOrThrow(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            SessionOperationalRouteSnapshot previousCompletedRoute,
            string source,
            string reason,
            string blockedReason,
            string blockedDetail,
            OperationalRouteHandoffExitResult exitResult)
        {
            DebugUtility.LogWarning<SessionOperationalPipeline>(
                $"[OBS][SessionOperationalPipeline][Route] RouteRequestBlockedByOperationalHandoff routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' handoffIdentity='{previousCompletedRoute.ActivityIdentity}' blockedReason='{blockedReason}' detail='{blockedDetail}' exitResult='{exitResult}' source='{source}' reasonDetail='{reason}'.");
            throw new RouteRequestBlockedByOperationalHandoffException(blockedReason, blockedDetail);
        }

        private sealed class RouteRequestBlockedByOperationalHandoffException : Exception
        {
            public RouteRequestBlockedByOperationalHandoffException(string blockedReason, string blockedDetail)
                : base("route_request_blocked_by_operational_handoff")
            {
                BlockedReason = Normalize(blockedReason);
                BlockedDetail = Normalize(blockedDetail);
            }

            public string BlockedReason { get; }
            public string BlockedDetail { get; }
        }

        private static bool ShouldRequireOperationalRouteHandoffExit(
            SessionOperationalRouteSnapshot previousCompletedRoute,
            IReadOnlyList<SceneKeyAsset> finalScenesToUnload)
        {
            if (!previousCompletedRoute.IsValid)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(previousCompletedRoute.ActivityIdentity))
            {
                return false;
            }

            if (finalScenesToUnload == null || finalScenesToUnload.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < finalScenesToUnload.Count; i++)
            {
                SceneKeyAsset candidate = finalScenesToUnload[i];
                if (candidate == null)
                {
                    continue;
                }

                if (previousCompletedRoute.ActiveSceneKey != null && ReferenceEquals(candidate, previousCompletedRoute.ActiveSceneKey))
                {
                    return true;
                }

                if (ContainsSceneKey(previousCompletedRoute.RouteOwnedLoadedSceneKeys, candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsSceneKey(IReadOnlyList<SceneKeyAsset> collection, SceneKeyAsset candidate)
        {
            if (collection == null || collection.Count == 0 || candidate == null)
            {
                return false;
            }

            for (int i = 0; i < collection.Count; i++)
            {
                if (ReferenceEquals(collection[i], candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private static SessionOperationalLoadingCommand ResolveLoadingCommandOrFail(
            SessionOperationalRoutePlan plan,
            RuntimeModeConfig runtimeModeConfig,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (!plan.IsValid)
            {
                throw new ArgumentException("SessionOperationalRoutePlan is invalid.", nameof(plan));
            }

            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] RuntimeModeConfig obrigatorio ausente para resolver loading.");
            }

            SessionOperationalRouteLoadingMode effectiveLoadingMode = plan.LoadingMode;
            RuntimeLoadingProfileAsset effectiveLoadingProfile = plan.LoadingProfile;

            if (effectiveLoadingMode == SessionOperationalRouteLoadingMode.RuntimeDefault)
            {
                SessionOperationalRuntimeLoadingDefaults loadingDefaults =
                    SessionOperationalRuntimeConfigResolver.ResolveLoadingDefaultsOrFail(runtimeModeConfig);

                effectiveLoadingMode = loadingDefaults.Mode;
                effectiveLoadingProfile = loadingDefaults.Profile;
            }

            if (effectiveLoadingMode == SessionOperationalRouteLoadingMode.Profile)
            {
                if (effectiveLoadingProfile == null)
                {
                    string message = $"[FATAL][Config][SessionOperationalRoute] loadingProfile is required when loadingMode=Profile routeIdentity='{plan.RouteIdentity}'.";
                    DebugUtility.LogError<SessionOperationalPipeline>(message);
                    throw new InvalidOperationException(message);
                }

                if (!effectiveLoadingProfile.TryValidate(out string profileValidationError))
                {
                    string message = $"[FATAL][Config][SessionOperationalRoute] loadingProfile is invalid routeIdentity='{plan.RouteIdentity}' profile='{effectiveLoadingProfile.ProfileId}' detail='{profileValidationError}'.";
                    DebugUtility.LogError<SessionOperationalPipeline>(message);
                    throw new InvalidOperationException(message);
                }
            }
            else if (effectiveLoadingMode == SessionOperationalRouteLoadingMode.None)
            {
                effectiveLoadingProfile = null;
            }

            string loadingSceneName = string.Empty;
            if (effectiveLoadingMode != SessionOperationalRouteLoadingMode.None)
            {
                RuntimePersistentScenesPolicyAsset persistentScenesPolicy = RuntimePolicyConfigResolver.ResolvePersistentScenesPolicyOrFail(runtimeModeConfig);
                if (persistentScenesPolicy == null)
                {
                    string message = $"[FATAL][Config][SessionOperationalPipeline] RuntimePersistentScenesPolicyAsset obrigatorio ausente para loading efetivo routeIdentity='{plan.RouteIdentity}'.";
                    DebugUtility.LogError<SessionOperationalPipeline>(message);
                    throw new InvalidOperationException(message);
                }

                loadingSceneName = persistentScenesPolicy.ResolveSceneNameByRoleOrFail(
                    RuntimePersistentSceneRole.Loading,
                    nameof(SessionOperationalPipeline));
            }

            SessionOperationalLoadingCommand loadingCommand = new(
                plan.RouteIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason,
                effectiveLoadingMode,
                effectiveLoadingProfile,
                loadingSceneName,
                effectiveLoadingProfile != null ? effectiveLoadingProfile.FinalProgressHoldSeconds : 0f);

            if (!loadingCommand.IsValid)
            {
                string message = $"[FATAL][Config][SessionOperationalPipeline] loading command invalid routeIdentity='{plan.RouteIdentity}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' loadingSceneName='{loadingCommand.LoadingSceneName}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.";
                DebugUtility.LogError<SessionOperationalPipeline>(message);
                throw new InvalidOperationException(message);
            }

            return loadingCommand;
        }

        private IAudioAdapter ResolveAudioAdapterOrFail()
        {
            return _dependencies.AudioAdapter;
        }

        private static string ResolveRouteAudioCueTypeOrFail(AudioCueAsset cue)
        {
            if (cue is AudioBgmCueAsset)
            {
                return nameof(AudioBgmCueAsset);
            }

            if (cue is AudioSfxCueAsset)
            {
                return nameof(AudioSfxCueAsset);
            }

            string cueType = cue == null ? "<null>" : cue.GetType().Name;
            string message = $"[FATAL][Config][SessionOperationalPipeline][Audio] unsupported routeAudioCue type='{cueType}'.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private static string BuildAudioPipelineLog(
            string prefix,
            SessionOperationalRouteCommand command,
            string source,
            string reason,
            string extra)
        {
            return $"{prefix} routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' routeAudioMode='{command.Audio.RouteAudioMode}' routeAudioTiming='{command.Audio.RouteAudioTiming}' routeAudioCue='{command.Audio.RouteAudioCueName}' stopPreviousRouteAudio='{command.Audio.StopPreviousRouteAudio}' source='{source}' reason='{reason}' {extra}.";
        }

        private ISessionOperationalActivitySaveAdapter ResolveSessionOperationalActivitySaveAdapterOrFail()
        {
            return _dependencies.ActivitySaveAdapter;
        }

        private void ExecuteRouteCameraPresentationStageOrFail(
            SessionOperationalRouteCommand command,
            SessionOperationalRouteSnapshot previousCompletedRoute,
            string activeSceneName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            ISessionOperationalRouteCameraAdapter routeCameraAdapter = ResolveSessionOperationalRouteCameraAdapterOrFail();

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationStageStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activeScene='{Normalize(activeSceneName)}' operationalSurfaceKind='{command.SurfaceKind}' completionHandoff='{command.CompletionHandoff}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            SessionOperationalRouteCameraReleaseCommand releaseCommand = new(
                routeIdentity,
                previousCompletedRoute.RouteIdentity,
                source,
                reason);

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationReleasePreviousStarted currentRouteIdentity='{routeIdentity}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            if (!routeCameraAdapter.TryReleaseRouteCamera(releaseCommand, out SessionOperationalRouteCameraReleaseResult releaseResult, out string releaseReason))
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][RouteCamera] release_previous_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' reason='{releaseReason}'.");
            }

            if (releaseResult.IsSkipped)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationReleasePreviousSkipped currentRouteIdentity='{routeIdentity}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' skipReason='{releaseResult.SkipReason}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
            }
            else if (releaseResult.IsReleased)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationReleasePreviousCompleted currentRouteIdentity='{routeIdentity}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' releaseRouteIdentity='{releaseResult.ReleasedFact.RouteIdentity}' releaseRequirementId='{releaseResult.ReleasedFact.RequirementId}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Success);
            }
            else
            {
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][RouteCamera] release_previous_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' reason='{releaseReason}'.");
            }

            SessionOperationalRouteCameraPrepareCommand prepareCommand = new(
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                command.SurfaceKind.ToString(),
                command.CompletionHandoff,
                activeSceneName,
                command.SurfacePresentationProfile,
                command.ActivityPresentationProfile,
                source,
                reason);

            if (!routeCameraAdapter.TryPrepareRouteCamera(prepareCommand, out SessionOperationalRouteCameraPrepareResult prepareResult, out string prepareReason))
            {
                bool required = command.SurfacePresentationProfile != null && command.SurfacePresentationProfile.Required;
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][RouteCamera] prepare_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{command.SurfaceKind}' required='{required}' reason='{prepareReason}'.");
            }

            if (prepareResult.IsSkipped)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{command.SurfaceKind}' completionHandoff='{command.CompletionHandoff}' reason='{prepareResult.SkipReason}' source='{source}' reasonDetail='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (prepareResult.IsPrepared)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationStagePrepared routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{command.SurfaceKind}' requirementId='{prepareResult.ReadyFact.RequirementId}' outputCamera='{prepareResult.ReadyFact.OutputCameraName}' presentationRig='{prepareResult.ReadyFact.PresentationRigName}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            string failureReason = prepareResult.FailureFact != null
                ? prepareResult.FailureFact.FailureReason
                : prepareReason;
            bool profileRequired = command.SurfacePresentationProfile != null && command.SurfacePresentationProfile.Required;

            DebugUtility.LogError(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationFailed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{command.SurfaceKind}' profileRequired='{profileRequired}' reason='{failureReason}' source='{source}' reasonDetail='{reason}'.");

            throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][RouteCamera] prepare_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{command.SurfaceKind}' required='{profileRequired}' reason='{failureReason}'.");
        }

        private void ExecuteActivityCameraPreparationStageOrFail(
            SessionOperationalRouteCommand command,
            string activeSceneName,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string activityIdentity,
            string source,
            string reason)
        {
            ISessionOperationalActivityCameraAdapter activityCameraAdapter = ResolveSessionOperationalActivityCameraAdapterOrFail();

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPreparationStageStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activeScene='{Normalize(activeSceneName)}' completionHandoff='{command.CompletionHandoff}' activityIdentity='{Normalize(activityIdentity)}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            SessionOperationalActivityCameraPrepareCommand prepareCommand = new(
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                activityIdentity,
                command.CompletionHandoff,
                activeSceneName,
                command.ActivityPresentationProfile,
                source,
                reason);

            if (!activityCameraAdapter.TryPrepareActivityCamera(prepareCommand, out SessionOperationalActivityCameraPrepareResult prepareResult, out string prepareReason))
            {
                bool required = command.ActivityPresentationProfile != null && command.ActivityPresentationProfile.Required;
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][ActivityCamera] prepare_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{command.CompletionHandoff}' required='{required}' reason='{prepareReason}'.");
            }

            if (prepareResult.IsSkipped)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{command.CompletionHandoff}' activityIdentity='{Normalize(activityIdentity)}' reason='{prepareResult.SkipReason}' source='{source}' reasonDetail='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (prepareResult.IsPrepared)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPreparationStagePrepared routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{command.CompletionHandoff}' activityIdentity='{prepareResult.ReadyFact.ActivityIdentity}' requirementId='{prepareResult.ReadyFact.RequirementId}' outputCamera='{prepareResult.ReadyFact.Handle?.UnityCamera?.name}' presentationRig='{prepareResult.ReadyFact.Handle?.CameraRigInstance?.name}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            string failureReason = prepareResult.FailureFact != null
                ? prepareResult.FailureFact.FailureReason
                : prepareReason;
            bool profileRequired = command.ActivityPresentationProfile != null && command.ActivityPresentationProfile.Required;

            DebugUtility.LogError(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationFailed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{command.CompletionHandoff}' profileRequired='{profileRequired}' activityIdentity='{Normalize(activityIdentity)}' reason='{failureReason}' source='{source}' reasonDetail='{reason}'.");

            throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][ActivityCamera] prepare_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{command.CompletionHandoff}' required='{profileRequired}' reason='{failureReason}'.");
        }

        private void ExecuteReleasePreviousActivityCameraStageOrFail(
            SessionOperationalRouteSnapshot previousCompletedRoute,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            ISessionOperationalActivityCameraAdapter activityCameraAdapter = ResolveSessionOperationalActivityCameraAdapterOrFail();

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraReleasePreviousStageStarted currentRouteIdentity='{routeIdentity}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            SessionOperationalActivityCameraReleaseCommand releaseCommand = new(
                routeIdentity,
                previousCompletedRoute.RouteIdentity,
                source,
                reason);

            if (!activityCameraAdapter.TryReleaseActivityCamera(releaseCommand, out SessionOperationalActivityCameraReleaseResult releaseResult, out string releaseReason))
            {
                DebugUtility.LogError(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraReleasePreviousStageFailed currentRouteIdentity='{routeIdentity}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' reason='{releaseReason}' source='{source}' reasonDetail='{reason}'.");

                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][ActivityCamera] release_previous_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' reason='{releaseReason}'.");
            }

            if (releaseResult.IsSkipped)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraReleasePreviousStageSkipped currentRouteIdentity='{routeIdentity}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' skipReason='{releaseResult.SkipReason}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (releaseResult.IsReleased)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraReleasePreviousStageCompleted currentRouteIdentity='{routeIdentity}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' releaseRouteIdentity='{releaseResult.ReleasedFact?.RouteIdentity}' releaseActivityIdentity='{releaseResult.ReleasedFact?.ActivityIdentity}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            DebugUtility.LogError(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraReleasePreviousStageFailed currentRouteIdentity='{routeIdentity}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' reason='{releaseReason}' source='{source}' reasonDetail='{reason}'.");

            throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][ActivityCamera] release_previous_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' reason='{releaseReason}'.");
        }

        private void ExecuteRouteActivitySaveSaveOnExitOrFail(
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRouteSnapshot previousCompletedRoute,
            RouteActivitySavePlan routeActivitySavePlan,
            string source,
            string reason)
        {
            RouteActivitySaveOnExitPlan saveOnExitPlan = routeActivitySavePlan.SaveOnExit;
            string currentRouteIdentity = saveOnExitPlan.CurrentRouteIdentity;
            string currentRouteOperationId = saveOnExitPlan.CurrentRouteOperationId;
            string currentTransitionId = saveOnExitPlan.CurrentTransitionId;
            int currentRouteSequence = saveOnExitPlan.CurrentRouteSequence;

            if (!saveOnExitPlan.ShouldSave)
            {
                LogRouteActivitySaveSaveSkipped(
                    previousCompletedRoute,
                    currentRouteIdentity,
                    currentRouteOperationId,
                    currentTransitionId,
                    currentRouteSequence,
                    saveOnExitPlan.SkipKind,
                    saveOnExitPlan.SkipDetail,
                    source,
                    reason);
                return;
            }

            string previousActivityIdentity = saveOnExitPlan.PreviousActivityIdentity;
            string previousActivitySaveKey = saveOnExitPlan.PreviousActivitySaveKey;

            if (!TryResolvePreviousActivitySnapshotPayload(previousCompletedRoute, out string activitySnapshotPayload, out RouteActivitySnapshotPayloadResolution payloadResolution))
            {
                bool isCaptureFailed = payloadResolution.FailureKind == RouteActivitySaveSnapshotFailureKind.SnapshotCaptureFailed;
                RouteActivitySaveSkipKind skipKind = ResolveSnapshotPayloadSkipReason(payloadResolution.FailureKind);
                string checkpointStatus = isCaptureFailed ? "Failed" : "Waiting";
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotPayload' checkpointStatus='{checkpointStatus}' activityIdentity='{Normalize(previousActivityIdentity)}' sourceActivityId='<none>' sourceEntrySequence='0' payloadResolved='false' payloadObjectCount='0' targetIds='<none>' payloadSize='0' failureReason='{Normalize(payloadResolution.FailureReason)}'.",
                    DebugUtility.Colors.Info);
                if (isCaptureFailed)
                {
                    LogRouteActivitySaveCaptureFailed(
                        previousCompletedRoute,
                        currentRouteIdentity,
                        currentRouteOperationId,
                        currentTransitionId,
                        currentRouteSequence,
                        skipKind,
                        Normalize(payloadResolution.FailureReason),
                        source,
                        reason);
                }
                else
                {
                    LogRouteActivitySaveSaveSkipped(
                        previousCompletedRoute,
                        currentRouteIdentity,
                        currentRouteOperationId,
                        currentTransitionId,
                        currentRouteSequence,
                        skipKind,
                        Normalize(payloadResolution.FailureReason),
                        source,
                        reason);
                }
                return;
            }

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySnapshotPayloadResolved previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' previousRouteOperationId='{previousCompletedRoute.RouteOperationId}' previousRouteSequence='{previousCompletedRoute.RouteSequence}' activityIdentity='{Normalize(previousActivityIdentity)}' sourceActivityId='{Normalize(payloadResolution.SourceActivityId)}' sourceEntrySequence='{payloadResolution.SourceEntrySequence}' payloadObjectCount='{payloadResolution.PayloadObjectCount}' targetIds='{Normalize(payloadResolution.TargetIds)}' schemaId='{Normalize(payloadResolution.SchemaId)}' payloadSize='{payloadResolution.PayloadSize}'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotPayload' checkpointStatus='Passed' activityIdentity='{Normalize(previousActivityIdentity)}' sourceActivityId='{Normalize(payloadResolution.SourceActivityId)}' sourceEntrySequence='{payloadResolution.SourceEntrySequence}' payloadResolved='true' payloadObjectCount='{payloadResolution.PayloadObjectCount}' targetIds='{Normalize(payloadResolution.TargetIds)}' payloadSize='{payloadResolution.PayloadSize}'.",
                DebugUtility.Colors.Info);

            if (string.IsNullOrWhiteSpace(activitySnapshotPayload))
            {
                LogRouteActivitySaveSaveSkipped(
                    previousCompletedRoute,
                    currentRouteIdentity,
                    currentRouteOperationId,
                    currentTransitionId,
                    currentRouteSequence,
                    RouteActivitySaveSkipKind.NoSnapshotPayload,
                    "snapshot da activity da rota anterior ausente.",
                    source,
                    reason);
                return;
            }

            ISessionOperationalActivitySaveAdapter adapter = ResolveSessionOperationalActivitySaveAdapterOrFail();
            ProgressionSlotContext slotContext = ResolveProgressionSlotContextOrFail(
                routeIdentity: currentRouteIdentity,
                routeOperationId: currentRouteOperationId,
                transitionId: currentTransitionId,
                routeSequence: currentRouteSequence,
                source: source,
                reason: reason);

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveStarted previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' previousRouteOperationId='{previousCompletedRoute.RouteOperationId}' previousRouteSequence='{previousCompletedRoute.RouteSequence}' previousActivityIdentity='{Normalize(previousActivityIdentity)}' previousActivitySaveKey='{previousActivitySaveKey}' currentRouteIdentity='{Normalize(currentRouteIdentity)}' currentRouteOperationId='{Normalize(currentRouteOperationId)}' currentTransitionId='{Normalize(currentTransitionId)}' routeSequence='{currentRouteSequence}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            RouteActivitySaveSaveResult saveResult = adapter.SaveActivityOnExit(
                runtimeModeConfig,
                slotContext,
                previousActivityIdentity,
                activitySnapshotPayload);

            if (saveResult.IsSkipped)
            {
                LogRouteActivitySaveSaveSkipped(
                    previousCompletedRoute,
                    currentRouteIdentity,
                    currentRouteOperationId,
                    currentTransitionId,
                    currentRouteSequence,
                    saveResult.SkipKind,
                    saveResult.Detail,
                    source,
                    reason);
                return;
            }

            if (!saveResult.IsSaved)
            {
                string message =
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] save-on-exit retornou estado invalido previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' previousRouteOperationId='{previousCompletedRoute.RouteOperationId}' previousRouteSequence='{previousCompletedRoute.RouteSequence}' currentRouteIdentity='{currentRouteIdentity}' currentRouteOperationId='{currentRouteOperationId}' currentTransitionId='{currentTransitionId}'.";
                DebugUtility.LogError<SessionOperationalPipeline>(message);
                throw new InvalidOperationException(message);
            }

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveCompleted previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' previousRouteOperationId='{previousCompletedRoute.RouteOperationId}' previousRouteSequence='{previousCompletedRoute.RouteSequence}' previousActivityIdentity='{Normalize(previousActivityIdentity)}' previousActivitySaveKey='{previousActivitySaveKey}' currentRouteIdentity='{Normalize(currentRouteIdentity)}' currentRouteOperationId='{Normalize(currentRouteOperationId)}' currentTransitionId='{Normalize(currentTransitionId)}' routeSequence='{currentRouteSequence}' detail='{Normalize(saveResult.Detail)}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Success);
        }

        private void ExecuteRouteActivitySaveLoadOnEnterOrFail(
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRouteCommand command,
            RouteActivitySavePlan routeActivitySavePlan,
            string source,
            string reason)
        {
            RouteActivitySaveLoadPlan loadOnEnterPlan = routeActivitySavePlan.LoadOnEnter;
            string activityIdentity = loadOnEnterPlan.ActivityIdentity;
            string routeIdentity = loadOnEnterPlan.RouteIdentity;
            string routeOperationId = loadOnEnterPlan.RouteOperationId;
            string transitionId = loadOnEnterPlan.TransitionId;
            int routeSequence = loadOnEnterPlan.RouteSequence;

            if (!loadOnEnterPlan.ShouldLoad)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' skipKind='{loadOnEnterPlan.SkipKind}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(loadOnEnterPlan.SkipKind)}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            ISessionOperationalActivitySaveAdapter adapter = ResolveSessionOperationalActivitySaveAdapterOrFail();
            _pendingLoadedRouteActivitySnapshotPayload = default;
            if (!TryResolveCurrentSnapshotIdForRouteActivityLoad(out string currentSnapshotId, out string snapshotFailureReason))
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Waiting' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='false' sourceActivityId='<none>' sourceEntrySequence='0' payloadObjectCount='0' targetIds='<none>' payloadSize='0'.",
                    DebugUtility.Colors.Info);
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' skipKind='{RouteActivitySaveSkipKind.NoCurrentSnapshot}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(RouteActivitySaveSkipKind.NoCurrentSnapshot)}' detail='snapshotPointerReason={Normalize(snapshotFailureReason)}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            ProgressionSlotContext slotContext = ResolveProgressionSlotContextOrFail(
                routeIdentity: routeIdentity,
                routeOperationId: routeOperationId,
                transitionId: transitionId,
                routeSequence: routeSequence,
                source: source,
                reason: reason);
            if (!string.Equals(slotContext.SnapshotId.Value, currentSnapshotId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] ProgressionSlotContext snapshotId mismatch with CurrentSnapshotId routeIdentity='{Normalize(routeIdentity)}' routeOperationId='{Normalize(routeOperationId)}' transitionId='{Normalize(transitionId)}' routeSequence='{routeSequence}' slotSnapshotId='{Normalize(slotContext.SnapshotId.Value)}' currentSnapshotId='{Normalize(currentSnapshotId)}'.");
            }

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' slotId='{slotContext.SlotId}' snapshotId='{slotContext.SnapshotId}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            RouteActivitySaveLoadResult result = adapter.LoadActivitySaveOnEnter(
                runtimeModeConfig,
                command,
                slotContext,
                activityIdentity);

            if (result.IsSkipped)
            {
                if (result.FailureKind == RouteActivitySaveSnapshotFailureKind.SnapshotPayloadMissing)
                {
                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Waiting' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='false' sourceActivityId='<none>' sourceEntrySequence='0' payloadObjectCount='0' targetIds='<none>' payloadSize='0'.",
                        DebugUtility.Colors.Info);
                }

                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' skipKind='{result.SkipKind}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(result.SkipKind)}' detail='{Normalize(result.Detail)}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!result.IsLoaded)
            {
                string message = $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] load-on-enter retornou estado invalido routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.";
                DebugUtility.LogError<SessionOperationalPipeline>(message);
                throw new InvalidOperationException(message);
            }

            LoadedSessionActivitySnapshotPayloadParseResult parseResult =
                LoadedSessionActivitySnapshotPayloadParser.Parse(result.ActivitySnapshotPayload, RouteActivitySnapshotSchemaId);
            if (!parseResult.Succeeded)
            {
                DebugUtility.LogError(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Failed' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='false' sourceActivityId='<none>' sourceEntrySequence='0' payloadObjectCount='0' targetIds='<none>' payloadSize='0' failureKind='{parseResult.FailureKind}' failureReason='{Normalize(parseResult.FailureReason)}' detail='{Normalize(parseResult.Detail)}'.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] payload invalido no load-on-enter routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' failureKind='{parseResult.FailureKind}' failureReason='{Normalize(parseResult.FailureReason)}'.");
            }

            LoadedSessionActivitySnapshotPayload loadedPayload = parseResult.Payload;
            _pendingLoadedRouteActivitySnapshotPayload = new LoadedRouteActivitySnapshotPayloadContext(
                Normalize(activityIdentity),
                loadedPayload,
                result.ActivitySnapshotPayload.Length);
            string loadedTargetIds = BuildLoadedSnapshotTargetIds(loadedPayload.Objects);
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySnapshotPayloadLoaded activityIdentity='{Normalize(activityIdentity)}' sourceActivityId='{Normalize(loadedPayload.ActivityId)}' sourceEntrySequence='{loadedPayload.SourceEntrySequence}' payloadObjectCount='{loadedPayload.Objects.Count}' targetIds='{loadedTargetIds}' schemaId='{Normalize(loadedPayload.SchemaId)}' payloadSize='{result.ActivitySnapshotPayload.Length}'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Passed' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='true' sourceActivityId='{Normalize(loadedPayload.ActivityId)}' sourceEntrySequence='{loadedPayload.SourceEntrySequence}' payloadObjectCount='{loadedPayload.Objects.Count}' targetIds='{loadedTargetIds}' payloadSize='{result.ActivitySnapshotPayload.Length}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadCompleted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' detail='{Normalize(result.Detail)}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Success);
        }

        private static void LogRouteActivitySaveSaveSkipped(
            SessionOperationalRouteSnapshot previousCompletedRoute,
            string currentRouteIdentity,
            string currentRouteOperationId,
            string currentTransitionId,
            int currentRouteSequence,
            RouteActivitySaveSkipKind skipKind,
            string detail,
            string source,
            string reason)
        {
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveSkipped previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' previousRouteOperationId='{previousCompletedRoute.RouteOperationId}' previousRouteSequence='{previousCompletedRoute.RouteSequence}' previousActivityIdentity='{Normalize(previousCompletedRoute.ActivityIdentity)}' previousActivitySaveKey='{BuildActivitySaveKey(previousCompletedRoute.ActivityIdentity)}' currentRouteIdentity='{Normalize(currentRouteIdentity)}' currentRouteOperationId='{Normalize(currentRouteOperationId)}' currentTransitionId='{Normalize(currentTransitionId)}' routeSequence='{currentRouteSequence}' skipKind='{skipKind}' skipReason='{RouteActivitySaveSkipKindMapper.ToCode(skipKind)}' detail='{Normalize(detail)}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogRouteActivitySaveCaptureFailed(
            SessionOperationalRouteSnapshot previousCompletedRoute,
            string currentRouteIdentity,
            string currentRouteOperationId,
            string currentTransitionId,
            int currentRouteSequence,
            RouteActivitySaveSkipKind failureKind,
            string detail,
            string source,
            string reason)
        {
            // Semantic failure: snapshot capture failed - this is NOT a normal skip but a failure condition
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveCaptureFailed previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' previousRouteOperationId='{previousCompletedRoute.RouteOperationId}' previousRouteSequence='{previousCompletedRoute.RouteSequence}' previousActivityIdentity='{Normalize(previousCompletedRoute.ActivityIdentity)}' previousActivitySaveKey='{BuildActivitySaveKey(previousCompletedRoute.ActivityIdentity)}' currentRouteIdentity='{Normalize(currentRouteIdentity)}' currentRouteOperationId='{Normalize(currentRouteOperationId)}' currentTransitionId='{Normalize(currentTransitionId)}' routeSequence='{currentRouteSequence}' failureKind='{failureKind}' failureReason='{RouteActivitySaveSkipKindMapper.ToCode(failureKind)}' detail='{Normalize(detail)}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Warning);
        }

        private static string BuildActivitySaveKey(string activityIdentity)
        {
            string normalized = Normalize(activityIdentity);
            return string.IsNullOrWhiteSpace(normalized) ? string.Empty : $"activity:{normalized}";
        }

        private bool TryResolvePreviousActivitySnapshotPayload(
            SessionOperationalRouteSnapshot previousCompletedRoute,
            out string activitySnapshotPayload,
            out RouteActivitySnapshotPayloadResolution resolution)
        {
            activitySnapshotPayload = string.Empty;
            resolution = default;

            if (!previousCompletedRoute.IsValid)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    payloadObjectCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.NoPreviousRoute,
                    failureReason: "previous_route_invalid");
                return false;
            }

            string sessionStateId = Normalize(previousCompletedRoute.ActivityIdentity);
            if (string.IsNullOrWhiteSpace(sessionStateId))
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    payloadObjectCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.NoCurrentActivity,
                    failureReason: "session_state_id_missing");
                return false;
            }

            if (!_dependencies.TryResolveActivitySnapshotPayloadProvider(out ISessionActivitySnapshotPayloadProvider provider))
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    payloadObjectCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.SnapshotProviderUnavailable,
                    failureReason: "no_snapshot_provider");
                return false;
            }

            bool resolved = provider.TryGetSnapshotPayloadForSaveOnExit(
                sessionStateId,
                out SessionActivitySnapshotPayload payload,
                out string failureReason);

            if (!resolved)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    payloadObjectCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.SnapshotPayloadMissing,
                    failureReason: string.IsNullOrWhiteSpace(failureReason) ? "no_snapshot_payload" : Normalize(failureReason));
                return false;
            }

            if (!payload.IsValid)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    payloadObjectCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureKind: RouteActivitySaveSnapshotFailureKind.SnapshotCaptureFailed,
                    failureReason: string.IsNullOrWhiteSpace(failureReason) ? "snapshot_capture_failed" : Normalize(failureReason));
                return false;
            }

            activitySnapshotPayload = SerializeSnapshotPayload(payload);
            if (string.IsNullOrWhiteSpace(activitySnapshotPayload))
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    payload.SchemaId,
                    payload.ActivityId,
                    payload.EntrySequence,
                    payload.Objects.Count,
                    BuildTargetIdsLabel(payload.Objects),
                    0,
                    RouteActivitySaveSnapshotFailureKind.SnapshotPayloadInvalid,
                    "snapshot_payload_serialization_failed");
                return false;
            }

            resolution = new RouteActivitySnapshotPayloadResolution(
                payload.SchemaId,
                payload.ActivityId,
                payload.EntrySequence,
                payload.Objects.Count,
                BuildTargetIdsLabel(payload.Objects),
                activitySnapshotPayload.Length,
                RouteActivitySaveSnapshotFailureKind.None,
                "resolved");
            return true;
        }

        private static RouteActivitySaveSkipKind ResolveSnapshotPayloadSkipReason(RouteActivitySaveSnapshotFailureKind failureKind)
        {
            if (failureKind == RouteActivitySaveSnapshotFailureKind.SnapshotProviderUnavailable)
            {
                return RouteActivitySaveSkipKind.NoSnapshotProvider;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.SnapshotCaptureFailed)
            {
                return RouteActivitySaveSkipKind.SnapshotCaptureFailed;
            }

            if (failureKind == RouteActivitySaveSnapshotFailureKind.SnapshotPayloadInvalid)
            {
                return RouteActivitySaveSkipKind.SnapshotPayloadInvalid;
            }

            return RouteActivitySaveSkipKind.NoSnapshotPayload;
        }

        private static string SerializeSnapshotPayload(SessionActivitySnapshotPayload payload)
        {
            StringBuilder builder = new(512);
            builder.Append('{');
            AppendJsonField(builder, "schemaId", payload.SchemaId);
            builder.Append(',');
            AppendJsonField(builder, "sessionStateId", payload.SessionStateId);
            builder.Append(',');
            AppendJsonField(builder, "activityId", payload.ActivityId);
            builder.Append(',');
            AppendJsonField(builder, "entrySequence", payload.EntrySequence.ToString(CultureInfo.InvariantCulture), isNumber: true);
            builder.Append(',');
            builder.Append("\"objects\":[");

            for (int index = 0; index < payload.Objects.Count; index++)
            {
                SessionActivitySnapshotPayloadObject obj = payload.Objects[index];
                if (index > 0)
                {
                    builder.Append(',');
                }

                builder.Append('{');
                AppendJsonField(builder, "targetId", obj.TargetId);
                builder.Append(',');
                builder.Append("\"position\":{");
                AppendJsonField(builder, "x", obj.PositionX.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "y", obj.PositionY.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "z", obj.PositionZ.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append("},");
                builder.Append("\"rotation\":{");
                AppendJsonField(builder, "x", obj.RotationX.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "y", obj.RotationY.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "z", obj.RotationZ.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "w", obj.RotationW.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append("},");
                builder.Append("\"scale\":{");
                AppendJsonField(builder, "x", obj.ScaleX.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "y", obj.ScaleY.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append(',');
                AppendJsonField(builder, "z", obj.ScaleZ.ToString("0.######", CultureInfo.InvariantCulture), isNumber: true);
                builder.Append("}");
                builder.Append('}');
            }

            builder.Append("]}");
            return builder.ToString();
        }

        private static void AppendJsonField(StringBuilder builder, string key, string value, bool isNumber = false)
        {
            builder.Append('"').Append(key).Append("\":");
            if (isNumber)
            {
                builder.Append(string.IsNullOrWhiteSpace(value) ? "0" : value);
                return;
            }

            builder.Append('"').Append(EscapeJson(value)).Append('"');
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private static string BuildTargetIdsLabel(IReadOnlyList<SessionActivitySnapshotPayloadObject> objects)
        {
            if (objects == null || objects.Count == 0)
            {
                return "<none>";
            }

            HashSet<string> targetIds = new(StringComparer.Ordinal);
            for (int index = 0; index < objects.Count; index++)
            {
                string targetId = Normalize(objects[index].TargetId);
                if (!string.IsNullOrWhiteSpace(targetId))
                {
                    targetIds.Add(targetId);
                }
            }

            if (targetIds.Count == 0)
            {
                return "<none>";
            }

            return string.Join(",", targetIds);
        }

        private static string BuildLoadedSnapshotTargetIds(IReadOnlyList<LoadedSessionActivitySnapshotPayloadObject> objects)
        {
            if (objects == null || objects.Count == 0)
            {
                return "<none>";
            }

            HashSet<string> targetIds = new(StringComparer.Ordinal);
            for (int index = 0; index < objects.Count; index++)
            {
                string targetId = Normalize(objects[index].TargetId);
                if (!string.IsNullOrWhiteSpace(targetId))
                {
                    targetIds.Add(targetId);
                }
            }

            return targetIds.Count == 0 ? "<none>" : string.Join(",", targetIds);
        }

        private readonly struct RouteActivitySnapshotPayloadResolution
        {
            public RouteActivitySnapshotPayloadResolution(
                string schemaId,
                string sourceActivityId,
                int sourceEntrySequence,
                int payloadObjectCount,
                string targetIds,
                int payloadSize,
                RouteActivitySaveSnapshotFailureKind failureKind,
                string failureReason)
            {
                SchemaId = Normalize(schemaId);
                SourceActivityId = Normalize(sourceActivityId);
                SourceEntrySequence = sourceEntrySequence < 0 ? 0 : sourceEntrySequence;
                PayloadObjectCount = payloadObjectCount < 0 ? 0 : payloadObjectCount;
                TargetIds = Normalize(targetIds);
                PayloadSize = payloadSize < 0 ? 0 : payloadSize;
                FailureKind = failureKind;
                FailureReason = Normalize(failureReason);
            }

            public string SchemaId { get; }
            public string SourceActivityId { get; }
            public int SourceEntrySequence { get; }
            public int PayloadObjectCount { get; }
            public string TargetIds { get; }
            public int PayloadSize { get; }
            public RouteActivitySaveSnapshotFailureKind FailureKind { get; }
            public string FailureReason { get; }
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

        private ProgressionSlotContext ResolveProgressionSlotContextOrFail(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            IProgressionSlotContextResolver resolver = _dependencies.ProgressionSlotContextResolver;

            bool resolved = resolver.TryResolveForRouteActivitySave(
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason,
                out ProgressionSlotContext slotContext,
                out string failureReason);

            if (!resolved || slotContext == null || !slotContext.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] ProgressionSlotContext obrigatorio ausente/invalido routeIdentity='{Normalize(routeIdentity)}' routeOperationId='{Normalize(routeOperationId)}' transitionId='{Normalize(transitionId)}' routeSequence='{routeSequence}' failureReason='{Normalize(failureReason)}'.");
            }

            return slotContext;
        }

        private bool TryResolveCurrentSnapshotIdForRouteActivityLoad(
            out string currentSnapshotId,
            out string failureReason)
        {
            currentSnapshotId = string.Empty;
            if (!_dependencies.TryResolveSaveStateService(out ISaveStateService saveStateService))
            {
                failureReason = "save_state_service_missing";
                return false;
            }

            if (!saveStateService.HasCurrent)
            {
                failureReason = "current_save_missing";
                return false;
            }

            SaveCurrentState currentState = saveStateService.CurrentState;
            if (currentState == null || !currentState.IsValid)
            {
                failureReason = "current_state_invalid";
                return false;
            }

            string snapshotPointer = Normalize(currentState.CurrentSnapshotId);
            if (string.IsNullOrWhiteSpace(snapshotPointer))
            {
                failureReason = "current_snapshot_missing";
                return false;
            }

            currentSnapshotId = snapshotPointer;
            failureReason = "resolved";
            return true;
        }

        private static SessionOperationalInputModeKind PrepareInputCapabilityOrFail(
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRoutePlan plan,
            SessionOperationalInputPolicy inputPolicy,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (!plan.IsValid)
            {
                throw new ArgumentException("SessionOperationalRoutePlan is invalid.", nameof(plan));
            }

            SessionOperationalInputModeKind initialInputMode = ResolveInitialInputModeFromPolicyOrFail(
                inputPolicy,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);

            UnityOperationalInputRuntimeAdapter.PrepareOrFail(
                runtimeModeConfig,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                source,
                reason);

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][InputCapability] InputCapabilityPrepared routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{plan.OperationalSurfaceKind}' inputPolicy='{inputPolicy}' inputMode='{initialInputMode}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return initialInputMode;
        }

        private static SessionOperationalInputModeKind ResolveInitialInputModeFromPolicyOrFail(
            SessionOperationalInputPolicy inputPolicy,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            return inputPolicy switch
            {
                SessionOperationalInputPolicy.MenuNavigation => SessionOperationalInputModeKind.FrontendMenu,
                SessionOperationalInputPolicy.ActivityGameplay => SessionOperationalInputModeKind.ActivityDefault,
                SessionOperationalInputPolicy.OverlayNavigation => SessionOperationalInputModeKind.PauseOverlay,
                SessionOperationalInputPolicy.InputLocked => SessionOperationalInputModeKind.InputLocked,
                _ => throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalInputCapability] inputPolicy invalida routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' inputPolicy='{inputPolicy}' source='{source}' reason='{reason}'."),
            };
        }

        private static IReadOnlyList<PlayerSetEntry> ResolvePlayerSetFromPlan(SessionOperationalRoutePlan plan)
        {
            if (plan.RouteParticipantSetDefinition == null)
            {
                return Array.Empty<PlayerSetEntry>();
            }

            return plan.RouteParticipantSetDefinition.ResolveEntriesOrFail(nameof(SessionOperationalPipeline));
        }

        private static string FormatPlayerPreparationOutcome(PlayerPreparationOutcome outcome)
        {
            return outcome switch
            {
                PlayerPreparationOutcome.ObservedNoOp => "observed_noop",
                PlayerPreparationOutcome.PlannedOnly => "planned_only",
                PlayerPreparationOutcome.Materialized => "materialized",
                _ => "unknown",
            };
        }

        private static string FormatPlayerIdsForHandoff(IReadOnlyList<PlayerPlannedEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "<none>";
            }

            List<string> playerIds = new(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(entries[i].PlayerId))
                {
                    playerIds.Add(entries[i].PlayerId);
                }
            }

            return playerIds.Count == 0 ? "<none>" : string.Join(", ", playerIds);
        }

        private static IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> ResolvePlayerTechnicalEntriesFromPlan(SessionOperationalRoutePlan plan)
        {
            if (plan.RouteParticipantSetDefinition == null)
            {
                return Array.Empty<PlayerSetDefinitionAsset.PlayerActorResolvedEntry>();
            }

            IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> entries =
                plan.RouteParticipantSetDefinition.ResolvePlayerActorEntriesOrFail(nameof(SessionOperationalPipeline));
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<PlayerSetDefinitionAsset.PlayerActorResolvedEntry>();
            }

            return entries;
        }

        private static string ResolveRouteParticipantSetDefinitionLabel(SessionOperationalRoutePlan plan)
        {
            if (plan.RouteParticipantSetDefinition == null)
            {
                return "<none>";
            }

            return string.IsNullOrWhiteSpace(plan.RouteParticipantSetDefinition.name)
                ? "<unnamed>"
                : plan.RouteParticipantSetDefinition.name.Trim();
        }

        private static SessionOperationalLoadingFact CreateLoadingFact(
            SessionOperationalLoadingCommand command,
            SessionOperationalLoadingStage stage,
            SessionOperationalLoadingOutcomeKind outcomeKind,
            float normalizedProgress,
            string stepLabel,
            string message)
        {
            return new SessionOperationalLoadingFact(command, stage, outcomeKind, normalizedProgress, stepLabel, message);
        }

        private static void LogLoadingProgress(
            SessionOperationalLoadingCommand command,
            SessionOperationalLoadingStage stage,
            float normalizedProgress,
            string stepLabel,
            string source,
            string reason)
        {
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Loading] LoadingProgress routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' loadingMode='{command.LoadingMode}' loadingProfile='{command.LoadingProfileId}' stage='{stage}' normalizedProgress='{normalizedProgress:0.###}' stepLabel='{stepLabel}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private RuntimeModeConfig ResolveRuntimeModeConfigOrFail()
        {
            return _dependencies.RuntimeModeConfig;
        }

        private void RecordLastCompletedRouteSnapshot(SessionOperationalRouteCommand command)
        {
            if (!command.IsValid)
            {
                return;
            }

            SessionOperationalRouteSnapshot snapshot = new(
                command.RouteIdentity,
                command.RouteOperationId,
                command.RouteSequence,
                command.ActiveSceneKey,
                command.ActivitySavePolicy.SaveActivityOnExit,
                command.HandoffSessionStateId,
                command.FinalScenesToLoad);

            if (!snapshot.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionOperationalPipeline),
                    $"[FATAL][Config][SessionOperationalPipeline] failed to record last completed route snapshot routeIdentity='{command.RouteIdentity}' routeSequence='{command.RouteSequence}'.");
            }

            _lastCompletedRouteSnapshot = snapshot;
        }

        private static string FormatSceneNames(IReadOnlyList<SceneKeyAsset> sceneKeys)
        {
            if (sceneKeys == null || sceneKeys.Count == 0)
            {
                return "<none>";
            }

            List<string> sceneNames = new(sceneKeys.Count);
            for (int i = 0; i < sceneKeys.Count; i++)
            {
                sceneNames.Add(ResolveSceneName(sceneKeys[i], $"sceneKeys[{i}]"));
            }

            return string.Join(", ", sceneNames);
        }

        private static void LogPreviousRouteUnloadPlan(
            SessionOperationalRouteSnapshot previousCompletedRoute,
            string currentRouteIdentity,
            IReadOnlyList<SceneKeyAsset> explicitScenesToUnload,
            IReadOnlyList<SceneKeyAsset> autoScenesToUnload,
            IReadOnlyList<SceneKeyAsset> finalScenesToUnload)
        {
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Route] unload='previous_route_owned_scenes_applied' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' currentRouteIdentity='{Normalize(currentRouteIdentity)}' previousRouteSequence='{previousCompletedRoute.RouteSequence}' previousRouteActiveSceneKey='{previousCompletedRoute.ActiveSceneKey?.name ?? string.Empty}' previousRouteOwnedSceneKeys=[{FormatSceneNames(previousCompletedRoute.RouteOwnedLoadedSceneKeys)}] autoScenesToUnload=[{FormatSceneNames(autoScenesToUnload)}] explicitScenesToUnload=[{FormatSceneNames(explicitScenesToUnload)}] finalScenesToUnload=[{FormatSceneNames(finalScenesToUnload)}].",
                DebugUtility.Colors.Info);
        }

        private readonly struct SessionOperationalRouteSnapshot
        {
            public SessionOperationalRouteSnapshot(
                string routeIdentity,
                string routeOperationId,
                int routeSequence,
                SceneKeyAsset activeSceneKey,
                bool saveActivityOnExit,
                string activityIdentity,
                IReadOnlyList<SceneKeyAsset> routeOwnedLoadedSceneKeys)
            {
                RouteIdentity = Normalize(routeIdentity);
                RouteOperationId = Normalize(routeOperationId);
                RouteSequence = routeSequence < 0 ? 0 : routeSequence;
                ActiveSceneKey = activeSceneKey;
                SaveActivityOnExit = saveActivityOnExit;
                ActivityIdentity = Normalize(activityIdentity);
                RouteOwnedLoadedSceneKeys = routeOwnedLoadedSceneKeys ?? throw new ArgumentNullException(nameof(routeOwnedLoadedSceneKeys));
            }

            public string RouteIdentity { get; }
            public string RouteOperationId { get; }
            public int RouteSequence { get; }
            public SceneKeyAsset ActiveSceneKey { get; }
            public bool SaveActivityOnExit { get; }
            public string ActivityIdentity { get; }
            public IReadOnlyList<SceneKeyAsset> RouteOwnedLoadedSceneKeys { get; }

            public bool IsValid =>
                !string.IsNullOrWhiteSpace(RouteIdentity) &&
                !string.IsNullOrWhiteSpace(RouteOperationId) &&
                RouteSequence > 0 &&
                ActiveSceneKey != null &&
                RouteOwnedLoadedSceneKeys != null;
        }


        private SessionOperationalResult CompleteOperationalRouteOperation(
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string routeIdentity,
            string source,
            string reason)
        {
            SessionOperationalIdentity identity = new(
                _sessionOperationalPipelineId,
                routeOperationId,
                transitionId,
                routeSequence,
                routeIdentity,
                routeIdentity,
                source,
                reason,
                SessionOperationalStage.Completed);

            SessionOperationalFact fact = new(
                SessionOperationalFactKind.Completed,
                identity,
                source,
                reason,
                "Operational route completed.");

            _state.SetCurrentIdentity(identity);
            _state.MarkStarted();
            _state.MarkCompleted();
            _state.AppendFact(fact);
            _state.AppendTrace(
                $"[OBS][SessionOperationalPipeline] fact='OperationalRouteCompleted' stage='{identity.Stage}' routeOperationId='{identity.RouteOperationId}' transitionId='{identity.TransitionId}' routeSequence='{identity.TransitionSequence}' routeId='{identity.RouteId}' routeProfileId='{identity.RouteProfileId}' source='{source}' reason='{reason}' message='Operational route completed.'");

            return new SessionOperationalResult(
                SessionOperationalResultKind.Completed,
                identity,
                _state.Facts,
                "Operational route completed.");
        }
    }
}
