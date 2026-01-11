using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Gate;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Hooks;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Reset;
using _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Spawn;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.WorldLifecycle.Runtime
{
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleController : MonoBehaviour
    {
        [Header("Lifecycle")]
        [Tooltip("Quando true, o controller executa ResetWorldAsync automaticamente no Start(). " +
                 "Para testes automatizados, um runner deve desligar isto antes do Start().")]
        [SerializeField] private bool autoInitializeOnStart = false;

        [Header("Debug")]
        [SerializeField] private bool verboseLogs = true;

        [Inject] private ISimulationGateService _gateService;
        [Inject] private IWorldSpawnServiceRegistry _spawnRegistry;
        [Inject] private IActorRegistry _actorRegistry;

        // Guardrail: este controller apenas consome o WorldLifecycleHookRegistry criado no bootstrapper.
        [Inject] private WorldLifecycleHookRegistry _hookRegistry;

        private readonly List<IWorldSpawnService> _spawnServices = new();
        private WorldLifecycleOrchestrator _orchestrator;
        private bool _dependenciesInjected;

        // Fila simples: garante execução sequencial de resets (World/Players/etc).
        private readonly object _queueLock = new();
        private readonly Queue<ResetRequest> _resetQueue = new();
        private bool _processingQueue;

        private const int ResetQueueBacklogWarningThreshold = 3;

        private string _sceneName = string.Empty;

        public bool AutoInitializeOnStart
        {
            get => autoInitializeOnStart;
            set => autoInitializeOnStart = value;
        }

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
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
                    DebugUtility.Log(typeof(WorldLifecycleController),
                        $"AutoInitializeOnStart desabilitado — aguardando acionamento externo (scene='{_sceneName}').");
                }
                return;
            }

            // Se a cena foi carregada via SceneFlow, o reset "production" deve ocorrer no driver (ScenesReady).
            // Evita reset duplo (Start + ScenesReady) quando o GameReadinessService mantém o token ativo durante a transição.
            if (_gateService != null && _gateService.IsTokenActive(SimulationGateTokens.SceneTransition))
            {
                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(WorldLifecycleController),
                        $"AutoInitializeOnStart suprimido: SceneTransition gate ativo. Aguardando driver (ScenesReady). scene='{_sceneName}'.");
                }
                return;
            }

            _ = InitializeWorldAsync();
        }

        private void OnDestroy()
        {
            // Evita que awaits fiquem pendurados em unload/destruição do controller.
            lock (_queueLock)
            {
                while (_resetQueue.Count > 0)
                {
                    var req = _resetQueue.Dequeue();
                    req.TryCancel();
                }
                _processingQueue = false;
            }
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
            if (runner == null) throw new ArgumentNullException(nameof(runner));

            ResetRequest request;
            int position;
            bool willStartProcessing;

            lock (_queueLock)
            {
                request = new ResetRequest(label, runner);

                _resetQueue.Enqueue(request);
                position = _resetQueue.Count;

                willStartProcessing = !_processingQueue;
                if (willStartProcessing)
                {
                    _processingQueue = true;
                    _ = ProcessQueueAsync();
                }
            }
            if (position > ResetQueueBacklogWarningThreshold)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleController),
                    $"Reset queue backlog detectado (count={position}). Possível tempestade de resets. lastLabel='{label}', scene='{_sceneName}'.");
            }

            var queuedBehindActiveReset = !willStartProcessing;
            if (queuedBehindActiveReset)
            {
                if (position > 1)
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleController),
                        $"Reset enfileirado (posição={position}). motivo='Reset já em andamento'. label='{label}', scene='{_sceneName}'.");
                }
                else if (verboseLogs)
                {
                    DebugUtility.LogVerbose(typeof(WorldLifecycleController),
                        $"Reset enfileirado (posição={position}). motivo='Reset já em andamento'. label='{label}', scene='{_sceneName}'.");
                }
            }

            return request.Task;
        }

        private async Task ProcessQueueAsync()
        {
            while (true)
            {
                ResetRequest request;

                lock (_queueLock)
                {
                    if (_resetQueue.Count == 0)
                    {
                        _processingQueue = false;
                        return;
                    }

                    request = _resetQueue.Dequeue();
                }

                try
                {
                    if (verboseLogs)
                    {
                        DebugUtility.Log(typeof(WorldLifecycleController),
                            $"Processando reset. label='{request.Label}', scene='{_sceneName}'.");
                    }

                    await request.Runner();
                }
                catch (Exception ex)
                {
                    // Guardrail: Reset não deve quebrar o fluxo do caller (best-effort).
                    DebugUtility.LogError(typeof(WorldLifecycleController),
                        $"Exception while processing reset queue item (label='{request.Label}', scene='{_sceneName}'): {ex}",
                        this);
                }
                finally
                {
                    request.TryComplete();
                }
            }
        }

        private async Task RunWorldResetAsync(string reason)
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
                    DebugUtility.Log(typeof(WorldLifecycleController),
                        $"Reset iniciado. reason='{reason}', scene='{_sceneName}'.");
                }

                BuildSpawnServices();
                _orchestrator = CreateOrchestrator();

                await _orchestrator.ResetWorldAsync();

                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(WorldLifecycleController),
                        $"Reset concluído. reason='{reason}', scene='{_sceneName}'.");
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"Exception during world reset in scene '{_sceneName}' (reason='{reason}'): {ex}",
                    this);
            }
        }

        private async Task RunPlayersResetAsync(string reason)
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
                    DebugUtility.Log(typeof(WorldLifecycleController),
                        $"Soft reset (Players) iniciado. reason='{reason}', scene='{_sceneName}'.");
                }

                BuildSpawnServices();
                _orchestrator = CreateOrchestrator();

                await _orchestrator.ResetScopesAsync(
                    new List<ResetScope> { ResetScope.Players },
                    reason);

                if (verboseLogs)
                {
                    DebugUtility.Log(typeof(WorldLifecycleController),
                        $"Soft reset (Players) concluído. reason='{reason}', scene='{_sceneName}'.");
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"Exception during players soft reset in scene '{_sceneName}' (reason='{reason}'): {ex}",
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

            if (_hookRegistry == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleController),
                    $"WorldLifecycleHookRegistry não encontrado para a cena '{_sceneName}'. Hooks via registry ficarão desativados.");
            }

            if (_gateService == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleController),
                    $"ISimulationGateService não injetado para a cena '{_sceneName}'. Reset seguirá sem gate.");
            }

            if (_spawnRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"IWorldSpawnServiceRegistry não encontrado para a cena '{_sceneName}'.");
            }

            if (_actorRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"IActorRegistry não encontrado para a cena '{_sceneName}'.");
            }
        }

        private void BuildSpawnServices()
        {
            _spawnServices.Clear();

            if (_spawnRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"Nenhum IWorldSpawnServiceRegistry encontrado para a cena '{_sceneName}'. Lista ficará vazia.",
                    this);
                return;
            }

            foreach (var service in _spawnRegistry.Services)
            {
                if (service != null)
                {
                    _spawnServices.Add(service);
                }
                else
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleController),
                        $"Serviço de spawn nulo detectado na cena '{_sceneName}'. Ignorado.");
                }
            }

            DebugUtility.Log(typeof(WorldLifecycleController),
                $"Spawn services coletados para a cena '{_sceneName}': {_spawnServices.Count} (registry total: {_spawnRegistry.Services.Count}).");

            if (_actorRegistry != null)
            {
                DebugUtility.Log(typeof(WorldLifecycleController),
                    $"ActorRegistry count antes de orquestrar: {_actorRegistry.Count}");
            }
        }

        private WorldLifecycleOrchestrator CreateOrchestrator()
        {
            EnsureDependenciesInjected();

            return new WorldLifecycleOrchestrator(
                _gateService,
                _spawnServices,
                _actorRegistry,
                provider: DependencyManager.Provider,
                sceneName: _sceneName,
                hookRegistry: _hookRegistry);
        }

        private bool HasCriticalDependencies()
        {
            var valid = true;

            if (_spawnRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"Sem IWorldSpawnServiceRegistry para a cena '{_sceneName}'. Ciclo de vida não pode continuar.");
                valid = false;
            }

            if (_actorRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"Sem IActorRegistry para a cena '{_sceneName}'. Ciclo de vida não pode continuar.");
                valid = false;
            }

            return valid;
        }

        private readonly struct ResetRequest
        {
            private readonly TaskCompletionSource<bool> _tcs;

            public ResetRequest(string label, Func<Task> runner)
            {
                Label = label ?? "<null>";
                Runner = runner ?? throw new ArgumentNullException(nameof(runner));
                _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public string Label { get; }
            public Func<Task> Runner { get; }
            public Task Task => _tcs.Task;

            public void TryComplete()
            {
                _tcs.TrySetResult(true);
            }

            public void TryCancel()
            {
                _tcs.TrySetCanceled();
            }
        }
    }
}
