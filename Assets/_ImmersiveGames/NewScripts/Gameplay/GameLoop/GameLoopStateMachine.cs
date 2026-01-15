using _ImmersiveGames.NewScripts.Infrastructure.Actions;
using _ImmersiveGames.NewScripts.Infrastructure.Fsm;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
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
            if (_signals.ResetRequested)
            {
                return TransitionTo(GameLoopStateId.Boot);
            }

            // ReadyRequested tem prioridade alta. Se isso NÃO for desejado globalmente,
            // restrinja por estado (ex.: apenas Boot/PostPlay/Paused) e remova a prioridade global.
            if (_signals.ReadyRequested)
            {
                // Variante restrita (descomente se quiser limitar):
                // if (Current is GameLoopStateId.Boot or GameLoopStateId.Paused or GameLoopStateId.PostPlay)
                //     return TransitionTo(GameLoopStateId.Ready);

                return TransitionTo(GameLoopStateId.Ready);
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
        public bool IsActionAllowedByLoopState(ActionType action) => Current switch
        {
            GameLoopStateId.Boot =>
                action is ActionType.Navigate or ActionType.UiSubmit or ActionType.UiCancel,

            GameLoopStateId.Ready =>
                action is ActionType.Navigate or ActionType.UiSubmit or ActionType.UiCancel or ActionType.RequestReset or ActionType.RequestQuit,

            GameLoopStateId.IntroStage =>
                action is ActionType.Navigate or ActionType.UiSubmit or ActionType.UiCancel or ActionType.RequestReset or ActionType.RequestQuit,

            GameLoopStateId.Playing =>
                action is ActionType.Move or ActionType.Shoot or ActionType.Interact or ActionType.Spawn,

            GameLoopStateId.Paused =>
                action is ActionType.Navigate or ActionType.UiSubmit or ActionType.UiCancel or ActionType.RequestReset or ActionType.RequestQuit,

            GameLoopStateId.PostPlay =>
                action is ActionType.Navigate or ActionType.UiSubmit or ActionType.UiCancel or ActionType.RequestReset or ActionType.RequestQuit,

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
