using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalHandoffExitResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct OperationalHandoffExitCommand
    {
        public OperationalHandoffExitCommand(
            SessionOperationalRouteCommand routeCommand,
            string previousRouteIdentity,
            string previousActivityIdentity,
            SceneKeyAsset previousActiveSceneKey,
            IReadOnlyList<SceneKeyAsset> previousRouteOwnedSceneKeys,
            IReadOnlyList<SceneKeyAsset> finalScenesToUnload,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            PreviousRouteIdentity = previousRouteIdentity.TrimToEmpty();
            PreviousActivityIdentity = previousActivityIdentity.TrimToEmpty();
            PreviousActiveSceneKey = previousActiveSceneKey;
            PreviousRouteOwnedSceneKeys = previousRouteOwnedSceneKeys ?? Array.Empty<SceneKeyAsset>();
            FinalScenesToUnload = finalScenesToUnload ?? Array.Empty<SceneKeyAsset>();
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public string PreviousRouteIdentity { get; }
        public string PreviousActivityIdentity { get; }
        public SceneKeyAsset PreviousActiveSceneKey { get; }
        public IReadOnlyList<SceneKeyAsset> PreviousRouteOwnedSceneKeys { get; }
        public IReadOnlyList<SceneKeyAsset> FinalScenesToUnload { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            RouteCommand.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);
}

    public readonly struct OperationalHandoffExitResult
    {
        public OperationalHandoffExitResult(
            OperationalHandoffExitResultKind kind,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string previousRouteIdentity,
            string handoffIdentity,
            string reason,
            string detail)
        {
            Kind = kind;
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            TransitionId = transitionId.TrimToEmpty();
            RouteSequence = routeSequence;
            PreviousRouteIdentity = previousRouteIdentity.TrimToEmpty();
            HandoffIdentity = handoffIdentity.TrimToEmpty();
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public OperationalHandoffExitResultKind Kind { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string PreviousRouteIdentity { get; }
        public string HandoffIdentity { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalHandoffExitResultKind.Completed;
        public bool IsSkipped => Kind == OperationalHandoffExitResultKind.Skipped;
        public bool IsFailed => Kind == OperationalHandoffExitResultKind.Failed;
        public bool IsAccepted => IsCompleted || IsSkipped;
}

    public sealed class RouteRequestBlockedByOperationalHandoffException : Exception
    {
        public RouteRequestBlockedByOperationalHandoffException(string blockedReason, string blockedDetail)
            : base("route_request_blocked_by_operational_handoff")
        {
            BlockedReason = blockedReason.TrimToEmpty();
            BlockedDetail = blockedDetail.TrimToEmpty();
        }

        public string BlockedReason { get; }
        public string BlockedDetail { get; }
}

    public sealed class OperationalHandoffExitStage
    {
        private readonly OperationalFactRecorder _factRecorder;
        private readonly Func<IOperationalRouteHandoffExitPort> _handoffExitPortResolver;

        public OperationalHandoffExitStage(
            OperationalFactRecorder factRecorder,
            Func<IOperationalRouteHandoffExitPort> handoffExitPortResolver)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
            _handoffExitPortResolver = handoffExitPortResolver ?? throw new ArgumentNullException(nameof(handoffExitPortResolver));
        }

        public OperationalRouteHandoffExitPreflightResult EvaluatePreflight(
            string routeIdentity,
            string previousRouteIdentity,
            string handoffIdentity,
            string source,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(handoffIdentity))
            {
                return new OperationalRouteHandoffExitPreflightResult(
                    OperationalRouteHandoffExitPreflightKind.NotRequired,
                    "not_required",
                    "handoff_identity_missing");
            }

            var handoffExitPort = ResolveHandoffExitPortOrFail();
            OperationalRouteHandoffExitPreflightResult result = handoffExitPort.EvaluatePreflight(new OperationalRouteHandoffExitPreflightRequest(
                routeIdentity,
                previousRouteIdentity,
                handoffIdentity,
                source,
                reason));

            if (!result.IsValid)
            {
                return new OperationalRouteHandoffExitPreflightResult(
                    OperationalRouteHandoffExitPreflightKind.Failed,
                    "handoff_exit_preflight_invalid_result",
                    "handoff_exit_port_returned_invalid_preflight_result");
            }

            if (result.IsRejected || result.IsFailed)
            {
                DebugUtility.LogVerbose(typeof(OperationalHandoffExitStage),
                    $"OperationalHandoffExitPreflightBlocked reason='{result.Reason}' detail='{result.Detail}' owner='IOperationalRouteHandoffExitPort'.",
                    DebugUtility.Colors.Info);
            }

            return result;
        }

        public async Task<OperationalHandoffExitResult> ExecuteAsync(OperationalHandoffExitCommand command)
        {
            if (!command.IsValid)
            {
                return Failed(command, "invalid_command", "OperationalHandoffExitCommand invalido.");
            }

            var routeCommand = command.RouteCommand;
            if (!ShouldRequireOperationalRouteHandoffExit(command))
            {
                string skippedMsg = $"OperationalHandoffExitSkipped routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' handoffIdentity='{command.PreviousActivityIdentity}' skipReason='not_required' source='{command.Source}' reason='{command.Reason}'.";
                _factRecorder.TryRecordOperationStage(SessionOperationalStage.HandoffExit, command.Source, command.Reason, skippedMsg, typeof(OperationalHandoffExitStage), DebugUtility.Colors.Info);

                return new OperationalHandoffExitResult(
                    OperationalHandoffExitResultKind.Skipped,
                    routeCommand.RouteIdentity,
                    routeCommand.RouteOperationId,
                    routeCommand.TransitionId,
                    routeCommand.RouteSequence,
                    command.PreviousRouteIdentity,
                    command.PreviousActivityIdentity,
                    "not_required",
                    "handoff_exit_not_required");
            }

            OperationalRouteHandoffExitRequest request = new(
                routeCommand.RouteIdentity,
                routeCommand.RouteOperationId,
                routeCommand.TransitionId,
                routeCommand.RouteSequence,
                command.PreviousRouteIdentity,
                command.PreviousActivityIdentity,
                command.Source,
                command.Reason);

            string deferredMsg = $"OperationalRouteRequestDeferredForHandoffExit routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' handoffIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.";
            _factRecorder.TryRecordOperationStage(SessionOperationalStage.HandoffExit, command.Source, command.Reason, deferredMsg, typeof(OperationalHandoffExitStage), DebugUtility.Colors.Info);
            string startedMsg = $"OperationalHandoffExitStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' handoffIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.";
            _factRecorder.TryRecordOperationStage(SessionOperationalStage.HandoffExit, command.Source, command.Reason, startedMsg, typeof(OperationalHandoffExitStage), DebugUtility.Colors.Info);

            var handoffExitPort = ResolveHandoffExitPortOrFail();
            var exitResult = await handoffExitPort.RequestExitAsync(
                request,
                CancellationToken.None);

            if (!exitResult.IsValid)
            {
                FailOperationalRouteHandoffExitOrThrow(
                    command,
                    "handoff_exit_invalid_result",
                    "Operational handoff exit port returned invalid result.",
                    exitResult);
            }

            if (exitResult.IsCompleted)
            {
                string completedMsg = $"OperationalHandoffExitCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' handoffIdentity='{command.PreviousActivityIdentity}' exitResult='{exitResult}' source='{command.Source}' reason='{command.Reason}'.";
                _factRecorder.TryRecordOperationStage(SessionOperationalStage.HandoffExit, command.Source, command.Reason, completedMsg, typeof(OperationalHandoffExitStage), DebugUtility.Colors.Success);

                return new OperationalHandoffExitResult(
                    OperationalHandoffExitResultKind.Completed,
                    routeCommand.RouteIdentity,
                    routeCommand.RouteOperationId,
                    routeCommand.TransitionId,
                    routeCommand.RouteSequence,
                    command.PreviousRouteIdentity,
                    command.PreviousActivityIdentity,
                    exitResult.Reason,
                    exitResult.Detail);
            }

            FailOperationalRouteHandoffExitOrThrow(
                command,
                string.IsNullOrWhiteSpace(exitResult.Reason) ? "handoff_exit_failed" : exitResult.Reason,
                string.IsNullOrWhiteSpace(exitResult.Detail) ? "Operational handoff exit failed." : exitResult.Detail,
                exitResult);

            return Failed(command, "handoff_exit_failed", "unreachable_after_handoff_exit_failure");
        }

        private IOperationalRouteHandoffExitPort ResolveHandoffExitPortOrFail()
        {
            var handoffExitPort = _handoffExitPortResolver();
            if (handoffExitPort == null)
            {
                throw new InvalidOperationException("[FATAL][SessionOperationalPipeline][Route] IOperationalRouteHandoffExitPort obrigatorio ausente para handoff exit.");
            }

            return handoffExitPort;
        }


        private static void FailOperationalRouteHandoffExitOrThrow(
            OperationalHandoffExitCommand command,
            string blockedReason,
            string blockedDetail,
            OperationalRouteHandoffExitResult exitResult)
        {
            var routeCommand = command.RouteCommand;
            DebugUtility.LogWarning<OperationalHandoffExitStage>(
                $"RouteRequestBlockedByOperationalHandoff routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' handoffIdentity='{command.PreviousActivityIdentity}' blockedReason='{blockedReason}' detail='{blockedDetail}' exitResult='{exitResult}' source='{command.Source}' reasonDetail='{command.Reason}'.");
            throw new RouteRequestBlockedByOperationalHandoffException(blockedReason, blockedDetail);
        }

        private static bool ShouldRequireOperationalRouteHandoffExit(OperationalHandoffExitCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.PreviousRouteIdentity))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(command.PreviousActivityIdentity))
            {
                return false;
            }

            if (command.FinalScenesToUnload == null || command.FinalScenesToUnload.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < command.FinalScenesToUnload.Count; i++)
            {
                var candidate = command.FinalScenesToUnload[i];
                if (candidate == null)
                {
                    continue;
                }

                if (command.PreviousActiveSceneKey != null && ReferenceEquals(candidate, command.PreviousActiveSceneKey))
                {
                    return true;
                }

                if (ContainsSceneKey(command.PreviousRouteOwnedSceneKeys, candidate))
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

        private static OperationalHandoffExitResult Failed(
            OperationalHandoffExitCommand command,
            string reason,
            string detail)
        {
            var routeCommand = command.RouteCommand;
            return new OperationalHandoffExitResult(
                OperationalHandoffExitResultKind.Failed,
                routeCommand.RouteIdentity,
                routeCommand.RouteOperationId,
                routeCommand.TransitionId,
                routeCommand.RouteSequence,
                command.PreviousRouteIdentity,
                command.PreviousActivityIdentity,
                string.IsNullOrWhiteSpace(reason) ? "handoff_exit_failed" : reason,
                string.IsNullOrWhiteSpace(detail) ? "handoff_exit_failed" : detail);
        }
    }
}
