using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteMaterializationBoundaryResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2
    }

    public readonly struct OperationalRouteMaterializationBoundaryResult
    {
        public OperationalRouteMaterializationBoundaryResult(
            OperationalRouteMaterializationBoundaryResultKind kind,
            SessionOperationalRouteCompletedFact completionFact,
            bool loadingCompleted,
            bool loadingHidden)
        {
            Kind = kind;
            CompletionFact = completionFact;
            LoadingCompleted = loadingCompleted;
            LoadingHidden = loadingHidden;
        }

        public OperationalRouteMaterializationBoundaryResultKind Kind { get; }
        public SessionOperationalRouteCompletedFact CompletionFact { get; }
        public bool LoadingCompleted { get; }
        public bool LoadingHidden { get; }
        public bool IsCompleted => Kind == OperationalRouteMaterializationBoundaryResultKind.Completed && CompletionFact.IsValid;
    }

    public readonly struct OperationalRouteMaterializationBoundaryCommand
    {
        public OperationalRouteMaterializationBoundaryCommand(
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string source,
            string reason)
        {
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            TransitionId = transitionId.TrimToEmpty();
            RouteSequence = routeSequence;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0;
    }

    public sealed class OperationalRouteMaterializationBoundary
    {
        private readonly OperationalFactRecorder _factRecorder;

        public OperationalRouteMaterializationBoundary(OperationalFactRecorder factRecorder)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
        }

        public void Begin(OperationalRouteMaterializationBoundaryCommand command)
        {
            ValidateCommand(command);
            _factRecorder.TryRecordOperationStage(SessionOperationalStage.RoutePhysicalApplyObserved, command.Source, command.Reason, "materialization_started");
            LogMaterializationStarted(command);
        }

        public OperationalRouteMaterializationBoundaryResult Complete(
            OperationalRouteMaterializationBoundaryCommand command,
            SessionOperationalRouteCompletedFact adapterFact,
            OperationalLoadingCompletionState loadingState)
        {
            ValidateCommand(command);
            if (!adapterFact.IsValid)
            {
                throw new InvalidOperationException("SessionOperationalRouteCompletedFact is invalid.");
            }

            _factRecorder.TryRecordOperationStage(SessionOperationalStage.ScenesReadyObserved, command.Source, command.Reason, "materialization_completed");
            LogMaterializationCompleted(command);

            return new OperationalRouteMaterializationBoundaryResult(
                OperationalRouteMaterializationBoundaryResultKind.Completed,
                adapterFact,
                loadingState.LoadingCompleted,
                loadingState.LoadingHidden);
        }

        private static void ValidateCommand(OperationalRouteMaterializationBoundaryCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalRouteMaterializationBoundaryCommand is invalid.");
            }
        }

        private static void LogMaterializationStarted(OperationalRouteMaterializationBoundaryCommand command)
        {
            DebugUtility.LogVerbose(typeof(OperationalRouteMaterializationBoundary),
                $"OperationalRouteMaterializationStarted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' source='{command.Source.TrimToEmpty()}' reason='{command.Reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogMaterializationCompleted(OperationalRouteMaterializationBoundaryCommand command)
        {
            DebugUtility.Log(typeof(OperationalRouteMaterializationBoundary),
                $"OperationalRouteMaterializationCompleted routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' source='{command.Source.TrimToEmpty()}' reason='{command.Reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Success);
        }
    }
}
