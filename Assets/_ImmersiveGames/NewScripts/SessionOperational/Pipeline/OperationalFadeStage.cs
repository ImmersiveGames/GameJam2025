using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalFadeOperationKind
    {
        Unknown = 0,
        CloseCurtain = 1,
        OpenCurtain = 2,
        CleanupOpenCurtain = 3,
    }

    public readonly struct OperationalFadeCommand
    {
        public OperationalFadeCommand(
            SessionOperationalRouteCommand routeCommand,
            OperationalFadeOperationKind operationKind,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            OperationKind = operationKind;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public OperationalFadeOperationKind OperationKind { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid
        {
            get
            {
                if (!RouteCommand.IsValid)
                {
                    return false;
                }

                if (OperationKind == OperationalFadeOperationKind.Unknown)
                {
                    return false;
                }

                return true;
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalFadeStageResult
    {
        public OperationalFadeStageResult(OperationalFadeResult result, bool fadeCompleted)
        {
            Result = result;
            FadeCompleted = fadeCompleted;
        }

        public OperationalFadeResult Result { get; }
        public bool FadeCompleted { get; }
        public bool IsAccepted => Result.IsAccepted;
        public bool IsCompleted => Result.IsCompleted;
        public bool IsSkipped => Result.IsSkipped;
        public string Reason => Result.Reason;
        public string Detail => Result.Detail;
    }

    public sealed class OperationalFadeStage
    {
        private readonly Func<IOperationalFadePort> _fadePortResolver;

        public OperationalFadeStage(Func<IOperationalFadePort> fadePortResolver)
        {
            _fadePortResolver = fadePortResolver ?? throw new ArgumentNullException(nameof(fadePortResolver));
        }

        public async Task<OperationalFadeStageResult> ExecuteAsync(OperationalFadeCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalFadeCommand is invalid.");
            }

            var routeCommand = command.RouteCommand;
            var direction = ResolveDirection(command.OperationKind);
            string operationKindLabel = ResolveOperationKindLabel(command.OperationKind);

            if (!routeCommand.UsesTransition)
            {
                LogSkipped(
                    routeCommand,
                    command,
                    direction,
                    operationKindLabel,
                    "transition_disabled",
                    "Operational fade skipped because route transition is disabled.");

                return new OperationalFadeStageResult(
                    OperationalFadeResult.Skipped(
                        routeCommand,
                        direction,
                        "transition_disabled",
                        "Operational fade skipped because route transition is disabled."),
                    false);
            }

            LogStarted(routeCommand, command, direction, operationKindLabel);

            try
            {
                var fadePort = ResolveFadePortOrFail(routeCommand);
                var result = await fadePort.ExecuteAsync(
                    new OperationalFadeRequest(
                        routeCommand,
                        direction,
                        command.Source,
                        command.Reason));

                if (result.IsCompleted)
                {
                    LogCompleted(routeCommand, command, direction, operationKindLabel, result);
                    return new OperationalFadeStageResult(result, true);
                }

                if (result.IsSkipped)
                {
                    LogSkipped(routeCommand, command, direction, operationKindLabel, result.Reason, result.Detail);
                    return new OperationalFadeStageResult(result, false);
                }

                LogFailed(routeCommand, command, direction, operationKindLabel, result.Reason, result.Detail);

                throw new InvalidOperationException(
                    $"[FATAL][SessionOperationalPipeline][Transition] Operational fade failed routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' operationKind='{operationKindLabel}' fadeOperationKind='{command.OperationKind}' fadeDirection='{direction}' reason='{result.Reason}' detail='{result.Detail}'.");
            }
            catch (Exception ex)
            {
                LogFailed(routeCommand, command, direction, operationKindLabel, ex.GetType().Name, ex.Message);
                throw;
            }
        }

        private static void LogStarted(
            SessionOperationalRouteCommand routeCommand,
            OperationalFadeCommand command,
            OperationalFadeDirection direction,
            string operationKindLabel)
        {
            DebugUtility.LogVerbose(typeof(OperationalFadeStage),
                $"OperationalFadeStageStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' operationKind='{operationKindLabel}' fadeOperationKind='{command.OperationKind}' fadeDirection='{direction}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogCompleted(
            SessionOperationalRouteCommand routeCommand,
            OperationalFadeCommand command,
            OperationalFadeDirection direction,
            string operationKindLabel,
            OperationalFadeResult result)
        {
            DebugUtility.Log(typeof(OperationalFadeStage),
                $"OperationalFadeStageCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' operationKind='{operationKindLabel}' fadeOperationKind='{command.OperationKind}' fadeDirection='{direction}' resultReason='{result.Reason}' detail='{result.Detail}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
        }

        private static void LogSkipped(
            SessionOperationalRouteCommand routeCommand,
            OperationalFadeCommand command,
            OperationalFadeDirection direction,
            string operationKindLabel,
            string skipReason,
            string detail)
        {
            DebugUtility.LogVerbose(typeof(OperationalFadeStage),
                $"OperationalFadeStageSkipped routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' operationKind='{operationKindLabel}' fadeOperationKind='{command.OperationKind}' fadeDirection='{direction}' skipReason='{skipReason}' detail='{detail}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogFailed(
            SessionOperationalRouteCommand routeCommand,
            OperationalFadeCommand command,
            OperationalFadeDirection direction,
            string operationKindLabel,
            string failureReason,
            string detail)
        {
            DebugUtility.LogError<OperationalFadeStage>(
                $"OperationalFadeStageFailed routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' operationKind='{operationKindLabel}' fadeOperationKind='{command.OperationKind}' fadeDirection='{direction}' failureReason='{failureReason}' detail='{detail}' source='{command.Source}' reason='{command.Reason}'.");
        }

        private IOperationalFadePort ResolveFadePortOrFail(SessionOperationalRouteCommand routeCommand)
        {
            var fadePort = _fadePortResolver();
            if (fadePort == null)
            {
                throw new InvalidOperationException($"[FATAL][SessionOperationalPipeline][Transition] IOperationalFadePort is required for operational fade routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}'.");
            }

            return fadePort;
        }

        private static OperationalFadeDirection ResolveDirection(OperationalFadeOperationKind operationKind)
        {
            switch (operationKind)
            {
                case OperationalFadeOperationKind.CloseCurtain:
                    return OperationalFadeDirection.CloseCurtain;
                case OperationalFadeOperationKind.OpenCurtain:
                case OperationalFadeOperationKind.CleanupOpenCurtain:
                    return OperationalFadeDirection.OpenCurtain;
                default:
                    throw new InvalidOperationException($"Unknown operational fade operation kind '{operationKind}'.");
            }
        }

        private static string ResolveOperationKindLabel(OperationalFadeOperationKind operationKind)
        {
            switch (operationKind)
            {
                case OperationalFadeOperationKind.CloseCurtain:
                    return "TransitionBlackout";
                case OperationalFadeOperationKind.OpenCurtain:
                    return "RouteReveal";
                case OperationalFadeOperationKind.CleanupOpenCurtain:
                    return "FailureCleanup";
                default:
                    throw new InvalidOperationException($"Unknown operational fade operation kind '{operationKind}'.");
            }
        }
    }
}
