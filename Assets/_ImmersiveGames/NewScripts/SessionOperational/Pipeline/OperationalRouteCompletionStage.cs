using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneReferences;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    internal enum OperationalRouteCompletionResultKind
    {
        Unknown = 0,
        Accepted = 1,
        Completed = 2,
        Failed = 3,
    }

    internal readonly struct OperationalRouteCompletionCommand
    {
        public OperationalRouteCompletionCommand(
            string pipelineId,
            SessionOperationalRouteCommand routeCommand,
            string source,
            string reason)
        {
            PipelineId = Normalize(pipelineId);
            RouteCommand = routeCommand;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string PipelineId { get; }
        public SessionOperationalRouteCommand RouteCommand { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            RouteCommand.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            !string.IsNullOrWhiteSpace(Reason);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    internal readonly struct OperationalRouteCompletionResult
    {
        public OperationalRouteCompletionResult(
            OperationalRouteCompletionResultKind kind,
            string reason,
            string detail,
            SessionOperationalRouteSnapshot routeSnapshot,
            SessionOperationalResult operationalResult)
        {
            Kind = kind;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
            RouteSnapshot = routeSnapshot;
            OperationalResult = operationalResult;
        }

        public OperationalRouteCompletionResultKind Kind { get; }
        public string Reason { get; }
        public string Detail { get; }
        public SessionOperationalRouteSnapshot RouteSnapshot { get; }
        public SessionOperationalResult OperationalResult { get; }

        public bool IsAccepted => Kind == OperationalRouteCompletionResultKind.Accepted;
        public bool IsCompleted => Kind == OperationalRouteCompletionResultKind.Completed;

        public static OperationalRouteCompletionResult Accepted(string reason) =>
            new(OperationalRouteCompletionResultKind.Accepted, reason, string.Empty, default, default);

        public static OperationalRouteCompletionResult Failed(string reason, string detail) =>
            new(OperationalRouteCompletionResultKind.Failed, reason, detail, default, default);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    internal sealed class OperationalRouteCompletionStage
    {
        private readonly OperationalFactRecorder _factRecorder;

        public OperationalRouteCompletionStage(OperationalFactRecorder factRecorder)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
        }

        public OperationalRouteCompletionResult ExecuteTransitionPlanReady(OperationalRouteCompletionCommand command)
        {
            if (!command.IsValid)
            {
                return OperationalRouteCompletionResult.Failed("invalid_command", "OperationalRouteCompletionCommand is invalid for TransitionPlanReady.");
            }

            var routeCommand = command.RouteCommand;
            DebugUtility.Log(typeof(OperationalRouteCompletionStage),
                $"command='TransitionPlanReady' transitionMode='{routeCommand.TransitionMode}' transitionProfile='{routeCommand.TransitionProfileLabel}' routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            return OperationalRouteCompletionResult.Accepted("transition_plan_ready_logged");
        }

        public OperationalRouteCompletionResult ExecuteCompleted(
            OperationalRouteCompletionCommand command,
            SessionOperationalRouteCompletedFact completionFact)
        {
            if (!command.IsValid)
            {
                return OperationalRouteCompletionResult.Failed("invalid_command", "OperationalRouteCompletionCommand is invalid for route completion.");
            }

            var routeCommand = command.RouteCommand;
            var operationalResult = ApplyCompletedState(command);
            var routeSnapshot = BuildCompletedRouteSnapshot(routeCommand);

            DebugUtility.Log(typeof(OperationalRouteCompletionStage),
                $"fact='OperationalRouteCompleted' routeIdentity='{completionFact.RouteIdentity}' routeOperationId='{completionFact.RouteOperationId}' transitionId='{completionFact.TransitionId}' routeSequence='{completionFact.RouteSequence}' correlationId='{completionFact.CorrelationId}' message='{completionFact.Message}' transitionMode='{routeCommand.TransitionMode}' transitionProfile='{routeCommand.TransitionProfileLabel}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new OperationalRouteCompletionResult(
                OperationalRouteCompletionResultKind.Completed,
                "completed",
                "operational_route_completed",
                routeSnapshot,
                operationalResult);
        }

        private SessionOperationalResult ApplyCompletedState(OperationalRouteCompletionCommand command)
        {
            if (_factRecorder.CurrentIdentity.Stage != SessionOperationalStage.Completed)
            {
                throw new InvalidOperationException(
                    $"[FATAL][H1][SessionOperationalPipeline][Transition] Completed fact must be recorded before building operational result routeIdentity='{command.RouteCommand.RouteIdentity}' routeOperationId='{command.RouteCommand.RouteOperationId}' transitionId='{command.RouteCommand.TransitionId}' routeSequence='{command.RouteCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.");
            }

            return new SessionOperationalResult(
                SessionOperationalResultKind.Completed,
                _factRecorder.CurrentIdentity,
                _factRecorder.Facts,
                "Operational route completed.");
        }

        private static SessionOperationalRouteSnapshot BuildCompletedRouteSnapshot(SessionOperationalRouteCommand command)
        {
            SessionOperationalRouteSnapshot snapshot = new(
                command.RouteIdentity,
                command.RouteOperationId,
                command.RouteSequence,
                command.ActiveSceneKey,
                command.ActivitySavePolicy.SaveActivityOnExit,
                command.ActivitySavePolicy.ContributorScopePolicy,
                command.HandoffSessionStateId,
                command.FinalScenesToLoad);

            if (!snapshot.IsValid)
            {
                HardFailFastH1.Trigger(typeof(OperationalRouteCompletionStage),
                    $"[FATAL][Config][SessionOperationalPipeline] failed to record last completed route snapshot routeIdentity='{command.RouteIdentity}' routeSequence='{command.RouteSequence}'.");
            }

            return snapshot;
        }
    }

    internal readonly struct SessionOperationalRouteSnapshot
    {
        public SessionOperationalRouteSnapshot(
            string routeIdentity,
            string routeOperationId,
            int routeSequence,
            SceneKeyAsset activeSceneKey,
            bool saveActivityOnExit,
            RouteActivitySaveContributorScopePolicy contributorScopePolicy,
            string activityIdentity,
            IReadOnlyList<SceneKeyAsset> routeOwnedLoadedSceneKeys)
        {
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            ActiveSceneKey = activeSceneKey;
            SaveActivityOnExit = saveActivityOnExit;
            ContributorScopePolicy = contributorScopePolicy;
            ActivityIdentity = Normalize(activityIdentity);
            RouteOwnedLoadedSceneKeys = routeOwnedLoadedSceneKeys ?? throw new ArgumentNullException(nameof(routeOwnedLoadedSceneKeys));
        }

        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public int RouteSequence { get; }
        public SceneKeyAsset ActiveSceneKey { get; }
        public bool SaveActivityOnExit { get; }
        public RouteActivitySaveContributorScopePolicy ContributorScopePolicy { get; }
        public string ActivityIdentity { get; }
        public IReadOnlyList<SceneKeyAsset> RouteOwnedLoadedSceneKeys { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            RouteSequence > 0 &&
            ActiveSceneKey != null &&
            RouteOwnedLoadedSceneKeys != null;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
