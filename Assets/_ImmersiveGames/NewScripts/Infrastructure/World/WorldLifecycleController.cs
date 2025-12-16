using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleController : MonoBehaviour
    {
        [Inject] private ISimulationGateService _gateService;
        [Inject] private IWorldSpawnServiceRegistry _spawnRegistry;
        [Inject] private IActorRegistry _actorRegistry;

        private readonly List<IWorldSpawnService> _spawnServices = new();
        private WorldLifecycleOrchestrator _orchestrator;
        private bool _dependenciesInjected;
        private string _sceneName = string.Empty;

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

            _ = InitializeWorldAsync();
        }

        [ContextMenu("QA/Reset World Now")]
        public async void ResetWorldNow()
        {
            DebugUtility.Log(
                typeof(WorldLifecycleController),
                $"Reset manual iniciado na cena '{_sceneName}'.");

            try
            {
                EnsureDependenciesInjected();
                if (!HasCriticalDependencies())
                {
                    return;
                }

                BuildSpawnServices();
                _orchestrator = new WorldLifecycleOrchestrator(_gateService, _spawnServices, _actorRegistry);

                await _orchestrator.ResetWorldAsync();

                DebugUtility.Log(
                    typeof(WorldLifecycleController),
                    $"Reset manual finalizado na cena '{_sceneName}'.");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(
                    typeof(WorldLifecycleController),
                    $"Exception during manual world reset in scene '{_sceneName}': {ex}",
                    this);
            }
        }

        private async Task InitializeWorldAsync()
        {
            try
            {
                EnsureDependenciesInjected();
                if (!HasCriticalDependencies())
                {
                    return;
                }

                BuildSpawnServices();

                _orchestrator = new WorldLifecycleOrchestrator(_gateService, _spawnServices, _actorRegistry);
                await _orchestrator.ResetWorldAsync();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(
                    typeof(WorldLifecycleController),
                    $"Exception during world initialization in scene '{_sceneName}': {ex}",
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

            if (_gateService == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"ISimulationGateService não injetado para a cena '{_sceneName}'.");
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
                DebugUtility.LogError(
                    typeof(WorldLifecycleController),
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

            DebugUtility.Log(
                typeof(WorldLifecycleController),
                $"Spawn services coletados para a cena '{_sceneName}': {_spawnServices.Count} (registry total: {_spawnRegistry.Services.Count}).");

            if (_actorRegistry != null)
            {
                DebugUtility.Log(typeof(WorldLifecycleController),
                    $"ActorRegistry count antes de orquestrar: {_actorRegistry.Count}");
            }
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
