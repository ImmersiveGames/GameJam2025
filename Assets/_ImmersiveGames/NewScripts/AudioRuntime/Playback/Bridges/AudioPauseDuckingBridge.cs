using System;
using ImmersiveGames.GameJam2025.Core.Events;
using ImmersiveGames.GameJam2025.Core.Logging;
using ImmersiveGames.GameJam2025.Experience.Audio.Runtime.Core;
using ImmersiveGames.GameJam2025.Orchestration.GameLoop.RunLifecycle.Core;
using UnityEngine;

namespace ImmersiveGames.GameJam2025.Experience.Audio.Bridges
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class AudioPauseDuckingBridge : MonoBehaviour, IDisposable
    {
        private const string RuntimeObjectName = "NewScripts_AudioPauseDuckingBridge";
        private const string ReasonPauseWillEnter = "PauseWillEnterEvent";
        private const string ReasonPauseWillExit = "PauseWillExitEvent";
        private const string ReasonPauseStateChanged = "PauseStateChangedEvent";

        private EventBinding<PauseWillEnterEvent> _pauseWillEnterBinding;
        private EventBinding<PauseWillExitEvent> _pauseWillExitBinding;
        private EventBinding<PauseStateChangedEvent> _pauseStateBinding;
        private IAudioBgmService _bgmService;
        private bool _pauseDuckingApplied;
        private bool _bindingsRegistered;
        private bool _configured;
        private bool _disposed;

        public static AudioPauseDuckingBridge EnsureCreated(IAudioBgmService bgmService)
        {
            var existing = FindFirstObjectByType<AudioPauseDuckingBridge>();
            if (existing != null)
            {
                existing.Configure(bgmService);
                return existing;
            }

            var go = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(go);
            var bridge = go.AddComponent<AudioPauseDuckingBridge>();
            bridge.Configure(bgmService);
            return bridge;
        }

        private void Awake()
        {
            _pauseWillEnterBinding ??= new EventBinding<PauseWillEnterEvent>(OnPauseWillEnter);
            _pauseWillExitBinding ??= new EventBinding<PauseWillExitEvent>(OnPauseWillExit);
            _pauseStateBinding ??= new EventBinding<PauseStateChangedEvent>(OnPauseStateChanged);
        }

        public void Configure(IAudioBgmService bgmService)
        {
            _bgmService = bgmService ?? throw new InvalidOperationException("[FATAL][Config][Audio] IAudioBgmService obrigatorio ausente para o AudioPauseDuckingBridge.");
            _configured = true;
            TryRegisterBindings();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_bindingsRegistered)
            {
                EventBus<PauseWillEnterEvent>.Unregister(_pauseWillEnterBinding);
                EventBus<PauseWillExitEvent>.Unregister(_pauseWillExitBinding);
                EventBus<PauseStateChangedEvent>.Unregister(_pauseStateBinding);
                _bindingsRegistered = false;
            }

            ReleaseDucking("Dispose");
        }

        private void TryRegisterBindings()
        {
            if (_bindingsRegistered)
            {
                return;
            }

            if (!_configured || _bgmService == null)
            {
                throw new InvalidOperationException("[FATAL][Config][Audio] AudioPauseDuckingBridge precisa ser configurado com IAudioBgmService antes do registro no EventBus.");
            }

            try
            {
                EventBus<PauseWillEnterEvent>.Register(_pauseWillEnterBinding);
                EventBus<PauseWillExitEvent>.Register(_pauseWillExitBinding);
                EventBus<PauseStateChangedEvent>.Register(_pauseStateBinding);
                _bindingsRegistered = true;

                DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                    "[Audio][BOOT] AudioPauseDuckingBridge registrado nos hooks PauseWillEnterEvent/PauseWillExitEvent/PauseStateChangedEvent com owner IAudioBgmService explícito.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("[FATAL][Config][Audio] Falha ao registrar AudioPauseDuckingBridge no EventBus.", ex);
            }
        }

        private void OnPauseStateChanged(PauseStateChangedEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (evt.IsPaused)
            {
                if (_pauseDuckingApplied)
                {
                    DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                        $"[Audio][PauseDuck] PauseStateChanged no-op reason='{ReasonPauseStateChanged}' state='paused' applied='true'.",
                        DebugUtility.Colors.Info);
                    return;
                }

                ApplyDucking(ReasonPauseStateChanged);
                return;
            }

            if (!_pauseDuckingApplied)
            {
                DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                    $"[Audio][PauseDuck] PauseStateChanged no-op reason='{ReasonPauseStateChanged}' state='resumed' applied='false'.",
                    DebugUtility.Colors.Info);
                return;
            }

            ReleaseDucking(ReasonPauseStateChanged);
        }

        private void OnPauseWillEnter(PauseWillEnterEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (_pauseDuckingApplied)
            {
                DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                    $"[Audio][PauseDuck] PauseAudioDuckingRequestedEarly no-op reason='{SafeReason(evt.Reason)}' applied='true'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                $"[Audio][PauseDuck] PauseAudioDuckingRequestedEarly reason='{SafeReason(evt.Reason)}'.",
                DebugUtility.Colors.Info);

            ApplyDucking(ReasonPauseWillEnter);
        }

        private void OnPauseWillExit(PauseWillExitEvent evt)
        {
            if (evt == null)
            {
                return;
            }

            if (!_pauseDuckingApplied)
            {
                DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                    $"[Audio][PauseDuck] PauseAudioDuckingReleaseRequestedEarly no-op reason='{SafeReason(evt.Reason)}' applied='false'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                $"[Audio][PauseDuck] PauseAudioDuckingReleaseRequestedEarly reason='{SafeReason(evt.Reason)}'.",
                DebugUtility.Colors.Info);

            ReleaseDucking(ReasonPauseWillExit);
        }

        private void ApplyDucking(string reason)
        {
            if (_pauseDuckingApplied)
            {
                DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                    $"[Audio][PauseDuck] PauseAudioDuckingApply no-op reason='{reason}' applied='true'.",
                    DebugUtility.Colors.Info);
                return;
            }

            RequireBgmService().SetPauseDucking(true, reason);
            _pauseDuckingApplied = true;

            DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                $"[Audio][PauseDuck] PauseAudioDuckingApplied reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private void ReleaseDucking(string reason)
        {
            if (!_pauseDuckingApplied)
            {
                DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                    $"[Audio][PauseDuck] PauseAudioDuckingRelease no-op reason='{reason}' applied='false'.",
                    DebugUtility.Colors.Info);
                return;
            }

            RequireBgmService().SetPauseDucking(false, reason);
            _pauseDuckingApplied = false;

            DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                $"[Audio][PauseDuck] PauseAudioDuckingReleased reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private IAudioBgmService RequireBgmService()
        {
            if (_bgmService != null)
            {
                return _bgmService;
            }

            throw new InvalidOperationException("[FATAL][Config][Audio] IAudioBgmService obrigatorio ausente no AudioPauseDuckingBridge.");
        }

        private static string SafeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason;
        }
    }
}

