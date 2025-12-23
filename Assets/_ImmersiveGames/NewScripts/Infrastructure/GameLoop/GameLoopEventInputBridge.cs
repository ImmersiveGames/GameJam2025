using System;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Infrastructure.GameLoop
{
    /// <summary>
    /// Bridge de eventos do GameLoop (EventBus -> IGameLoopService).
    ///
    /// Importante:
    /// - Quando o fluxo Opção B (GameLoopSceneFlowCoordinator) está ativo,
    ///   GameStartEvent é apenas "pedido" e NÃO deve chamar RequestStart() aqui.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopEventInputBridge : IDisposable
    {
        private readonly EventBinding<GameStartEvent> _onStart;
        private readonly EventBinding<GamePauseEvent> _onPause;
        private readonly EventBinding<GameResumeRequestedEvent> _onResume;
        private readonly EventBinding<GameResetRequestedEvent> _onResetRequested;

        public GameLoopEventInputBridge()
        {
            _onStart = new EventBinding<GameStartEvent>(OnGameStart);
            _onPause = new EventBinding<GamePauseEvent>(OnGamePause);
            _onResume = new EventBinding<GameResumeRequestedEvent>(OnGameResumeRequested);
            _onResetRequested = new EventBinding<GameResetRequestedEvent>(OnGameResetRequested);

            EventBus<GameStartEvent>.Register(_onStart);
            EventBus<GamePauseEvent>.Register(_onPause);
            EventBus<GameResumeRequestedEvent>.Register(_onResume);
            EventBus<GameResetRequestedEvent>.Register(_onResetRequested);

            DebugUtility.LogVerbose<GameLoopEventInputBridge>(
                "[GameLoop] Bridge de entrada registrado no EventBus.");
        }

        public void Dispose()
        {
            EventBus<GameStartEvent>.Unregister(_onStart);
            EventBus<GamePauseEvent>.Unregister(_onPause);
            EventBus<GameResumeRequestedEvent>.Unregister(_onResume);
            EventBus<GameResetRequestedEvent>.Unregister(_onResetRequested);
        }

        private static bool TryResolveLoop(out IGameLoopService loop)
        {
            loop = null;
            var provider = DependencyManager.Provider;
            return provider.TryGetGlobal<IGameLoopService>(out loop) && loop != null;
        }

        private void OnGameStart(GameStartEvent evt)
        {
            // Critério robusto: se o coordinator está instalado, o Bridge NÃO inicia.
            if (GameLoopSceneFlowCoordinator.IsInstalled)
            {
                DebugUtility.LogVerbose<GameLoopEventInputBridge>(
                    "[GameLoop] GameStartEvent ignorado pelo Bridge (GameLoopSceneFlowCoordinator.IsInstalled=true). " +
                    "Start será liberado no ScenesReady pelo coordinator.");
                return;
            }

            if (!TryResolveLoop(out var loop))
            {
                DebugUtility.LogError<GameLoopEventInputBridge>(
                    "[GameLoop] IGameLoopService não encontrado no DI global ao processar GameStartEvent.");
                return;
            }

            DebugUtility.Log<GameLoopEventInputBridge>(
                "[GameLoop] GameStartEvent recebido. Liberando GameLoop.RequestStart() (modo legado / sem coordinator).",
                DebugUtility.Colors.Info);

            loop.Initialize();
            loop.RequestStart();
        }

        private void OnGamePause(GamePauseEvent evt)
        {
            if (!TryResolveLoop(out var loop))
                return;

            if (evt != null && evt.IsPaused)
                loop.RequestPause();
            else
                loop.RequestResume();
        }

        private void OnGameResumeRequested(GameResumeRequestedEvent evt)
        {
            if (!TryResolveLoop(out var loop))
                return;

            loop.RequestResume();
        }

        private void OnGameResetRequested(GameResetRequestedEvent evt)
        {
            if (!TryResolveLoop(out var loop))
                return;

            loop.RequestReset();
        }
    }
}
