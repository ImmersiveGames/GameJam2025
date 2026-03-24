using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Run
{
    /// <summary>
    /// Bridge de entrada para requests de encerramento de run.
    ///
    /// Em produção, sistemas de gameplay devem publicar <see cref="GameRunEndRequestedEvent"/>.
    /// Este bridge converte o evento em chamadas ao <see cref="IGameRunOutcomeService"/>,
    /// mantendo a lógica de idempotência e de estado dentro do serviço.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunOutcomeRequestBridge : IDisposable
    {
        private readonly IGameRunOutcomeService _outcome;
        private readonly GameLoopEventSubscriptionSet _subscriptions = new();
        private readonly EventBinding<GameRunEndRequestedEvent> _binding;
        private bool _disposed;

        public GameRunOutcomeRequestBridge(IGameRunOutcomeService outcome)
        {
            _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));

            _binding = new EventBinding<GameRunEndRequestedEvent>(OnEndRequested);
            _subscriptions.Register(_binding);

            DebugUtility.LogVerbose<GameRunOutcomeRequestBridge>(
                "GameRunOutcomeRequestBridge registered.");
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
                    DebugUtility.LogWarning<GameRunOutcomeRequestBridge>(
                        $"Ignored GameRunEndRequestedEvent with Outcome={evt.Outcome}. Reason='{GameLoopReasonFormatter.Format(evt.Reason)}'.");
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
            _subscriptions.Dispose();

            DebugUtility.LogVerbose<GameRunOutcomeRequestBridge>(
                "GameRunOutcomeRequestBridge disposed.");
        }
    }
}
