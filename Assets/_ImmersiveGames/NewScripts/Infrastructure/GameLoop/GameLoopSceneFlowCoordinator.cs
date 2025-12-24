using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
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

        private readonly EventBinding<GameStartRequestedEvent> _onStartRequest;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _onScenesReady;
        private readonly EventBinding<WorldLifecycleResetCompletedEvent> _onWorldResetCompleted;

        private int _pendingStart; // 0/1
        private string _pendingProfileName;

        public GameLoopSceneFlowCoordinator(
            ISceneTransitionService sceneTransitionService,
            SceneTransitionRequest startPlan)
        {
            _sceneTransitionService = sceneTransitionService ?? throw new ArgumentNullException(nameof(sceneTransitionService));
            _startPlan = startPlan ?? throw new ArgumentNullException(nameof(startPlan));

            Interlocked.Exchange(ref _installed, 1);

            _onStartRequest = new EventBinding<GameStartRequestedEvent>(OnStartRequested);
            _onScenesReady = new EventBinding<SceneTransitionScenesReadyEvent>(OnScenesReady);
            _onWorldResetCompleted = new EventBinding<WorldLifecycleResetCompletedEvent>(OnWorldResetCompleted);

            EventBus<GameStartRequestedEvent>.Register(_onStartRequest);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_onScenesReady);
            EventBus<WorldLifecycleResetCompletedEvent>.Register(_onWorldResetCompleted);

            DebugUtility.Log<GameLoopSceneFlowCoordinator>(
                $"[GameLoopSceneFlow] Coordinator registrado. StartPlan: Load=[{string.Join(", ", _startPlan.ScenesToLoad ?? Array.Empty<string>())}], " +
                $"Unload=[{string.Join(", ", _startPlan.ScenesToUnload ?? Array.Empty<string>())}], Active='{_startPlan.TargetActiveScene}', " +
                $"UseFade={_startPlan.UseFade}, Profile='{_startPlan.TransitionProfileName}'.");
        }

        public void Dispose()
        {
            EventBus<GameStartRequestedEvent>.Unregister(_onStartRequest);
            EventBus<SceneTransitionScenesReadyEvent>.Unregister(_onScenesReady);
            EventBus<WorldLifecycleResetCompletedEvent>.Unregister(_onWorldResetCompleted);
            Interlocked.Exchange(ref _installed, 0);
        }

        private void OnStartRequested(GameStartRequestedEvent evt)
        {
            if (Interlocked.CompareExchange(ref _pendingStart, 1, 0) == 1)
            {
                DebugUtility.LogWarning<GameLoopSceneFlowCoordinator>(
                    "[GameLoopSceneFlow] Start já está pendente. Ignorando novo REQUEST.");
                return;
            }

            _pendingProfileName = _startPlan.TransitionProfileName;

            DebugUtility.Log<GameLoopSceneFlowCoordinator>(
                "[GameLoopSceneFlow] Start REQUEST recebido. Disparando transição de cenas...",
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

            // IMPORTANTÍSSIMO:
            // Aqui NÃO iniciamos o GameLoop. Apenas aguardamos o reset do WorldLifecycle.
            DebugUtility.Log<GameLoopSceneFlowCoordinator>(
                "[GameLoopSceneFlow] ScenesReady recebido. Aguardando WorldLifecycle concluir reset para emitir COMMAND start...",
                DebugUtility.Colors.Info);
        }

        private void OnWorldResetCompleted(WorldLifecycleResetCompletedEvent evt)
        {
            if (Volatile.Read(ref _pendingStart) != 1)
                return;

            var expectedProfile = _pendingProfileName;
            var actualProfile = evt.SceneTransitionProfileName;

            if (!string.IsNullOrWhiteSpace(expectedProfile) &&
                !string.Equals(expectedProfile, actualProfile, StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<GameLoopSceneFlowCoordinator>(
                    $"[GameLoopSceneFlow] WorldResetCompleted ignorado (profile diferente). expected='{expectedProfile}', actual='{actualProfile}'.");
                return;
            }

            DebugUtility.Log<GameLoopSceneFlowCoordinator>(
                "[GameLoopSceneFlow] Reset concluído. Emitindo GameStartEvent (COMMAND) para iniciar o GameLoop.",
                DebugUtility.Colors.Success);

            EventBus<GameStartEvent>.Raise(new GameStartEvent());

            _pendingProfileName = null;
            Interlocked.Exchange(ref _pendingStart, 0);
        }
    }

    /// <summary>
    /// Evento infra (COMMAND) emitido quando o WorldLifecycle terminou o hard reset após ScenesReady.
    /// Isso permite que o coordinator não dependa de polling ou de acoplamento direto ao controller.
    /// </summary>
    public readonly struct WorldLifecycleResetCompletedEvent : _ImmersiveGames.NewScripts.Infrastructure.Events.IEvent
    {
        public WorldLifecycleResetCompletedEvent(string sceneTransitionProfileName, string reason)
        {
            SceneTransitionProfileName = sceneTransitionProfileName;
            Reason = reason;
        }

        public string SceneTransitionProfileName { get; }
        public string Reason { get; }
    }
}
