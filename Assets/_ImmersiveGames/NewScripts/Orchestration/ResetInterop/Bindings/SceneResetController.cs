using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.Spawn;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Hooks;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Runtime;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.ResetInterop.Bindings
{
    [DisallowMultipleComponent]
    public sealed class SceneResetController : MonoBehaviour, IWorldResetLocalExecutor
    {
        // Borda local do reset de cena. O pipeline real permanece em SceneReset; a entrada e o wiring
        // ficam em ResetInterop para nao competir com WorldReset.
        // Nao publica eventos V1/V2 de gate/telemetria do trilho macro.
        [Header("Lifecycle")]
        [Tooltip("Quando true, o controller executa ResetWorldAsync automaticamente no Start(). " +
                 "Para testes automatizados, um runner deve desligar isto antes do Start().")]
        [SerializeField] private bool autoInitializeOnStart;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        [Inject] private ISimulationGateService _gateService;
        [Inject] private IWorldSpawnServiceRegistry _spawnRegistry;
        [Inject] private IActorRegistry _actorRegistry;

        // Guardrail: este controller apenas consome o SceneResetHookRegistry criado no bootstrapper.
        [Inject] private SceneResetHookRegistry _hookRegistry;

        private bool _dependenciesInjected;
        private bool _destroyed;
        private string _sceneName = string.Empty;
        private SceneResetRequestQueue _requestQueue;
        private SceneResetRuntimeFactory _runtimeFactory;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
            _requestQueue = new SceneResetRequestQueue(_sceneName, verboseLogs);
            // IMPORTANT: Do NOT inject here. Scene services may not be registered yet.
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
                        $"AutoInitializeOnStart desabilitado — aguardando acionamento externo (scene='{_sceneName}').");
                }
                return;
            }

            // Se a cena foi carregada via SceneFlow, o reset production deve ocorrer no driver (ScenesReady).
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

        /// <summary>
        /// API pública. Enfileira um reset completo (hard reset).
        /// Se já houver um reset em andamento, este reset aguardará sua vez.
        /// </summary>
        public Task ResetWorldAsync(string reason)
        {
            return EnqueueReset(
                label: $"WorldReset(reason='{reason ?? "<null>"}')",
                runner: () => RunWorldResetAsync(reason));
        }

        /// <summary>
        /// Soft reset focado apenas no escopo de jogadores (Players).
        /// Se já houver um reset em andamento, este reset aguardará sua vez.
        /// </summary>
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
                endLog: $"Reset concluído. reason='{reason}', scene='{_sceneName}'.",
                exceptionLog: $"Exception during world reset in scene '{_sceneName}' (reason='{reason}'): ",
                execute: runner => runner.ExecuteWorldResetAsync());
        }

        private async Task RunPlayersResetAsync(string reason)
        {
            await RunResetInternalAsync(
                startLog: $"Soft reset (Players) iniciado. reason='{reason}', scene='{_sceneName}'.",
                endLog: $"Soft reset (Players) concluído. reason='{reason}', scene='{_sceneName}'.",
                exceptionLog: $"Exception during players soft reset in scene '{_sceneName}' (reason='{reason}'): ",
                execute: runner => runner.ExecutePlayersResetAsync(reason));
        }

        private async Task RunResetInternalAsync(
            string startLog,
            string endLog,
            string exceptionLog,
            Func<SceneResetRunner, Task> execute)
        {
            try
            {
                EnsureDependenciesInjected();
                if (!HasCriticalDependencies())
                {
                    return;
                }

                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(SceneResetController), startLog);
                }

                SceneResetRunner runner = _runtimeFactory.CreateRunner();
                await execute(runner);

                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(SceneResetController), endLog);
                }
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
