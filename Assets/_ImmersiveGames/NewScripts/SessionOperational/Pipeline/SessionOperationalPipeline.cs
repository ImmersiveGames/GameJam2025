using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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

        public SessionOperationalPipeline(string sessionOperationalPipelineId = DefaultPipelineId)
        {
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

            RouteRequestSubmissionResult preflight = TryPreflightRouteRequest(route, sourceText, reasonText);
            if (!preflight.IsAccepted)
            {
                return preflight;
            }

            try
            {
                ValidatePersistentScenesPolicyOrFail(route);
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

            if (!route.TryValidate(out string routeValidationError))
            {
                string message = $"[FATAL][Config][SessionOperationalRoute] {routeValidationError}";
                DebugUtility.LogError<SessionOperationalPipeline>(message);
                throw new InvalidOperationException(message);
            }

            ValidatePersistentScenesPolicyOrFail(route);
            ISceneCompositionAdapter routeExecutor = ResolveRouteExecutorOrFail();
            RuntimeModeConfig runtimeModeConfig = ResolveRuntimeModeConfigOrFail();
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = RuntimePolicyConfigResolver.ResolvePersistentScenesPolicyOrFail(runtimeModeConfig);
            IFadeAdapter fadeAdapter = null;
            ILoadingAdapter loadingAdapter = null;
            if (route.UsesTransition)
            {
                fadeAdapter = ResolveFadeAdapterOrFail();
            }

            string routeIdentity = route.RouteIdentity;
            string sourceText = Normalize(source);
            string reasonText = Normalize(reason);
            string activeSceneName = ResolveSceneName(route.ActiveSceneKey, nameof(route.ActiveSceneKey));
            SessionOperationalRouteLoadPlan loadPlan = default;
            SessionOperationalRouteSnapshot previousCompletedRoute = default;
            SessionOperationalRouteUnloadPlan unloadPlan = default;

            string routeOperationId;
            string transitionId;
            int routeSequence;
            SessionOperationalLoadingCommand loadingCommand;

            lock (_operationalRouteSync)
            {
                if (_hasActiveOperationalRouteOperation)
                {
                    DebugUtility.LogWarning<SessionOperationalPipeline>(
                        $"[OBS][SessionOperationalPipeline][Route] rejected reason='stale_or_foreign_route' routeIdentity='{routeIdentity}' activeRouteIdentity='{_activeOperationalRouteIdentity}' activeRouteOperationId='{_activeOperationalRouteOperationId}' activeTransitionId='{_activeOperationalTransitionId}' source='{sourceText}' reason='{reasonText}'.");
                    throw new InvalidOperationException("Operational route operation is already in flight.");
                }

                _operationalRouteSequence += 1;
                routeSequence = _operationalRouteSequence;
                routeOperationId = BuildRouteOperationId(routeIdentity, activeSceneName, routeSequence);
                transitionId = BuildTransitionId(routeIdentity, activeSceneName, routeSequence);
                loadPlan = ResolveRouteLoadPlanOrFail(route, persistentScenesPolicy, activeSceneName);
                previousCompletedRoute = _lastCompletedRouteSnapshot;
                unloadPlan = ResolveRouteUnloadPlanOrFail(
                    route,
                    previousCompletedRoute,
                    persistentScenesPolicy,
                    loadPlan.FinalScenesToLoad,
                    activeSceneName);

                if (route.UnloadPreviousRouteOwnedScenes)
                {
                    LogPreviousRouteUnloadPlan(
                        previousCompletedRoute,
                        routeIdentity,
                        unloadPlan.ExplicitScenesToUnload,
                        unloadPlan.AutoScenesToUnload,
                        unloadPlan.FinalScenesToUnload);
                }

                _hasActiveOperationalRouteOperation = true;
                _activeOperationalRouteOperationId = routeOperationId;
                _activeOperationalTransitionId = transitionId;
                _activeOperationalRouteIdentity = routeIdentity;
            }

            loadingCommand = ResolveLoadingCommandOrFail(
                route,
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

            SessionOperationalRouteAudioCommand audioCommand = BuildRouteAudioCommandOrFail(
                route,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                sourceText,
                reasonText);
            RouteActivitySavePolicy routeActivitySavePolicy = route.ActivitySavePolicy;

            SessionOperationalRouteCommand command = route.CreateCommand(
                routeOperationId,
                transitionId,
                routeSequence,
                sourceText,
                reasonText,
                route.TransitionMode,
                route.TransitionProfile,
                audioCommand,
                routeActivitySavePolicy,
                loadPlan.FinalScenesToLoad,
                unloadPlan.AutoScenesToUnload,
                unloadPlan.FinalScenesToUnload);

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Route] command='OperationalRouteCommand' routeIdentity='{routeIdentity}' activeScene='{activeSceneName}' activeSceneKey='{route.ActiveSceneKey?.name ?? string.Empty}' activeSceneImplicitLoad='{loadPlan.ActiveSceneImplicitLoad}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{route.CompletionHandoff}' finalScenesToLoad=[{FormatSceneNames(loadPlan.FinalScenesToLoad)}] autoScenesToUnload=[{FormatSceneNames(unloadPlan.AutoScenesToUnload)}] explicitScenesToUnload=[{FormatSceneNames(unloadPlan.ExplicitScenesToUnload)}] finalScenesToUnload=[{FormatSceneNames(unloadPlan.FinalScenesToUnload)}] source='{sourceText}' reason='{reasonText}'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySavePlanReady routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' routeSequence='{routeSequence}' loadActivitySaveOnEnter='{routeActivitySavePolicy.LoadActivitySaveOnEnter}' saveActivityOnExit='{routeActivitySavePolicy.SaveActivityOnExit}' source='{sourceText}' reason='{reasonText}'.",
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
                await EnsureSessionActivityRouteExitTeardownOrFailAsync(
                    previousCompletedRoute,
                    unloadPlan,
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
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
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
                    route,
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
                    route.HandoffSessionStateId,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    source,
                    reason);

                SessionOperationalInputPolicy inputPolicy = route.InputPolicy;
                SessionOperationalInputModeKind initialInputMode = PrepareInputCapabilityOrFail(
                    runtimeModeConfig,
                    route,
                    inputPolicy,
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    source,
                    reason);

                string routeClass = route.OperationalSurfaceKind.ToString();
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
                        $"[FATAL][H1][SessionOperationalPipeline][InputMode] Failed to record InputCapabilityPrepared routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{route.OperationalSurfaceKind}' inputPolicy='{inputPolicy}' resolvedInputMode='{initialInputMode}' source='{sourceText}' reason='{reasonText}'.");
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
                        $"[FATAL][H1][SessionOperationalPipeline][InputMode] Failed to record InitialInputModePrepared routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{route.OperationalSurfaceKind}' inputPolicy='{inputPolicy}' resolvedInputMode='{initialInputMode}' source='{sourceText}' reason='{reasonText}'.");
                }

                if (route.CompletionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
                {
                    if (string.IsNullOrWhiteSpace(route.HandoffSessionStateId))
                    {
                        throw new InvalidOperationException("handoffSessionStateId is required when completionHandoff=SessionActivityEntry.");
                    }

                    PlayerPreparationIdentity playerPreparationIdentity = new(
                        _sessionOperationalPipelineId,
                        route.HandoffSessionStateId,
                        routeIdentity,
                        routeOperationId,
                        routeSequence,
                        transitionId);
                    PlayerPreparationPlan playerPreparationPlan = new(
                        playerPreparationIdentity,
                        route.CompletionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry,
                        new PlayerSet(ResolvePlayerSetFromRoute(route)),
                        sourceText,
                        reasonText);
                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerPreparationStarted' pipelineId='{playerPreparationIdentity.PipelineId}' sessionId='{playerPreparationIdentity.SessionId}' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' routeSessionParticipantPreparation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(route)}' source='{sourceText}' reason='{reasonText}'.",
                        DebugUtility.Colors.Info);

                    playerPreparationResult = PlayerPreparationStage.Execute(playerPreparationPlan);
                    if (!playerPreparationResult.IsValid)
                    {
                        throw new InvalidOperationException("PlayerPreparationStage returned an invalid result.");
                    }
                    hasPlayerPreparationResult = true;

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerPreparationIntentPrepared' pipelineId='{playerPreparationIdentity.PipelineId}' sessionId='{playerPreparationIdentity.SessionId}' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' routeSessionParticipantPreparation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(route)}' source='{sourceText}' reason='{reasonText}' playerIds='{FormatPlayerIdsForHandoff(playerPreparationResult.Snapshot.PlannedEntries)}'.",
                        DebugUtility.Colors.Info);

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][PlayerPreparation] event='PlayerPreparationCompleted' pipelineId='{playerPreparationIdentity.PipelineId}' sessionId='{playerPreparationIdentity.SessionId}' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' routeSessionParticipantPreparation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(route)}' source='{sourceText}' reason='{reasonText}' outcome='{(playerPreparationResult.IsObservedNoOp ? "observed_noop" : (playerPreparationResult.IsPlannedOnly ? "planned_only" : "materialized"))}'.",
                        DebugUtility.Colors.Info);

                    ExecuteActivityCameraPreparationStageOrFail(
                        route,
                        activeSceneName,
                        routeIdentity,
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        route.HandoffSessionStateId,
                        source,
                        reason);
                }

                if (loadingCommand.IsEnabled)
                {
                    await loadingAdapter.UpdateLoadingAsync(
                        loadingCommand,
                        CreateLoadingFact(
                            loadingCommand,
                            SessionOperationalLoadingStage.MaterializationCompleted,
                            0.80f,
                            "Materialization completed",
                            "MaterializationCompleted"));

                    LogLoadingProgress(
                        loadingCommand,
                        SessionOperationalLoadingStage.MaterializationCompleted,
                        0.80f,
                        "Materialization completed",
                        sourceText,
                        reasonText);

                    await loadingAdapter.UpdateLoadingAsync(
                        loadingCommand,
                        CreateLoadingFact(
                            loadingCommand,
                            SessionOperationalLoadingStage.OperationalRouteCompleted,
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
                RecordLastCompletedRouteSnapshot(route, routeOperationId, routeSequence, loadPlan.FinalScenesToLoad);

                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][Transition] fact='OperationalRouteCompleted' routeIdentity='{adapterFact.RouteIdentity}' routeOperationId='{adapterFact.RouteOperationId}' transitionId='{adapterFact.TransitionId}' routeSequence='{adapterFact.RouteSequence}' correlationId='{adapterFact.CorrelationId}' message='{adapterFact.Message}' transitionMode='{command.TransitionMode}' transitionProfile='{command.TransitionProfileLabel}' source='{sourceText}' reason='{reasonText}'.",
                    DebugUtility.Colors.Success);

                if (route.CompletionHandoff == SessionOperationalRouteCompletionHandoffKind.SessionActivityEntry)
                {
                    if (string.IsNullOrWhiteSpace(route.HandoffSessionStateId))
                    {
                        throw new InvalidOperationException("handoffSessionStateId is required when completionHandoff=SessionActivityEntry.");
                    }

                    ISessionActivityEntryHandoffReceiver activityReceiver = ResolveActivityReceiverOrFail();
                    if (!string.Equals(activityReceiver.SessionId, route.HandoffSessionStateId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"handoffSessionStateId '{route.HandoffSessionStateId}' does not match the active SessionActivityPipeline session '{activityReceiver.SessionId}'.");
                    }

                    if (!hasPlayerPreparationResult || !playerPreparationResult.IsValid)
                    {
                        throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][PlayerPreparation] Missing valid PlayerPreparationResult for SessionActivity handoff routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.");
                    }

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][Route] handoff='SessionActivityEntryHandoffEmitted' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{sourceText}' reason='{reasonText}' pendingHandoff='SessionActivityEntry' routeSessionParticipantPreparation='true' routeParticipantSetDefinition='{ResolveRouteParticipantSetDefinitionLabel(route)}' playerPreparationOutcome='{FormatPlayerPreparationOutcome(playerPreparationResult.Snapshot.Outcome)}' plannedPlayers='{playerPreparationResult.Snapshot.PlannedPlayersCount}' materializedPlayers='{playerPreparationResult.Snapshot.MaterializedPlayersCount}' pendingRequiredPlayers='{playerPreparationResult.Snapshot.PendingRequiredPlayersCount}'.",
                        DebugUtility.Colors.Info);

                    SessionActivityPlayerPreparationHandoff playerPreparationHandoff = new(
                        playerPreparationResult.Snapshot.Identity.PipelineId,
                        playerPreparationResult.Snapshot.Identity.SessionId,
                        playerPreparationResult.Snapshot.Identity.RouteIdentity,
                        playerPreparationResult.Snapshot.Identity.RouteOperationId,
                        playerPreparationResult.Snapshot.Identity.TransitionId,
                        playerPreparationResult.Snapshot.Identity.RouteSequence,
                        FormatPlayerPreparationOutcome(playerPreparationResult.Snapshot.Outcome),
                        playerPreparationResult.Snapshot.ParticipationKind.ToString(),
                        playerPreparationResult.Snapshot.PlannedPlayersCount,
                        playerPreparationResult.Snapshot.RequiredPlayersCount,
                        playerPreparationResult.Snapshot.OptionalPlayersCount,
                        playerPreparationResult.Snapshot.MaterializedPlayersCount,
                        playerPreparationResult.Snapshot.SkippedPlayersCount,
                        playerPreparationResult.Snapshot.PendingRequiredPlayersCount,
                        FormatPlayerIdsForHandoff(playerPreparationResult.Snapshot.PlannedEntries),
                        BuildPlayerTechnicalPlanEntriesForHandoff(route));

                    SessionActivityEntryHandoff handoff = new(
                        string.Empty,
                        0,
                        0,
                        route.HandoffSessionStateId,
                        playerPreparationHandoff,
                        new SessionActivityRouteTransitionContext(
                            route.UsesTransition && route.TransitionProfile != null,
                            route.TransitionProfile,
                            loadingCommand.LoadingMode == SessionOperationalRouteLoadingMode.Profile && loadingCommand.LoadingProfile != null,
                            loadingCommand.LoadingProfile),
                        sourceText,
                        reasonText);

                    SessionActivityCommandResult activityResult = activityReceiver.StartFromPreparedHandoff(handoff, sourceText, reasonText);
                    if (!activityResult.IsValid || activityResult.IsRejected)
                    {
                        throw new InvalidOperationException($"SessionActivityPipeline rejected the prepared handoff. result='{activityResult.Kind}' reason='{activityResult.Reason}'.");
                    }
                }

                routeOperationSucceeded = true;
                completionReason = "completed";
                return adapterFact;
            }
            catch (RouteRequestBlockedBySessionActivityException blockedException)
            {
                completionReason = $"blocked:{blockedException.BlockedReason}";
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][Route] RouteRequestRejectedByPolicy routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' blockedReason='{blockedException.BlockedReason}' detail='{blockedException.BlockedDetail}' source='{sourceText}' reason='{reasonText}'.",
                    DebugUtility.Colors.Info);
                return new SessionOperationalRouteCompletedFact(
                    command,
                    routeOperationId,
                    $"route_request_blocked_by_session_activity:{blockedException.BlockedReason}");
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
            OperationalRouteAsset route,
            string source,
            string reason)
        {
            string routeIdentity = Normalize(route.RouteIdentity);
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

            ISessionActivityRouteExitTeardownBoundary boundary = ResolveActivityRouteExitTeardownBoundaryOrFail();
            if (boundary.HasPendingOperation)
            {
                return RejectByPolicy(routeIdentity, "pending_operation_active", source, reason, boundary.CurrentStage, boundary.CurrentRailKind);
            }

            if (boundary.CurrentStage == SessionActivityStage.ActivationWindowReady ||
                boundary.CurrentStage == SessionActivityStage.ActivationWindowStarted ||
                boundary.CurrentStage == SessionActivityStage.ActivationWindowSceneLoading ||
                boundary.CurrentStage == SessionActivityStage.ActivationWindowAdditiveSceneLoadStarted ||
                boundary.CurrentStage == SessionActivityStage.ActivationWindowAdditiveSceneLoaded)
            {
                return RejectByPolicy(routeIdentity, "activation_window_not_completed", source, reason, boundary.CurrentStage, boundary.CurrentRailKind);
            }

            if (IsDeactivationStage(boundary.CurrentStage) &&
                boundary.CurrentRailKind != SessionActivityRailKind.ActivityRouteExitRail)
            {
                return RejectByPolicy(routeIdentity, "activity_transition_in_progress", source, reason, boundary.CurrentStage, boundary.CurrentRailKind);
            }

            return new RouteRequestSubmissionResult(RouteRequestSubmissionKind.Accepted, routeIdentity, "accepted", string.Empty);
        }

        private RouteRequestSubmissionResult RejectByPolicy(
            string routeIdentity,
            string reason,
            string source,
            string detail,
            SessionActivityStage stage,
            SessionActivityRailKind railKind)
        {
            DebugUtility.LogWarning<SessionOperationalPipeline>(
                $"[OBS][SessionOperationalPipeline][Route] RouteRequestBlockedBySessionActivity routeIdentity='{routeIdentity}' reason='{reason}' stage='{stage}' railKind='{railKind}' source='{Normalize(source)}' reasonDetail='{Normalize(detail)}'.");
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Route] RouteRequestRejectedByPolicy routeIdentity='{routeIdentity}' reason='{reason}' stage='{stage}' railKind='{railKind}' source='{Normalize(source)}' reasonDetail='{Normalize(detail)}'.",
                DebugUtility.Colors.Info);
            return new RouteRequestSubmissionResult(
                RouteRequestSubmissionKind.RejectedByPolicy,
                routeIdentity,
                reason,
                $"stage='{stage}' railKind='{railKind}'");
        }

        private static bool IsDeactivationStage(SessionActivityStage stage)
        {
            return stage == SessionActivityStage.DeactivationWindowStarted ||
                   stage == SessionActivityStage.DeactivationWindowSceneLoading ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoadStarted ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoaded ||
                   stage == SessionActivityStage.DeactivationWindowReady ||
                   stage == SessionActivityStage.DeactivationWindowCompleted ||
                   stage == SessionActivityStage.DeactivationWindowSceneUnloading ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloadStarted ||
                   stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloaded ||
                   stage == SessionActivityStage.DeactivationWindowSkippedNoContent;
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

            if (!CanAcceptStage(
                    stage,
                    normalizedRouteOperationId,
                    normalizedTransitionId,
                    transitionSequence,
                    normalizedRouteId,
                    normalizedRouteProfileId,
                    normalizedSource,
                    normalizedReason))
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

            if (stage == SessionOperationalStage.InitialInputModePrepared)
            {
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

            if (stage == SessionOperationalStage.Completed)
            {
                _state.MarkCompleted();
            }

            return true;
        }

        private bool CanAcceptStage(
            SessionOperationalStage stage,
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string source,
            string reason)
        {
            if (!string.Equals(_state.SessionOperationalPipelineId, _sessionOperationalPipelineId, StringComparison.Ordinal) ||
                !string.Equals(_state.RouteOperationId, routeOperationId, StringComparison.Ordinal) ||
                !string.Equals(_state.TransitionId, transitionId, StringComparison.Ordinal) ||
                _state.TransitionSequence != transitionSequence ||
                !string.Equals(_state.RouteId, routeId, StringComparison.Ordinal) ||
                !string.Equals(_state.RouteProfileId, routeProfileId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!_state.HasStarted)
            {
                return stage == SessionOperationalStage.RouteOperationStarted;
            }

            if (_state.HasCompleted)
            {
                return false;
            }
            // Aceita progresso monotônico/sparse:
            // - mesma identity já validada acima
            // - não aceita retrocesso
            // - não aceita duplicata do stage atual
            return (int)stage > (int)_state.CurrentStage;
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
            return $"{Normalize(routeIdentity)}|{Normalize(activeScene)}|{sequence}";
        }

        private static string BuildTransitionId(string routeIdentity, string activeScene, int sequence)
        {
            return $"{Normalize(routeIdentity)}|{Normalize(activeScene)}|{sequence}|sandbox";
        }

        private static ISceneCompositionAdapter ResolveRouteExecutorOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISceneCompositionAdapter>(out var executor) && executor != null)
            {
                return executor;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline] ISceneCompositionAdapter obrigatorio ausente para o trilho operacional.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private static IFadeAdapter ResolveFadeAdapterOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<IFadeAdapter>(out var fadeAdapter) && fadeAdapter != null)
            {
                return fadeAdapter;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline] IFadeAdapter obrigatorio ausente para transitionMode=Profile.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private static ILoadingAdapter ResolveLoadingAdapterOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<ILoadingAdapter>(out var loadingAdapter) && loadingAdapter != null)
            {
                return loadingAdapter;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline] ILoadingAdapter obrigatorio ausente para o rail canonico de loading.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private static ISessionOperationalRouteCameraAdapter ResolveSessionOperationalRouteCameraAdapterOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISessionOperationalRouteCameraAdapter>(out var routeCameraAdapter) && routeCameraAdapter != null)
            {
                return routeCameraAdapter;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline][RouteCamera] ISessionOperationalRouteCameraAdapter obrigatorio ausente para route/surface camera presentation stage.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private static ISessionOperationalActivityCameraAdapter ResolveSessionOperationalActivityCameraAdapterOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISessionOperationalActivityCameraAdapter>(out var activityCameraAdapter) && activityCameraAdapter != null)
            {
                return activityCameraAdapter;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline][ActivityCamera] ISessionOperationalActivityCameraAdapter obrigatorio ausente para activity camera presentation stage.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private static ISessionActivityEntryHandoffReceiver ResolveActivityReceiverOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISessionActivityEntryHandoffReceiver>(out var receiver) && receiver != null)
            {
                return receiver;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline] ISessionActivityEntryHandoffReceiver obrigatorio ausente para o trilho operacional.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private static ISessionActivityRouteExitTeardownBoundary ResolveActivityRouteExitTeardownBoundaryOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISessionActivityRouteExitTeardownBoundary>(out var boundary) && boundary != null)
            {
                return boundary;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline] ISessionActivityRouteExitTeardownBoundary obrigatorio ausente para teardown canonico pre-unload.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private async Task EnsureSessionActivityRouteExitTeardownOrFailAsync(
            SessionOperationalRouteSnapshot previousCompletedRoute,
            SessionOperationalRouteUnloadPlan unloadPlan,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (!ShouldRequireSessionActivityRouteExitTeardown(previousCompletedRoute, unloadPlan))
            {
                return;
            }

            ISessionActivityRouteExitTeardownBoundary teardownBoundary = ResolveActivityRouteExitTeardownBoundaryOrFail();
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Route] OperationalRouteRequestDeferredForSessionActivityTeardown routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' sessionStateId='{previousCompletedRoute.ActivityIdentity}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Route] SessionActivityRouteExitTeardownStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' sessionStateId='{previousCompletedRoute.ActivityIdentity}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            int pollCount = 0;
            while (true)
            {
                SessionActivityRouteExitTeardownResult teardownResult = teardownBoundary.RequestRouteExitTeardown(
                    previousCompletedRoute.ActivityIdentity,
                    source,
                    reason);

                bool stageIsClosed =
                    teardownResult.Stage == SessionActivityStage.Deactivation ||
                    teardownResult.Stage == SessionActivityStage.Completed ||
                    teardownResult.Stage == SessionActivityStage.ClosedForRouteExit;

                if (!teardownResult.IsValid)
                {
                    FailRouteExitTeardownOrThrow(
                        routeIdentity,
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        previousCompletedRoute,
                        source,
                        reason,
                        "invalid_teardown_result",
                        "Teardown boundary returned invalid result.",
                        teardownResult);
                }

                if (teardownResult.Kind == SessionActivityRouteExitTeardownKind.NotRequired)
                {
                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][Route] SessionActivityRouteExitTeardownCompleted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' sessionStateId='{previousCompletedRoute.ActivityIdentity}' teardownResult='{teardownResult}' source='{source}' reason='{reason}'.",
                        DebugUtility.Colors.Success);
                    return;
                }

                if (teardownResult.Kind == SessionActivityRouteExitTeardownKind.Completed)
                {
                    if (teardownResult.HasPendingHandoff)
                    {
                        FailRouteExitTeardownOrThrow(
                            routeIdentity,
                            routeOperationId,
                            transitionId,
                            routeSequence,
                            previousCompletedRoute,
                            source,
                            reason,
                            "pending_activity_handoff_after_route_exit_close",
                            $"Teardown completed with pending handoff for activityId='{teardownResult.ActivityId}'.",
                            teardownResult);
                    }

                    if (!stageIsClosed)
                    {
                        FailRouteExitTeardownOrThrow(
                            routeIdentity,
                            routeOperationId,
                            transitionId,
                            routeSequence,
                            previousCompletedRoute,
                            source,
                            reason,
                            "teardown_stage_not_closed",
                            $"Teardown ended in stage='{teardownResult.Stage}', expected deactivated/closed stage.",
                            teardownResult);
                    }

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][Route] SessionActivityRouteExitTeardownCompleted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' sessionStateId='{previousCompletedRoute.ActivityIdentity}' teardownResult='{teardownResult}' source='{source}' reason='{reason}'.",
                        DebugUtility.Colors.Success);
                    return;
                }

                if (teardownResult.Kind == SessionActivityRouteExitTeardownKind.Started ||
                    teardownResult.Kind == SessionActivityRouteExitTeardownKind.InProgress)
                {
                    if (teardownResult.HasPendingHandoff)
                    {
                        FailRouteExitTeardownOrThrow(
                            routeIdentity,
                            routeOperationId,
                            transitionId,
                            routeSequence,
                            previousCompletedRoute,
                            source,
                            reason,
                            "pending_activity_handoff_during_route_exit",
                            $"Route-exit teardown reported handoff while in-progress. activityId='{teardownResult.ActivityId}'.",
                            teardownResult);
                    }

                    if (pollCount % 120 == 0)
                    {
                        DebugUtility.Log(typeof(SessionOperationalPipeline),
                            $"[OBS][SessionOperationalPipeline][Route] SessionActivityRouteExitTeardownInProgress routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' sessionStateId='{previousCompletedRoute.ActivityIdentity}' pollCount='{pollCount}' teardownResult='{teardownResult}' source='{source}' reason='{reason}'.",
                            DebugUtility.Colors.Info);
                    }

                    pollCount += 1;
                    await Task.Delay(100);
                    continue;
                }

                if (teardownResult.Kind == SessionActivityRouteExitTeardownKind.Failed)
                {
                    FailRouteExitTeardownOrThrow(
                        routeIdentity,
                        routeOperationId,
                        transitionId,
                        routeSequence,
                        previousCompletedRoute,
                        source,
                        reason,
                        string.IsNullOrWhiteSpace(teardownResult.Reason) ? "route_exit_teardown_failed" : teardownResult.Reason,
                        string.IsNullOrWhiteSpace(teardownResult.Detail) ? "SessionActivity route-exit teardown failed." : teardownResult.Detail,
                        teardownResult);
                }

                FailRouteExitTeardownOrThrow(
                    routeIdentity,
                    routeOperationId,
                    transitionId,
                    routeSequence,
                    previousCompletedRoute,
                    source,
                    reason,
                    "unknown_route_exit_teardown_kind",
                    $"Unsupported route-exit teardown kind '{teardownResult.Kind}'.",
                    teardownResult);
            }
        }

        private static void FailRouteExitTeardownOrThrow(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            SessionOperationalRouteSnapshot previousCompletedRoute,
            string source,
            string reason,
            string blockedReason,
            string blockedDetail,
            SessionActivityRouteExitTeardownResult teardownResult)
        {
            string routeRequestBlockedReason = ResolveRouteRequestBlockedBySessionActivityReason(teardownResult, blockedReason);
            DebugUtility.LogWarning<SessionOperationalPipeline>(
                $"[OBS][SessionOperationalPipeline][Route] RouteRequestBlockedBySessionActivity routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' sessionStateId='{previousCompletedRoute.ActivityIdentity}' reason='{routeRequestBlockedReason}' blockedReason='{blockedReason}' detail='{blockedDetail}' teardownResult='{teardownResult}' source='{source}' reasonDetail='{reason}'.");
            throw new RouteRequestBlockedBySessionActivityException(blockedReason, blockedDetail);
        }

        private static string ResolveRouteRequestBlockedBySessionActivityReason(
            SessionActivityRouteExitTeardownResult teardownResult,
            string blockedReason)
        {
            if (teardownResult.Stage == SessionActivityStage.ActivationWindowStarted ||
                teardownResult.Stage == SessionActivityStage.ActivationWindowSceneLoading ||
                teardownResult.Stage == SessionActivityStage.ActivationWindowAdditiveSceneLoadStarted ||
                teardownResult.Stage == SessionActivityStage.ActivationWindowAdditiveSceneLoaded ||
                teardownResult.Stage == SessionActivityStage.ActivationWindowReady ||
                teardownResult.Stage == SessionActivityStage.ActivationWindowCompleted ||
                teardownResult.Stage == SessionActivityStage.ActivationWindowSceneUnloading ||
                teardownResult.Stage == SessionActivityStage.ActivationWindowAdditiveSceneUnloadStarted ||
                teardownResult.Stage == SessionActivityStage.ActivationWindowAdditiveSceneUnloaded ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowStarted ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowSceneLoading ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoadStarted ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoaded ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowReady ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowCompleted ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowSceneUnloading ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloadStarted ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloaded ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowSkippedNoContent)
            {
                return "activation_window_not_completed";
            }

            if (teardownResult.Stage == SessionActivityStage.DeactivationWindowStarted ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowSceneLoading ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoadStarted ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowAdditiveSceneLoaded ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowReady ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowCompleted ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowSceneUnloading ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloadStarted ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowAdditiveSceneUnloaded ||
                teardownResult.Stage == SessionActivityStage.DeactivationWindowSkippedNoContent)
            {
                return "deactivation_window_in_progress";
            }

            if (teardownResult.Stage == SessionActivityStage.ActivityContentRetentionPlanResolved ||
                teardownResult.Stage == SessionActivityStage.ActivityContentReleaseStarted ||
                teardownResult.Stage == SessionActivityStage.ActivityContentSceneUnloading ||
                teardownResult.Stage == SessionActivityStage.ActivityContentSceneUnloaded ||
                teardownResult.Stage == SessionActivityStage.ActivityContentReleaseFailed)
            {
                return "activity_content_release_in_progress";
            }

            if (string.Equals(blockedReason, "pending_activity_handoff_during_route_exit", StringComparison.Ordinal) ||
                string.Equals(blockedReason, "pending_activity_handoff_after_route_exit_close", StringComparison.Ordinal))
            {
                return "pending_handoff_present";
            }

            if (string.Equals(blockedReason, "invalid_teardown_result", StringComparison.Ordinal))
            {
                return "invalid_teardown_result";
            }

            if (string.Equals(blockedReason, "deactivation_window_not_route_exit_owned", StringComparison.Ordinal))
            {
                return "activity_transition_in_progress";
            }

            return "session_activity_not_route_exit_safe";
        }

        private sealed class RouteRequestBlockedBySessionActivityException : Exception
        {
            public RouteRequestBlockedBySessionActivityException(string blockedReason, string blockedDetail)
                : base("route_request_blocked_by_session_activity")
            {
                BlockedReason = Normalize(blockedReason);
                BlockedDetail = Normalize(blockedDetail);
            }

            public string BlockedReason { get; }
            public string BlockedDetail { get; }
        }

        private static bool ShouldRequireSessionActivityRouteExitTeardown(
            SessionOperationalRouteSnapshot previousCompletedRoute,
            SessionOperationalRouteUnloadPlan unloadPlan)
        {
            if (!previousCompletedRoute.IsValid)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(previousCompletedRoute.ActivityIdentity))
            {
                return false;
            }

            if (unloadPlan.FinalScenesToUnload == null || unloadPlan.FinalScenesToUnload.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < unloadPlan.FinalScenesToUnload.Count; i++)
            {
                SceneKeyAsset candidate = unloadPlan.FinalScenesToUnload[i];
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
            OperationalRouteAsset route,
            RuntimeModeConfig runtimeModeConfig,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (runtimeModeConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline] RuntimeModeConfig obrigatorio ausente para resolver loading.");
            }

            SessionOperationalRouteLoadingMode effectiveLoadingMode = route.LoadingMode;
            RuntimeLoadingProfileAsset effectiveLoadingProfile = route.LoadingProfile;

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
                    string message = $"[FATAL][Config][SessionOperationalRoute] loadingProfile is required when loadingMode=Profile routeIdentity='{route.RouteIdentity}'.";
                    DebugUtility.LogError<SessionOperationalPipeline>(message);
                    throw new InvalidOperationException(message);
                }

                if (!effectiveLoadingProfile.TryValidate(out string profileValidationError))
                {
                    string message = $"[FATAL][Config][SessionOperationalRoute] loadingProfile is invalid routeIdentity='{route.RouteIdentity}' profile='{effectiveLoadingProfile.ProfileId}' detail='{profileValidationError}'.";
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
                    string message = $"[FATAL][Config][SessionOperationalPipeline] RuntimePersistentScenesPolicyAsset obrigatorio ausente para loading efetivo routeIdentity='{route.RouteIdentity}'.";
                    DebugUtility.LogError<SessionOperationalPipeline>(message);
                    throw new InvalidOperationException(message);
                }

                loadingSceneName = persistentScenesPolicy.ResolveSceneNameByRoleOrFail(
                    RuntimePersistentSceneRole.Loading,
                    nameof(SessionOperationalPipeline));
            }

            SessionOperationalLoadingCommand loadingCommand = new(
                route.RouteIdentity,
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
                string message = $"[FATAL][Config][SessionOperationalPipeline] loading command invalid routeIdentity='{route.RouteIdentity}' loadingMode='{loadingCommand.LoadingMode}' loadingProfile='{loadingCommand.LoadingProfileId}' loadingSceneName='{loadingCommand.LoadingSceneName}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.";
                DebugUtility.LogError<SessionOperationalPipeline>(message);
                throw new InvalidOperationException(message);
            }

            return loadingCommand;
        }

        private static IAudioAdapter ResolveAudioAdapterOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<IAudioAdapter>(out var audioAdapter) && audioAdapter != null)
            {
                return audioAdapter;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline][Audio] IAudioAdapter obrigatorio ausente para routeAudio cue.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
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

        private static ISessionOperationalActivitySaveAdapter ResolveSessionOperationalActivitySaveAdapterOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISessionOperationalActivitySaveAdapter>(out var adapter) && adapter != null)
            {
                return adapter;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] ISessionOperationalActivitySaveAdapter obrigatorio ausente para RouteActivitySave.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private static void ExecuteRouteCameraPresentationStageOrFail(
            OperationalRouteAsset route,
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
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationStageStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activeScene='{Normalize(activeSceneName)}' operationalSurfaceKind='{route.OperationalSurfaceKind}' completionHandoff='{route.CompletionHandoff}' source='{source}' reason='{reason}'.",
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
                route,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                route.OperationalSurfaceKind.ToString(),
                route.CompletionHandoff,
                activeSceneName,
                source,
                reason);

            if (!routeCameraAdapter.TryPrepareRouteCamera(prepareCommand, out SessionOperationalRouteCameraPrepareResult prepareResult, out string prepareReason))
            {
                bool required = route.SurfacePresentationProfile != null && route.SurfacePresentationProfile.Required;
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][RouteCamera] prepare_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{route.OperationalSurfaceKind}' required='{required}' reason='{prepareReason}'.");
            }

            if (prepareResult.IsSkipped)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{route.OperationalSurfaceKind}' completionHandoff='{route.CompletionHandoff}' reason='{prepareResult.SkipReason}' source='{source}' reasonDetail='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (prepareResult.IsPrepared)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationStagePrepared routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{route.OperationalSurfaceKind}' requirementId='{prepareResult.ReadyFact.RequirementId}' outputCamera='{prepareResult.ReadyFact.OutputCameraName}' presentationRig='{prepareResult.ReadyFact.PresentationRigName}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            string failureReason = prepareResult.FailureFact != null
                ? prepareResult.FailureFact.FailureReason
                : prepareReason;
            bool profileRequired = route.SurfacePresentationProfile != null && route.SurfacePresentationProfile.Required;

            DebugUtility.LogError(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteCamera] RouteCameraPresentationFailed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{route.OperationalSurfaceKind}' profileRequired='{profileRequired}' reason='{failureReason}' source='{source}' reasonDetail='{reason}'.");

            throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][RouteCamera] prepare_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{route.OperationalSurfaceKind}' required='{profileRequired}' reason='{failureReason}'.");
        }

        private static void ExecuteActivityCameraPreparationStageOrFail(
            OperationalRouteAsset route,
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
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPreparationStageStarted routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activeScene='{Normalize(activeSceneName)}' completionHandoff='{route.CompletionHandoff}' activityIdentity='{Normalize(activityIdentity)}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            SessionOperationalActivityCameraPrepareCommand prepareCommand = new(
                route,
                routeIdentity,
                routeOperationId,
                transitionId,
                routeSequence,
                activityIdentity,
                route.CompletionHandoff,
                activeSceneName,
                source,
                reason);

            if (!activityCameraAdapter.TryPrepareActivityCamera(prepareCommand, out SessionOperationalActivityCameraPrepareResult prepareResult, out string prepareReason))
            {
                bool required = route.ActivityPresentationProfile != null && route.ActivityPresentationProfile.Required;
                throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][ActivityCamera] prepare_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{route.CompletionHandoff}' required='{required}' reason='{prepareReason}'.");
            }

            if (prepareResult.IsSkipped)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{route.CompletionHandoff}' activityIdentity='{Normalize(activityIdentity)}' reason='{prepareResult.SkipReason}' source='{source}' reasonDetail='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (prepareResult.IsPrepared)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPreparationStagePrepared routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{route.CompletionHandoff}' activityIdentity='{prepareResult.ReadyFact.ActivityIdentity}' requirementId='{prepareResult.ReadyFact.RequirementId}' outputCamera='{prepareResult.ReadyFact.Handle?.UnityCamera?.name}' presentationRig='{prepareResult.ReadyFact.Handle?.CameraRigInstance?.name}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Success);
                return;
            }

            string failureReason = prepareResult.FailureFact != null
                ? prepareResult.FailureFact.FailureReason
                : prepareReason;
            bool profileRequired = route.ActivityPresentationProfile != null && route.ActivityPresentationProfile.Required;

            DebugUtility.LogError(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][ActivityCamera] ActivityCameraPresentationFailed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{route.CompletionHandoff}' profileRequired='{profileRequired}' activityIdentity='{Normalize(activityIdentity)}' reason='{failureReason}' source='{source}' reasonDetail='{reason}'.");

            throw new InvalidOperationException($"[FATAL][Config][SessionOperationalPipeline][ActivityCamera] prepare_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{route.CompletionHandoff}' required='{profileRequired}' reason='{failureReason}'.");
        }

        private static void ExecuteReleasePreviousActivityCameraStageOrFail(
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

        private static void ExecuteRouteActivitySaveSaveOnExitOrFail(
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRouteSnapshot previousCompletedRoute,
            string currentRouteIdentity,
            string currentRouteOperationId,
            string currentTransitionId,
            int currentRouteSequence,
            string source,
            string reason)
        {
            if (!previousCompletedRoute.IsValid)
            {
                LogRouteActivitySaveSaveSkipped(
                    previousCompletedRoute,
                    currentRouteIdentity,
                    currentRouteOperationId,
                    currentTransitionId,
                    currentRouteSequence,
                    "no_previous_route",
                    "previous completed route snapshot ausente.",
                    source,
                    reason);
                return;
            }

            if (!previousCompletedRoute.SaveActivityOnExit)
            {
                LogRouteActivitySaveSaveSkipped(
                    previousCompletedRoute,
                    currentRouteIdentity,
                    currentRouteOperationId,
                    currentTransitionId,
                    currentRouteSequence,
                    "disabled_by_previous_route",
                    "save-on-exit desabilitado na politica da rota anterior.",
                    source,
                    reason);
                return;
            }

            string previousActivityIdentity = previousCompletedRoute.ActivityIdentity;
            if (string.IsNullOrWhiteSpace(previousActivityIdentity))
            {
                LogRouteActivitySaveSaveSkipped(
                    previousCompletedRoute,
                    currentRouteIdentity,
                    currentRouteOperationId,
                    currentTransitionId,
                    currentRouteSequence,
                    "no_activity_identity",
                    "activity identity da rota anterior ausente.",
                    source,
                    reason);
                return;
            }

            string previousActivitySaveKey = BuildActivitySaveKey(previousActivityIdentity);
            if (string.IsNullOrWhiteSpace(previousActivitySaveKey))
            {
                LogRouteActivitySaveSaveSkipped(
                    previousCompletedRoute,
                    currentRouteIdentity,
                    currentRouteOperationId,
                    currentTransitionId,
                    currentRouteSequence,
                    "no_save_key",
                    "activity save key da rota anterior ausente.",
                    source,
                    reason);
                return;
            }

            if (!TryResolvePreviousActivitySnapshotPayload(previousCompletedRoute, out string activitySnapshotPayload, out RouteActivitySnapshotPayloadResolution payloadResolution))
            {
                bool isCaptureFailed = IsSnapshotCaptureFailedReason(payloadResolution.FailureReason);
                string skipReason = ResolveSnapshotPayloadSkipReason(payloadResolution.FailureReason);
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
                        skipReason,
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
                        skipReason,
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
                    "no_activity_snapshot",
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
                    saveResult.SkipReason,
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
            string activityIdentity,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (!command.ActivitySavePolicy.LoadActivitySaveOnEnter)
            {
                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' skipReason='disabled_by_route' source='{source}' reason='{reason}'.",
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
                    $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' skipReason='no_current_snapshot' detail='snapshotPointerReason={Normalize(snapshotFailureReason)}' source='{source}' reason='{reason}'.",
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
                if (string.Equals(Normalize(result.SkipReason), "no_snapshot", StringComparison.Ordinal) ||
                    string.Equals(Normalize(result.SkipReason), "no_activity_snapshot", StringComparison.Ordinal))
                {
                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Waiting' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='false' sourceActivityId='<none>' sourceEntrySequence='0' payloadObjectCount='0' targetIds='<none>' payloadSize='0'.",
                        DebugUtility.Colors.Info);
                }

                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveLoadSkipped routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' skipReason='{Normalize(result.SkipReason)}' detail='{Normalize(result.Detail)}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!result.IsLoaded)
            {
                string message = $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] load-on-enter retornou estado invalido routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}'.";
                DebugUtility.LogError<SessionOperationalPipeline>(message);
                throw new InvalidOperationException(message);
            }

            if (!TryParseLoadedSnapshotPayload(result.ActivitySnapshotPayload, out LoadedSessionActivitySnapshotPayload loadedPayload, out string parseFailureReason))
            {
                DebugUtility.LogError(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][QACheckpoint] checkpoint='RouteActivitySaveSnapshotLoad' checkpointStatus='Failed' activityIdentity='{Normalize(activityIdentity)}' payloadLoaded='false' sourceActivityId='<none>' sourceEntrySequence='0' payloadObjectCount='0' targetIds='<none>' payloadSize='0' failureReason='{Normalize(parseFailureReason)}'.");
                throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] payload invalido no load-on-enter routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' activityIdentity='{Normalize(activityIdentity)}' failureReason='{Normalize(parseFailureReason)}'.");
            }

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
            string skipReason,
            string detail,
            string source,
            string reason)
        {
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveSaveSkipped previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' previousRouteOperationId='{previousCompletedRoute.RouteOperationId}' previousRouteSequence='{previousCompletedRoute.RouteSequence}' previousActivityIdentity='{Normalize(previousCompletedRoute.ActivityIdentity)}' previousActivitySaveKey='{BuildActivitySaveKey(previousCompletedRoute.ActivityIdentity)}' currentRouteIdentity='{Normalize(currentRouteIdentity)}' currentRouteOperationId='{Normalize(currentRouteOperationId)}' currentTransitionId='{Normalize(currentTransitionId)}' routeSequence='{currentRouteSequence}' skipReason='{Normalize(skipReason)}' detail='{Normalize(detail)}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogRouteActivitySaveCaptureFailed(
            SessionOperationalRouteSnapshot previousCompletedRoute,
            string currentRouteIdentity,
            string currentRouteOperationId,
            string currentTransitionId,
            int currentRouteSequence,
            string failureReason,
            string detail,
            string source,
            string reason)
        {
            // Semantic failure: snapshot capture failed - this is NOT a normal skip but a failure condition
            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][RouteActivitySave] RouteActivitySaveCaptureFailed previousRouteIdentity='{previousCompletedRoute.RouteIdentity}' previousRouteOperationId='{previousCompletedRoute.RouteOperationId}' previousRouteSequence='{previousCompletedRoute.RouteSequence}' previousActivityIdentity='{Normalize(previousCompletedRoute.ActivityIdentity)}' previousActivitySaveKey='{BuildActivitySaveKey(previousCompletedRoute.ActivityIdentity)}' currentRouteIdentity='{Normalize(currentRouteIdentity)}' currentRouteOperationId='{Normalize(currentRouteOperationId)}' currentTransitionId='{Normalize(currentTransitionId)}' routeSequence='{currentRouteSequence}' failureReason='{Normalize(failureReason)}' detail='{Normalize(detail)}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Warning);
        }

        private static string BuildActivitySaveKey(string activityIdentity)
        {
            string normalized = Normalize(activityIdentity);
            return string.IsNullOrWhiteSpace(normalized) ? string.Empty : $"activity:{normalized}";
        }

        private static bool TryResolvePreviousActivitySnapshotPayload(
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
                    failureReason: "session_state_id_missing");
                return false;
            }

            if (!DependencyManager.Provider.TryGetGlobal<ISessionActivitySnapshotPayloadProvider>(out var provider) || provider == null)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    payloadObjectCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureReason: "no_snapshot_provider");
                return false;
            }

            bool resolved = provider.TryGetSnapshotPayloadForSaveOnExit(
                sessionStateId,
                out SessionActivitySnapshotPayload payload,
                out string failureReason);

            if (!resolved || !payload.IsValid)
            {
                resolution = new RouteActivitySnapshotPayloadResolution(
                    schemaId: string.Empty,
                    sourceActivityId: string.Empty,
                    sourceEntrySequence: 0,
                    payloadObjectCount: 0,
                    targetIds: string.Empty,
                    payloadSize: 0,
                    failureReason: string.IsNullOrWhiteSpace(failureReason) ? "no_snapshot_payload" : Normalize(failureReason));
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
                "resolved");
            return true;
        }

        private static string ResolveSnapshotPayloadSkipReason(string failureReason)
        {
            string normalized = Normalize(failureReason);
            if (string.Equals(normalized, "no_snapshot_provider", StringComparison.Ordinal))
            {
                return "no_snapshot_provider";
            }

            if (IsSnapshotCaptureFailedReason(normalized))
            {
                return "snapshot_capture_failed";
            }

            return "no_snapshot_payload";
        }

        private static bool IsSnapshotCaptureFailedReason(string failureReason)
        {
            string normalized = Normalize(failureReason);
            return normalized.StartsWith("snapshot_capture_failed", StringComparison.Ordinal) ||
                   normalized.Contains("target_transform_missing", StringComparison.Ordinal);
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

        private static bool TryParseLoadedSnapshotPayload(
            string payload,
            out LoadedSessionActivitySnapshotPayload loadedPayload,
            out string failureReason)
        {
            loadedPayload = default;
            string normalizedPayload = Normalize(payload);
            if (string.IsNullOrWhiteSpace(normalizedPayload))
            {
                failureReason = "payload_empty";
                return false;
            }

            string schemaId = ExtractJsonStringValue(normalizedPayload, "schemaId");
            if (!string.Equals(schemaId, RouteActivitySnapshotSchemaId, StringComparison.Ordinal))
            {
                failureReason = $"schema_id_invalid:{Normalize(schemaId)}";
                return false;
            }

            string sessionStateId = ExtractJsonStringValue(normalizedPayload, "sessionStateId");
            string activityId = ExtractJsonStringValue(normalizedPayload, "activityId");
            int sourceEntrySequence = ExtractJsonIntValue(normalizedPayload, "entrySequence");
            if (string.IsNullOrWhiteSpace(sessionStateId) ||
                string.IsNullOrWhiteSpace(activityId) ||
                sourceEntrySequence <= 0)
            {
                failureReason = "payload_header_invalid";
                return false;
            }

            MatchCollection objectMatches = Regex.Matches(
                normalizedPayload,
                "\\{\\s*\"targetId\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"position\"\\s*:\\s*\\{\\s*\"x\"\\s*:\\s*([-0-9.]+)\\s*,\\s*\"y\"\\s*:\\s*([-0-9.]+)\\s*,\\s*\"z\"\\s*:\\s*([-0-9.]+)\\s*\\}\\s*,\\s*\"rotation\"\\s*:\\s*\\{\\s*\"x\"\\s*:\\s*([-0-9.]+)\\s*,\\s*\"y\"\\s*:\\s*([-0-9.]+)\\s*,\\s*\"z\"\\s*:\\s*([-0-9.]+)\\s*,\\s*\"w\"\\s*:\\s*([-0-9.]+)\\s*\\}\\s*,\\s*\"scale\"\\s*:\\s*\\{\\s*\"x\"\\s*:\\s*([-0-9.]+)\\s*,\\s*\"y\"\\s*:\\s*([-0-9.]+)\\s*,\\s*\"z\"\\s*:\\s*([-0-9.]+)\\s*\\}\\s*\\}");
            if (objectMatches.Count == 0)
            {
                failureReason = "payload_objects_missing";
                return false;
            }

            List<LoadedSessionActivitySnapshotPayloadObject> objects = new(objectMatches.Count);
            for (int index = 0; index < objectMatches.Count; index++)
            {
                Match objectMatch = objectMatches[index];
                string targetId = Normalize(objectMatch.Groups[1].Value);
                if (string.IsNullOrWhiteSpace(targetId))
                {
                    failureReason = "payload_target_id_invalid";
                    return false;
                }

                if (!TryParseFloatInvariant(objectMatch.Groups[2].Value, out float positionX) ||
                    !TryParseFloatInvariant(objectMatch.Groups[3].Value, out float positionY) ||
                    !TryParseFloatInvariant(objectMatch.Groups[4].Value, out float positionZ) ||
                    !TryParseFloatInvariant(objectMatch.Groups[5].Value, out float rotationX) ||
                    !TryParseFloatInvariant(objectMatch.Groups[6].Value, out float rotationY) ||
                    !TryParseFloatInvariant(objectMatch.Groups[7].Value, out float rotationZ) ||
                    !TryParseFloatInvariant(objectMatch.Groups[8].Value, out float rotationW) ||
                    !TryParseFloatInvariant(objectMatch.Groups[9].Value, out float scaleX) ||
                    !TryParseFloatInvariant(objectMatch.Groups[10].Value, out float scaleY) ||
                    !TryParseFloatInvariant(objectMatch.Groups[11].Value, out float scaleZ))
                {
                    failureReason = "payload_numeric_parse_failed";
                    return false;
                }

                objects.Add(new LoadedSessionActivitySnapshotPayloadObject(
                    targetId,
                    positionX,
                    positionY,
                    positionZ,
                    rotationX,
                    rotationY,
                    rotationZ,
                    rotationW,
                    scaleX,
                    scaleY,
                    scaleZ));
            }

            loadedPayload = new LoadedSessionActivitySnapshotPayload(
                schemaId,
                sessionStateId,
                activityId,
                sourceEntrySequence,
                objects);
            if (!loadedPayload.IsValid)
            {
                failureReason = "loaded_payload_invalid";
                loadedPayload = default;
                return false;
            }

            failureReason = "parsed";
            return true;
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

        private static string ExtractJsonStringValue(string payload, string key)
        {
            Match match = Regex.Match(payload, $"\"{Regex.Escape(key)}\"\\s*:\\s*\"([^\"]*)\"");
            return match.Success ? Normalize(match.Groups[1].Value) : string.Empty;
        }

        private static int ExtractJsonIntValue(string payload, string key)
        {
            Match match = Regex.Match(payload, $"\"{Regex.Escape(key)}\"\\s*:\\s*(\\d+)");
            if (!match.Success)
            {
                return 0;
            }

            return int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : 0;
        }

        private static bool TryParseFloatInvariant(string value, out float parsed)
        {
            return float.TryParse(
                value,
                NumberStyles.Float | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out parsed);
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
                string failureReason)
            {
                SchemaId = Normalize(schemaId);
                SourceActivityId = Normalize(sourceActivityId);
                SourceEntrySequence = sourceEntrySequence < 0 ? 0 : sourceEntrySequence;
                PayloadObjectCount = payloadObjectCount < 0 ? 0 : payloadObjectCount;
                TargetIds = Normalize(targetIds);
                PayloadSize = payloadSize < 0 ? 0 : payloadSize;
                FailureReason = Normalize(failureReason);
            }

            public string SchemaId { get; }
            public string SourceActivityId { get; }
            public int SourceEntrySequence { get; }
            public int PayloadObjectCount { get; }
            public string TargetIds { get; }
            public int PayloadSize { get; }
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

        private static ProgressionSlotContext ResolveProgressionSlotContextOrFail(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (!DependencyManager.Provider.TryGetGlobal<IProgressionSlotContextResolver>(out var resolver) ||
                resolver == null)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalPipeline][RouteActivitySave] IProgressionSlotContextResolver obrigatorio ausente para resolver ProgressionSlotContext.");
            }

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

        private static bool TryResolveCurrentSnapshotIdForRouteActivityLoad(
            out string currentSnapshotId,
            out string failureReason)
        {
            currentSnapshotId = string.Empty;
            if (!DependencyManager.Provider.TryGetGlobal<ISaveStateService>(out var saveStateService) ||
                saveStateService == null)
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
            OperationalRouteAsset route,
            SessionOperationalInputPolicy inputPolicy,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
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
                $"[OBS][SessionOperationalPipeline][InputCapability] InputCapabilityPrepared routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' operationalSurfaceKind='{route.OperationalSurfaceKind}' inputPolicy='{inputPolicy}' inputMode='{initialInputMode}' source='{source}' reason='{reason}'.",
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

        private static IReadOnlyList<PlayerSetEntry> ResolvePlayerSetFromRoute(OperationalRouteAsset route)
        {
            if (route == null || route.RouteParticipantSetDefinition == null)
            {
                return Array.Empty<PlayerSetEntry>();
            }

            return route.RouteParticipantSetDefinition.ResolveEntriesOrFail(nameof(SessionOperationalPipeline));
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

        private static IReadOnlyList<SessionActivityPlayerTechnicalPlanEntry> BuildPlayerTechnicalPlanEntriesForHandoff(OperationalRouteAsset route)
        {
            if (route == null || route.RouteParticipantSetDefinition == null)
            {
                return Array.Empty<SessionActivityPlayerTechnicalPlanEntry>();
            }

            IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> entries =
                route.RouteParticipantSetDefinition.ResolvePlayerActorEntriesOrFail(nameof(SessionOperationalPipeline));
            if (entries == null || entries.Count == 0)
            {
                return Array.Empty<SessionActivityPlayerTechnicalPlanEntry>();
            }

            List<SessionActivityPlayerTechnicalPlanEntry> technicalEntries = new(entries.Count);
            for (int index = 0; index < entries.Count; index++)
            {
                PlayerSetDefinitionAsset.PlayerActorResolvedEntry entry = entries[index];
                if (!entry.IsValid)
                {
                    continue;
                }

                technicalEntries.Add(new SessionActivityPlayerTechnicalPlanEntry(
                    entry.PlayerId,
                    entry.Required,
                    entry.Prefab,
                    entry.PlacementMode,
                    entry.PlacementId,
                    entry.LocalPosition,
                    entry.LocalRotation));
            }

            return technicalEntries;
        }

        private static string ResolveRouteParticipantSetDefinitionLabel(OperationalRouteAsset route)
        {
            if (route == null || route.RouteParticipantSetDefinition == null)
            {
                return "<none>";
            }

            return string.IsNullOrWhiteSpace(route.RouteParticipantSetDefinition.name)
                ? "<unnamed>"
                : route.RouteParticipantSetDefinition.name.Trim();
        }

        private static SessionOperationalRouteAudioCommand BuildRouteAudioCommandOrFail(
            OperationalRouteAsset route,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            SessionOperationalRouteAudioMode routeAudioMode = route.RouteAudioMode;
            AudioCueAsset routeAudioCue = routeAudioMode == SessionOperationalRouteAudioMode.Cue
                ? route.RouteAudioCue
                : null;
            SessionOperationalRouteAudioTiming routeAudioTiming = route.RouteAudioTiming;
            bool stopPreviousRouteAudio = route.StopPreviousRouteAudio;

            if (routeAudioTiming != SessionOperationalRouteAudioTiming.BeforeFadeOut)
            {
                string message =
                    $"[FATAL][Config][SessionOperationalPipeline][Audio] routeAudioTiming must be BeforeFadeOut routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' routeAudioTiming='{routeAudioTiming}' source='{source}' reason='{reason}'.";

                DebugUtility.LogError<SessionOperationalPipeline>(message);
                throw new InvalidOperationException(message);
            }

            if (routeAudioMode == SessionOperationalRouteAudioMode.Cue && routeAudioCue == null)
            {
                string message =
                    $"[FATAL][Config][SessionOperationalPipeline][Audio] routeAudioCue is required when routeAudioMode=Cue routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{source}' reason='{reason}'.";

                DebugUtility.LogError<SessionOperationalPipeline>(message);
                throw new InvalidOperationException(message);
            }

            SessionOperationalRouteAudioCommand audioCommand = new(
                routeAudioMode,
                routeAudioCue,
                routeAudioTiming,
                stopPreviousRouteAudio);

            if (!audioCommand.IsValid)
            {
                string message =
                    $"[FATAL][Config][SessionOperationalPipeline][Audio] invalid audio payload routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' routeAudioMode='{routeAudioMode}' routeAudioTiming='{routeAudioTiming}' routeAudioCue='{audioCommand.RouteAudioCueName}' stopPreviousRouteAudio='{stopPreviousRouteAudio}' source='{source}' reason='{reason}'.";

                DebugUtility.LogError<SessionOperationalPipeline>(message);
                throw new InvalidOperationException(message);
            }

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Audio] RouteAudioPlanReady routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' routeAudioMode='{routeAudioMode}' routeAudioTiming='{routeAudioTiming}' routeAudioCue='{audioCommand.RouteAudioCueName}' stopPreviousRouteAudio='{stopPreviousRouteAudio}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return audioCommand;
        }

        private static SessionOperationalLoadingFact CreateLoadingFact(
            SessionOperationalLoadingCommand command,
            SessionOperationalLoadingStage stage,
            float normalizedProgress,
            string stepLabel,
            string message)
        {
            return new SessionOperationalLoadingFact(command, stage, normalizedProgress, stepLabel, message);
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

        private static void ValidatePersistentScenesPolicyOrFail(OperationalRouteAsset route)
        {
            RuntimeModeConfig runtimeModeConfig = ResolveRuntimeModeConfigOrFail();
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = RuntimePolicyConfigResolver.ResolvePersistentScenesPolicyOrFail(runtimeModeConfig);

            if (persistentScenesPolicy == null)
            {
                return;
            }

            if (!route.TryValidateAgainstPersistentScenesPolicy(persistentScenesPolicy, out string validationError))
            {
                string message = $"[FATAL][Config][SessionOperationalRoute] {validationError}";
                DebugUtility.LogError<SessionOperationalPipeline>(message);
                throw new InvalidOperationException(message);
            }
        }

        private static RuntimeModeConfig ResolveRuntimeModeConfigOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<RuntimeModeConfig>(out var runtimeModeConfig) && runtimeModeConfig != null)
            {
                return runtimeModeConfig;
            }

            string message = "[FATAL][Config][SessionOperationalRoute] RuntimeModeConfig obrigatorio ausente para validar persistent scenes.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private static SessionOperationalRouteLoadPlan ResolveRouteLoadPlanOrFail(
            OperationalRouteAsset route,
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy,
            string activeSceneName)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            IReadOnlyList<SceneKeyAsset> explicitScenesToLoad = route.ScenesToLoad ?? Array.Empty<SceneKeyAsset>();
            HashSet<string> persistentSceneSet = BuildPersistentSceneSetOrEmpty(persistentScenesPolicy);
            string normalizedActiveSceneName = Normalize(activeSceneName);
            bool activeSceneImplicitLoad = !ContainsScene(explicitScenesToLoad, normalizedActiveSceneName);
            List<SceneKeyAsset> finalScenesToLoad = new();
            HashSet<string> dedupe = new(StringComparer.Ordinal);

            AppendActiveSceneKeyToLoadPlan(route.ActiveSceneKey, finalScenesToLoad, dedupe);
            AppendAdditionalSceneKeysToLoadPlan(
                explicitScenesToLoad,
                finalScenesToLoad,
                dedupe,
                persistentSceneSet,
                normalizedActiveSceneName);

            if (finalScenesToLoad.Count == 0)
            {
                HardFailFastH1.Trigger(typeof(SessionOperationalPipeline),
                    $"[FATAL][Config][SessionOperationalPipeline] finalScenesToLoad cannot be empty routeIdentity='{route.RouteIdentity}'.");
            }

            return new SessionOperationalRouteLoadPlan(
                activeSceneImplicitLoad,
                finalScenesToLoad);
        }

        private static SessionOperationalRouteUnloadPlan ResolveRouteUnloadPlanOrFail(
            OperationalRouteAsset route,
            SessionOperationalRouteSnapshot previousCompletedRoute,
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy,
            IReadOnlyList<SceneKeyAsset> currentRouteLoadedSceneKeys,
            string currentActiveSceneName)
        {
            IReadOnlyList<SceneKeyAsset> explicitScenesToUnload = route.ScenesToUnload ?? Array.Empty<SceneKeyAsset>();
            HashSet<string> currentRouteLoadSceneSet = BuildSceneSetOrEmpty(currentRouteLoadedSceneKeys);
            string normalizedCurrentActiveSceneName = Normalize(currentActiveSceneName);

            IReadOnlyList<SceneKeyAsset> autoScenesToUnload = ResolveAutoScenesToUnloadOrFail(
                route,
                previousCompletedRoute,
                persistentScenesPolicy,
                currentRouteLoadSceneSet,
                normalizedCurrentActiveSceneName);

            IReadOnlyList<SceneKeyAsset> finalScenesToUnload = BuildFinalScenesToUnload(
                explicitScenesToUnload,
                autoScenesToUnload,
                currentRouteLoadSceneSet,
                normalizedCurrentActiveSceneName);

            return new SessionOperationalRouteUnloadPlan(
                previousCompletedRoute.RouteIdentity,
                explicitScenesToUnload,
                autoScenesToUnload,
                finalScenesToUnload);
        }

        private static IReadOnlyList<SceneKeyAsset> ResolveAutoScenesToUnloadOrFail(
            OperationalRouteAsset route,
            SessionOperationalRouteSnapshot previousCompletedRoute,
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy,
            HashSet<string> currentRouteLoadSceneSet,
            string currentActiveSceneName)
        {
            if (route == null || !route.UnloadPreviousRouteOwnedScenes || !previousCompletedRoute.IsValid)
            {
                return Array.Empty<SceneKeyAsset>();
            }

            HashSet<string> persistentSceneSet = BuildPersistentSceneSetOrEmpty(persistentScenesPolicy);
            IReadOnlyList<SceneKeyAsset> previousSceneKeys = previousCompletedRoute.RouteOwnedLoadedSceneKeys ?? Array.Empty<SceneKeyAsset>();
            List<SceneKeyAsset> autoScenesToUnload = new(previousSceneKeys.Count);
            HashSet<string> dedupe = new(StringComparer.Ordinal);

            for (int i = 0; i < previousSceneKeys.Count; i++)
            {
                SceneKeyAsset sceneKey = previousSceneKeys[i];
                string sceneName = ResolveSceneName(sceneKey, $"previousCompletedRoute.routeOwnedLoadedSceneKeys[{i}]");

                if (persistentSceneSet.Contains(sceneName))
                {
                    HardFailFastH1.Trigger(typeof(SessionOperationalPipeline),
                        $"[FATAL][Config][SessionOperationalPipeline] previous completed route snapshot contains runtime persistent scene. routeIdentity='{previousCompletedRoute.RouteIdentity}' scene='{sceneName}'.");
                }

                if ((currentRouteLoadSceneSet != null && currentRouteLoadSceneSet.Contains(sceneName)) ||
                    (!string.IsNullOrWhiteSpace(currentActiveSceneName) && string.Equals(sceneName, currentActiveSceneName, StringComparison.Ordinal)))
                {
                    continue;
                }

                if (!dedupe.Add(sceneName))
                {
                    continue;
                }

                autoScenesToUnload.Add(sceneKey);
            }

            return autoScenesToUnload.Count == 0 ? Array.Empty<SceneKeyAsset>() : autoScenesToUnload;
        }

        private static IReadOnlyList<SceneKeyAsset> BuildFinalScenesToUnload(
            IReadOnlyList<SceneKeyAsset> explicitScenesToUnload,
            IReadOnlyList<SceneKeyAsset> autoScenesToUnload,
            HashSet<string> currentRouteLoadSceneSet,
            string currentActiveSceneName)
        {
            List<SceneKeyAsset> finalScenesToUnload = new();
            HashSet<string> dedupe = new(StringComparer.Ordinal);

            AppendSceneKeysToFinalUnload(finalScenesToUnload, dedupe, explicitScenesToUnload, currentRouteLoadSceneSet, currentActiveSceneName);
            AppendSceneKeysToFinalUnload(finalScenesToUnload, dedupe, autoScenesToUnload, currentRouteLoadSceneSet, currentActiveSceneName);

            return finalScenesToUnload.Count == 0 ? Array.Empty<SceneKeyAsset>() : finalScenesToUnload;
        }

        private static void AppendSceneKeysToFinalUnload(
            List<SceneKeyAsset> finalScenesToUnload,
            HashSet<string> dedupe,
            IReadOnlyList<SceneKeyAsset> sceneKeys,
            HashSet<string> currentRouteLoadSceneSet,
            string currentActiveSceneName)
        {
            if (sceneKeys == null || sceneKeys.Count == 0)
            {
                return;
            }

            for (int i = 0; i < sceneKeys.Count; i++)
            {
                SceneKeyAsset sceneKey = sceneKeys[i];
                string sceneName = ResolveSceneName(sceneKey, $"scenesToUnload[{i}]");

                if ((currentRouteLoadSceneSet != null && currentRouteLoadSceneSet.Contains(sceneName)) ||
                    (!string.IsNullOrWhiteSpace(currentActiveSceneName) && string.Equals(sceneName, currentActiveSceneName, StringComparison.Ordinal)))
                {
                    continue;
                }

                if (!dedupe.Add(sceneName))
                {
                    continue;
                }

                finalScenesToUnload.Add(sceneKey);
            }
        }

        private static HashSet<string> BuildSceneSetOrEmpty(IReadOnlyList<SceneKeyAsset> sceneKeys)
        {
            HashSet<string> sceneSet = new(StringComparer.Ordinal);

            if (sceneKeys == null)
            {
                return sceneSet;
            }

            for (int i = 0; i < sceneKeys.Count; i++)
            {
                string sceneName = ResolveSceneName(sceneKeys[i], $"sceneKeys[{i}]");
                sceneSet.Add(sceneName);
            }

            return sceneSet;
        }

        private static bool ContainsScene(IReadOnlyList<SceneKeyAsset> scenes, string sceneName)
        {
            if (scenes == null || string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            string normalizedSceneName = Normalize(sceneName);
            for (int i = 0; i < scenes.Count; i++)
            {
                string currentSceneName = ResolveSceneName(scenes[i], $"scenes[{i}]");
                if (string.Equals(currentSceneName, normalizedSceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AppendActiveSceneKeyToLoadPlan(
            SceneKeyAsset activeSceneKey,
            List<SceneKeyAsset> finalScenesToLoad,
            HashSet<string> dedupe)
        {
            string activeSceneName = ResolveSceneName(activeSceneKey, nameof(activeSceneKey));
            if (!dedupe.Add(activeSceneName))
            {
                return;
            }

            finalScenesToLoad.Add(activeSceneKey);
        }

        private static void AppendAdditionalSceneKeysToLoadPlan(
            IReadOnlyList<SceneKeyAsset> scenesToLoad,
            List<SceneKeyAsset> finalScenesToLoad,
            HashSet<string> dedupe,
            HashSet<string> persistentSceneSet,
            string activeSceneName)
        {
            if (scenesToLoad == null || scenesToLoad.Count == 0)
            {
                return;
            }

            for (int i = 0; i < scenesToLoad.Count; i++)
            {
                SceneKeyAsset sceneKey = scenesToLoad[i];
                string sceneName = ResolveSceneName(sceneKey, $"scenesToLoad[{i}]");

                if (persistentSceneSet.Contains(sceneName))
                {
                    HardFailFastH1.Trigger(typeof(SessionOperationalPipeline),
                        $"[FATAL][Config][SessionOperationalPipeline] scenesToLoad cannot contain runtime persistent scene='{sceneName}'.");
                }

                if (!string.IsNullOrWhiteSpace(activeSceneName) && string.Equals(sceneName, activeSceneName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!dedupe.Add(sceneName))
                {
                    continue;
                }

                finalScenesToLoad.Add(sceneKey);
            }
        }

        private static HashSet<string> BuildPersistentSceneSetOrEmpty(RuntimePersistentScenesPolicyAsset persistentScenesPolicy)
        {
            if (persistentScenesPolicy == null)
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            IReadOnlyList<string> persistentSceneNames = persistentScenesPolicy.ResolveSceneNamesOrFail(nameof(SessionOperationalPipeline));
            return new HashSet<string>(persistentSceneNames, StringComparer.Ordinal);
        }

        private static IReadOnlyList<string> ResolveSceneNames(IReadOnlyList<SceneKeyAsset> sceneKeys, string fieldName)
        {
            if (sceneKeys == null)
            {
                HardFailFastH1.Trigger(typeof(SessionOperationalPipeline),
                    $"[FATAL][Config][SessionOperationalPipeline] {fieldName} is required.");
            }

            if (sceneKeys.Count == 0)
            {
                return Array.Empty<string>();
            }

            List<string> sceneNames = new(sceneKeys.Count);
            HashSet<string> dedupe = new(StringComparer.Ordinal);

            for (int i = 0; i < sceneKeys.Count; i++)
            {
                string sceneName = ResolveSceneName(sceneKeys[i], $"{fieldName}[{i}]");
                if (!dedupe.Add(sceneName))
                {
                    continue;
                }

                sceneNames.Add(sceneName);
            }

            return sceneNames.Count == 0 ? Array.Empty<string>() : sceneNames;
        }

        private void RecordLastCompletedRouteSnapshot(
            OperationalRouteAsset route,
            string routeOperationId,
            int routeSequence,
            IReadOnlyList<SceneKeyAsset> finalScenesToLoad)
        {
            if (route == null)
            {
                return;
            }

            SessionOperationalRouteSnapshot snapshot = new(
                route.RouteIdentity,
                routeOperationId,
                routeSequence,
                route.ActiveSceneKey,
                route.ActivitySavePolicy.SaveActivityOnExit,
                route.HandoffSessionStateId,
                finalScenesToLoad);

            if (!snapshot.IsValid)
            {
                HardFailFastH1.Trigger(typeof(SessionOperationalPipeline),
                    $"[FATAL][Config][SessionOperationalPipeline] failed to record last completed route snapshot routeIdentity='{route.RouteIdentity}' routeSequence='{routeSequence}'.");
            }

            _lastCompletedRouteSnapshot = snapshot;
        }

        private readonly struct SessionOperationalRouteLoadPlan
        {
            public SessionOperationalRouteLoadPlan(
                bool activeSceneImplicitLoad,
                IReadOnlyList<SceneKeyAsset> finalScenesToLoad)
            {
                ActiveSceneImplicitLoad = activeSceneImplicitLoad;
                FinalScenesToLoad = finalScenesToLoad ?? throw new ArgumentNullException(nameof(finalScenesToLoad));
            }

            public bool ActiveSceneImplicitLoad { get; }
            public IReadOnlyList<SceneKeyAsset> FinalScenesToLoad { get; }
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

        private readonly struct SessionOperationalRouteUnloadPlan
        {
            public SessionOperationalRouteUnloadPlan(
                string previousRouteIdentity,
                IReadOnlyList<SceneKeyAsset> explicitScenesToUnload,
                IReadOnlyList<SceneKeyAsset> autoScenesToUnload,
                IReadOnlyList<SceneKeyAsset> finalScenesToUnload)
            {
                PreviousRouteIdentity = Normalize(previousRouteIdentity);
                ExplicitScenesToUnload = explicitScenesToUnload ?? throw new ArgumentNullException(nameof(explicitScenesToUnload));
                AutoScenesToUnload = autoScenesToUnload ?? throw new ArgumentNullException(nameof(autoScenesToUnload));
                FinalScenesToUnload = finalScenesToUnload ?? throw new ArgumentNullException(nameof(finalScenesToUnload));
            }

            public string PreviousRouteIdentity { get; }
            public IReadOnlyList<SceneKeyAsset> ExplicitScenesToUnload { get; }
            public IReadOnlyList<SceneKeyAsset> AutoScenesToUnload { get; }
            public IReadOnlyList<SceneKeyAsset> FinalScenesToUnload { get; }
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
