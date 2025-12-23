using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Infrastructure.GameLoop
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopSceneFlowCoordinator : IDisposable
    {
        private static int _installed; // 0/1
        public static bool IsInstalled => Volatile.Read(ref _installed) == 1;

        private readonly ISceneTransitionService _sceneTransitionService;
        private readonly SceneTransitionRequest _startPlan;

        private readonly EventBinding<GameStartEvent> _onGameStartRequested;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _onScenesReady;

        private int _pendingStart; // 0/1
        private string _pendingProfileName;

        public GameLoopSceneFlowCoordinator(
            ISceneTransitionService sceneTransitionService,
            SceneTransitionRequest startPlan)
        {
            _sceneTransitionService = sceneTransitionService ?? throw new ArgumentNullException(nameof(sceneTransitionService));
            _startPlan = startPlan ?? throw new ArgumentNullException(nameof(startPlan));

            Interlocked.Exchange(ref _installed, 1);

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
            EventBus<GameStartEvent>.Unregister(_onGameStartRequested);
            EventBus<SceneTransitionScenesReadyEvent>.Unregister(_onScenesReady);
            Interlocked.Exchange(ref _installed, 0);
        }

        private void OnGameStartRequested(GameStartEvent evt)
        {
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
            if (Volatile.Read(ref _pendingStart) != 1)
                return;

            var expectedProfile = _pendingProfileName;
            var actualProfile = evt.Context.TransitionProfileName;

            if (!string.IsNullOrWhiteSpace(expectedProfile) &&
                !string.Equals(expectedProfile, actualProfile, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<GameLoopSceneFlowCoordinator>(
                    $"[GameLoopSceneFlow] ScenesReady ignorado (profile diferente). expected='{expectedProfile}', actual='{actualProfile}'.");
                return;
            }

            if (!TryResolveLoop(out var loop))
            {
                DebugUtility.LogError<GameLoopSceneFlowCoordinator>(
                    "[GameLoopSceneFlow] IGameLoopService não encontrado no DI global ao liberar start em ScenesReady.");

                // IMPORTANTE: não deixar pendente para sempre.
                _pendingProfileName = null;
                Interlocked.Exchange(ref _pendingStart, 0);
                return;
            }

            DebugUtility.Log<GameLoopSceneFlowCoordinator>(
                "[GameLoopSceneFlow] ScenesReady recebido para o start. Liberando GameLoop.RequestStart().",
                DebugUtility.Colors.Success);

            loop.Initialize();
            loop.RequestStart();

            _pendingProfileName = null;
            Interlocked.Exchange(ref _pendingStart, 0);
        }

        private static bool TryResolveLoop(out IGameLoopService loop)
        {
            loop = null;
            var provider = DependencyManager.Provider;
            return provider.TryGetGlobal<IGameLoopService>(out loop) && loop != null;
        }
    }
}
