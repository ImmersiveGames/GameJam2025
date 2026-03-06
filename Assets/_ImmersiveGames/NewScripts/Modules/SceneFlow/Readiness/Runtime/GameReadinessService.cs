#nullable enable
using System;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Readiness.Runtime
{
    /// <summary>
    /// Orquestra readiness do jogo em resposta ao Scene Flow.
    /// Bloqueia simulaÃ§Ã£o durante transiÃ§Ãµes de cena usando ISimulationGateService
    /// e emite snapshots de readiness via EventBus para consumidores (ex.: StateDependentService).
    ///
    /// Nota: baseline/QA pode nÃ£o disparar Scene Flow; para isso existe SetGameplayReady(...) para sinalizaÃ§Ã£o manual.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameReadinessService : IDisposable
    {
        private readonly ISimulationGateService? _gateService;

        private IDisposable? _activeGateHandle;
        private bool _gameplayReady;

        private readonly EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _transitionScenesReadyBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;

        private bool _disposed;

        // "Clean option": evita publicar/logar snapshots redundantes.
        private bool _hasLastSnapshot;
        private ReadinessSnapshot _lastSnapshot;

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

            // Snapshot inicial (Ãºtil em bootstrap/QA). Publica apenas se houver mudanÃ§a (primeira vez sempre publica).
            PublishSnapshot("bootstrap", force: false);
        }

        public bool IsGameplayReady => _gameplayReady;

        /// <summary>
        /// API explÃ­cita para QA/baseline quando nÃ£o hÃ¡ Scene Flow.
        /// </summary>
        public void SetGameplayReady(bool gameplayReady, string reason)
        {
            if (_disposed)
            {
                return;
            }

            _gameplayReady = gameplayReady;

            // Manual/QA: sempre publica (mesmo se valor repetido), por ser uma intenÃ§Ã£o explÃ­cita.
            PublishSnapshot(reason, force: true);
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
        }

        private void OnSceneTransitionStarted(SceneTransitionStartedEvent evt)
        {
            _gameplayReady = false;
            AcquireGate();

            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SceneTransitionStarted â†’ gate adquirido e jogo marcado como NOT READY. Context={evt.Context}");

            PublishSnapshot("scene_transition_started", force: true);
        }

        private void OnSceneTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SceneTransitionScenesReady â†’ fase WorldLoaded sinalizada. Context={evt.Context}");

            // Ainda nÃ£o marca gameplayReady, apenas sinaliza fase intermediÃ¡ria.
            PublishSnapshot("scene_transition_scenes_ready", force: true);
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            ReleaseGateHandle();

            // SemÃ¢ntica correta: GameplayReady sÃ³ deve subir em transiÃ§Ã£o de gameplay.
            bool isGameplayTransition = evt.Context.TransitionProfileId.IsGameplay;
            _gameplayReady = isGameplayTransition;

            string readinessPhase = isGameplayTransition
                ? "GameplayReady"
                : "NonGameplayReady";

            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SceneTransitionCompleted â†’ gate liberado e fase {readinessPhase} marcada. " +
                $"gameplayReady={_gameplayReady}. Context={evt.Context}");

            PublishSnapshot(
                isGameplayTransition
                    ? "scene_transition_completed_gameplay"
                    : "scene_transition_completed_non_gameplay",
                force: true);
        }

        private void OnGateChanged(bool isOpen)
        {
            // "Clean option": sÃ³ publica se o snapshot realmente mudar.
            PublishSnapshot(isOpen ? "gate_opened" : "gate_closed", force: false);
        }

        private void AcquireGate()
        {
            ReleaseGateHandle();

            if (_gateService == null)
            {
                DebugUtility.LogError<GameReadinessService>(
                    "[Readiness] ISimulationGateService indisponÃ­vel. NÃ£o foi possÃ­vel bloquear a simulaÃ§Ã£o durante a transiÃ§Ã£o.");
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

        private void PublishSnapshot(string reason, bool force)
        {
            if (_disposed)
            {
                return;
            }

            var snapshot = new ReadinessSnapshot(
                gameplayReady: _gameplayReady,
                gateOpen: _gateService?.IsOpen ?? true,
                activeTokens: _gateService?.ActiveTokenCount ?? 0,
                reason: reason
            );

            if (!force && _hasLastSnapshot && SnapshotsEqual(_lastSnapshot, snapshot))
            {
                return;
            }

            _hasLastSnapshot = true;
            _lastSnapshot = snapshot;

            EventBus<ReadinessChangedEvent>.Raise(new ReadinessChangedEvent(snapshot));

            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] Snapshot publicado. gameplayReady={snapshot.GameplayReady}, gateOpen={snapshot.GateOpen}, activeTokens={snapshot.ActiveTokens}, reason='{snapshot.Reason}'.");
        }

        private static bool SnapshotsEqual(ReadinessSnapshot a, ReadinessSnapshot b)
        {
            return a.GameplayReady == b.GameplayReady &&
                   a.GateOpen == b.GateOpen &&
                   a.ActiveTokens == b.ActiveTokens;
            // ObservaÃ§Ã£o: reason NÃƒO entra na igualdade (clean option), pois nÃ£o altera comportamento de consumers.
        }
    }
}



