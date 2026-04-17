#nullable enable
using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Transition.Runtime;
namespace _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime
{
    /// <summary>
    /// Orquestra readiness do jogo em resposta ao Scene Flow.
    /// Consome transições para refletir gate/readiness; não decide reset nem policy de rota.
    /// Bloqueia simulação durante transições de cena usando ISimulationGateService
    /// e emite snapshots de readiness via EventBus para consumidores (ex.: GameplayStateGate).
    ///
    /// Nota: baseline/QA pode não disparar Scene Flow; para isso existe SetGameplayReady(...) para sinalização manual.
    /// </summary>
    // Boundary note: this service tracks technical readiness only.
    // The canonical "gameplay actually released" signal remains the GameLoop entering Playing.
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

            DebugUtility.LogVerbose<GameReadinessService>("[Readiness] GameReadinessService registrado como consumidor de Scene Flow -> SimulationGate.");

            // Snapshot inicial (útil em bootstrap/QA). Publica apenas se houver mudança (primeira vez sempre publica).
            PublishSnapshot("bootstrap", force: false);
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

            LogReadinessState(
                gameplayReady ? "GameplayReadySet" : "GameplayNotReadySet",
                reason,
                gateOpen: _gateService?.IsOpen ?? true,
                activeTokens: _gateService?.ActiveTokenCount ?? 0);

            // Manual/QA: sempre publica (mesmo se valor repetido), por ser uma intenção explícita.
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

            LogReadinessTransition("TransitionBlocking", "SceneTransitionStartedEvent", evt.context, gateOpen: false);
            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SceneTransitionStarted → gate adquirido e jogo marcado como NOT READY. consumer='GameReadinessService' Context={evt.context}");

            PublishSnapshot("scene_transition_started", force: true);
        }

        private void OnSceneTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            LogReadinessTransition("ReadinessMidway", "SceneTransitionScenesReadyEvent", evt.context, gateOpen: _gateService?.IsOpen ?? true);
            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SceneTransitionScenesReady → fase WorldLoaded sinalizada. consumer='GameReadinessService' Context={evt.context}");

            // Ainda não marca gameplayReady, apenas sinaliza fase intermediária.
            PublishSnapshot("scene_transition_scenes_ready", force: true);
        }

        private void OnSceneTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            ReleaseGateHandle();

            // Semântica correta: GameplayReady só deve subir em transição de gameplay.
            bool isGameplayTransition = evt.context.RouteKind == SceneRouteKind.Gameplay;
            _gameplayReady = isGameplayTransition;
            bool gateOpen = _gateService?.IsOpen ?? true;

            string readinessPhase = isGameplayTransition
                ? "GameplayReady"
                : "NonGameplayReady";

            LogReadinessTransition(
                isGameplayTransition ? "GameplayReady" : "NonGameplayReady",
                "SceneTransitionCompletedEvent",
                evt.context,
                gateOpen: gateOpen);

            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] SceneTransitionCompleted → token de transicao liberado; fase tecnica {readinessPhase} marcada; gateGlobal={(gateOpen ? "Open" : "Closed")}. consumer='GameReadinessService' gameplayReady={_gameplayReady}. Context={evt.context}");

            PublishSnapshot(
                isGameplayTransition
                    ? "scene_transition_completed_gameplay"
                    : "scene_transition_completed_non_gameplay",
                force: true);
        }

        private void OnGateChanged(bool isOpen)
        {
            LogReadinessState(isOpen ? "GateOpened" : "GateClosed", "GateChanged", isOpen, _gateService?.ActiveTokenCount ?? 0);

            // "Clean option": só publica se o snapshot realmente mudar.
            PublishSnapshot(isOpen ? "gate_opened" : "gate_closed", force: false);
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

        private static void LogReadinessState(string label, string reason, bool gateOpen, int activeTokens)
        {
            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] {label} reason='{reason}' gateOpen={gateOpen} activeTokens={activeTokens}.",
                DebugUtility.Colors.Info);
        }

        private static void LogReadinessTransition(string label, string eventName, SceneTransitionContext context, bool gateOpen)
        {
            DebugUtility.LogVerbose<GameReadinessService>(
                $"[Readiness] {label} event='{eventName}' signature='{SceneTransitionSignature.Compute(context)}' routeId='{context.RouteId}' routeKind='{context.RouteKind}' gateOpen={gateOpen} gameplayReady={context.RouteKind == SceneRouteKind.Gameplay}.",
                DebugUtility.Colors.Info);
        }

        private static bool SnapshotsEqual(ReadinessSnapshot a, ReadinessSnapshot b)
        {
            return a.GameplayReady == b.GameplayReady &&
                   a.GateOpen == b.GateOpen &&
                   a.ActiveTokens == b.ActiveTokens;
            // Observação: reason NÃO entra na igualdade (clean option), pois não altera comportamento de consumers.
        }
    }
}






