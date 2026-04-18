using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.SimulationGate;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Spawn;
using _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Hooks;
namespace _ImmersiveGames.NewScripts.ResetFlow.SceneReset.Bindings
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
            if (_gateService == null)
            {
                DebugUtility.LogWarning(typeof(SceneResetController),
                    $"ISimulationGateService nao injetado para a cena '{_sceneName}'. Reset seguira sem gate.");
            }

            if (verboseLogs)
            {
                int hooksCount = _hookRegistry?.Hooks.Count ?? 0;
                DebugUtility.LogVerbose(typeof(SceneResetController),
                    $"SceneReset runtime pronto. scene='{_sceneName}', hooksCount='{hooksCount}'.");
            }
        }

        public bool HasCriticalDependencies()
        {
            bool valid = true;

            if (_provider == null)
            {
                DebugUtility.LogError(typeof(SceneResetController),
                    $"Sem IDependencyProvider para a cena '{_sceneName}'. Runtime local de reset nao pode compor hooks/participants de cena.");
                valid = false;
            }

            if (_hookRegistry == null)
            {
                DebugUtility.LogError(typeof(SceneResetController),
                    $"Sem SceneResetHookRegistry para a cena '{_sceneName}'. Ciclo de reset local nao pode continuar sem hooks registry.");
                valid = false;
            }

            if (_spawnRegistry == null)
            {
                DebugUtility.LogError(typeof(SceneResetController),
                    $"Sem IWorldSpawnServiceRegistry para a cena '{_sceneName}'. Ciclo de vida nao pode continuar.");
                valid = false;
            }

            if (_actorRegistry == null)
            {
                DebugUtility.LogError(typeof(SceneResetController),
                    $"Sem IActorRegistry para a cena '{_sceneName}'. Ciclo de vida nao pode continuar.");
                valid = false;
            }

            return valid;
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
                            $"Limpando SceneResetHookRegistry na destruicao do controller. scene='{_sceneName}', hooksCount='{_hookRegistry.Hooks.Count}'");
                    }

                    _hookRegistry.Clear();
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning(typeof(SceneResetController),
                        $"Falha ao limpar SceneResetHookRegistry na destruicao do controller. scene='{_sceneName}'. {ex}");
                }
            }

            if (_spawnRegistry != null)
            {
                try
                {
                    if (verboseLogs)
                    {
                        DebugUtility.Log(typeof(SceneResetController),
                            $"Limpando IWorldSpawnServiceRegistry na destruicao do controller. scene='{_sceneName}', servicesCount='{_spawnRegistry.Services.Count}'");
                    }

                    _spawnRegistry.Clear();
                }
                catch (Exception ex)
                {
                    DebugUtility.LogWarning(typeof(SceneResetController),
                        $"Falha ao limpar IWorldSpawnServiceRegistry na destruicao do controller. scene='{_sceneName}'. {ex}");
                }
            }
        }
    }
}
