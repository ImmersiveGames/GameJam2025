using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Produtor oficial do fim de run em produção.
    ///
    /// Regras:
    /// - Publica <see cref="GameRunEndedEvent"/> no máximo uma vez por run.
    /// - Um novo <see cref="GameRunStartedEvent"/> rearma o serviço para a próxima run.
    /// - Para evitar efeitos colaterais (pause/estado), o fim de run só é aceito quando o GameLoop está em Playing.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameRunOutcomeService : IGameRunOutcomeService, IDisposable
    {
        private readonly IGameLoopService _gameLoopService;
        private readonly EventBinding<GameRunStartedEvent> _runStartedBinding;

        // Mantém consistência caso algum QA/bridge publique o evento diretamente.
        private readonly EventBinding<GameRunEndedEvent> _runEndedObservedBinding;

        private bool _hasEndedThisRun;

        public bool HasEnded => _hasEndedThisRun;

        public GameRunOutcomeService(IGameLoopService gameLoopService)
        {
            _gameLoopService = gameLoopService;

            _runStartedBinding = new EventBinding<GameRunStartedEvent>(OnRunStarted);
            _runEndedObservedBinding = new EventBinding<GameRunEndedEvent>(OnRunEndedObserved);

            EventBus<GameRunStartedEvent>.Register(_runStartedBinding);
            EventBus<GameRunEndedEvent>.Register(_runEndedObservedBinding);

            DebugUtility.LogVerbose<GameRunOutcomeService>(
                "[GameLoop] GameRunOutcomeService registrado no EventBus<GameRunStartedEvent> e observando EventBus<GameRunEndedEvent>.");
        }

        public bool TryEnd(GameRunOutcome outcome, string reason = null)
        {
            if (outcome != GameRunOutcome.Victory && outcome != GameRunOutcome.Defeat)
            {
                DebugUtility.LogWarning<GameRunOutcomeService>(
                    $"[GameLoop] TryEnd ignorado: Outcome inválido/nao terminal ({outcome}). Reason='{reason ?? "<null>"}'.");
                return false;
            }

            if (!IsInActiveGameplay())
            {
                var stateName = _gameLoopService?.CurrentStateIdName ?? "<null>";

                DebugUtility.LogVerbose<GameRunOutcomeService>(
                    $"[GameLoop] TryEnd ignorado: GameLoop não está em Playing (state={stateName}). Outcome={outcome}, Reason='{reason ?? "<null>"}'.");
                return false;
            }

            if (_hasEndedThisRun)
            {
                DebugUtility.LogVerbose<GameRunOutcomeService>(
                    $"[GameLoop] TryEnd suprimido: fim de run já publicado nesta run. Outcome={outcome}, Reason='{reason ?? "<null>"}'.");
                return false;
            }

            _hasEndedThisRun = true;

            DebugUtility.Log<GameRunOutcomeService>(
                $"[GameLoop] Publicando GameRunEndedEvent. Outcome={outcome}, Reason='{reason ?? "<null>"}'.");

            EventBus<GameRunEndedEvent>.Raise(new GameRunEndedEvent(outcome, reason));
            return true;
        }

        public bool RequestVictory(string reason = null) => TryEnd(GameRunOutcome.Victory, reason);

        public bool RequestDefeat(string reason = null) => TryEnd(GameRunOutcome.Defeat, reason);

        private void OnRunStarted(GameRunStartedEvent evt)
        {
            // Re-arma para nova run.
            _hasEndedThisRun = false;

            DebugUtility.LogVerbose<GameRunOutcomeService>(
                $"[GameLoop] GameRunStartedEvent observado -> rearmando GameRunOutcomeService. state={evt?.StateId}");
        }

        private void OnRunEndedObserved(GameRunEndedEvent evt)
        {
            // Se alguém publicou diretamente (QA), marque como encerrado para manter idempotência.
            if (_hasEndedThisRun)
            {
                return;
            }

            // Não forçar estado quando não estamos em gameplay ativo.
            // Ex.: casos de testes ou chamadas prematuras não devem bloquear a run real.
            if (!IsInActiveGameplay())
            {
                return;
            }

            var outcome = evt?.Outcome ?? GameRunOutcome.Unknown;
            if (outcome != GameRunOutcome.Victory && outcome != GameRunOutcome.Defeat)
            {
                return;
            }

            _hasEndedThisRun = true;

            DebugUtility.LogVerbose<GameRunOutcomeService>(
                $"[GameLoop] GameRunEndedEvent observado externamente -> marcando HasEnded=true. Outcome={outcome}, Reason='{evt?.Reason ?? "<null>"}'.");
        }

        private bool IsInActiveGameplay()
        {
            // Dependência opcional: se indisponível, falhe seguro (não publicar fim de run).
            if (_gameLoopService == null)
            {
                DebugUtility.LogWarning<GameRunOutcomeService>(
                    "[GameLoop] IGameLoopService indisponível; GameRunOutcomeService não pode validar estado (Playing)." +
                    " Fim de run foi ignorado para evitar transições fora de contexto.");
                return false;
            }

            var stateName = _gameLoopService.CurrentStateIdName ?? string.Empty;
            return stateName == nameof(GameLoopStateId.Playing);
        }

        public void Dispose()
        {
            EventBus<GameRunStartedEvent>.Unregister(_runStartedBinding);
            EventBus<GameRunEndedEvent>.Unregister(_runEndedObservedBinding);
        }
    }
}
