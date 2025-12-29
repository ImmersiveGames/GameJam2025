using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Serviço simples que mantém o resultado da última run do jogo.
    /// Ouve GameRunEndedEvent e GameRunStartedEvent e expõe Outcome/Reason para UI e outros sistemas.
    /// Agora também integra com o GameLoop para encerrar a run (RequestEnd).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunStatusService : IGameRunStatusService, IDisposable
    {
        private readonly EventBinding<GameRunEndedEvent> _binding;
        private readonly EventBinding<GameRunStartedEvent> _startBinding;
        private readonly IGameLoopService _gameLoopService;

        public bool HasResult { get; private set; }
        public GameRunOutcome Outcome { get; private set; } = GameRunOutcome.Unknown;
        public string Reason { get; private set; }

        public GameRunStatusService(IGameLoopService gameLoopService)
        {
            _gameLoopService = gameLoopService;
            _binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            _startBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);

            EventBus<GameRunEndedEvent>.Register(_binding);
            EventBus<GameRunStartedEvent>.Register(_startBinding);

            DebugUtility.LogVerbose<GameRunStatusService>(
                "[GameLoop] GameRunStatusService registrado no EventBus<GameRunEndedEvent> e EventBus<GameRunStartedEvent>.");
        }

        public void Clear()
        {
            HasResult = false;
            Outcome = GameRunOutcome.Unknown;
            Reason = null;

            DebugUtility.LogVerbose<GameRunStatusService>(
                "[GameLoop] GameRunStatusService.Clear() chamado. Resultado resetado para Unknown.");
        }

        private void OnGameRunEnded(GameRunEndedEvent evt)
        {
            HasResult = true;
            Outcome = evt?.Outcome ?? GameRunOutcome.Unknown;
            Reason = evt?.Reason;

            DebugUtility.Log<GameRunStatusService>(
                $"[GameLoop] GameRunStatus atualizado. Outcome={Outcome}, Reason='{Reason ?? "<null>"}'.");

            // Integração com GameLoop:
            // Fim de run (Victory/Defeat) deve tirar o loop do estado Playing
            // para que IStateDependentService bloqueie ações de gameplay.
            if (_gameLoopService == null)
            {
                DebugUtility.LogWarning<GameRunStatusService>(
                    "[GameLoop] GameLoopService indisponível ao processar GameRunEndedEvent. RequestEnd() não foi chamado.");
                return;
            }

            // Evita chamadas redundantes quando já estamos fora do gameplay.
            // (Interface atual não expõe enum, então usamos o nome como melhor esforço.)
            var stateName = _gameLoopService.CurrentStateIdName ?? string.Empty;
            var shouldRequestEnd =
                stateName == nameof(GameLoopStateId.Playing) ||
                stateName == nameof(GameLoopStateId.Paused);

            if (!shouldRequestEnd)
            {
                DebugUtility.LogVerbose<GameRunStatusService>(
                    $"[GameLoop] GameRunEndedEvent recebido, mas GameLoop já está em '{stateName}'. RequestEnd() ignorado.");
                return;
            }

            DebugUtility.LogVerbose<GameRunStatusService>(
                "[GameLoop] GameRunEndedEvent recebido -> solicitando GameLoop.RequestEnd().");

            _gameLoopService.RequestEnd();
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            Clear();

            DebugUtility.LogVerbose<GameRunStatusService>(
                $"[GameLoop] GameRunStatusService: nova run iniciada (state={evt?.StateId}). Resultado anterior resetado.");
        }

        public void Dispose()
        {
            EventBus<GameRunEndedEvent>.Unregister(_binding);
            EventBus<GameRunStartedEvent>.Unregister(_startBinding);
        }
    }
}
