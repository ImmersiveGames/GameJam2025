using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Core;

namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Run
{
    /// <summary>
    /// Produtor oficial do fim de run em produção.
    ///
    /// Regras:
    /// - Publica <see cref="GameRunEndedEvent"/> no máximo uma vez por run.
    /// - Um novo <see cref="GameRunStartedEvent"/> rearma o serviço para a próxima run.
    /// - Para evitar efeitos colaterais, o fim de run só é aceito quando o GameLoop está em Playing.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunOutcomeService : IGameRunOutcomeService, IDisposable
    {
        private readonly IGameRunPlayingStateGuard _playingStateGuard;
        private readonly GameLoopEventSubscriptionSet _subscriptions = new();
        private readonly EventBinding<GameRunStartedEvent> _runStartedBinding;
        private readonly EventBinding<GameRunEndedEvent> _runEndedObservedBinding;

        private bool _hasEndedThisRun;
        private bool _disposed;

        public bool HasEnded => _hasEndedThisRun;

        public GameRunOutcomeService(IGameRunPlayingStateGuard playingStateGuard)
        {
            _playingStateGuard = playingStateGuard;

            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnRunStarted);
            _runEndedObservedBinding = new EventBinding<GameRunEndedEvent>(OnRunEndedObserved);

            _subscriptions.Register(_runStartedBinding);
            _subscriptions.Register(_runEndedObservedBinding);

            DebugUtility.LogVerbose<GameRunOutcomeService>(
                "[GameLoop] GameRunOutcomeService registrado no EventBus<GameRunStartedEvent> e observando EventBus<GameRunEndedEvent>.");
        }

        public bool TryEnd(GameRunOutcome outcome, string reason = null)
        {
            if (_disposed)
            {
                return false;
            }

            if (outcome != GameRunOutcome.Victory && outcome != GameRunOutcome.Defeat)
            {
                DebugUtility.LogWarning<GameRunOutcomeService>(
                    $"[GameLoop] TryEnd ignorado: Outcome inválido/não terminal ({outcome}). Reason='{GameLoopReasonFormatter.Format(reason)}'.");
                return false;
            }

            if (!_playingStateGuard.IsInActiveGameplay(out string stateName))
            {
                DebugUtility.LogVerbose<GameRunOutcomeService>(
                    $"[GameLoop] TryEnd ignorado: GameLoop não está em Playing (state={stateName}). Outcome={outcome}, Reason='{GameLoopReasonFormatter.Format(reason)}'.");
                return false;
            }

            if (_hasEndedThisRun)
            {
                DebugUtility.LogVerbose<GameRunOutcomeService>(
                    $"[GameLoop] TryEnd suprimido: fim de run já publicado nesta run. Outcome={outcome}, Reason='{GameLoopReasonFormatter.Format(reason)}'.");
                return false;
            }

            _hasEndedThisRun = true;

            DebugUtility.Log<GameRunOutcomeService>(
                $"[GameLoop] Publicando GameRunEndedEvent. Outcome={outcome}, Reason='{GameLoopReasonFormatter.Format(reason)}'.");

            EventBus<GameRunEndedEvent>.Raise(new GameRunEndedEvent(outcome, reason));
            return true;
        }

        public bool RequestVictory(string reason = null) => TryEnd(GameRunOutcome.Victory, reason);

        public bool RequestDefeat(string reason = null) => TryEnd(GameRunOutcome.Defeat, reason);

        private void OnRunStarted(GameRunStartedEvent evt)
        {
            if (_disposed)
            {
                return;
            }

            _hasEndedThisRun = false;

            DebugUtility.LogVerbose<GameRunOutcomeService>(
                $"[GameLoop] GameRunStartedEvent observado -> rearmando GameRunOutcomeService. state={evt?.StateId}");
        }

        private void OnRunEndedObserved(GameRunEndedEvent evt)
        {
            if (_disposed || evt == null || _hasEndedThisRun)
            {
                return;
            }

            if (evt.Outcome != GameRunOutcome.Victory && evt.Outcome != GameRunOutcome.Defeat)
            {
                return;
            }

            if (!_playingStateGuard.IsInActiveGameplay(out _))
            {
                return;
            }

            _hasEndedThisRun = true;

            DebugUtility.LogVerbose<GameRunOutcomeService>(
                $"[GameLoop] GameRunEndedEvent observado externamente -> marcando HasEnded=true. Outcome={evt.Outcome}, Reason='{GameLoopReasonFormatter.Format(evt.Reason)}'.");
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
    }
}
