using _ImmersiveGames.NewScripts.Game.Gameplay.State.Core;
namespace _ImmersiveGames.NewScripts.Orchestration.GameLoop.RunLifecycle.Core
{
    public sealed class GameLoopStateMachine
    {
        private readonly IGameLoopSignals _signals;
        private readonly IGameLoopStateObserver _observer;

        public GameLoopStateId Current { get; private set; } = GameLoopStateId.Boot;
        public bool IsGameActive => Current.IsActiveGameplayState();

        private bool _lastIsGameActive;

        public GameLoopStateMachine(IGameLoopSignals signals, IGameLoopStateObserver observer = null)
        {
            _signals = signals;
            _observer = observer;

            _lastIsGameActive = IsGameActive;
            NotifyEnter(Current, IsGameActive);
        }

        public void Update()
        {
            // Executa no maximo uma transicao por tick.
            // StartRequested pode permanecer true ate o fim do Tick; evitar encadear Boot -> Ready -> Playing no mesmo frame.
            TryTransitionOnce();
        }

        private bool TryTransitionOnce()
        {
            // Reset tem prioridade maxima (deterministico).
            // Importante (contrato): Reset/Reinicio volta ao Boot para reiniciar a run por etapas.
            if (_signals.ResetRequested)
            {
                return TransitionTo(GameLoopStateId.Boot);
            }

            // ReadyRequested tem prioridade alta APENAS em estados nao-ativos.
            // Evita capturar reset/restart durante transicoes em gameplay.
            if (_signals.ReadyRequested)
            {
                if (Current.IsPreGameplayState()
                    || Current.IsPausedState()
                    || Current.IsTerminalRunState())
                {
                    return TransitionTo(GameLoopStateId.Ready);
                }
            }

            var next = Current;

            switch (Current)
            {
                case GameLoopStateId.Boot:
                    if (_signals.StartRequested)
                    {
                        next = GameLoopStateId.Ready;
                    }
                    break;

                case GameLoopStateId.Ready:
                    if (_signals.StartRequested)
                    {
                        next = GameLoopStateId.Playing;
                    }
                    break;

                case GameLoopStateId.Playing:
                    if (_signals.EndRequested)
                    {
                        next = GameLoopStateId.RunEnded;
                    }
                    else if (_signals.PauseRequested)
                    {
                        next = GameLoopStateId.Paused;
                    }
                    break;

                case GameLoopStateId.Paused:
                    if (_signals.ResumeRequested)
                    {
                        next = GameLoopStateId.Playing;
                    }
                    break;

                case GameLoopStateId.RunEnded:
                    // Mantem RunEnded como estado terminal interno do GameLoop.
                    if (_signals.StartRequested)
                    {
                        next = GameLoopStateId.Ready;
                    }
                    break;
            }

            if (next == Current)
            {
                return false;
            }

            return TransitionTo(next);
        }

        private bool TransitionTo(GameLoopStateId next)
        {
            if (next == Current)
            {
                return false;
            }

            NotifyExit(Current);
            Current = next;
            NotifyEnter(Current, IsGameActive);
            return true;
        }

        /// <summary>
        /// Capability map por estado macro do GameLoop.
        ///
        /// Importante:
        /// - Este metodo NAO e gate-aware (nao consulta SimulationGate/Readiness).
        /// - Nao deve ser usado como autorizacao final de gameplay.
        /// - A decisao final deve ocorrer em IGameplayStateGate (gate-aware).
        /// </summary>
        public bool IsGameplayActionAllowedByLoopState(GameplayAction action)
            => Current.IsActiveGameplayState();

        public bool IsUiActionAllowedByLoopState(UiAction action) => Current switch
        {
            _ when Current.IsPreGameplayState() => true,
            _ when Current.IsPausedState() => true,
            _ => false
        };

        public bool IsSystemActionAllowedByLoopState(SystemAction action) => Current switch
        {
            _ when Current.IsPreGameplayState() => true,
            _ when Current.IsPausedState() => true,
            _ => false
        };

        private void NotifyEnter(GameLoopStateId id, bool active)
        {
            _observer?.OnStateEntered(id, active);

            if (active != _lastIsGameActive)
            {
                _lastIsGameActive = active;
                _observer?.OnGameActivityChanged(active);
            }
        }

        private void NotifyExit(GameLoopStateId id) => _observer?.OnStateExited(id);
    }
}
