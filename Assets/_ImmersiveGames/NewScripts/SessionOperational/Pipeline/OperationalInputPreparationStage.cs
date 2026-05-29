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
        private readonly OperationalFactRecorder _factRecorder;
        private readonly Func<IOperationalInputModeRequestPort> _inputModeRequestPortResolver;

        public OperationalInputPreparationStage(
            OperationalFactRecorder factRecorder,
            Func<IOperationalInputModeRequestPort> inputModeRequestPortResolver)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
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
            return _factRecorder.TryRecordInputStage(
                stage,
                command.RouteOperationId,
                command.TransitionId,
                command.RouteSequence,
                command.RouteIdentity,
                command.RouteIdentity,
                command.RouteClass,
                command.InputPolicy,
                initialInputMode,
                command.Source,
                command.Reason,
                message);
        }

        private void SubmitInitialInputModeOrFail(
            OperationalInputPreparationCommand command,
            SessionOperationalInputModeKind initialInputMode)
        {
            EnsureInitialInputModePreparedFactOrFail(command);
            OperationalInputModeRequest request = BuildInitialInputModeRequest(command, initialInputMode);

            IOperationalInputModeRequestPort inputModeRequestPort = ResolveInputModeRequestPortOrFail(command);
            OperationalInputModeRequestResult result = inputModeRequestPort.SubmitInitialInputMode(request);
            if (!result.IsSubmitted)
            {
                throw new InvalidOperationException(
                    $"[FATAL][H1][SessionOperationalPipeline][InputMode] Input mode request rejected routeIdentity='{request.Identity.RouteId}' routeOperationId='{request.Identity.RouteOperationId}' transitionId='{request.Identity.TransitionId}' routeSequence='{request.Identity.TransitionSequence}' resultKind='{result.Kind}' resultReason='{result.Reason}' detail='{result.Detail}' source='{request.Source}' reason='{request.Reason}'.");
            }

            DebugUtility.Log(typeof(OperationalInputPreparationStage),
                $"[OBS][SessionOperationalPipeline][InputMode] command='OperationalInputModeRequest' routeIdentity='{request.Identity.RouteId}' routeOperationId='{request.Identity.RouteOperationId}' transitionId='{request.Identity.TransitionId}' routeSequence='{request.Identity.TransitionSequence}' contextSignature='{request.ContextSignature}' operationalSurfaceKind='{request.RouteClass}' inputPolicy='{request.InputPolicy}' initialInputMode='{request.InitialInputMode}' source='{request.Source}' reason='{request.Reason}'.",
                DebugUtility.Colors.Success);
        }

        private void EnsureInitialInputModePreparedFactOrFail(OperationalInputPreparationCommand command)
        {
            SessionOperationalIdentity currentIdentity = _factRecorder.CurrentIdentity;
            if (currentIdentity.Stage != SessionOperationalStage.InitialInputModePrepared ||
                !string.Equals(currentIdentity.RouteOperationId, command.RouteOperationId, StringComparison.Ordinal) ||
                !string.Equals(currentIdentity.TransitionId, command.TransitionId, StringComparison.Ordinal) ||
                currentIdentity.TransitionSequence != command.RouteSequence ||
                !string.Equals(currentIdentity.RouteId, command.RouteIdentity, StringComparison.Ordinal) ||
                !string.Equals(currentIdentity.RouteProfileId, command.RouteIdentity, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"[FATAL][H1][SessionOperationalPipeline][InputMode] Cannot submit initial input mode before matching InitialInputModePrepared fact routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' currentStage='{currentIdentity.Stage}' currentRouteOperationId='{currentIdentity.RouteOperationId}' currentTransitionId='{currentIdentity.TransitionId}' currentRouteSequence='{currentIdentity.TransitionSequence}' source='{command.Source}' reason='{command.Reason}'.");
            }
        }

        private OperationalInputModeRequest BuildInitialInputModeRequest(
            OperationalInputPreparationCommand command,
            SessionOperationalInputModeKind initialInputMode)
        {
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
                initialInputMode,
                command.InputPolicy,
                command.RouteClass);

            if (!request.IsValid)
            {
                throw new InvalidOperationException("Cannot submit invalid operational input mode request.");
            }

            return request;
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
