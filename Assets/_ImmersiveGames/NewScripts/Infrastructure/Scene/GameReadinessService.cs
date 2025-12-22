using System;
using _ImmersiveGames.Scripts.SceneManagement.Transition;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Execution.Gate;

namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Orquestra readiness do jogo em resposta ao Scene Flow.
    /// Bloqueia simulação durante transições de cena usando SimulationGateService
    /// e emite logs verbosos para QA manual.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameReadinessService
    {
        private readonly ISimulationGateService _gateService;

        private IDisposable _activeGateHandle;
        private bool _gameplayReady;

        public GameReadinessService(ISimulationGateService gateService)
        {
            _gateService = gateService;

            var transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnSceneTransitionStarted);
            var transitionScenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnSceneTransitionScenesReady);
            var transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);

            EventBus<SceneTransitionStartedEvent>.Register(transitionStartedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Register(transitionScenesReadyBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(transitionCompletedBinding);

            DebugUtility.LogVerbose<GameReadinessService>("[Readiness] GameReadinessService registrado nos eventos de Scene Flow.");
        }

        public bool IsGameplayReady => _gameplayReady;

        private void OnSceneTransitionStarted(SceneTransitionStartedEvent evt)
        {
            _gameplayReady = false;
            AcquireGate();

            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context={evt.Context}");
        }

        private void OnSceneTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SceneTransitionScenesReady → fase WorldLoaded sinalizada. Context={evt.Context}");
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            ReleaseGateHandle();
            _gameplayReady = true;

            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. Context={evt.Context}");
        }

        private void AcquireGate()
        {
            ReleaseGateHandle();

            if (_gateService == null)
            {
                DebugUtility.LogError<GameReadinessService>(
                    "[Readiness] ISimulationGateService indisponível. Não foi possível bloquear a simulação durante a transição.");
                return;
            }

            _activeGateHandle = _gateService.Acquire(SimulationGateTokens.SceneTransition);
            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SimulationGate adquirido com token='{SimulationGateTokens.SceneTransition}'. Active={_gateService.ActiveTokenCount}. IsOpen={_gateService.IsOpen}");
        }

        private void ReleaseGateHandle()
        {
            if (_activeGateHandle == null)
            {
                return;
            }

            try
            {
                _activeGateHandle.Dispose();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<GameReadinessService>($"[Readiness] Erro ao liberar SimulationGate: {ex}");
            }
            finally
            {
                _activeGateHandle = null;
            }
        }
    }
}
