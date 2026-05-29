using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalTransitionBlackoutResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct OperationalTransitionBlackoutCommand
    {
        public OperationalTransitionBlackoutCommand(
            SessionOperationalRouteCommand routeCommand,
            IFadeAdapter fadeAdapter,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            FadeAdapter = fadeAdapter;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public IFadeAdapter FadeAdapter { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid => RouteCommand.IsValid;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct OperationalTransitionBlackoutResult
    {
        public OperationalTransitionBlackoutResult(
            OperationalTransitionBlackoutResultKind kind,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            bool fadeInCompleted,
            string reason,
            string detail)
        {
            Kind = kind;
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence;
            FadeInCompleted = fadeInCompleted;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalTransitionBlackoutResultKind Kind { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public bool FadeInCompleted { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalTransitionBlackoutResultKind.Completed;
        public bool IsSkipped => Kind == OperationalTransitionBlackoutResultKind.Skipped;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class OperationalTransitionBlackoutStage
    {
        public async Task<OperationalTransitionBlackoutResult> ExecuteAsync(OperationalTransitionBlackoutCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("OperationalTransitionBlackoutCommand is invalid.");
            }

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;
            string source = Normalize(command.Source);
            string reason = Normalize(command.Reason);

            if (routeCommand.UsesTransition)
            {
                if (command.FadeAdapter == null)
                {
                    throw new InvalidOperationException("[FATAL][SessionOperationalPipeline][Transition] IFadeAdapter is required for operational transition blackout.");
                }

                DebugUtility.Log(typeof(OperationalTransitionBlackoutStage),
                    $"[OBS][SessionOperationalPipeline][Transition] OperationalTransitionBlackoutStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Info);

                await command.FadeAdapter.FadeInAsync(routeCommand);

                DebugUtility.Log(typeof(OperationalTransitionBlackoutStage),
                    $"[OBS][SessionOperationalPipeline][Transition] OperationalTransitionBlackoutCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{source}' reason='{reason}'.",
                    DebugUtility.Colors.Success);

                return new OperationalTransitionBlackoutResult(
                    OperationalTransitionBlackoutResultKind.Completed,
                    routeCommand.RouteIdentity,
                    routeCommand.RouteOperationId,
                    routeCommand.TransitionId,
                    routeCommand.RouteSequence,
                    true,
                    "blackout_completed",
                    "Operational transition blackout completed.");
            }

            DebugUtility.Log(typeof(OperationalTransitionBlackoutStage),
                $"[OBS][SessionOperationalPipeline][Transition] OperationalTransitionBlackoutSkipped routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{source}' reason='{reason}'.",
                DebugUtility.Colors.Info);

            return new OperationalTransitionBlackoutResult(
                OperationalTransitionBlackoutResultKind.Skipped,
                routeCommand.RouteIdentity,
                routeCommand.RouteOperationId,
                routeCommand.TransitionId,
                routeCommand.RouteSequence,
                false,
                "blackout_skipped",
                "Operational transition blackout skipped because route transition is disabled.");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
