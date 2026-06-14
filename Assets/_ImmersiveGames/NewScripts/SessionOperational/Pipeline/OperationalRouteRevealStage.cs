using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteRevealResultKind
    {
        Unknown = 0,
        Completed = 1,
        Failed = 2,
    }

    public readonly struct OperationalRouteRevealResult
    {
        public OperationalRouteRevealResult(
            OperationalRouteRevealResultKind kind,
            bool audioSubmitted,
            bool fadeOutCompleted,
            string reason,
            string detail)
        {
            Kind = kind;
            AudioSubmitted = audioSubmitted;
            FadeOutCompleted = fadeOutCompleted;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalRouteRevealResultKind Kind { get; }
        public bool AudioSubmitted { get; }
        public bool FadeOutCompleted { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalRouteRevealResultKind.Completed;

        public static OperationalRouteRevealResult Completed(bool audioSubmitted, bool fadeOutCompleted)
        {
            return new OperationalRouteRevealResult(
                OperationalRouteRevealResultKind.Completed,
                audioSubmitted,
                fadeOutCompleted,
                "completed",
                string.Empty);
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public sealed class OperationalRouteRevealCommand
    {
        public OperationalRouteRevealCommand(
            SessionOperationalRouteCommand routeCommand,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
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

                return true;
            }
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public sealed class OperationalRouteRevealStage
    {
        private readonly OperationalFactRecorder _factRecorder;

        public OperationalRouteRevealStage(OperationalFactRecorder factRecorder)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
        }

        public void Begin(OperationalRouteRevealCommand command)
        {
            Validate(command);
            _factRecorder.TryRecordOperationStage(SessionOperationalStage.RouteReveal, command.Source, command.Reason, "route_reveal_started");
            LogRevealStarted(command);
        }

        public OperationalRouteRevealResult Complete(
            OperationalRouteRevealCommand command,
            bool audioSubmitted,
            bool fadeOutCompleted)
        {
            Validate(command);
            _factRecorder.TryRecordOperationStage(SessionOperationalStage.RouteReveal, command.Source, command.Reason, "route_reveal_completed");
            LogRevealCompleted(command);
            return OperationalRouteRevealResult.Completed(audioSubmitted, fadeOutCompleted);
        }

        private static void Validate(OperationalRouteRevealCommand command)
        {
            if (command == null || !command.IsValid)
            {
                throw new InvalidOperationException("OperationalRouteRevealCommand is invalid.");
            }
        }

        private static void LogRevealStarted(OperationalRouteRevealCommand command)
        {
            var routeCommand = command.RouteCommand;

            DebugUtility.LogVerbose(typeof(OperationalRouteRevealStage),
                $"OperationalRouteRevealStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogRevealCompleted(OperationalRouteRevealCommand command)
        {
            var routeCommand = command.RouteCommand;

            DebugUtility.Log(typeof(OperationalRouteRevealStage),
                $"OperationalRouteRevealCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
        }
    }
}
