using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Hooks;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Spawn;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Bindings
{
    /// <summary>
    /// Runner interno do trilho local de reset da cena.
    /// Responsável por montar os serviços de spawn e instanciar o orchestrator,
    /// mantendo o controller focado em fila/lifecycle e o orchestrator focado no pipeline determinístico.
    /// </summary>
    internal sealed class WorldLifecycleSceneResetRunner
    {
        private readonly ISimulationGateService _gateService;
        private readonly IWorldSpawnServiceRegistry _spawnRegistry;
        private readonly IActorRegistry _actorRegistry;
        private readonly IDependencyProvider _provider;
        private readonly WorldLifecycleHookRegistry _hookRegistry;
        private readonly string _sceneName;

        public WorldLifecycleSceneResetRunner(
            ISimulationGateService gateService,
            IWorldSpawnServiceRegistry spawnRegistry,
            IActorRegistry actorRegistry,
            IDependencyProvider provider,
            WorldLifecycleHookRegistry hookRegistry,
            string sceneName)
        {
            _gateService = gateService;
            _spawnRegistry = spawnRegistry;
            _actorRegistry = actorRegistry;
            _provider = provider;
            _hookRegistry = hookRegistry;
            _sceneName = sceneName ?? string.Empty;
        }

        public Task ExecuteWorldResetAsync()
        {
            WorldLifecycleOrchestrator orchestrator = CreateOrchestrator();
            return orchestrator.ResetWorldAsync();
        }

        public Task ExecutePlayersResetAsync(string reason)
        {
            WorldLifecycleOrchestrator orchestrator = CreateOrchestrator();
            return orchestrator.ResetScopesAsync(new List<WorldResetScope> { WorldResetScope.Players }, reason);
        }

        private WorldLifecycleOrchestrator CreateOrchestrator()
        {
            List<IWorldSpawnService> spawnServices = CollectSpawnServices();
            return new WorldLifecycleOrchestrator(
                _gateService,
                spawnServices,
                _actorRegistry,
                provider: _provider,
                sceneName: _sceneName,
                hookRegistry: _hookRegistry);
        }

        private List<IWorldSpawnService> CollectSpawnServices()
        {
            var spawnServices = new List<IWorldSpawnService>();

            if (_spawnRegistry == null)
            {
                DebugUtility.LogError(typeof(WorldLifecycleController),
                    $"Nenhum IWorldSpawnServiceRegistry encontrado para a cena '{_sceneName}'. Lista ficará vazia.");
                return spawnServices;
            }

            foreach (var service in _spawnRegistry.Services)
            {
                if (service != null)
                {
                    spawnServices.Add(service);
                }
                else
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleController),
                        $"Serviço de spawn nulo detectado na cena '{_sceneName}'. Ignorado.");
                }
            }

            DebugUtility.Log(typeof(WorldLifecycleController),
                $"Spawn services coletados para a cena '{_sceneName}': {spawnServices.Count} (registry total: {_spawnRegistry.Services.Count}).");

            if (_actorRegistry != null)
            {
                DebugUtility.Log(typeof(WorldLifecycleController),
                    $"ActorRegistry count antes de orquestrar: {_actorRegistry.Count}");
            }

            return spawnServices;
        }
    }
}
