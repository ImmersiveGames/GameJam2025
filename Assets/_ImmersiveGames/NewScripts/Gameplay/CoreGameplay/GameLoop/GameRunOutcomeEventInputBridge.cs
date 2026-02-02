using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.GameLoop
{
    /// <summary>
    /// Bridge de entrada para requests de encerramento de run.
    ///
    /// Em produção, sistemas de gameplay devem publicar <see cref="GameRunEndRequestedEvent"/>.
    /// Este bridge converte o evento em chamadas ao <see cref="IGameRunOutcomeService"/>,
    /// mantendo a lógica de idempotência e de estado dentro do serviço.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunOutcomeEventInputBridge : IDisposable
    {
        private readonly IGameRunOutcomeService _outcome;
        private readonly EventBinding<GameRunEndRequestedEvent> _binding;
        private bool _disposed;

        public GameRunOutcomeEventInputBridge(IGameRunOutcomeService outcome)
        {
            _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));

            _binding = new EventBinding<GameRunEndRequestedEvent>(OnEndRequested);
            EventBus<GameRunEndRequestedEvent>.Register(_binding);

            DebugUtility.LogVerbose<GameRunOutcomeEventInputBridge>(
                "GameRunOutcomeEventInputBridge registered.");
        }

        private void OnEndRequested(GameRunEndRequestedEvent evt)
        {
            if (_disposed || evt == null)
            {
                return;
            }

            switch (evt.Outcome)
            {
                case GameRunOutcome.Victory:
                    _outcome.RequestVictory(evt.Reason);
                    break;

                case GameRunOutcome.Defeat:
                    _outcome.RequestDefeat(evt.Reason);
                    break;

                default:
                    DebugUtility.LogWarning<GameRunOutcomeEventInputBridge>(
                        $"Ignored GameRunEndRequestedEvent with Outcome={evt.Outcome}. Reason='{evt.Reason}'.");
                    break;
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try { EventBus<GameRunEndRequestedEvent>.Unregister(_binding); } catch { /* best-effort */ }

            DebugUtility.LogVerbose<GameRunOutcomeEventInputBridge>(
                "GameRunOutcomeEventInputBridge disposed.");
        }
    }
}
