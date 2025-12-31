using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Serviço simples que mantém o resultado da última run do jogo.
    /// Ouve GameRunEndedEvent e GameRunStartedEvent e expõe Outcome/Reason para UI e outros sistemas.
    ///
    /// Atualização:
    /// - Victory/Defeat devem pausar a simulação: ao receber GameRunEndedEvent, publica GamePauseCommandEvent(true).
    /// - O encerramento do "estado ativo" fica a cargo do GameLoop reagir ao GamePauseCommandEvent
    ///   (ex.: transicionar para Paused). Isso garante gate fechado e simulação congelada.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunStatusService : IGameRunStatusService, IDisposable
    {
        private readonly EventBinding<GameRunEndedEvent> _binding;
        private readonly EventBinding<GameRunStartedEvent> _startBinding;
        private readonly IGameLoopService _gameLoopService;

        // Evita log ruidoso no primeiro Start do ciclo (boot → first playing).
        private bool _hasEverStarted;

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

            // Victory/Defeat devem pausar a simulação (gate fechado via PauseGateBridge).
            // Evita pausar se já não estamos em gameplay ativo.
            if (_gameLoopService == null)
            {
                DebugUtility.LogWarning<GameRunStatusService>(
                    "[GameLoop] GameLoopService indisponível ao processar GameRunEndedEvent. Pausa não foi solicitada.");
                return;
            }

            var stateName = _gameLoopService.CurrentStateIdName ?? string.Empty;
            var isInActiveGameplay =
                stateName == nameof(GameLoopStateId.Playing);

            if (!isInActiveGameplay)
            {
                DebugUtility.LogVerbose<GameRunStatusService>(
                    $"[GameLoop] GameRunEndedEvent recebido, mas GameLoop já está em '{stateName}'. Pausa ignorada.");
                return;
            }

            // Só faz sentido pausar quando há um resultado terminal.
            if (Outcome != GameRunOutcome.Victory && Outcome != GameRunOutcome.Defeat)
            {
                DebugUtility.LogVerbose<GameRunStatusService>(
                    $"[GameLoop] GameRunEndedEvent com Outcome={Outcome} não é terminal. Pausa ignorada.");
                return;
            }

            DebugUtility.LogVerbose<GameRunStatusService>(
                $"[GameLoop] GameRunEndedEvent (Outcome={Outcome}) -> publicando GamePauseCommandEvent(true) para congelar simulação.");

            EventBus<GamePauseCommandEvent>.Raise(new GamePauseCommandEvent(true));
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            // Primeiro start do ciclo: não gerar log ruidoso de "resume/duplicado".
            if (!_hasEverStarted)
            {
                _hasEverStarted = true;

                // Se por qualquer razão vier sujo (ex.: domínio recarregado, serviços reaproveitados), limpa.
                if (HasResult || Outcome != GameRunOutcome.Unknown || !string.IsNullOrEmpty(Reason))
                {
                    Clear();
                }

                DebugUtility.LogVerbose<GameRunStatusService>(
                    $"[GameLoop] GameRunStartedEvent inicial observado (state={evt?.StateId}).");
                return;
            }

            // Após o primeiro start, se já estamos limpos (sem resultado),
            // trate como start duplicado/resume e não limpe.
            if (!HasResult && Outcome == GameRunOutcome.Unknown && string.IsNullOrEmpty(Reason))
            {
                DebugUtility.LogVerbose<GameRunStatusService>(
                    $"[GameLoop] GameRunStartedEvent suprimido (estado já limpo). Provável Resume/duplicado. state={evt?.StateId}");
                return;
            }

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
