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
            IOperationalFadePort fadePort,
            IOperationalRouteAudioPort routeAudioPort,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            FadePort = fadePort;
            RouteAudioPort = routeAudioPort;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public IOperationalFadePort FadePort { get; }
        public IOperationalRouteAudioPort RouteAudioPort { get; }
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

                if (RouteCommand.UsesTransition && FadePort == null)
                {
                    return false;
                }

                if (RouteCommand.Audio.RouteAudioMode != SessionOperationalRouteAudioMode.None && RouteAudioPort == null)
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
        private readonly OperationalRouteAudioStage _routeAudioStage = new();
        private readonly OperationalFadeStage _fadeStage = new();

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
                    command.RouteAudioPort,
                    command.Source,
                    command.Reason));
            bool audioSubmitted = audioResult.AudioSubmitted;
            bool fadeOutCompleted = false;

            if (routeCommand.UsesTransition)
            {
                OperationalFadeStageResult fadeResult = await _fadeStage.ExecuteAsync(
                    new OperationalFadeCommand(
                        routeCommand,
                        command.FadePort,
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
