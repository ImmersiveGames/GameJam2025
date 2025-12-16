using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Controlador de cena que inicializa o pipeline de reset/spawn do mundo.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldLifecycleController : MonoBehaviour
    {
        [Inject] private ISimulationGateService _gateService;
        [Inject] private IWorldSpawnServiceRegistry _spawnRegistry;

        private readonly List<IWorldSpawnService> _spawnServices = new();
        private WorldLifecycleOrchestrator _orchestrator;
        private string _sceneName = string.Empty;

        private void Awake()
        {
            _sceneName = gameObject.scene.name;
            DependencyManager.Provider.InjectDependencies(this);
        }

        private void Start()
        {
            _ = InitializeWorldAsync();
        }

        private async Task InitializeWorldAsync()
        {
            try
            {
                BuildSpawnServices();
                _orchestrator = new WorldLifecycleOrchestrator(_gateService, _spawnServices);
                await _orchestrator.ResetWorldAsync();
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"Exception during world initialization in scene '{_sceneName}': {ex}",
                    this);
            }
        }

        private void BuildSpawnServices()
        {
            _spawnServices.Clear();

            if (_spawnRegistry == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleController),
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
            }

            DebugUtility.Log(typeof(WorldLifecycleController),
                $"Spawn services coletados para a cena '{_sceneName}': {_spawnServices.Count}.",
                this);
        }
    }
}
