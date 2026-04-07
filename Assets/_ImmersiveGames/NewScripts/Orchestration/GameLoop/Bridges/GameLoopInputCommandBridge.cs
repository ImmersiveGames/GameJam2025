using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core;
using _ImmersiveGames.NewScripts.Orchestration.Navigation;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.Bridges
{
    /// <summary>
    /// Bridge de eventos definitivos do GameLoop (EventBus -> owners corretos).
    ///
    /// Regras (alinhadas ao GameLoop.md):
    /// - NAO consome eventos de intencao de start-plan (BootStartPlanRequestedEvent). Start-plan e coordenado via SceneFlow.
    /// - Consome intent de Play e delega para o owner de Navigation.
    /// - Consome apenas eventos definitivos: pause/resume.
    /// - Restart e ExitToMenu seguem owner downstream canonico; este bridge nao absorve essas intents.
    /// </summary>
    public sealed class GameLoopInputCommandBridge : IDisposable
    {
        private readonly IGameLoopService _gameLoop;
        private readonly IGameNavigationService _navigation;
        private readonly GameLoopEventSubscriptionSet _subscriptions = new();
        private readonly EventBinding<GamePlayRequestedEvent> _onPlay;
        private readonly EventBinding<GamePauseCommandEvent> _onPause;
        private readonly EventBinding<GameResumeRequestedEvent> _onResume;

        private bool _disposed;
        private int _lastPlayFrame = -1;
        private string _lastPlayKey = string.Empty;
        private int _lastPauseFrame = -1;
        private string _lastPauseKey = string.Empty;
        private int _lastResumeFrame = -1;
        private string _lastResumeKey = string.Empty;

        public GameLoopInputCommandBridge(IGameLoopService gameLoop, IGameNavigationService navigation)
        {
            _gameLoop = gameLoop ?? throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameLoopService obrigatorio ausente para GameLoopInputCommandBridge.");
            _navigation = navigation ?? throw new InvalidOperationException("[FATAL][Config][GameLoop] IGameNavigationService obrigatorio ausente para GameLoopInputCommandBridge.");
            _onPlay = new EventBinding<GamePlayRequestedEvent>(OnGamePlayRequested);
            _onPause = new EventBinding<GamePauseCommandEvent>(OnGamePause);
            _onResume = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);

            _subscriptions.Register(_onPlay);
            _subscriptions.Register(_onPause);
            _subscriptions.Register(_onResume);

            DebugUtility.LogVerbose<GameLoopInputCommandBridge>(
                "[OBS][NavigationAdapters][Operational] GameLoopInputCommandBridge registrado no EventBus (play/pause/resume) como adapter de entrada operacional.",
                DebugUtility.Colors.Info);
            DebugUtility.LogVerbose<GameLoopInputCommandBridge>(
                "[OBS][NavigationAdapters][Operational] Restart/ExitToMenu nao passam por este bridge; owners canonicos ficam em GameplaySessionFlow/Navigation.",
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

        private void OnGamePlayRequested(GamePlayRequestedEvent evt)
        {
            string key = $"play|reason={GameLoopReasonFormatter.Format(evt?.Reason)}";
            int frame = UnityEngine.Time.frameCount;
            if (_lastPlayFrame == frame && string.Equals(_lastPlayKey, key, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<GameLoopInputCommandBridge>(
                    $"[OBS][GRS] GamePlayRequestedEvent dedupe_same_frame consumer='{nameof(GameLoopInputCommandBridge)}' key='{key}' frame='{frame}'",
                    DebugUtility.Colors.Info);
                return;
            }

            _lastPlayFrame = frame;
            _lastPlayKey = key;
            DebugUtility.LogVerbose<GameLoopInputCommandBridge>(
                $"[OBS][GRS] GamePlayRequestedEvent consumed consumer='{nameof(GameLoopInputCommandBridge)}' key='{key}' frame='{frame}'",
                DebugUtility.Colors.Info);

            _ = _navigation.NavigateAsync(GameNavigationIntentKind.Gameplay, evt?.Reason);
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

            if (evt != null && evt.IsPaused)
            {
                _gameLoop.RequestPause(evt.Reason);
            }
            else
            {
                _gameLoop.RequestResume(evt?.Reason);
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

            _gameLoop.RequestResume(evt?.Reason);
        }

        private static string BuildPauseKey(GamePauseCommandEvent evt)
        {
            string reason = GameLoopReasonFormatter.Format(evt?.Reason);
            bool isPaused = evt is { IsPaused: true };
            return $"pause|isPaused={isPaused}|reason={reason}";
        }

        private static string BuildResumeKey(GameResumeRequestedEvent evt)
            => $"resume|reason={GameLoopReasonFormatter.Format(evt?.Reason)}";
    }
}
