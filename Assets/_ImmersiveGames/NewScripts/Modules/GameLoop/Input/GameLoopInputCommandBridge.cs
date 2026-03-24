using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Input
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
    public sealed class GameLoopInputCommandBridge : IDisposable
    {
        private readonly GameLoopEventSubscriptionSet _subscriptions = new();
        private readonly EventBinding<GamePauseCommandEvent> _onPause;
        private readonly EventBinding<GameResumeRequestedEvent> _onResume;

        private bool _disposed;
        private int _lastPauseFrame = -1;
        private string _lastPauseKey = string.Empty;
        private int _lastResumeFrame = -1;
        private string _lastResumeKey = string.Empty;

        public GameLoopInputCommandBridge()
        {
            _onPause = new EventBinding<GamePauseCommandEvent>(OnGamePause);
            _onResume = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);

            _subscriptions.Register(_onPause);
            _subscriptions.Register(_onResume);

            DebugUtility.LogVerbose<GameLoopInputCommandBridge>(
                "[GameLoop] Bridge de entrada registrado no EventBus (pause/resume).",
                DebugUtility.Colors.Info);
            DebugUtility.LogVerbose<GameLoopInputCommandBridge>(
                "[OBS][LEGACY] GameResetRequestedEvent listener disabled in GameLoopInputCommandBridge; MacroRestartCoordinator owns canonical restart.",
                DebugUtility.Colors.Info);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _subscriptions.Dispose();
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
                DebugUtility.LogVerbose<GameLoopInputCommandBridge>(
                    $"[OBS][GRS] GamePauseCommandEvent dedupe_same_frame consumer='{nameof(GameLoopInputCommandBridge)}' key='{key}' frame='{frame}'",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastPauseFrame = frame;
            _lastPauseKey = key;
            DebugUtility.LogVerbose<GameLoopInputCommandBridge>(
                $"[OBS][GRS] GamePauseCommandEvent consumed consumer='{nameof(GameLoopInputCommandBridge)}' key='{key}' frame='{frame}'",
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
                DebugUtility.LogVerbose<GameLoopInputCommandBridge>(
                    $"[OBS][GRS] GameResumeRequestedEvent dedupe_same_frame consumer='{nameof(GameLoopInputCommandBridge)}' key='{key}' frame='{frame}'",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastResumeFrame = frame;
            _lastResumeKey = key;
            DebugUtility.LogVerbose<GameLoopInputCommandBridge>(
                $"[OBS][GRS] GameResumeRequestedEvent consumed consumer='{nameof(GameLoopInputCommandBridge)}' key='{key}' frame='{frame}'",
                DebugUtility.Colors.Info);

            if (!TryResolveLoop(out var loop))
            {
                return;
            }

            loop.RequestResume();
        }

        private static string BuildPauseKey(GamePauseCommandEvent evt)
        {
            string reason = GameLoopReasonFormatter.Format(null);
            bool isPaused = evt is { IsPaused: true };
            return $"pause|isPaused={isPaused}|reason={reason}";
        }

        private static string BuildResumeKey(GameResumeRequestedEvent evt)
            => $"resume|reason={GameLoopReasonFormatter.Format(null)}";
    }
}
