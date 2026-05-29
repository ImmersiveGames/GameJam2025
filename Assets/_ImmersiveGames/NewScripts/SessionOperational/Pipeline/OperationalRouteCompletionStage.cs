using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
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
            SessionOperationalRuntimeState runtimeState,
            SessionOperationalRouteCommand routeCommand,
            string source,
            string reason)
        {
            PipelineId = Normalize(pipelineId);
            RuntimeState = runtimeState;
            RouteCommand = routeCommand;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string PipelineId { get; }
        public SessionOperationalRuntimeState RuntimeState { get; }
        public SessionOperationalRouteCommand RouteCommand { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            RuntimeState != null &&
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
        public OperationalRouteCompletionResult ExecuteTransitionPlanReady(OperationalRouteCompletionCommand command)
        {
            if (!command.IsValid)
            {
                return OperationalRouteCompletionResult.Failed("invalid_command", "OperationalRouteCompletionCommand is invalid for TransitionPlanReady.");
            }

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;
            DebugUtility.Log(typeof(OperationalRouteCompletionStage),
                $"[OBS][SessionOperationalPipeline][Transition] command='TransitionPlanReady' transitionMode='{routeCommand.TransitionMode}' transitionProfile='{routeCommand.TransitionProfileLabel}' routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
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

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;
            SessionOperationalResult operationalResult = ApplyCompletedState(command);
            SessionOperationalRouteSnapshot routeSnapshot = BuildCompletedRouteSnapshot(routeCommand);

            DebugUtility.Log(typeof(OperationalRouteCompletionStage),
                $"[OBS][SessionOperationalPipeline][Transition] fact='OperationalRouteCompleted' routeIdentity='{completionFact.RouteIdentity}' routeOperationId='{completionFact.RouteOperationId}' transitionId='{completionFact.TransitionId}' routeSequence='{completionFact.RouteSequence}' correlationId='{completionFact.CorrelationId}' message='{completionFact.Message}' transitionMode='{routeCommand.TransitionMode}' transitionProfile='{routeCommand.TransitionProfileLabel}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return new OperationalRouteCompletionResult(
                OperationalRouteCompletionResultKind.Completed,
                "completed",
                "operational_route_completed",
                routeSnapshot,
                operationalResult);
        }

        private static SessionOperationalResult ApplyCompletedState(OperationalRouteCompletionCommand command)
        {
            SessionOperationalRouteCommand routeCommand = command.RouteCommand;
            SessionOperationalIdentity identity = new(
                command.PipelineId,
                routeCommand.RouteOperationId,
                routeCommand.TransitionId,
                routeCommand.RouteSequence,
                routeCommand.RouteIdentity,
                routeCommand.RouteIdentity,
                command.Source,
                command.Reason,
                SessionOperationalStage.Completed);

            SessionOperationalFact fact = new(
                SessionOperationalFactKind.Completed,
                identity,
                command.Source,
                command.Reason,
                "Operational route completed.");

            command.RuntimeState.SetCurrentIdentity(identity);
            command.RuntimeState.MarkStarted();
            command.RuntimeState.MarkCompleted();
            command.RuntimeState.AppendFact(fact);
            command.RuntimeState.AppendTrace(
                $"[OBS][SessionOperationalPipeline] fact='OperationalRouteCompleted' stage='{identity.Stage}' routeOperationId='{identity.RouteOperationId}' transitionId='{identity.TransitionId}' routeSequence='{identity.TransitionSequence}' routeId='{identity.RouteId}' routeProfileId='{identity.RouteProfileId}' source='{command.Source}' reason='{command.Reason}' message='Operational route completed.'");

            return new SessionOperationalResult(
                SessionOperationalResultKind.Completed,
                identity,
                command.RuntimeState.Facts,
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

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
