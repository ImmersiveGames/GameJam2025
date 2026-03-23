using System.Collections.Generic;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Hooks;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Spawn;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Bindings
{
    /// <summary>
    /// Monta o trilho local de reset da cena.
    /// Resolve serviços de spawn, cria a façade do reset de cena e mantém o controller livre de detalhes de pipeline.
    /// </summary>
    internal sealed class SceneResetRunner
    {
        private readonly ISimulationGateService _gateService;
        private readonly IWorldSpawnServiceRegistry _spawnRegistry;
        private readonly IActorRegistry _actorRegistry;
        private readonly IDependencyProvider _provider;
        private readonly SceneResetHookRegistry _hookRegistry;
        private readonly string _sceneName;

        public SceneResetRunner(
            ISimulationGateService gateService,
            IWorldSpawnServiceRegistry spawnRegistry,
            IActorRegistry actorRegistry,
            IDependencyProvider provider,
            SceneResetHookRegistry hookRegistry,
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
            SceneResetFacade orchestrator = CreateOrchestrator();
            return orchestrator.ResetWorldAsync();
        }

        public Task ExecutePlayersResetAsync(string reason)
        {
            SceneResetFacade orchestrator = CreateOrchestrator();
            return orchestrator.ResetScopesAsync(new List<WorldResetScope> { WorldResetScope.Players }, reason);
        }

        private SceneResetFacade CreateOrchestrator()
        {
            List<IWorldSpawnService> spawnServices = CollectSpawnServices();
            return new SceneResetFacade(
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
                DebugUtility.LogError(typeof(SceneResetController),
                    $"Nenhum IWorldSpawnServiceRegistry encontrado para a cena '{_sceneName}'. Lista ficará vazia.");
                return spawnServices;
            }

            foreach (IWorldSpawnService service in _spawnRegistry.Services)
            {
                if (service != null)
                {
                    spawnServices.Add(service);
                }
                else
                {
                    DebugUtility.LogWarning(typeof(SceneResetController),
                        $"Serviço de spawn nulo detectado na cena '{_sceneName}'. Ignorado.");
                }
            }

            DebugUtility.Log(typeof(SceneResetController),
                $"Spawn services coletados para a cena '{_sceneName}': {spawnServices.Count} (registry total: {_spawnRegistry.Services.Count}).");

            if (_actorRegistry != null)
            {
                DebugUtility.Log(typeof(SceneResetController),
                    $"ActorRegistry count antes de orquestrar: {_actorRegistry.Count}");
            }

            return spawnServices;
        }
    }
}
