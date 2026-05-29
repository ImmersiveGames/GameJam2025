using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

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
        private readonly OperationalRouteAudioStage _routeAudioStage;
        private readonly OperationalFadeStage _fadeStage;

        public OperationalRouteRevealStage(OperationalRouteAudioStage routeAudioStage, OperationalFadeStage fadeStage)
        {
            _routeAudioStage = routeAudioStage ?? throw new ArgumentNullException(nameof(routeAudioStage));
            _fadeStage = fadeStage ?? throw new ArgumentNullException(nameof(fadeStage));
        }

        public async Task<OperationalRouteRevealResult> ExecuteAsync(OperationalRouteRevealCommand command)
        {
            if (command == null || !command.IsValid)
            {
                throw new InvalidOperationException("OperationalRouteRevealCommand is invalid.");
            }

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;

            LogRevealStarted(command);
            OperationalRouteAudioStageResult audioResult = _routeAudioStage.Execute(
                new OperationalRouteAudioCommand(
                    routeCommand,
                    command.Source,
                    command.Reason));
            bool audioSubmitted = audioResult.AudioSubmitted;
            bool fadeOutCompleted = false;

            if (routeCommand.UsesTransition)
            {
                OperationalFadeStageResult fadeResult = await _fadeStage.ExecuteAsync(
                    new OperationalFadeCommand(
                        routeCommand,
                        OperationalFadeOperationKind.OpenCurtain,
                        command.Source,
                        command.Reason));
                fadeOutCompleted = fadeResult.FadeCompleted;
            }

            LogRevealCompleted(command);

            return OperationalRouteRevealResult.Completed(audioSubmitted, fadeOutCompleted);
        }

        private static void LogRevealStarted(OperationalRouteRevealCommand command)
        {
            SessionOperationalRouteCommand routeCommand = command.RouteCommand;

            DebugUtility.Log(typeof(OperationalRouteRevealStage),
                $"[OBS][SessionOperationalPipeline][Route] OperationalRouteRevealStarted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
        }

        private static void LogRevealCompleted(OperationalRouteRevealCommand command)
        {
            SessionOperationalRouteCommand routeCommand = command.RouteCommand;

            DebugUtility.Log(typeof(OperationalRouteRevealStage),
                $"[OBS][SessionOperationalPipeline][Route] OperationalRouteRevealCompleted routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
        }
    }
}
