using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
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
        private readonly OperationalFactRecorder _factRecorder;
        private readonly Func<IOperationalRouteAudioPort> _routeAudioPortResolver;

        public OperationalRouteAudioStage(OperationalFactRecorder factRecorder, Func<IOperationalRouteAudioPort> routeAudioPortResolver)
        {
            _factRecorder = factRecorder ?? throw new ArgumentNullException(nameof(factRecorder));
            _routeAudioPortResolver = routeAudioPortResolver ?? throw new ArgumentNullException(nameof(routeAudioPortResolver));
        }

        public OperationalRouteAudioResult Execute(OperationalRouteAudioCommand command)
        {
            if (command == null || !command.IsValid)
            {
                throw new InvalidOperationException("OperationalRouteAudioCommand is invalid.");
            }

            var routeCommand = command.RouteCommand;

            if (routeCommand.Audio.RouteAudioMode == SessionOperationalRouteAudioMode.None)
            {
                string skippedMsg = BuildAudioPipelineLog(
                    "RouteRevealAudioSkipped",
                    command,
                    "skipReason='route_audio_disabled'");
                _factRecorder.TryRecordOperationStage(SessionOperationalStage.RouteAudio, command.Source, command.Reason, skippedMsg, typeof(OperationalRouteAudioStage), DebugUtility.Colors.Info);

                return OperationalRouteAudioResult.Skipped("route_audio_disabled", "route audio disabled by route policy");
            }

            string cueType = ResolveRouteAudioCueTypeOrFail(routeCommand.Audio.RouteAudioCue);

            string startedMsg = BuildAudioPipelineLog(
                "RouteRevealAudioStarted",
                command,
                $"cueType='{cueType}'");
            _factRecorder.TryRecordOperationStage(SessionOperationalStage.RouteAudio, command.Source, command.Reason, startedMsg, typeof(OperationalRouteAudioStage), DebugUtility.Colors.Info);

            var routeAudioPort = ResolveRouteAudioPortOrFail(routeCommand);
            var result = routeAudioPort.SubmitRouteRevealAudio(
                new OperationalRouteAudioRequest(routeCommand, command.Source, command.Reason));

            if (!result.IsCompleted)
            {
                string failedMsg = $"[FATAL][SessionOperationalPipeline][Audio] OperationalRouteAudioPort failed routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' resultKind='{result.Kind}' reason='{result.Reason}' detail='{result.Detail}'.";
                _factRecorder.TryRecordOperationStage(SessionOperationalStage.RouteAudio, command.Source, command.Reason, failedMsg, typeof(OperationalRouteAudioStage));
                DebugUtility.LogError<OperationalRouteAudioStage>(failedMsg);
                throw new InvalidOperationException(failedMsg);
            }

            string completedMsg = BuildAudioPipelineLog(
                "RouteRevealAudioSubmitted",
                command,
                $"cueType='{cueType}'");
            _factRecorder.TryRecordOperationStage(SessionOperationalStage.RouteAudio, command.Source, command.Reason, completedMsg, typeof(OperationalRouteAudioStage), DebugUtility.Colors.Success);

            return result;
        }

        private IOperationalRouteAudioPort ResolveRouteAudioPortOrFail(SessionOperationalRouteCommand routeCommand)
        {
            var routeAudioPort = _routeAudioPortResolver();
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
            var routeCommand = command.RouteCommand;

            return $"{prefix} routeIdentity='{routeCommand.RouteIdentity}' routeOperationId='{routeCommand.RouteOperationId}' transitionId='{routeCommand.TransitionId}' routeSequence='{routeCommand.RouteSequence}' routeAudioMode='{routeCommand.Audio.RouteAudioMode}' routeAudioTiming='{routeCommand.Audio.RouteAudioTiming}' routeAudioCue='{routeCommand.Audio.RouteAudioCueName}' stopPreviousRouteAudio='{routeCommand.Audio.StopPreviousRouteAudio}' source='{command.Source}' reason='{command.Reason}' {extra}.";
        }
    }
}
