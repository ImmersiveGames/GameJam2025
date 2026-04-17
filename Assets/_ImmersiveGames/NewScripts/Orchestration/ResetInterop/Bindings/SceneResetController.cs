using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.Spawn;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.ResetInterop.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Hooks;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Orchestration.ResetInterop.Bindings
{
    [DisallowMultipleComponent]
    public sealed class SceneResetController : MonoBehaviour, IWorldResetLocalExecutor
    {
        // Borda local do reset de cena. O pipeline canonico vive em SceneReset.
        // Esta borda apenas injeta dependencias e agenda a execucao.
        [Header("Lifecycle")]
        [Tooltip("When true, the controller executes ResetWorldAsync automatically on Start().")]
        [SerializeField] private bool autoInitializeOnStart;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        [Inject] private ISimulationGateService _gateService;
        [Inject] private IWorldSpawnServiceRegistry _spawnRegistry;
        [Inject] private IActorRegistry _actorRegistry;
        [Inject] private IGameplayParticipationFlowService _participationFlowService;
        [Inject] private SceneResetHookRegistry _hookRegistry;

        private readonly SceneResetPipeline _pipeline = SceneResetPipeline.CreateDefault();

        private bool _dependenciesInjected;
        private bool _destroyed;
        private string _sceneName = string.Empty;
        private SceneResetRequestQueue _requestQueue;
        private SceneResetRuntimeFactory _runtimeFactory;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
            _requestQueue = new SceneResetRequestQueue(_sceneName, verboseLogs);
        }

        private void Start()
        {
            EnsureDependenciesInjected();
            if (!HasCriticalDependencies())
            {
                return;
            }

            if (!autoInitializeOnStart)
            {
                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(SceneResetController),
                        $"AutoInitializeOnStart desabilitado - aguardando acionamento externo (scene='{_sceneName}').");
                }

                return;
            }

            if (_gateService != null && _gateService.IsTokenActive(SimulationGateTokens.SceneTransition))
            {
                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(SceneResetController),
                        $"AutoInitializeOnStart suprimido: SceneTransition gate ativo. Aguardando driver (ScenesReady). scene='{_sceneName}'.");
                }

                return;
            }

            _ = InitializeWorldAsync();
        }

        private void OnDestroy()
        {
            if (_destroyed)
            {
                return;
            }

            _destroyed = true;
            _requestQueue?.CancelPending();
            _runtimeFactory?.Cleanup(verboseLogs);
        }

        public Task ResetWorldAsync(string reason)
        {
            return EnqueueReset(
                label: $"WorldReset(reason='{reason ?? "<null>"}')",
                runner: () => RunWorldResetAsync(reason));
        }

        public Task ResetPlayersAsync(string reason = "PlayersSoftReset")
        {
            return EnqueueReset(
                label: $"PlayersReset(reason='{reason ?? "<null>"}')",
                runner: () => RunPlayersResetAsync(reason));
        }

        private Task InitializeWorldAsync()
        {
            return ResetWorldAsync("AutoInitialize/Start");
        }

        private Task EnqueueReset(string label, Func<Task> runner)
        {
            return _requestQueue.Enqueue(label, runner);
        }

        private async Task RunWorldResetAsync(string reason)
        {
            await RunResetInternalAsync(
                startLog: $"Reset iniciado. reason='{reason}', scene='{_sceneName}'.",
                endLog: $"Reset concluido. reason='{reason}', scene='{_sceneName}'.",
                exceptionLog: $"Exception during world reset in scene '{_sceneName}' (reason='{reason}'): ",
                resetContext: null,
                gateToken: WorldResetTokens.WorldResetToken);
        }

        private async Task RunPlayersResetAsync(string reason)
        {
            await RunResetInternalAsync(
                startLog: $"Soft reset (Players) iniciado. reason='{reason}', scene='{_sceneName}'.",
                endLog: $"Soft reset (Players) concluido. reason='{reason}', scene='{_sceneName}'.",
                exceptionLog: $"Exception during players soft reset in scene '{_sceneName}' (reason='{reason}'): ",
                resetContext: new WorldResetContext(
                    reason,
                    new[] { WorldResetScope.Players },
                    WorldResetFlags.SoftReset),
                gateToken: SimulationGateTokens.SoftReset);
        }

        private async Task RunResetInternalAsync(
            string startLog,
            string endLog,
            string exceptionLog,
            WorldResetContext? resetContext,
            string gateToken)
        {
            try
            {
                EnsureDependenciesInjected();
                if (!HasCriticalDependencies())
                {
                    return;
                }

                var context = new SceneResetContext(
                    _gateService,
                    _spawnRegistry?.Services,
                    _actorRegistry,
                    _participationFlowService,
                    DependencyManager.Provider,
                    _sceneName,
                    _hookRegistry,
                    resetContext,
                    gateToken,
                    startLog,
                    endLog);

                await _pipeline.ExecuteAsync(context, default);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(SceneResetController),
                    $"{exceptionLog}{ex}",
                    this);
            }
        }

        private void EnsureDependenciesInjected()
        {
            if (_dependenciesInjected)
            {
                return;
            }

            DependencyManager.Provider.InjectDependencies(this);
            _dependenciesInjected = true;
            _runtimeFactory = new SceneResetRuntimeFactory(
                _sceneName,
                _gateService,
                _spawnRegistry,
                _actorRegistry,
                DependencyManager.Provider,
                _hookRegistry);
            _runtimeFactory.LogOptionalDependencies(verboseLogs);
        }

        private bool HasCriticalDependencies()
        {
            return _runtimeFactory != null && _runtimeFactory.HasCriticalDependencies();
        }
    }
}
