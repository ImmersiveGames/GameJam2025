using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.World;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleController : MonoBehaviour
    {
        [Header("Lifecycle")]
        [Tooltip("Quando true, o controller executa ResetWorldAsync automaticamente no Start(). " +
                 "Para testes automatizados (Opção B), o AutoTestRunner deve desligar isto antes do Start().")]
        [SerializeField] private bool autoInitializeOnStart = true;

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
        private bool _isResetting;
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

            _ = InitializeWorldAsync();
        }

        /// <summary>
        /// API para automação/QA. Garante não concorrência e executa um reset completo.
        /// </summary>
        public async Task ResetWorldAsync(string reason)
        {
            if (_isResetting)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleController),
                    $"Reset ignorado (já em andamento). reason='{reason}', scene='{_sceneName}'.");
                return;
            }

            _isResetting = true;
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
            finally
            {
                _isResetting = false;
            }
        }

        [ContextMenu("QA/Reset World Now")]
        public async void ResetWorldNow()
        {
            await ResetWorldAsync("ContextMenu/ResetWorldNow");
        }

        private async Task InitializeWorldAsync()
        {
            await ResetWorldAsync("AutoInitialize/Start");
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
    }
}
