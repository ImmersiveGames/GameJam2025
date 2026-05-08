using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Core;
using _ImmersiveGames.NewScripts.AudioRuntime.Playback.Runtime.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionOperational.Integration.Base11Sandbox
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class Base11SandboxSessionOperationalAudioAdapter : ISessionOperationalAudioAdapter
    {
        private enum PlaybackKind
        {
            None = 0,
            Bgm = 1,
            Sfx = 2,
        }

        private readonly object _sync = new();
        private PlaybackKind _previousPlaybackKind;
        private IAudioPlaybackHandle _previousSfxHandle;

        public void PlayRouteRevealAudio(SessionOperationalRouteCommand command)
        {
            ValidateCommandOrFail(command);

            if (command.Audio.RouteAudioMode == SessionOperationalRouteAudioMode.None)
            {
                DebugUtility.Log(typeof(Base11SandboxSessionOperationalAudioAdapter),
                    BuildAudioLog(
                        "[OBS][SessionOperationalAudio][Adapter] playSkipped",
                        command,
                        extra: "skipReason='route_audio_disabled'"),
                    DebugUtility.Colors.Info);
                return;
            }

            AudioCueAsset cue = command.Audio.RouteAudioCue;
            string cueName = cue != null ? cue.name : "<none>";
            string cueType = ResolveCueTypeOrFail(cue);

            StopPreviousRouteAudioIfRequested(command);

            DebugUtility.Log(typeof(Base11SandboxSessionOperationalAudioAdapter),
                BuildAudioLog(
                    "[OBS][SessionOperationalAudio][Adapter] playStarted",
                    command,
                    $"cueType='{cueType}' cue='{cueName}'"),
                DebugUtility.Colors.Info);

            PlaybackKind playbackKind = DispatchCueOrFail(command, cue, cueType);

            lock (_sync)
            {
                _previousPlaybackKind = playbackKind;
                if (playbackKind != PlaybackKind.Sfx)
                {
                    _previousSfxHandle = null;
                }
            }

            DebugUtility.Log(typeof(Base11SandboxSessionOperationalAudioAdapter),
                BuildAudioLog(
                    "[OBS][SessionOperationalAudio][Adapter] playSubmitted",
                    command,
                    $"cueType='{cueType}' cue='{cueName}'"),
                DebugUtility.Colors.Success);
        }

        private PlaybackKind DispatchCueOrFail(
            SessionOperationalRouteCommand command,
            AudioCueAsset cue,
            string cueType)
        {
            switch (cue)
            {
                case AudioBgmCueAsset bgmCue:
                    return DispatchBgmOrFail(command, bgmCue, cueType);
                case AudioSfxCueAsset sfxCue:
                    return DispatchSfxOrFail(command, sfxCue, cueType);
                default:
                    throw new InvalidOperationException($"[FATAL][Audio][SessionOperationalPipeline] unsupported route audio cue type='{cue.GetType().Name}'.");
            }
        }

        private static PlaybackKind DispatchBgmOrFail(
            SessionOperationalRouteCommand command,
            AudioBgmCueAsset cue,
            string cueType)
        {
            if (!DependencyManager.Provider.TryGetGlobal<IAudioBgmService>(out var bgmService) || bgmService == null)
            {
                throw new InvalidOperationException("[FATAL][Audio][SessionOperationalPipeline] IAudioBgmService obrigatorio ausente para routeAudio cue do tipo BGM.");
            }

            bgmService.Play(cue, fadeInSeconds: -1f, reason: BuildReason(command, cueType));

            if (!ReferenceEquals(bgmService.ActiveCue, cue))
            {
                throw new InvalidOperationException($"[FATAL][Audio][SessionOperationalPipeline] BGM submission not confirmed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' cue='{cue.name}'.");
            }

            return PlaybackKind.Bgm;
        }

        private PlaybackKind DispatchSfxOrFail(
            SessionOperationalRouteCommand command,
            AudioSfxCueAsset cue,
            string cueType)
        {
            if (!DependencyManager.Provider.TryGetGlobal<IGlobalAudioService>(out var audioService) || audioService == null)
            {
                throw new InvalidOperationException("[FATAL][Audio][SessionOperationalPipeline] IGlobalAudioService obrigatorio ausente para routeAudio cue do tipo SFX.");
            }

            IAudioPlaybackHandle handle = audioService.Play(cue, AudioPlaybackContext.Global(BuildReason(command, cueType)));
            if (handle == null || !handle.IsValid)
            {
                throw new InvalidOperationException($"[FATAL][Audio][SessionOperationalPipeline] SFX submission not confirmed routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' cue='{cue.name}'.");
            }

            lock (_sync)
            {
                _previousSfxHandle = handle;
                _previousPlaybackKind = PlaybackKind.Sfx;
            }

            return PlaybackKind.Sfx;
        }

        private void StopPreviousRouteAudioIfRequested(SessionOperationalRouteCommand command)
        {
            if (!command.Audio.StopPreviousRouteAudio)
            {
                return;
            }

            PlaybackKind previousKind;
            IAudioPlaybackHandle previousSfxHandle;
            lock (_sync)
            {
                previousKind = _previousPlaybackKind;
                previousSfxHandle = _previousSfxHandle;
            }

            if (previousKind == PlaybackKind.Bgm)
            {
                if (!DependencyManager.Provider.TryGetGlobal<IAudioBgmService>(out var bgmService) || bgmService == null)
                {
                    throw new InvalidOperationException("[FATAL][Audio][SessionOperationalPipeline] IAudioBgmService obrigatorio ausente para parar o audio de rota anterior.");
                }

                bgmService.StopImmediate(BuildReason(command, "stop_previous_route_audio"));
                lock (_sync)
                {
                    _previousPlaybackKind = PlaybackKind.None;
                    _previousSfxHandle = null;
                }
                return;
            }

            if (previousKind == PlaybackKind.Sfx && previousSfxHandle != null)
            {
                previousSfxHandle.Stop(0f);
            }

            lock (_sync)
            {
                _previousPlaybackKind = PlaybackKind.None;
                _previousSfxHandle = null;
            }
        }

        private static void ValidateCommandOrFail(SessionOperationalRouteCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalAudio] SessionOperationalRouteCommand invalido.");
            }

            if (!command.Audio.IsValid)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalAudio] SessionOperationalRouteAudioCommand invalido.");
            }

            if (command.Audio.RouteAudioTiming != SessionOperationalRouteAudioTiming.BeforeFadeOut)
            {
                throw new InvalidOperationException("[FATAL][Config][SessionOperationalAudio] routeAudioTiming must be BeforeFadeOut.");
            }
        }

        private static string ResolveCueTypeOrFail(AudioCueAsset cue)
        {
            if (cue == null)
            {
                throw new InvalidOperationException("[FATAL][Audio][SessionOperationalPipeline] routeAudioCue is required.");
            }

            if (cue is AudioBgmCueAsset)
            {
                return nameof(AudioBgmCueAsset);
            }

            if (cue is AudioSfxCueAsset)
            {
                return nameof(AudioSfxCueAsset);
            }

            throw new InvalidOperationException($"[FATAL][Audio][SessionOperationalPipeline] unsupported route audio cue type='{cue.GetType().Name}'.");
        }

        private static string BuildReason(SessionOperationalRouteCommand command, string cueType)
        {
            return $"route_audio:{cueType}:{command.RouteOperationId}:{command.TransitionId}";
        }

        private static string BuildAudioLog(string prefix, SessionOperationalRouteCommand command, string extra)
        {
            string routeAudioMode = command.Audio.RouteAudioMode.ToString();
            string routeAudioTiming = command.Audio.RouteAudioTiming.ToString();
            string routeAudioCue = command.Audio.RouteAudioCueName;

            return $"{prefix} routeIdentity='{command.RouteIdentity}' routeOperationId='{command.RouteOperationId}' transitionId='{command.TransitionId}' routeSequence='{command.RouteSequence}' routeAudioMode='{routeAudioMode}' routeAudioTiming='{routeAudioTiming}' routeAudioCue='{routeAudioCue}' stopPreviousRouteAudio='{command.Audio.StopPreviousRouteAudio}' source='{command.Source}' reason='{command.Reason}' {extra}.";
        }
    }
}

