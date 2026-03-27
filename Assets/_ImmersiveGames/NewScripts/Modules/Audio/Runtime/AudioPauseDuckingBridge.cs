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
        private const string ReasonPauseStateChanged = "PauseStateChangedEvent";
        private const string ReasonMissingBgmService = "missing_IAudioBgmService";

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
                EventBus<PauseStateChangedEvent>.Register(_pauseStateBinding);
                _bindingsRegistered = true;

                DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                    "[Audio][BOOT] AudioPauseDuckingBridge registrado no hook PauseStateChangedEvent.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<AudioPauseDuckingBridge>(
                    $"[Audio][BOOT] Falha ao registrar bridge de ducking no PauseStateChangedEvent ({ex.GetType().Name}).");
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
                ApplyDucking();
            }
            else
            {
                ReleaseDucking(ReasonPauseStateChanged);
            }
        }

        private void ApplyDucking()
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

            _bgmService.SetPauseDucking(true, ReasonPauseStateChanged);
            _pauseDuckingApplied = true;

            DebugUtility.LogVerbose<AudioPauseDuckingBridge>(
                "[Audio][PauseDuck] PauseAudioDuckingApplied.",
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
                "[Audio][PauseDuck] PauseAudioDuckingReleased.",
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
    }
}
