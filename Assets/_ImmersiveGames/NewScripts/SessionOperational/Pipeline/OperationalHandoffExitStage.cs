using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;

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
            IOperationalRouteHandoffExitPort handoffExitPort,
            SessionOperationalRouteCommand routeCommand,
            string previousRouteIdentity,
            string previousActivityIdentity,
            SceneKeyAsset previousActiveSceneKey,
            IReadOnlyList<SceneKeyAsset> previousRouteOwnedSceneKeys,
            IReadOnlyList<SceneKeyAsset> finalScenesToUnload,
            string source,
            string reason)
        {
            HandoffExitPort = handoffExitPort;
            RouteCommand = routeCommand;
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            PreviousActivityIdentity = Normalize(previousActivityIdentity);
            PreviousActiveSceneKey = previousActiveSceneKey;
            PreviousRouteOwnedSceneKeys = previousRouteOwnedSceneKeys ?? Array.Empty<SceneKeyAsset>();
            FinalScenesToUnload = finalScenesToUnload ?? Array.Empty<SceneKeyAsset>();
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public IOperationalRouteHandoffExitPort HandoffExitPort { get; }
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
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
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            PreviousRouteIdentity = Normalize(previousRouteIdentity);
            HandoffIdentity = Normalize(handoffIdentity);
            Reason = Normalize(reason);
            Detail = Normalize(detail);
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class RouteRequestBlockedByOperationalHandoffException : Exception
    {
        public RouteRequestBlockedByOperationalHandoffException(string blockedReason, string blockedDetail)
            : base("route_request_blocked_by_operational_handoff")
        {
            BlockedReason = Normalize(blockedReason);
            BlockedDetail = Normalize(blockedDetail);
        }

        public string BlockedReason { get; }
        public string BlockedDetail { get; }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalHandoffExitStage
    {
        public OperationalRouteHandoffExitPreflightResult EvaluatePreflight(
            IOperationalRouteHandoffExitPort handoffExitPort,
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

            if (handoffExitPort == null)
            {
                return new OperationalRouteHandoffExitPreflightResult(
                    OperationalRouteHandoffExitPreflightKind.Failed,
                    "missing_handoff_exit_port",
                    "IOperationalRouteHandoffExitPort obrigatorio ausente para handoff exit preflight.");
            }

            return handoffExitPort.EvaluatePreflight(
                new OperationalRouteHandoffExitPreflightRequest(
                    routeIdentity,
                    previousRouteIdentity,
                    handoffIdentity,
                    source,
                    reason));
        }

        public async Task<OperationalHandoffExitResult> ExecuteAsync(OperationalHandoffExitCommand command)
        {
            if (!command.IsValid)
            {
                return Failed(command, "invalid_command", "OperationalHandoffExitCommand invalido.");
            }

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;
            if (!ShouldRequireOperationalRouteHandoffExit(command))
            {
                DebugUtility.Log(typeof(OperationalHandoffExitStage),
                    $"[OBS][SessionOperationalPipeline][Route] OperationalHandoffExitSkipped routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' handoffIdentity='{command.PreviousActivityIdentity}' skipReason='not_required' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Info);

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

            if (command.HandoffExitPort == null)
            {
                return Failed(command, "missing_handoff_exit_port", "IOperationalRouteHandoffExitPort obrigatorio ausente para handoff exit pre-unload.");
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

            DebugUtility.Log(typeof(OperationalHandoffExitStage),
                $"[OBS][SessionOperationalPipeline][Route] OperationalRouteRequestDeferredForHandoffExit routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' handoffIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            DebugUtility.Log(typeof(OperationalHandoffExitStage),
                $"[OBS][SessionOperationalPipeline][Route] OperationalHandoffExitStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' handoffIdentity='{command.PreviousActivityIdentity}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            OperationalRouteHandoffExitResult exitResult = await command.HandoffExitPort.RequestExitAsync(
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
                DebugUtility.Log(typeof(OperationalHandoffExitStage),
                    $"[OBS][SessionOperationalPipeline][Route] OperationalHandoffExitCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' handoffIdentity='{command.PreviousActivityIdentity}' exitResult='{exitResult}' source='{command.Source}' reason='{command.Reason}'.",
                    DebugUtility.Colors.Success);

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

        private static void FailOperationalRouteHandoffExitOrThrow(
            OperationalHandoffExitCommand command,
            string blockedReason,
            string blockedDetail,
            OperationalRouteHandoffExitResult exitResult)
        {
            SessionOperationalRouteCommand routeCommand = command.RouteCommand;
            DebugUtility.LogWarning<OperationalHandoffExitStage>(
                $"[OBS][SessionOperationalPipeline][Route] RouteRequestBlockedByOperationalHandoff routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' previousRouteIdentity='{command.PreviousRouteIdentity}' handoffIdentity='{command.PreviousActivityIdentity}' blockedReason='{blockedReason}' detail='{blockedDetail}' exitResult='{exitResult}' source='{command.Source}' reasonDetail='{command.Reason}'.");
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
                SceneKeyAsset candidate = command.FinalScenesToUnload[i];
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
            SessionOperationalRouteCommand routeCommand = command.RouteCommand;
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
