using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Runtime
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class AudioPauseDuckingBridge : MonoBehaviour, IDisposable
    {
        private const string RuntimeObjectName = "NewScripts_AudioPauseDuckingBridge";
        private const string ReasonPauseWillEnter = "PauseWillEnterEvent";
        private const string ReasonPauseWillExit = "PauseWillExitEvent";
        private const string ReasonPauseStateChanged = "PauseStateChangedEvent";
        private const string ReasonMissingBgmService = "missing_IAudioBgmService";

        private EventBinding<PauseWillEnterEvent> _pauseWillEnterBinding;
        private EventBinding<PauseWillExitEvent> _pauseWillExitBinding;
        private EventBinding<PauseStateChangedEvent> _pauseStateBinding;
        private IAudioBgmService _bgmService;
        private bool _pauseDuckingApplied;
        private bool _bindingsRegistered;
        private bool _warnedMissingBgmService;
        private bool _disposed;

        public static AudioPauseDuckingBridge EnsureCreated()
        {
            var existing = FindFirstObjectByType<AudioPauseDuckingBridge>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(RuntimeObjectName);
            DontDestroyOnLoad(go);
            return go.AddComponent<AudioPauseDuckingBridge>();
        }

        private void Awake()
        {
            _pauseWillEnterBinding ??= new EventBinding<PauseWillEnterEvent>(OnPauseWillEnter);
            _pauseWillExitBinding ??= new EventBinding<PauseWillExitEvent>(OnPauseWillExit);
            _pauseStateBinding ??= new EventBinding<PauseStateChangedEvent>(OnPauseStateChanged);
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

            try
            {
                EventBus<PauseWillEnterEvent>.Register(_pauseWillEnterBinding);
                EventBus<PauseWillExitEvent>.Register(_pauseWillExitBinding);
                EventBus<PauseStateChangedEvent>.Register(_pauseStateBinding);
                _bindingsRegistered = true;

                DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                    "[Audio][BOOT] AudioPauseDuckingBridge registrado nos hooks PauseWillEnterEvent/PauseWillExitEvent/PauseStateChangedEvent.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<AudioPauseDuckingBridge>(
                    $"[Audio][BOOT] Falha ao registrar bridge de ducking nos hooks de pause ({ex.GetType().Name}).");
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
                ApplyDucking(ReasonPauseStateChanged);
            }
            else
            {
                ReleaseDucking(ReasonPauseStateChanged);
            }
        }

        private void OnPauseWillEnter(PauseWillEnterEvent evt)
        {
            if (evt == null)
            {
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

            DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                $"[Audio][PauseDuck] PauseAudioDuckingReleaseRequestedEarly reason='{SafeReason(evt.Reason)}'.",
                DebugUtility.Colors.Info);

            ReleaseDucking(ReasonPauseWillExit);
        }

        private void ApplyDucking(string reason)
        {
            if (_pauseDuckingApplied)
            {
                return;
            }

            if (!EnsureBgmService())
            {
                WarnMissingBgmService();
                return;
            }

            _bgmService.SetPauseDucking(true, reason);
            _pauseDuckingApplied = true;

            DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                $"[Audio][PauseDuck] PauseAudioDuckingApplied reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private void ReleaseDucking(string reason)
        {
            if (!_pauseDuckingApplied)
            {
                return;
            }

            if (EnsureBgmService())
            {
                _bgmService.SetPauseDucking(false, reason);
            }

            _pauseDuckingApplied = false;

            DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                $"[Audio][PauseDuck] PauseAudioDuckingReleased reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private bool EnsureBgmService()
        {
            if (_bgmService != null)
            {
                return true;
            }

            if (!DependencyManager.HasInstance || DependencyManager.Provider == null)
            {
                return false;
            }

            if (DependencyManager.Provider.TryGetGlobal<IAudioBgmService>(out var bgmService) && bgmService != null)
            {
                _bgmService = bgmService;
                _warnedMissingBgmService = false;
                return true;
            }

            return false;
        }

        private void WarnMissingBgmService()
        {
            if (_warnedMissingBgmService)
            {
                return;
            }

            _warnedMissingBgmService = true;
            DebugUtility.LogWarning<AudioPauseDuckingBridge>(
                $"[Audio][PauseDuck] ducking skipped: IAudioBgmService unavailable. reason='{ReasonMissingBgmService}'.");
        }

        private static string SafeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason;
        }
    }
}
