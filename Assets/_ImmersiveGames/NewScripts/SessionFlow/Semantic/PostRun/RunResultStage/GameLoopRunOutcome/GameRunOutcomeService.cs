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
    /// - O consumidor de request faz a deduplicacao e o rearmamento por run.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunOutcomeService : IGameRunOutcomeService, IDisposable
    {
        private readonly IGameRunEndRequestService _runEndRequestService;
        private readonly GameLoopEventSubscriptionSet _subscriptions = new();
        private readonly EventBinding<GameRunStartedEvent> _runStartedBinding;
        private readonly EventBinding<GameRunEndedEvent> _runEndedObservedBinding;

        private bool _hasEndedThisRun;
        private bool _disposed;

        public bool HasEnded => _hasEndedThisRun;

        public GameRunOutcomeService(IGameRunEndRequestService runEndRequestService)
        {
            _runEndRequestService = runEndRequestService ?? throw new ArgumentNullException(nameof(runEndRequestService));

            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnRunStarted);
            _runEndedObservedBinding = new EventBinding<GameRunEndedEvent>(OnRunEndedObserved);

            _subscriptions.Register(_runStartedBinding);
            _subscriptions.Register(_runEndedObservedBinding);

            DebugUtility.LogVerbose<GameRunOutcomeService>(
                "[OBS][RunPipeline][Outcome] GameRunOutcomeService registrado no EventBus<GameRunStartedEvent> e observando EventBus<GameRunEndedEvent>.");
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
                    $"[RunPipeline][Outcome] TryEnd ignorado: Outcome invalido/nao terminal ({outcome}). Reason='{GameLoopReasonFormatter.Format(reason)}'.");
                return false;
            }

            if (_hasEndedThisRun)
            {
                DebugUtility.LogVerbose<GameRunOutcomeService>(
                    $"[RunPipeline][Outcome] TryEnd suprimido: fim de run ja publicado nesta run. Outcome={outcome}, Reason='{GameLoopReasonFormatter.Format(reason)}'.");
                return false;
            }

            _hasEndedThisRun = true;

            _runEndRequestService.RequestRunEnd(outcome, reason);

            DebugUtility.Log<GameRunOutcomeService>(
                $"[OBS][RunPipeline][Outcome] GameRunEndAccepted outcome='{outcome}' reason='{GameLoopReasonFormatter.Format(reason)}' publish='GameRunEndedEvent' handshake='IGameRunEndRequestService'.");

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
                $"[OBS][RunPipeline][Outcome] GameRunStartedEvent observado -> rearmando GameRunOutcomeService. state={evt?.StateId}");
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

            _hasEndedThisRun = true;

            DebugUtility.LogVerbose<GameRunOutcomeService>(
                $"[OBS][RunPipeline][Outcome] GameRunEndedEvent observado externamente -> marcando HasEnded=true. Outcome={evt.Outcome}, Reason='{GameLoopReasonFormatter.Format(evt.Reason)}'.");
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

