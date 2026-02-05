using _ImmersiveGames.NewScripts.Modules.Gameplay.Actions;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime
{
    public sealed class GameLoopStateMachine
    {
        private readonly IGameLoopSignals _signals;
        private readonly IGameLoopStateObserver _observer;

        public GameLoopStateId Current { get; private set; } = GameLoopStateId.Boot;
        public bool IsGameActive => Current == GameLoopStateId.Playing;

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
            // Executa no máximo uma transição por tick.
            // StartRequested pode permanecer true até o fim do Tick; evitar encadear Boot -> Ready -> Playing no mesmo frame.
            TryTransitionOnce();
        }

        private bool TryTransitionOnce()
        {
            // Reset tem prioridade máxima (determinístico).
            // Importante (contrato): Reset/Reinício volta ao Boot para reiniciar a run por etapas.
            if (_signals.ResetRequested)
            {
                return TransitionTo(GameLoopStateId.Boot);
            }

            // ReadyRequested tem prioridade alta APENAS em estados não-ativos.
            // Evita capturar reset/restart durante transições em gameplay.
            if (_signals.ReadyRequested)
            {
                if (Current is GameLoopStateId.Boot
                    or GameLoopStateId.Paused
                    or GameLoopStateId.PostPlay
                    or GameLoopStateId.IntroStage)
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
                    if (_signals.IntroStageRequested)
                    {
                        next = GameLoopStateId.IntroStage;
                    }
                    else if (_signals.StartRequested)
                    {
                        next = GameLoopStateId.Playing;
                    }
                    break;

                case GameLoopStateId.IntroStage:
                    if (_signals.StartRequested && _signals.IntroStageCompleted)
                    {
                        next = GameLoopStateId.Playing;
                    }
                    break;

                case GameLoopStateId.Playing:
                    if (_signals.EndRequested)
                    {
                        next = GameLoopStateId.PostPlay;
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

                case GameLoopStateId.PostPlay:
                    // Mantém PostPlay como estado pós-run.
                    // Permite um caminho simples “Play Again” sem exigir Reset duro.
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
        /// - Este método NÃO é gate-aware (não consulta SimulationGate/Readiness).
        /// - Não deve ser usado como autorização final de gameplay.
        /// - A decisão final deve ocorrer em IStateDependentService (gate-aware).
        /// </summary>
        public bool IsGameplayActionAllowedByLoopState(GameplayAction action)
            => Current == GameLoopStateId.Playing;

        public bool IsUiActionAllowedByLoopState(UiAction action) => Current switch
        {
            GameLoopStateId.Boot => true,
            GameLoopStateId.Ready => true,
            GameLoopStateId.IntroStage => true,
            GameLoopStateId.Paused => true,
            GameLoopStateId.PostPlay => true,
            _ => false
        };

        public bool IsSystemActionAllowedByLoopState(SystemAction action) => Current switch
        {
            GameLoopStateId.Boot => true,
            GameLoopStateId.Ready => true,
            GameLoopStateId.IntroStage => true,
            GameLoopStateId.Paused => true,
            GameLoopStateId.PostPlay => true,
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

