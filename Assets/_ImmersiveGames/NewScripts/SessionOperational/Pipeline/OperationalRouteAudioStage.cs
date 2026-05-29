using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteAudioStageResultKind
    {
        Unknown = 0,
        Completed = 1,
        Skipped = 2,
        Failed = 3,
    }

    public readonly struct OperationalRouteAudioStageResult
    {
        public OperationalRouteAudioStageResult(
            OperationalRouteAudioStageResultKind kind,
            bool audioSubmitted,
            string reason,
            string detail)
        {
            Kind = kind;
            AudioSubmitted = audioSubmitted;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalRouteAudioStageResultKind Kind { get; }
        public bool AudioSubmitted { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsCompleted => Kind == OperationalRouteAudioStageResultKind.Completed;
        public bool IsSkipped => Kind == OperationalRouteAudioStageResultKind.Skipped;

        public static OperationalRouteAudioStageResult Completed(bool audioSubmitted)
        {
            return new OperationalRouteAudioStageResult(
                OperationalRouteAudioStageResultKind.Completed,
                audioSubmitted,
                "completed",
                string.Empty);
        }

        public static OperationalRouteAudioStageResult Skipped(string reason, string detail)
        {
            return new OperationalRouteAudioStageResult(
                OperationalRouteAudioStageResultKind.Skipped,
                false,
                reason,
                detail);
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public sealed class OperationalRouteAudioCommand
    {
        public OperationalRouteAudioCommand(
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

    public sealed class OperationalRouteAudioStage
    {
        private readonly Func<IOperationalRouteAudioPort> _routeAudioPortResolver;

        public OperationalRouteAudioStage(Func<IOperationalRouteAudioPort> routeAudioPortResolver)
        {
            _routeAudioPortResolver = routeAudioPortResolver ?? throw new ArgumentNullException(nameof(routeAudioPortResolver));
        }

        public OperationalRouteAudioStageResult Execute(OperationalRouteAudioCommand command)
        {
            if (command == null || !command.IsValid)
            {
                throw new InvalidOperationException("OperationalRouteAudioCommand is invalid.");
            }

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;

            if (routeCommand.Audio.RouteAudioMode == SessionOperationalRouteAudioMode.None)
            {
                DebugUtility.Log(typeof(OperationalRouteAudioStage),
                    BuildAudioPipelineLog(
                        "[OBS][SessionOperationalPipeline][Audio] RouteRevealAudioSkipped",
                        command,
                        "skipReason='route_audio_disabled'"),
                    DebugUtility.Colors.Info);

                return OperationalRouteAudioStageResult.Skipped("route_audio_disabled", "route audio disabled by route policy");
            }

            string cueType = ResolveRouteAudioCueTypeOrFail(routeCommand.Audio.RouteAudioCue);

            DebugUtility.Log(typeof(OperationalRouteAudioStage),
                BuildAudioPipelineLog(
                    "[OBS][SessionOperationalPipeline][Audio] RouteRevealAudioStarted",
                    command,
                    $"cueType='{cueType}'"),
                DebugUtility.Colors.Info);

            IOperationalRouteAudioPort routeAudioPort = ResolveRouteAudioPortOrFail(routeCommand);
            OperationalRouteAudioResult result = routeAudioPort.SubmitRouteRevealAudio(
                new OperationalRouteAudioRequest(routeCommand, command.Source, command.Reason));

            if (!result.IsCompleted)
            {
                string message = $"[FATAL][SessionOperationalPipeline][Audio] OperationalRouteAudioPort failed routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' resultKind='{result.Kind}' reason='{result.Reason}' detail='{result.Detail}'.";
                DebugUtility.LogError<OperationalRouteAudioStage>(message);
                throw new InvalidOperationException(message);
            }

            DebugUtility.Log(typeof(OperationalRouteAudioStage),
                BuildAudioPipelineLog(
                    "[OBS][SessionOperationalPipeline][Audio] RouteRevealAudioSubmitted",
                    command,
                    $"cueType='{cueType}'"),
                DebugUtility.Colors.Success);

            return OperationalRouteAudioStageResult.Completed(true);
        }

        private IOperationalRouteAudioPort ResolveRouteAudioPortOrFail(SessionOperationalRouteCommand routeCommand)
        {
            IOperationalRouteAudioPort routeAudioPort = _routeAudioPortResolver();
            if (routeAudioPort == null)
            {
                throw new InvalidOperationException($"[FATAL][SessionOperationalPipeline][Audio] IOperationalRouteAudioPort is required routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}'.");
            }

            return routeAudioPort;
        }

        private static string ResolveRouteAudioCueTypeOrFail(AudioCueAsset cue)
        {
            if (cue is AudioBgmCueAsset)
            {
                return nameof(AudioBgmCueAsset);
            }

            if (cue is AudioSfxCueAsset)
            {
                return nameof(AudioSfxCueAsset);
            }

            string cueType = cue == null ? "<null>" : cue.GetType().Name;
            string message = $"[FATAL][Config][OperationalRouteAudioStage][Audio] unsupported routeAudioCue type='{cueType}'.";
            DebugUtility.LogError<OperationalRouteAudioStage>(message);
            throw new InvalidOperationException(message);
        }

        private static string BuildAudioPipelineLog(
            string prefix,
            OperationalRouteAudioCommand command,
            string extra)
        {
            SessionOperationalRouteCommand routeCommand = command.RouteCommand;

            return $"{prefix} routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' routeAudioMode='{routeCommand.Audio.RouteAudioMode}' routeAudioTiming='{routeCommand.Audio.RouteAudioTiming}' routeAudioCue='{routeCommand.Audio.RouteAudioCueName}' stopPreviousRouteAudio='{routeCommand.Audio.StopPreviousRouteAudio}' source='{command.Source}' reason='{command.Reason}' {extra}.";
        }
    }
}
