using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionActivityPipeline;

namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline
{
    public sealed class SessionOperationalPipeline
    {
        private const string DefaultPipelineId = "SessionOperationalPipeline.v0";

        private readonly SessionOperationalRuntimeState _state = new();
        private readonly string _sessionOperationalPipelineId;
        private readonly object _sandboxRouteSync = new();
        private int _sandboxRouteSequence;
        private SessionOperationalRouteSnapshot _lastCompletedRouteSnapshot;
        private bool _hasActiveSandboxRouteOperation;
        private string _activeSandboxRouteOperationId = string.Empty;
        private string _activeSandboxTransitionId = string.Empty;
        private string _activeSandboxRouteIdentity = string.Empty;

        public SessionOperationalPipeline(string sessionOperationalPipelineId = DefaultPipelineId)
        {
            _sessionOperationalPipelineId = Normalize(sessionOperationalPipelineId);

            if (string.IsNullOrWhiteSpace(_sessionOperationalPipelineId))
            {
                throw new ArgumentException("sessionOperationalPipelineId is required.", nameof(sessionOperationalPipelineId));
            }
        }

        public SessionOperationalRuntimeState State => _state;

        public async Task<SessionOperationalRouteCompletedFact> RequestOperationalRouteAsync(
            SessionOperationalRouteAsset route,
            string source,
            string reason)
        {
            if (route == null)
            {
                throw new InvalidOperationException("SessionOperationalRouteAsset is required.");
            }

            if (!route.TryValidate(out string routeValidationError))
            {
                string message = $"[FATAL][Config][SessionOperationalRoute] {routeValidationError}";
                DebugUtility.LogError<SessionOperationalPipeline>(message);
                throw new InvalidOperationException(message);
            }

            ValidatePersistentScenesPolicyOrFail(route);
            ISessionOperationalRouteTransitionExecutor routeExecutor = ResolveRouteExecutorOrFail();
            RuntimeModeConfig runtimeModeConfig = ResolveRuntimeModeConfigOrFail();
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = runtimeModeConfig.RuntimePersistentScenesPolicy;

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

            lock (_sandboxRouteSync)
            {
                if (_hasActiveSandboxRouteOperation)
                {
                    DebugUtility.LogWarning<SessionOperationalPipeline>(
                        $"[OBS][SessionOperationalPipeline][Route] rejected reason='stale_or_foreign_route' routeIdentity='{routeIdentity}' activeRouteIdentity='{_activeSandboxRouteIdentity}' activeRouteOperationId='{_activeSandboxRouteOperationId}' activeTransitionId='{_activeSandboxTransitionId}' source='{sourceText}' reason='{reasonText}'.");
                    throw new InvalidOperationException("Sandbox route operation is already in flight.");
                }

                _sandboxRouteSequence += 1;
                routeSequence = _sandboxRouteSequence;
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

                _hasActiveSandboxRouteOperation = true;
                _activeSandboxRouteOperationId = routeOperationId;
                _activeSandboxTransitionId = transitionId;
                _activeSandboxRouteIdentity = routeIdentity;
            }

            SessionOperationalRouteCommand command = route.CreateCommand(
                routeOperationId,
                transitionId,
                routeSequence,
                sourceText,
                reasonText,
                loadPlan.FinalScenesToLoad,
                unloadPlan.AutoScenesToUnload,
                unloadPlan.FinalScenesToUnload);

            _state.Reset(
                _sessionOperationalPipelineId,
                routeOperationId,
                transitionId,
                routeSequence,
                routeIdentity,
                routeIdentity);

            DebugUtility.Log(typeof(SessionOperationalPipeline),
                $"[OBS][SessionOperationalPipeline][Route] command='OperationalRouteCommand' routeIdentity='{routeIdentity}' activeScene='{activeSceneName}' activeSceneKey='{route.ActiveSceneKey.name}' activeSceneImplicitLoad='{loadPlan.ActiveSceneImplicitLoad}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' completionHandoff='{route.CompletionHandoff}' finalScenesToLoad=[{FormatSceneNames(loadPlan.FinalScenesToLoad)}] autoScenesToUnload=[{FormatSceneNames(unloadPlan.AutoScenesToUnload)}] explicitScenesToUnload=[{FormatSceneNames(unloadPlan.ExplicitScenesToUnload)}] finalScenesToUnload=[{FormatSceneNames(unloadPlan.FinalScenesToUnload)}] source='{sourceText}' reason='{reasonText}'.",
                DebugUtility.Colors.Info);

            try
            {
                SessionOperationalRouteCompletedFact adapterFact = await routeExecutor.ApplyOperationalRouteAsync(command);
                if (!adapterFact.IsValid)
                {
                    throw new InvalidOperationException("Sandbox route executor returned an invalid completion fact.");
                }

                DebugUtility.Log(typeof(SessionOperationalPipeline),
                    $"[OBS][SessionOperationalPipeline][Route] fact='OperationalRouteCompleted' routeIdentity='{adapterFact.RouteIdentity}' routeOperationId='{adapterFact.RouteOperationId}' transitionId='{adapterFact.TransitionId}' routeSequence='{adapterFact.RouteSequence}' correlationId='{adapterFact.CorrelationId}' message='{adapterFact.Message}' source='{sourceText}' reason='{reasonText}'.",
                    DebugUtility.Colors.Success);

                CompleteSandboxRouteOperation(routeOperationId, transitionId, routeSequence, routeIdentity, sourceText, reasonText);
                RecordLastCompletedRouteSnapshot(route, routeSequence, loadPlan.FinalScenesToLoad);

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

                    SessionActivityEntryHandoff handoff = new(
                        string.Empty,
                        0,
                        0,
                        route.HandoffSessionStateId,
                        sourceText,
                        reasonText);

                    DebugUtility.Log(typeof(SessionOperationalPipeline),
                        $"[OBS][SessionOperationalPipeline][Route] handoff='SessionActivityEntryHandoffEmitted' routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' handoff='{handoff}' source='{sourceText}' reason='{reasonText}'.",
                        DebugUtility.Colors.Info);

                    SessionActivityCommandResult activityResult = activityReceiver.StartFromPreparedHandoff(handoff, sourceText, reasonText);
                    if (!activityResult.IsValid || activityResult.IsRejected)
                    {
                        throw new InvalidOperationException($"SessionActivityPipeline rejected the prepared handoff. result='{activityResult.Kind}' reason='{activityResult.Reason}'.");
                    }
                }

                return adapterFact;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<SessionOperationalPipeline>(
                    $"[OBS][SessionOperationalPipeline][Route] route_transition_failed routeIdentity='{routeIdentity}' routeOperationId='{routeOperationId}' transitionId='{transitionId}' routeSequence='{routeSequence}' source='{sourceText}' reason='{reasonText}' exceptionType='{ex.GetType().Name}' exceptionMessage='{ex.Message}'.");
                throw;
            }
            finally
            {
                lock (_sandboxRouteSync)
                {
                    _hasActiveSandboxRouteOperation = false;
                    _activeSandboxRouteOperationId = string.Empty;
                    _activeSandboxTransitionId = string.Empty;
                    _activeSandboxRouteIdentity = string.Empty;
                }
            }
        }

        public Task<Base11SandboxOperationalRouteCompletedFact> RequestBase11SandboxOperationalRouteAsync(Base11SandboxOperationalRoute route)
        {
            throw new NotSupportedException("Legacy Base11SandboxOperationalRoute path is disabled. Use SessionOperationalRouteAsset instead.");
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

        public bool TryObserveInputCapabilityPrepared(
            string routeOperationId,
            string transitionId,
            int transitionSequence,
            string routeId,
            string routeProfileId,
            string routeClass,
            SessionOperationalInputModeKind initialInputMode,
            string source,
            string reason)
        {
            _state.SetInputModeContext(Normalize(routeClass), initialInputMode);
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
            SessionOperationalInputModeKind initialInputMode,
            string source,
            string reason)
        {
            _state.SetInputModeContext(Normalize(routeClass), initialInputMode);
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
            return $"[OBS][SessionOperationalPipeline] pipelineId='{_state.SessionOperationalPipelineId}' routeOperationId='{_state.RouteOperationId}' transitionId='{_state.TransitionId}' transitionSequence='{_state.TransitionSequence}' routeId='{_state.RouteId}' routeProfileId='{_state.RouteProfileId}' routeClass='{_state.RouteClass}' initialInputMode='{_state.CurrentInitialInputMode}' stage='{_state.CurrentStage}' started='{_state.HasStarted}' completed='{_state.HasCompleted}' factsCount='{_state.Facts.Count}'";
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
                    $"[OBS][SessionOperationalPipeline][InputMode] fact='{fact.Kind}' stage='{fact.Identity.Stage}' routeId='{fact.Identity.RouteId}' routeProfileId='{fact.Identity.RouteProfileId}' routeClass='{Normalize(_state.RouteClass)}' routeKind='{Normalize(_state.RouteClass)}' initialInputMode='{_state.CurrentInitialInputMode}' source='{fact.Source}' reason='{fact.Reason}'");
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
                    $"[OBS][SessionOperationalPipeline][InputMode] command='SessionOperationalInputModeCommand' contextSignature='{inputModeCommand.ContextSignature}' initialInputMode='{inputModeCommand.InitialInputMode}' routeClass='{inputModeCommand.RouteClass}' source='{inputModeCommand.Source}' reason='{inputModeCommand.Reason}'.");

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
            if (!_state.HasStarted)
            {
                return stage == SessionOperationalStage.RouteOperationStarted;
            }

            if (_state.HasCompleted)
            {
                return false;
            }

            if (!string.Equals(_state.SessionOperationalPipelineId, _sessionOperationalPipelineId, StringComparison.Ordinal) ||
                !string.Equals(_state.RouteOperationId, routeOperationId, StringComparison.Ordinal) ||
                !string.Equals(_state.TransitionId, transitionId, StringComparison.Ordinal) ||
                _state.TransitionSequence != transitionSequence ||
                !string.Equals(_state.RouteId, routeId, StringComparison.Ordinal) ||
                !string.Equals(_state.RouteProfileId, routeProfileId, StringComparison.Ordinal))
            {
                return false;
            }

            SessionOperationalStage expectedStage = (SessionOperationalStage)((int)_state.CurrentStage + 1);
            return stage == expectedStage;
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

        private static ISessionOperationalRouteTransitionExecutor ResolveRouteExecutorOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISessionOperationalRouteTransitionExecutor>(out var executor) && executor != null)
            {
                return executor;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline] ISessionOperationalRouteTransitionExecutor obrigatorio ausente para o trilho do Base11Sandbox.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private static ISessionActivityEntryHandoffReceiver ResolveActivityReceiverOrFail()
        {
            if (DependencyManager.Provider.TryGetGlobal<ISessionActivityEntryHandoffReceiver>(out var receiver) && receiver != null)
            {
                return receiver;
            }

            string message = "[FATAL][Config][SessionOperationalPipeline] ISessionActivityEntryHandoffReceiver obrigatorio ausente para o trilho do Base11Sandbox.";
            DebugUtility.LogError<SessionOperationalPipeline>(message);
            throw new InvalidOperationException(message);
        }

        private static void ValidatePersistentScenesPolicyOrFail(SessionOperationalRouteAsset route)
        {
            RuntimeModeConfig runtimeModeConfig = ResolveRuntimeModeConfigOrFail();
            RuntimePersistentScenesPolicyAsset persistentScenesPolicy = runtimeModeConfig.RuntimePersistentScenesPolicy;

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
            SessionOperationalRouteAsset route,
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
            SessionOperationalRouteAsset route,
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
            SessionOperationalRouteAsset route,
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

        private void RecordLastCompletedRouteSnapshot(SessionOperationalRouteAsset route, int routeSequence, IReadOnlyList<SceneKeyAsset> finalScenesToLoad)
        {
            if (route == null)
            {
                return;
            }

            SessionOperationalRouteSnapshot snapshot = new(
                route.RouteIdentity,
                routeSequence,
                route.ActiveSceneKey,
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
                int routeSequence,
                SceneKeyAsset activeSceneKey,
                IReadOnlyList<SceneKeyAsset> routeOwnedLoadedSceneKeys)
            {
                RouteIdentity = Normalize(routeIdentity);
                RouteSequence = routeSequence < 0 ? 0 : routeSequence;
                ActiveSceneKey = activeSceneKey;
                RouteOwnedLoadedSceneKeys = routeOwnedLoadedSceneKeys ?? throw new ArgumentNullException(nameof(routeOwnedLoadedSceneKeys));
            }

            public string RouteIdentity { get; }
            public int RouteSequence { get; }
            public SceneKeyAsset ActiveSceneKey { get; }
            public IReadOnlyList<SceneKeyAsset> RouteOwnedLoadedSceneKeys { get; }

            public bool IsValid =>
                !string.IsNullOrWhiteSpace(RouteIdentity) &&
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

        private SessionOperationalResult CompleteSandboxRouteOperation(
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
                "Sandbox route completed.");

            _state.SetCurrentIdentity(identity);
            _state.MarkStarted();
            _state.MarkCompleted();
            _state.AppendFact(fact);
            _state.AppendTrace(
                $"[OBS][SessionOperationalPipeline] fact='OperationalRouteCompleted' stage='{identity.Stage}' routeOperationId='{identity.RouteOperationId}' transitionId='{identity.TransitionId}' routeSequence='{identity.TransitionSequence}' routeId='{identity.RouteId}' routeProfileId='{identity.RouteProfileId}' source='{source}' reason='{reason}' message='Sandbox route completed.'");

            return new SessionOperationalResult(
                SessionOperationalResultKind.Completed,
                identity,
                _state.Facts,
                "Sandbox route completed.");
        }
    }
}



