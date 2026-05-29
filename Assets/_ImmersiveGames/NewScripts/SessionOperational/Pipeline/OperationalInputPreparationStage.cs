using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalInputPreparationResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct OperationalInputPreparationResult
    {
        public OperationalInputPreparationResult(
            OperationalInputPreparationResultKind kind,
            SessionOperationalInputModeKind initialInputMode)
        {
            Kind = kind;
            InitialInputMode = initialInputMode;
        }

        public OperationalInputPreparationResultKind Kind { get; }
        public SessionOperationalInputModeKind InitialInputMode { get; }
        public bool IsCompleted => Kind == OperationalInputPreparationResultKind.Completed;
    }

    public readonly struct OperationalInputPreparationCommand
    {
        public OperationalInputPreparationCommand(
            string pipelineId,
            RuntimeModeConfig runtimeModeConfig,
            SessionOperationalRoutePlan routePlan,
            SessionOperationalInputPolicy inputPolicy,
            string routeClass,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            PipelineId = Normalize(pipelineId);
            RuntimeModeConfig = runtimeModeConfig;
            RoutePlan = routePlan;
            InputPolicy = inputPolicy;
            RouteClass = Normalize(routeClass);
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string PipelineId { get; }
        public RuntimeModeConfig RuntimeModeConfig { get; }
        public SessionOperationalRoutePlan RoutePlan { get; }
        public SessionOperationalInputPolicy InputPolicy { get; }
        public string RouteClass { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            RuntimeModeConfig != null &&
            RoutePlan.IsValid &&
            InputPolicy != SessionOperationalInputPolicy.Unknown &&
            !string.IsNullOrWhiteSpace(RouteClass) &&
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalInputPreparationStage
    {
        private readonly SessionOperationalRuntimeState _runtimeState;
        private readonly SessionOperationalStageOrderPolicy _stageOrderPolicy;
        private readonly Func<IOperationalInputModeRequestPort> _inputModeRequestPortResolver;

        public OperationalInputPreparationStage(
            SessionOperationalRuntimeState runtimeState,
            SessionOperationalStageOrderPolicy stageOrderPolicy,
            Func<IOperationalInputModeRequestPort> inputModeRequestPortResolver)
        {
            _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            _stageOrderPolicy = stageOrderPolicy ?? throw new ArgumentNullException(nameof(stageOrderPolicy));
            _inputModeRequestPortResolver = inputModeRequestPortResolver ?? throw new ArgumentNullException(nameof(inputModeRequestPortResolver));
        }

        public OperationalInputPreparationResult Execute(OperationalInputPreparationCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalInputPreparationCommand is invalid.");
            }

            SessionOperationalInputModeKind initialInputMode = ResolveInitialInputModeFromPolicyOrFail(command);

            UnityOperationalInputRuntimeAdapter.PrepareOrFail(
                command.RuntimeModeConfig,
                command.RouteIdentity,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.Source,
                command.Reason);

            LogInputCapabilityPrepared(command, initialInputMode);

            if (!RecordInputStageOrReject(
                    command,
                    SessionOperationalStage.InputCapabilityPrepared,
                    initialInputMode,
                    "Input capability prepared."))
            {
                throw new InvalidOperationException(BuildMissingInputCapabilityPreparedMessage(command, initialInputMode));
            }

            if (!RecordInputStageOrReject(
                    command,
                    SessionOperationalStage.InitialInputModePrepared,
                    initialInputMode,
                    "Initial input mode prepared."))
            {
                throw new InvalidOperationException(BuildMissingInitialInputModePreparedMessage(command, initialInputMode));
            }

            SubmitInitialInputModeOrFail(command, initialInputMode);

            return new OperationalInputPreparationResult(
                OperationalInputPreparationResultKind.Completed,
                initialInputMode);
        }

        private static SessionOperationalInputModeKind ResolveInitialInputModeFromPolicyOrFail(OperationalInputPreparationCommand command)
        {
            return command.InputPolicy switch
            {
                SessionOperationalInputPolicy.MenuNavigation => SessionOperationalInputModeKind.FrontendMenu,
                SessionOperationalInputPolicy.ActivityGameplay => SessionOperationalInputModeKind.ActivityDefault,
                SessionOperationalInputPolicy.OverlayNavigation => SessionOperationalInputModeKind.PauseOverlay,
                SessionOperationalInputPolicy.InputLocked => SessionOperationalInputModeKind.InputLocked,
                _ => throw new InvalidOperationException(
                    $"[FATAL][Config][SessionOperationalInputCapability] inputPolicy invalida routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' inputPolicy='{command.InputPolicy}' source='{command.Source}' reason='{command.Reason}'."),
            };
        }

        private bool RecordInputStageOrReject(
            OperationalInputPreparationCommand command,
            SessionOperationalStage stage,
            SessionOperationalInputModeKind initialInputMode,
            string message)
        {
            _runtimeState.SetInputModeContext(command.RouteClass, command.InputPolicy, initialInputMode);

            SessionOperationalTransitionKey incomingTransitionKey = BuildTransitionKey(
                command.PipelineId,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.RouteIdentity,
                command.RouteIdentity);
            SessionOperationalStageKey incomingStageKey = new(incomingTransitionKey, stage);

            if (!CanAcceptStage(command, incomingStageKey))
            {
                return Reject(
                    command,
                    SessionOperationalFactKind.IgnoredForeignOrStale,
                    stage,
                    $"Stage '{stage}' ignored because it is foreign, stale, or out of order.");
            }

            SessionOperationalIdentity identity = new(
                command.PipelineId,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.RouteIdentity,
                command.RouteIdentity,
                command.Source,
                command.Reason,
                stage);

            SessionOperationalFact fact = new(
                MapFactKind(stage),
                identity,
                command.Source,
                command.Reason,
                message);

            if (!fact.IsValid)
            {
                throw new InvalidOperationException($"Cannot emit invalid operational fact for stage '{stage}'.");
            }

            _runtimeState.SetCurrentIdentity(identity);
            _runtimeState.MarkStarted();
            _runtimeState.AppendFact(fact);
            _runtimeState.AppendTrace(
                $"[OBS][SessionOperationalPipeline] fact='{fact.Kind}' stage='{fact.Identity.Stage}' routeOperationId='{fact.Identity.RouteOperationId}' transitionId='{fact.Identity.TransitionId}' transitionSequence='{fact.Identity.TransitionSequence}' routeId='{fact.Identity.RouteId}' routeProfileId='{fact.Identity.RouteProfileId}' source='{fact.Source}' reason='{fact.Reason}' message='{fact.Message}'");
            _runtimeState.AppendTrace(
                $"[OBS][SessionOperationalPipeline][InputMode] fact='{fact.Kind}' stage='{fact.Identity.Stage}' routeId='{fact.Identity.RouteId}' routeProfileId='{fact.Identity.RouteProfileId}' operationalSurfaceKind='{command.RouteClass}' inputPolicy='{command.InputPolicy}' inputMode='{initialInputMode}' source='{fact.Source}' reason='{fact.Reason}'");

            return true;
        }

        private void SubmitInitialInputModeOrFail(
            OperationalInputPreparationCommand command,
            SessionOperationalInputModeKind initialInputMode)
        {
            if (_runtimeState.CurrentStage != SessionOperationalStage.InitialInputModePrepared ||
                _runtimeState.RouteOperationId != command.RouteOperationId ||
                _runtimeState.TransitionId != command.TransitionId ||
                _runtimeState.TransitionSequence != command.RouteSequence ||
                _runtimeState.RouteId != command.RouteIdentity ||
                _runtimeState.RouteProfileId != command.RouteIdentity)
            {
                throw new InvalidOperationException(
                    $"[FATAL][H1][SessionOperationalPipeline][InputMode] Cannot submit initial input mode before matching InitialInputModePrepared fact routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' currentStage='{_runtimeState.CurrentStage}' currentRouteOperationId='{_runtimeState.RouteOperationId}' currentTransitionId='{_runtimeState.TransitionId}' currentRouteSequence='{_runtimeState.TransitionSequence}' source='{command.Source}' reason='{command.Reason}'.");
            }

            _runtimeState.SetInputModeContext(command.RouteClass, command.InputPolicy, initialInputMode);

            SessionOperationalIdentity identity = new(
                command.PipelineId,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.RouteIdentity,
                command.RouteIdentity,
                command.Source,
                command.Reason,
                SessionOperationalStage.InitialInputModePrepared);

            OperationalInputModeRequest request = new(
                identity,
                _runtimeState.CurrentInitialInputMode,
                _runtimeState.CurrentInputPolicy,
                _runtimeState.RouteClass);

            if (!request.IsValid)
            {
                throw new InvalidOperationException("Cannot submit invalid operational input mode request.");
            }

            _runtimeState.AppendTrace(
                $"[OBS][SessionOperationalPipeline][InputMode] command='OperationalInputModeRequest' routeIdentity='{identity.RouteId}' routeOperationId='{identity.RouteOperationId}' transitionId='{identity.TransitionId}' routeSequence='{identity.TransitionSequence}' contextSignature='{request.ContextSignature}' operationalSurfaceKind='{_runtimeState.RouteClass}' inputPolicy='{_runtimeState.CurrentInputPolicy}' initialInputMode='{request.InitialInputMode}' source='{request.Source}' reason='{request.Reason}'.");

            IOperationalInputModeRequestPort inputModeRequestPort = ResolveInputModeRequestPortOrFail(command);
            OperationalInputModeRequestResult result = inputModeRequestPort.SubmitInitialInputMode(request);
            if (!result.IsSubmitted)
            {
                throw new InvalidOperationException(
                    $"[FATAL][H1][SessionOperationalPipeline][InputMode] Input mode request rejected routeIdentity='{identity.RouteId}' routeOperationId='{identity.RouteOperationId}' transitionId='{identity.TransitionId}' routeSequence='{identity.TransitionSequence}' resultKind='{result.Kind}' resultReason='{result.Reason}' detail='{result.Detail}' source='{request.Source}' reason='{request.Reason}'.");
            }
        }

        private bool CanAcceptStage(
            OperationalInputPreparationCommand command,
            SessionOperationalStageKey stageKey)
        {
            if (!stageKey.IsValid)
            {
                return false;
            }

            SessionOperationalTransitionKey activeTransitionKey = BuildTransitionKey(
                command.PipelineId,
                _runtimeState.RouteOperationId,
                _runtimeState.TransitionId,
                _runtimeState.TransitionSequence,
                _runtimeState.RouteId,
                _runtimeState.RouteProfileId);
            if (stageKey.TransitionKey != activeTransitionKey)
            {
                return false;
            }

            if (!_runtimeState.HasStarted)
            {
                return _stageOrderPolicy.CanStart(stageKey.Stage);
            }

            if (_runtimeState.HasCompleted)
            {
                return false;
            }

            return _stageOrderPolicy.CanAdvance(_runtimeState.CurrentStage, stageKey.Stage);
        }

        private IOperationalInputModeRequestPort ResolveInputModeRequestPortOrFail(OperationalInputPreparationCommand command)
        {
            IOperationalInputModeRequestPort inputModeRequestPort = _inputModeRequestPortResolver();
            if (inputModeRequestPort == null)
            {
                throw new InvalidOperationException($"[FATAL][H1][SessionOperationalPipeline][InputMode] IOperationalInputModeRequestPort is required routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.");
            }

            return inputModeRequestPort;
        }

        private static SessionOperationalTransitionKey BuildTransitionKey(
            string pipelineId,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string routeId,
            string routeProfileId)
        {
            SessionOperationalRouteKey routeKey = new(
                pipelineId,
                routeId,
                routeOperationId,
                routeId,
                routeProfileId,
                routeSequence);
            return new SessionOperationalTransitionKey(routeKey, transitionId);
        }

        private bool Reject(
            OperationalInputPreparationCommand command,
            SessionOperationalFactKind factKind,
            SessionOperationalStage stage,
            string message)
        {
            SessionOperationalIdentity identity = new(
                command.PipelineId,
                _runtimeState.RouteOperationId,
                _runtimeState.TransitionId,
                _runtimeState.TransitionSequence,
                _runtimeState.RouteId,
                _runtimeState.RouteProfileId,
                command.Source,
                command.Reason,
                stage);

            if (!identity.IsValid)
            {
                _runtimeState.AppendTrace(
                    $"[OBS][SessionOperationalPipeline] rejected_stage='{stage}' source='{command.Source}' reason='{command.Reason}' message='{message}'");
                return false;
            }

            SessionOperationalFact fact = new(factKind, identity, command.Source, command.Reason, message);
            _runtimeState.AppendFact(fact);
            _runtimeState.AppendTrace(
                $"[OBS][SessionOperationalPipeline] fact='{fact.Kind}' stage='{fact.Identity.Stage}' source='{fact.Source}' reason='{fact.Reason}' message='{fact.Message}'");
            return false;
        }

        private static SessionOperationalFactKind MapFactKind(SessionOperationalStage stage)
        {
            return stage switch
            {
                SessionOperationalStage.InputCapabilityPrepared => SessionOperationalFactKind.InputCapabilityPrepared,
                SessionOperationalStage.InitialInputModePrepared => SessionOperationalFactKind.InitialInputModePrepared,
                _ => SessionOperationalFactKind.Unknown,
            };
        }

        private static void LogInputCapabilityPrepared(
            OperationalInputPreparationCommand command,
            SessionOperationalInputModeKind initialInputMode)
        {
            DebugUtility.Log(typeof(OperationalInputPreparationStage),
                $"[OBS][SessionOperationalPipeline][InputCapability] InputCapabilityPrepared routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{command.RoutePlan.OperationalSurfaceKind}' inputPolicy='{command.InputPolicy}' inputMode='{initialInputMode}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static string BuildMissingInputCapabilityPreparedMessage(
            OperationalInputPreparationCommand command,
            SessionOperationalInputModeKind initialInputMode)
        {
            return $"[FATAL][H1][SessionOperationalPipeline][InputMode] Failed to record InputCapabilityPrepared routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{command.RoutePlan.OperationalSurfaceKind}' inputPolicy='{command.InputPolicy}' resolvedInputMode='{initialInputMode}' source='{command.Source}' reason='{command.Reason}'.";
        }

        private static string BuildMissingInitialInputModePreparedMessage(
            OperationalInputPreparationCommand command,
            SessionOperationalInputModeKind initialInputMode)
        {
            return $"[FATAL][H1][SessionOperationalPipeline][InputMode] Failed to record InitialInputModePrepared routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' operationalSurfaceKind='{command.RoutePlan.OperationalSurfaceKind}' inputPolicy='{command.InputPolicy}' resolvedInputMode='{initialInputMode}' source='{command.Source}' reason='{command.Reason}'.";
        }
    }
}
