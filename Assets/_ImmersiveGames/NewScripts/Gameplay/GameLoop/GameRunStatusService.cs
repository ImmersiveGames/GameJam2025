using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Serviço simples que mantém o resultado da última run do jogo.
    /// Ouve GameRunEndedEvent e expõe Outcome/Reason para UI e outros sistemas.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunStatusService : IGameRunStatusService, IDisposable
    {
        private readonly EventBinding<GameRunEndedEvent> _binding;
        private readonly EventBinding<GameRunStartedEvent> _startBinding;

        public bool HasResult { get; private set; }
        public GameRunOutcome Outcome { get; private set; } = GameRunOutcome.Unknown;
        public string Reason { get; private set; }

        public GameRunStatusService()
        {
            _binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            _startBinding = new EventBinding<GameRunStartedEvent>(OnGameRunStarted);
            EventBus<GameRunEndedEvent>.Register(_binding);
            EventBus<GameRunStartedEvent>.Register(_startBinding);

            DebugUtility.LogVerbose<GameRunStatusService>(
                "[GameLoop] GameRunStatusService registrado no EventBus<GameRunEndedEvent> e GameRunStartedEvent>.");
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
