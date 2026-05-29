using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Adapters;

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
            IFadeAdapter fadeAdapter,
            IAudioAdapter audioAdapter,
            string source,
            string reason)
        {
            RouteCommand = routeCommand;
            FadeAdapter = fadeAdapter;
            AudioAdapter = audioAdapter;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SessionOperationalRouteCommand RouteCommand { get; }
        public IFadeAdapter FadeAdapter { get; }
        public IAudioAdapter AudioAdapter { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid
        {
            get
            {

                if (RouteCommand.UsesTransition && FadeAdapter == null)
                {
                    return false;
                }

                if (RouteCommand.Audio.RouteAudioMode != SessionOperationalRouteAudioMode.None && AudioAdapter == null)
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
        public async Task<OperationalRouteRevealResult> ExecuteAsync(OperationalRouteRevealCommand command)
        {
            if (command == null || !command.IsValid)
            {
                throw new InvalidOperationException("OperationalRouteRevealCommand is invalid.");
            }

            SessionOperationalRouteCommand routeCommand = command.RouteCommand;

            LogRevealStarted(command);
            bool audioSubmitted = ExecuteRouteRevealAudio(command);
            bool fadeOutCompleted = false;

            if (routeCommand.UsesTransition)
            {
                await command.FadeAdapter.FadeOutAsync(routeCommand);
                fadeOutCompleted = true;
            }

            LogRevealCompleted(command);

            return OperationalRouteRevealResult.Completed(audioSubmitted, fadeOutCompleted);
        }

        private static bool ExecuteRouteRevealAudio(OperationalRouteRevealCommand command)
        {
            SessionOperationalRouteCommand routeCommand = command.RouteCommand;

            if (routeCommand.Audio.RouteAudioMode == SessionOperationalRouteAudioMode.None)
            {
                DebugUtility.Log(typeof(OperationalRouteRevealStage),
                    BuildAudioPipelineLog(
                        "[OBS][SessionOperationalPipeline][Audio] RouteRevealAudioSkipped",
                        command,
                        "skipReason='route_audio_disabled'"),
                    DebugUtility.Colors.Info);

                return false;
            }

            string cueType = ResolveRouteAudioCueTypeOrFail(routeCommand.Audio.RouteAudioCue);

            DebugUtility.Log(typeof(OperationalRouteRevealStage),
                BuildAudioPipelineLog(
                    "[OBS][SessionOperationalPipeline][Audio] RouteRevealAudioStarted",
                    command,
                    $"cueType='{cueType}'"),
                DebugUtility.Colors.Info);

            command.AudioAdapter.PlayRouteRevealAudio(routeCommand);

            DebugUtility.Log(typeof(OperationalRouteRevealStage),
                BuildAudioPipelineLog(
                    "[OBS][SessionOperationalPipeline][Audio] RouteRevealAudioSubmitted",
                    command,
                    $"cueType='{cueType}'"),
                DebugUtility.Colors.Success);

            return true;
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
            string message = $"[FATAL][Config][OperationalRouteRevealStage][Audio] unsupported routeAudioCue type='{cueType}'.";
            DebugUtility.LogError<OperationalRouteRevealStage>(message);
            throw new InvalidOperationException(message);
        }

        private static string BuildAudioPipelineLog(
            string prefix,
            OperationalRouteRevealCommand command,
            string extra)
        {
            SessionOperationalRouteCommand routeCommand = command.RouteCommand;

            return $"{prefix} routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' routeAudioMode='{routeCommand.Audio.RouteAudioMode}' routeAudioTiming='{routeCommand.Audio.RouteAudioTiming}' routeAudioCue='{routeCommand.Audio.RouteAudioCueName}' stopPreviousRouteAudio='{routeCommand.Audio.StopPreviousRouteAudio}' source='{command.Source}' reason='{command.Reason}' {extra}.";
        }
    }
}
