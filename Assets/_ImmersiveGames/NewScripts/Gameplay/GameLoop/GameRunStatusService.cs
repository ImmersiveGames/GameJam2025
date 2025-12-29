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

        public bool HasResult { get; private set; }
        public GameRunOutcome Outcome { get; private set; } = GameRunOutcome.Unknown;
        public string Reason { get; private set; }

        public GameRunStatusService()
        {
            _binding = new EventBinding<GameRunEndedEvent>(OnGameRunEnded);
            EventBus<GameRunEndedEvent>.Register(_binding);

            DebugUtility.LogVerbose<GameRunStatusService>(
                "[GameLoop] GameRunStatusService registrado no EventBus<GameRunEndedEvent>.");
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

        public void Dispose()
        {
            EventBus<GameRunEndedEvent>.Unregister(_binding);
        }
    }
}
