using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.GameLoop
{
    /// <summary>
    /// Serviço simples que mantém o resultado da última run do jogo.
    /// Ouve GameRunEndedEvent e GameRunStartedEvent e expõe Outcome/Reason para UI e outros sistemas.
    ///
    /// Atualização:
    /// - Victory/Defeat entram em PostGame sem usar PauseOverlay.
    /// - O congelamento do gameplay ocorre via GameLoop (PostPlay) + StateDependentService,
    ///   evitando o toggle de PauseOverlay/InputMode.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunStatusService : IGameRunStatusService, IDisposable
    {
        private readonly EventBinding<GameRunEndedEvent> _binding;
        private readonly EventBinding<GameRunStartedEvent> _startBinding;
        private readonly IGameLoopService _gameLoopService;

        // Evita log ruidoso no primeiro Start do ciclo (boot → first playing).
        private bool _hasEverStarted;
        private bool _disposed;

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
            if (_disposed || evt == null)
            {
                return;
            }

            // Idempotência: se já temos resultado, ignore eventos duplicados.
            if (HasResult)
            {
                DebugUtility.LogVerbose<GameRunStatusService>(
                    $"[GameLoop] GameRunEndedEvent duplicado ignorado. CurrentOutcome={Outcome}, IncomingOutcome={evt.Outcome}, IncomingReason='{evt.Reason ?? "<null>"}'.");
                return;
            }

            HasResult = true;
            Outcome = evt.Outcome;
            Reason = evt.Reason;

            DebugUtility.Log<GameRunStatusService>(
                $"[GameLoop] GameRunStatus atualizado. Outcome={Outcome}, Reason='{Reason ?? "<null>"}'.");

            if (_gameLoopService == null)
            {
                DebugUtility.LogWarning<GameRunStatusService>(
                    "[GameLoop] GameLoopService indisponível ao processar GameRunEndedEvent. PostGame seguirá sem gate de pausa.");
                return;
            }

            if (!IsInActiveGameplay(out string stateName))
            {
                DebugUtility.LogVerbose<GameRunStatusService>(
                    $"[GameLoop] GameRunEndedEvent recebido, mas GameLoop j\u00e1 est\u00e1 em '{stateName}'. PostGame segue sem pausa.");
                return;
            }

            if (!ShouldEnterPostGameWithoutPause(Outcome))
            {
                DebugUtility.LogVerbose<GameRunStatusService>(
                    $"[GameLoop] GameRunEndedEvent com Outcome={Outcome} não é terminal. PostGame não será acionado.");
                return;
            }

            DebugUtility.LogVerbose<GameRunStatusService>(
                $"[GameLoop] GameRunEndedEvent (Outcome={Outcome}) -> PostGame sem PauseOverlay (pausa suprimida).");
        }

        private static bool ShouldEnterPostGameWithoutPause(GameRunOutcome outcome)
        {
            return outcome == GameRunOutcome.Victory || outcome == GameRunOutcome.Defeat;
        }

        private void OnGameRunStarted(GameRunStartedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

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

        private bool IsInActiveGameplay(out string stateName)
        {
            stateName = _gameLoopService?.CurrentStateIdName ?? string.Empty;
            return string.Equals(stateName, nameof(GameLoopStateId.Playing), StringComparison.Ordinal);
        }


        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try { EventBus<GameRunEndedEvent>.Unregister(_binding); } catch { /* best-effort */ }
            try { EventBus<GameRunStartedEvent>.Unregister(_startBinding); } catch { /* best-effort */ }
        }
    }
}
