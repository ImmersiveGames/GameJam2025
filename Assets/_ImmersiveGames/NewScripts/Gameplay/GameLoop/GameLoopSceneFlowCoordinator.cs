using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Infrastructure.GameLoop
{
    /// <summary>
    /// Coordena GameLoop ↔ SceneTransitionService.
    ///
    /// Fluxo (Opção B):
    /// - Recebe GameStartEvent (interpreta como "pedido de start").
    /// - Dispara SceneTransitionService.TransitionAsync(plan).
    /// - Ao receber SceneTransitionScenesReadyEvent do plan, chama IGameLoopService.RequestStart().
    ///
    /// Observação: WorldLifecycleRuntimeDriver já reage a SceneTransitionScenesReadyEvent e executa reset.
    /// Este coordenador só "libera" o GameLoop após as cenas estarem prontas (e o reset ter sido disparado).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopSceneFlowCoordinator : IDisposable
    {
        private readonly IGameLoopService _gameLoopService;
        private readonly ISceneTransitionService _sceneTransitionService;
        private readonly SceneTransitionRequest _startPlan;

        private readonly EventBinding<GameStartEvent> _onGameStartRequested;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _onScenesReady;

        private int _pendingStart; // 0/1
        private string _pendingProfileName;
        private bool _disposed;

        public GameLoopSceneFlowCoordinator(
            IGameLoopService gameLoopService,
            ISceneTransitionService sceneTransitionService,
            SceneTransitionRequest startPlan)
        {
            _gameLoopService = gameLoopService ?? throw new ArgumentNullException(nameof(gameLoopService));
            _sceneTransitionService = sceneTransitionService ?? throw new ArgumentNullException(nameof(sceneTransitionService));
            _startPlan = startPlan ?? throw new ArgumentNullException(nameof(startPlan));

            _onGameStartRequested = new EventBinding<GameStartEvent>(OnGameStartRequested);
            _onScenesReady = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);

            EventBus<GameStartEvent>.Register(_onGameStartRequested);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_onScenesReady);

            DebugUtility.Log<GameLoopSceneFlowCoordinator>(
                $"[GameLoopSceneFlow] Coordinator registrado. StartPlan: Load=[{string.Join(", ", _startPlan.ScenesToLoad)}], " +
                $"Unload=[{string.Join(", ", _startPlan.ScenesToUnload)}], Active='{_startPlan.TargetActiveScene}', " +
                $"UseFade={_startPlan.UseFade}, Profile='{_startPlan.TransitionProfileName}'.");
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            EventBus<GameStartEvent>.Unregister(_onGameStartRequested);
            EventBus<SceneTransitionScenesReadyEvent>.Unregister(_onScenesReady);

            _pendingProfileName = null;
            Interlocked.Exchange(ref _pendingStart, 0);
        }

        private void OnGameStartRequested(GameStartEvent evt)
        {
            if (_disposed)
                return;

            if (Interlocked.CompareExchange(ref _pendingStart, 1, 0) == 1)
            {
                DebugUtility.LogWarning<GameLoopSceneFlowCoordinator>(
                    "[GameLoopSceneFlow] Start já está pendente. Ignorando novo pedido.");
                return;
            }

            _pendingProfileName = _startPlan.TransitionProfileName;

            DebugUtility.Log<GameLoopSceneFlowCoordinator>(
                "[GameLoopSceneFlow] Start solicitado. Disparando transição de cenas...",
                DebugUtility.Colors.Info);

            _ = RunStartTransitionAsync();
        }

        private async Task RunStartTransitionAsync()
        {
            try
            {
                await _sceneTransitionService.TransitionAsync(_startPlan);
                // Liberação do GameLoop acontece no ScenesReady.
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<GameLoopSceneFlowCoordinator>(
                    $"[GameLoopSceneFlow] Falha na transição de start: {ex}");

                _pendingProfileName = null;
                Interlocked.Exchange(ref _pendingStart, 0);
            }
        }

        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            if (_disposed)
                return;

            // Só reage se tiver start pendente.
            if (Interlocked.CompareExchange(ref _pendingStart, 1, 1) == 0)
                return;

            var expectedProfile = _pendingProfileName;
            var actualProfile = evt.Context.TransitionProfileName;

            // Filtro por profile quando definido no plano.
            if (!string.IsNullOrWhiteSpace(expectedProfile) &&
                !string.Equals(expectedProfile, actualProfile, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<GameLoopSceneFlowCoordinator>(
                    $"[GameLoopSceneFlow] ScenesReady ignorado (profile diferente). expected='{expectedProfile}', actual='{actualProfile}'.");
                return;
            }

            DebugUtility.Log<GameLoopSceneFlowCoordinator>(
                "[GameLoopSceneFlow] ScenesReady recebido para o start. Liberando GameLoop.RequestStart().",
                DebugUtility.Colors.Success);

            _gameLoopService.Initialize();
            _gameLoopService.RequestStart();

            _pendingProfileName = null;
            Interlocked.Exchange(ref _pendingStart, 0);
        }
    }
}
