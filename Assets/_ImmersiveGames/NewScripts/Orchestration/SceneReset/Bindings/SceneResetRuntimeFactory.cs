using System;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Hooks;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Spawn;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneReset.Bindings
{
    internal sealed class SceneResetRuntimeFactory
    {
        private readonly string _sceneName;
        private readonly ISimulationGateService _gateService;
        private readonly IWorldSpawnServiceRegistry _spawnRegistry;
        private readonly IActorRegistry _actorRegistry;
        private readonly IDependencyProvider _provider;
        private readonly SceneResetHookRegistry _hookRegistry;

        public SceneResetRuntimeFactory(
            string sceneName,
            ISimulationGateService gateService,
            IWorldSpawnServiceRegistry spawnRegistry,
            IActorRegistry actorRegistry,
            IDependencyProvider provider,
            SceneResetHookRegistry hookRegistry)
        {
            _sceneName = sceneName ?? string.Empty;
            _gateService = gateService;
            _spawnRegistry = spawnRegistry;
            _actorRegistry = actorRegistry;
            _provider = provider;
            _hookRegistry = hookRegistry;
        }

        public void LogOptionalDependencies(bool verboseLogs)
        {
            if (_hookRegistry == null)
            {
                DebugUtility.LogWarning(typeof(SceneResetController),
                    $"SceneResetHookRegistry não encontrado para a cena '{_sceneName}'. Hooks via registry ficarão desativados.");
            }

            if (_gateService == null)
            {
                DebugUtility.LogWarning(typeof(SceneResetController),
                    $"ISimulationGateService não injetado para a cena '{_sceneName}'. Reset seguirá sem gate.");
            }

            if (verboseLogs && _hookRegistry != null)
            {
                DebugUtility.LogVerbose(typeof(SceneResetController),
                    $"SceneReset runtime pronto. scene='{_sceneName}', hooksCount='{_hookRegistry.Hooks.Count}'.");
            }
        }

        public bool HasCriticalDependencies()
        {
            bool valid = true;

            if (_spawnRegistry == null)
            {
                DebugUtility.LogError(typeof(SceneResetController),
                    $"Sem IWorldSpawnServiceRegistry para a cena '{_sceneName}'. Ciclo de vida não pode continuar.");
                valid = false;
            }

            if (_actorRegistry == null)
            {
                DebugUtility.LogError(typeof(SceneResetController),
                    $"Sem IActorRegistry para a cena '{_sceneName}'. Ciclo de vida não pode continuar.");
                valid = false;
            }

            return valid;
        }

        public SceneResetRunner CreateRunner()
        {
            return new SceneResetRunner(
                _gateService,
                _spawnRegistry,
                _actorRegistry,
                provider: _provider,
                hookRegistry: _hookRegistry,
                sceneName: _sceneName);
        }

        public void Cleanup(bool verboseLogs)
        {
            if (_hookRegistry != null)
            {
                try
                {
                    if (verboseLogs)
                    {
                        DebugUtility.Log(typeof(SceneResetController),
                            $"Limpando SceneResetHookRegistry na destruição do controller. scene='{_sceneName}', hooksCount='{_hookRegistry.Hooks.Count}'");
                    }

                    _hookRegistry.Clear();
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning(typeof(SceneResetController),
                        $"Falha ao limpar SceneResetHookRegistry na destruição do controller. scene='{_sceneName}'. {ex}");
                }
            }

            if (_spawnRegistry != null)
            {
                try
                {
                    if (verboseLogs)
                    {
                        DebugUtility.Log(typeof(SceneResetController),
                            $"Limpando IWorldSpawnServiceRegistry na destruição do controller. scene='{_sceneName}', servicesCount='{_spawnRegistry.Services.Count}'");
                    }

                    _spawnRegistry.Clear();
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning(typeof(SceneResetController),
                        $"Falha ao limpar IWorldSpawnServiceRegistry na destruição do controller. scene='{_sceneName}'. {ex}");
                }
            }
        }
    }
}
