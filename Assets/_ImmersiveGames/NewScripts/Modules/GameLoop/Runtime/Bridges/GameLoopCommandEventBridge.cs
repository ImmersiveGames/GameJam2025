using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Bridges
{
    /// <summary>
    /// Bridge de eventos definitivos do GameLoop (EventBus -> IGameLoopService).
    ///
    /// Regras (alinhadas ao GameLoop.md):
    /// - NAO consome eventos de intencao de start (GameStartCommandEvent). Start e coordenado via SceneFlow.
    /// - Consome apenas eventos definitivos: pause/resume.
    /// - MacroRestart canonico e coordenado por MacroRestartCoordinator (sem listener de reset aqui).
    /// - ExitToMenu canonico e coordenado por ExitToMenuCoordinator.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopCommandEventBridge : IDisposable
    {
        private readonly EventBinding<GamePauseCommandEvent> _onPause;
        private readonly EventBinding<GameResumeRequestedEvent> _onResume;

        private bool _disposed;
        private int _lastPauseFrame = -1;
        private string _lastPauseKey = string.Empty;
        private int _lastResumeFrame = -1;
        private string _lastResumeKey = string.Empty;

        public GameLoopCommandEventBridge()
        {
            _onPause = new EventBinding<GamePauseCommandEvent>(OnGamePause);
            _onResume = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);

            EventBus<GamePauseCommandEvent>.Register(_onPause);
            EventBus<GameResumeRequestedEvent>.Register(_onResume);

            DebugUtility.LogVerbose<GameLoopCommandEventBridge>(
                "[GameLoop] Bridge de entrada registrado no EventBus (pause/resume).",
                DebugUtility.Colors.Info);
            DebugUtility.LogVerbose<GameLoopCommandEventBridge>(
                "[OBS][LEGACY] GameResetRequestedEvent listener disabled in GameLoopCommandEventBridge; MacroRestartCoordinator owns canonical restart.",
                DebugUtility.Colors.Info);
            DebugUtility.LogVerbose<GameLoopCommandEventBridge>(
                "[OBS][LEGACY] ExitToMenu listener disabled in GameLoopCommandEventBridge; ExitToMenuCoordinator owns canonical exit.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EventBus<GamePauseCommandEvent>.Unregister(_onPause);
            EventBus<GameResumeRequestedEvent>.Unregister(_onResume);
        }

        private static bool TryResolveLoop(out IGameLoopService loop)
        {
            loop = null;
            return DependencyManager.Provider.TryGetGlobal(out loop) && loop != null;
        }

        private void OnGamePause(GamePauseCommandEvent evt)
        {
            string key = BuildPauseKey(evt);
            int frame = UnityEngine.Time.frameCount;
            if (_lastPauseFrame == frame && string.Equals(_lastPauseKey, key, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<GameLoopCommandEventBridge>(
                    $"[OBS][GRS] GamePauseCommandEvent dedupe_same_frame consumer='{nameof(GameLoopCommandEventBridge)}' key='{key}' frame='{frame}'",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastPauseFrame = frame;
            _lastPauseKey = key;
            DebugUtility.LogVerbose<GameLoopCommandEventBridge>(
                $"[OBS][GRS] GamePauseCommandEvent consumed consumer='{nameof(GameLoopCommandEventBridge)}' key='{key}' frame='{frame}'",
                DebugUtility.Colors.Info);

            if (!TryResolveLoop(out var loop))
            {
                return;
            }

            if (evt != null && evt.IsPaused)
            {
                loop.RequestPause();
            }
            else
            {
                loop.RequestResume();
            }
        }

        private void OnGameResumeRequested(GameResumeRequestedEvent evt)
        {
            string key = BuildResumeKey(evt);
            int frame = UnityEngine.Time.frameCount;
            if (_lastResumeFrame == frame && string.Equals(_lastResumeKey, key, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<GameLoopCommandEventBridge>(
                    $"[OBS][GRS] GameResumeRequestedEvent dedupe_same_frame consumer='{nameof(GameLoopCommandEventBridge)}' key='{key}' frame='{frame}'",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastResumeFrame = frame;
            _lastResumeKey = key;
            DebugUtility.LogVerbose<GameLoopCommandEventBridge>(
                $"[OBS][GRS] GameResumeRequestedEvent consumed consumer='{nameof(GameLoopCommandEventBridge)}' key='{key}' frame='{frame}'",
                DebugUtility.Colors.Info);

            if (!TryResolveLoop(out var loop))
            {
                return;
            }

            loop.RequestResume();
        }

        private static string BuildPauseKey(GamePauseCommandEvent evt)
        {
            string reason = "<null>";
            bool isPaused = evt is { IsPaused: true };
            return $"pause|isPaused={isPaused}|reason={reason}";
        }

        private static string BuildResumeKey(GameResumeRequestedEvent evt)
            => $"resume|reason={NormalizeReason(null)}";

        private static string NormalizeReason(string reason)
            => string.IsNullOrWhiteSpace(reason) ? "<null>" : reason.Trim();
    }
}
