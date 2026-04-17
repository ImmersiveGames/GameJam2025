using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionFlow.GameLoop.RunLifecycle.Core;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.PostRun.RunResultStage.GameLoopRunOutcome
{
    /// <summary>
    /// Produtor operacional do fim de run em producao.
    ///
    /// Regras:
    /// - Publica <see cref="GameRunEndedEvent"/> no maximo uma vez por run.
    /// - Um novo <see cref="GameRunStartedEvent"/> rearma o servico para a proxima run.
    /// - Para evitar efeitos colaterais, o fim de run so e aceito quando o GameLoop esta em Playing.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunOutcomeService : IGameRunOutcomeService, IDisposable
    {
        private readonly IGameRunPlayingStateGuard _playingStateGuard;
        private readonly IGameLoopService _gameLoopService;
        private readonly GameLoopEventSubscriptionSet _subscriptions = new();
        private readonly EventBinding<GameRunStartedEvent> _runStartedBinding;
        private readonly EventBinding<GameRunEndedEvent> _runEndedObservedBinding;

        private bool _hasEndedThisRun;
        private bool _disposed;

        public bool HasEnded => _hasEndedThisRun;

        public GameRunOutcomeService(IGameRunPlayingStateGuard playingStateGuard, IGameLoopService gameLoopService)
        {
            _playingStateGuard = playingStateGuard ?? throw new ArgumentNullException(nameof(playingStateGuard));
            _gameLoopService = gameLoopService ?? throw new ArgumentNullException(nameof(gameLoopService));

            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnRunStarted);
            _runEndedObservedBinding = new EventBinding<GameRunEndedEvent>(OnRunEndedObserved);

            _subscriptions.Register(_runStartedBinding);
            _subscriptions.Register(_runEndedObservedBinding);

            DebugUtility.LogVerbose<GameRunOutcomeService>(
                "[OBS][GameLoop][Operational] GameRunOutcomeService registrado no EventBus<GameRunStartedEvent> e observando EventBus<GameRunEndedEvent>.");
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
                    $"[GameLoop][Operational] TryEnd ignorado: Outcome invalido/nao terminal ({outcome}). Reason='{GameLoopReasonFormatter.Format(reason)}'.");
                return false;
            }

            if (!_playingStateGuard.IsInActiveGameplay(out string stateName))
            {
                DebugUtility.LogVerbose<GameRunOutcomeService>(
                    $"[GameLoop][Operational] TryEnd ignorado: GameLoop nao esta em Playing (state={stateName}). Outcome={outcome}, Reason='{GameLoopReasonFormatter.Format(reason)}'.");
                return false;
            }

            if (_hasEndedThisRun)
            {
                DebugUtility.LogVerbose<GameRunOutcomeService>(
                    $"[GameLoop][Operational] TryEnd suprimido: fim de run ja publicado nesta run. Outcome={outcome}, Reason='{GameLoopReasonFormatter.Format(reason)}'.");
                return false;
            }

            _hasEndedThisRun = true;

            _gameLoopService.RequestRunEnd();

            DebugUtility.Log<GameRunOutcomeService>(
                $"[OBS][GameLoop][Operational] GameRunEndAccepted state='{stateName}' outcome='{outcome}' reason='{GameLoopReasonFormatter.Format(reason)}' publish='GameRunEndedEvent' handshake='GameLoop.RequestRunEnd'.");

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
                $"[OBS][GameLoop][Operational] GameRunStartedEvent observado -> rearmando GameRunOutcomeService. state={evt?.StateId}");
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
                $"[OBS][GameLoop][Operational] GameRunEndedEvent observado externamente -> marcando HasEnded=true. Outcome={evt.Outcome}, Reason='{GameLoopReasonFormatter.Format(evt.Reason)}'.");
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

