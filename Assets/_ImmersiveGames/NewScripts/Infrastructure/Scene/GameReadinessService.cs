using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;

namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Orquestra readiness do jogo em resposta ao Scene Flow.
    /// Bloqueia simulação durante transições de cena usando ISimulationGateService
    /// e emite snapshots de readiness via EventBus para consumidores (ex.: StateDependentService).
    ///
    /// Nota: baseline/QA pode não disparar Scene Flow; para isso existe SetGameplayReady(...) para sinalização manual.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameReadinessService : IDisposable
    {
        private readonly ISimulationGateService _gateService;

        private IDisposable _activeGateHandle;
        private bool _gameplayReady;

        private readonly EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _transitionScenesReadyBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;

        private bool _disposed;

        public GameReadinessService(ISimulationGateService gateService)
        {
            _gateService = gateService;

            _transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnSceneTransitionStarted);
            _transitionScenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnSceneTransitionScenesReady);
            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnSceneTransitionCompleted);

            EventBus<SceneTransitionStartedEvent>.Register(_transitionStartedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_transitionScenesReadyBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);

            if (_gateService != null)
            {
                _gateService.GateChanged += OnGateChanged;
            }

            DebugUtility.LogVerbose<GameReadinessService>("[Readiness] GameReadinessService registrado nos eventos de Scene Flow.");

            // Snapshot inicial (útil em bootstrap/QA).
            PublishSnapshot("bootstrap");
        }

        public bool IsGameplayReady => _gameplayReady;

        /// <summary>
        /// API explícita para QA/baseline quando não há Scene Flow.
        /// </summary>
        public void SetGameplayReady(bool gameplayReady, string reason)
        {
            if (_disposed)
            {
                return;
            }

            _gameplayReady = gameplayReady;
            PublishSnapshot(reason);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                EventBus<SceneTransitionStartedEvent>.Unregister(_transitionStartedBinding);
                EventBus<SceneTransitionScenesReadyEvent>.Unregister(_transitionScenesReadyBinding);
                EventBus<SceneTransitionCompletedEvent>.Unregister(_transitionCompletedBinding);
            }
            catch
            {
                // best-effort
            }

            if (_gateService != null)
            {
                _gateService.GateChanged -= OnGateChanged;
            }

            ReleaseGateHandle();

            // Evita publicar snapshot após disposed em cenários onde EventBus pode não estar estável.
            // (Se você quiser manter esse snapshot, remova o if abaixo.)
            // PublishSnapshot("dispose");
        }

        private void OnSceneTransitionStarted(SceneTransitionStartedEvent evt)
        {
            _gameplayReady = false;
            AcquireGate();

            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. Context={evt.Context}");

            PublishSnapshot("scene_transition_started");
        }

        private void OnSceneTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SceneTransitionScenesReady → fase WorldLoaded sinalizada. Context={evt.Context}");

            // Ainda não marca gameplayReady, apenas sinaliza fase intermediária.
            PublishSnapshot("scene_transition_scenes_ready");
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            ReleaseGateHandle();
            _gameplayReady = true;

            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SceneTransitionCompleted → gate liberado e fase GameplayReady marcada. Context={evt.Context}");

            PublishSnapshot("scene_transition_completed");
        }

        private void OnGateChanged(bool isOpen)
        {
            // Re-emite snapshot para consumidores que dependem do gate (ex.: bloquear Move enquanto gate fechado).
            PublishSnapshot(isOpen ? "gate_opened" : "gate_closed");
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

        private void PublishSnapshot(string reason)
        {
            if (_disposed)
            {
                return;
            }

            // Gate pode estar indisponível em cenários de bootstrap.
            var snapshot = new ReadinessSnapshot(
                gameplayReady: _gameplayReady,
                gateOpen: _gateService?.IsOpen ?? true,
                activeTokens: _gateService?.ActiveTokenCount ?? 0,
                reason: reason
            );

            EventBus<ReadinessChangedEvent>.Raise(new ReadinessChangedEvent(snapshot));

            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] Snapshot publicado. gameplayReady={snapshot.GameplayReady}, gateOpen={snapshot.GateOpen}, activeTokens={snapshot.ActiveTokens}, reason='{snapshot.Reason}'.");
        }
    }
}
