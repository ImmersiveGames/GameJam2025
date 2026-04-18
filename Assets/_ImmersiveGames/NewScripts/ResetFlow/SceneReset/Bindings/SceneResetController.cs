using System;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
using _ImmersiveGames.NewScripts.ResetFlow.Interop.Runtime;
using _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Hooks;
using _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Runtime;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Domain;
using _ImmersiveGames.NewScripts.ResetFlow.WorldReset.Runtime;
using _ImmersiveGames.NewScripts.SceneFlow.Readiness.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Integration.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Bindings
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

        private ISimulationGateService _gateService;
        private IWorldSpawnServiceRegistry _spawnRegistry;
        private IActorRegistry _actorRegistry;
        private ISessionIntegrationContextService _sessionIntegrationContextService;
        private SceneResetHookRegistry _hookRegistry;
        private IWorldResetLocalExecutorRegistry _localExecutorRegistry;
        private IDependencyProvider _provider;

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
            RegisterAsLocalExecutor();
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

        private void OnEnable()
        {
            EnsureDependenciesInjected();
            RegisterAsLocalExecutor();
        }

        private void OnDisable()
        {
            UnregisterAsLocalExecutor();
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
                    _sessionIntegrationContextService,
                    _provider,
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
            if (_dependenciesInjected && HasCriticalDependencies())
            {
                return;
            }

            if (_provider == null)
            {
                _provider = DependencyManager.Provider;
            }

            if (_provider == null)
            {
                HardFailFastH1.Trigger(typeof(SceneResetController),
                    "[FATAL][H1][SceneReset] IDependencyProvider obrigatorio ausente para compor SceneResetController.");
            }

            if (!IsSceneScopeReady())
            {
                _dependenciesInjected = false;
                _runtimeFactory = null;

                if (verboseLogs)
                {
                    DebugUtility.LogVerbose(typeof(SceneResetController),
                        $"[OBS][SceneReset] Scene scope ainda nao pronto para '{_sceneName}'. Injeção/validação critica adiada.",
                        DebugUtility.Colors.Info);
                }

                return;
            }

            ResolveGlobalDependencies();
            ResolveSceneLocalDependencies();
            bool previousResolved = _dependenciesInjected;
            _dependenciesInjected = HasInjectedSceneLocalDependencies();

            if (!previousResolved && !_dependenciesInjected && verboseLogs)
            {
                DebugUtility.LogVerbose(typeof(SceneResetController),
                    $"[OBS][SceneReset] Dependencias scene-local ainda indisponiveis para '{_sceneName}'. Aguardando compose da cena para reinjetar.",
                    DebugUtility.Colors.Info);
            }
            else if (!previousResolved && _dependenciesInjected && verboseLogs)
            {
                DebugUtility.LogVerbose(typeof(SceneResetController),
                    $"[OBS][SceneReset] Dependencias scene-local resolvidas para '{_sceneName}'.",
                    DebugUtility.Colors.Success);
            }

            _runtimeFactory = new SceneResetRuntimeFactory(
                _sceneName,
                _gateService,
                _spawnRegistry,
                _actorRegistry,
                _provider,
                _hookRegistry);
            _runtimeFactory.LogOptionalDependencies(verboseLogs);
        }

        private bool HasCriticalDependencies()
        {
            if (!IsSceneScopeReady())
            {
                return false;
            }

            if (_localExecutorRegistry == null)
            {
                DebugUtility.LogError(typeof(SceneResetController),
                    $"Sem IWorldResetLocalExecutorRegistry para a cena '{_sceneName}'. Reset macro nao conseguira resolver executor local.");
                return false;
            }

            return _runtimeFactory != null && _runtimeFactory.HasCriticalDependencies();
        }

        private bool HasInjectedSceneLocalDependencies()
        {
            return _provider != null &&
                   _spawnRegistry != null &&
                   _actorRegistry != null &&
                   _hookRegistry != null;
        }

        private void ResolveGlobalDependencies()
        {
            if (_provider == null)
            {
                return;
            }

            if (_gateService == null)
            {
                _provider.TryGetGlobal(out _gateService);
            }

            if (_localExecutorRegistry == null)
            {
                _provider.TryGetGlobal(out _localExecutorRegistry);
            }

            if (_sessionIntegrationContextService == null)
            {
                _provider.TryGetGlobal(out _sessionIntegrationContextService);
            }
        }

        private void ResolveSceneLocalDependencies()
        {
            if (_provider == null || string.IsNullOrWhiteSpace(_sceneName))
            {
                return;
            }

            _provider.TryGetForScene(_sceneName, out _spawnRegistry);
            _provider.TryGetForScene(_sceneName, out _actorRegistry);
            _provider.TryGetForScene(_sceneName, out _hookRegistry);
        }

        private bool IsSceneScopeReady()
        {
            if (_provider == null || string.IsNullOrWhiteSpace(_sceneName))
            {
                return false;
            }

            return _provider.TryGetForScene<ISceneScopeMarker>(_sceneName, out _);
        }

        private void RegisterAsLocalExecutor()
        {
            if (_localExecutorRegistry == null)
            {
                return;
            }

            _localExecutorRegistry.Register(_sceneName, this);
            DebugUtility.LogVerbose(typeof(SceneResetController),
                $"[OBS][SceneReset] Local executor registrado. scene='{_sceneName}' type='{GetType().Name}'.",
                DebugUtility.Colors.Info);
        }

        private void UnregisterAsLocalExecutor()
        {
            if (_localExecutorRegistry == null)
            {
                return;
            }

            _localExecutorRegistry.Unregister(_sceneName, this);
            DebugUtility.LogVerbose(typeof(SceneResetController),
                $"[OBS][SceneReset] Local executor removido. scene='{_sceneName}' type='{GetType().Name}'.",
                DebugUtility.Colors.Info);
        }
    }
}

