using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Orquestra o ciclo de reset do mundo de forma determinística.
    /// Responsável por garantir a ordem: Acquire Gate → (Hooks) → Despawn → (Hooks) → Spawn → (Hooks) → Release Gate.
    /// </summary>
    public sealed class WorldLifecycleOrchestrator
    {
        private readonly ISimulationGateService _gateService;
        private readonly IReadOnlyList<IWorldSpawnService> _spawnServices;
        private readonly IActorRegistry _actorRegistry;
        private readonly IDependencyProvider _provider;
        private readonly string _sceneName;
        private readonly WorldLifecycleHookRegistry _hookRegistry;

        public WorldLifecycleOrchestrator(
            ISimulationGateService gateService,
            IReadOnlyList<IWorldSpawnService> spawnServices,
            IActorRegistry actorRegistry,
            IDependencyProvider provider = null,
            string sceneName = null,
            WorldLifecycleHookRegistry hookRegistry = null)
        {
            _gateService = gateService;
            _spawnServices = spawnServices ?? Array.Empty<IWorldSpawnService>();
            _actorRegistry = actorRegistry;
            _provider = provider;
            _sceneName = sceneName;
            _hookRegistry = hookRegistry;
        }

        public async Task ResetWorldAsync()
        {
            var resetWatch = Stopwatch.StartNew();
            DebugUtility.Log(typeof(WorldLifecycleOrchestrator), "World Reset Started");

            IDisposable gateHandle = null;
            var gateAcquired = false;
            var completed = false;

            try
            {
                LogActorRegistryCount("Reset start");

                if (_gateService != null)
                {
                    gateHandle = _gateService.Acquire(WorldLifecycleTokens.WorldResetToken);
                    gateAcquired = true;
                    DebugUtility.Log(typeof(WorldLifecycleOrchestrator), "Gate Acquired");
                }
                else
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                        "ISimulationGateService ausente: reset seguirá sem gate.");
                }

                await RunHookPhaseAsync("OnBeforeDespawn", hook => hook.OnBeforeDespawnAsync());

                await RunPhaseAsync("Despawn", service => service.DespawnAsync());
                LogActorRegistryCount("After Despawn");

                await RunHookPhaseAsync("OnAfterDespawn", hook => hook.OnAfterDespawnAsync());
                await RunHookPhaseAsync("OnBeforeSpawn", hook => hook.OnBeforeSpawnAsync());

                await RunPhaseAsync("Spawn", service => service.SpawnAsync());
                LogActorRegistryCount("After Spawn");

                await RunHookPhaseAsync("OnAfterSpawn", hook => hook.OnAfterSpawnAsync());

                completed = true;
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleOrchestrator), $"World reset failed: {ex}");
                throw;
            }
            finally
            {
                resetWatch.Stop();

                if (gateHandle != null)
                {
                    gateHandle.Dispose();
                }

                if (gateAcquired)
                {
                    DebugUtility.Log(typeof(WorldLifecycleOrchestrator), "Gate Released");
                }

                if (completed)
                {
                    DebugUtility.Log(typeof(WorldLifecycleOrchestrator), "World Reset Completed");
                }

                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"Reset duration: {resetWatch.ElapsedMilliseconds}ms");
            }
        }

        private async Task RunPhaseAsync(string phaseName, Func<IWorldSpawnService, Task> phaseAction)
        {
            var phaseWatch = Stopwatch.StartNew();
            DebugUtility.Log(typeof(WorldLifecycleOrchestrator), $"{phaseName} started");

            if (_spawnServices == null || _spawnServices.Count == 0)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                    $"{phaseName} skipped (no spawn services registered).");
                phaseWatch.Stop();
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"{phaseName} duration: {phaseWatch.ElapsedMilliseconds}ms");
                return;
            }

            foreach (var service in _spawnServices)
            {
                if (service == null)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                        $"{phaseName} service é nulo e será ignorado.");
                    continue;
                }

                DebugUtility.Log(typeof(WorldLifecycleOrchestrator),
                    $"{phaseName} service started: {service.Name}");

                var serviceWatch = Stopwatch.StartNew();
                try
                {
                    await phaseAction(service);
                }
                finally
                {
                    serviceWatch.Stop();
                    DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                        $"{phaseName} service duration: {service.Name} => {serviceWatch.ElapsedMilliseconds}ms");
                }

                DebugUtility.Log(typeof(WorldLifecycleOrchestrator),
                    $"{phaseName} service completed: {service.Name}");
            }

            phaseWatch.Stop();
            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{phaseName} duration: {phaseWatch.ElapsedMilliseconds}ms");
        }

        private async Task RunHookPhaseAsync(string hookName, Func<IWorldLifecycleHook, Task> hookAction)
        {
            var spawnServiceHooksCount = 0;
            if (_spawnServices != null)
            {
                for (int i = 0; i < _spawnServices.Count; i++)
                {
                    if (_spawnServices[i] is IWorldLifecycleHook)
                    {
                        spawnServiceHooksCount++;
                    }
                }
            }

            var sceneHooks = ResolveSceneHooks();
            var sceneHooksCount = sceneHooks.Count;

            var registryHooks = ResolveRegistryHooks();
            var registryHooksCount = registryHooks.Count;

            if (spawnServiceHooksCount == 0 && sceneHooksCount == 0 && registryHooksCount == 0)
            {
                return;
            }

            var totalHooks = spawnServiceHooksCount + sceneHooksCount + registryHooksCount;

            var phaseWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{hookName} phase started (hooks={totalHooks})");

            try
            {
                foreach (var service in _spawnServices)
                {
                    if (service == null)
                    {
                        DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                            $"{hookName} hook service é nulo e será ignorado.");
                        continue;
                    }

                    if (service is not IWorldLifecycleHook hook)
                    {
                        continue;
                    }

                    await RunHookAsync(hookName, service.Name, hook, hookAction);
                }

                if (sceneHooksCount > 0)
                {
                    DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                        $"Scene hooks detected: {sceneHooksCount}");
                    DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                        $"Executing scene lifecycle hooks for {hookName}");

                    foreach (var hook in sceneHooks)
                    {
                        if (hook == null)
                        {
                            DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                                $"{hookName} scene hook é nulo e será ignorado.");
                            continue;
                        }

                        await RunHookAsync(hookName, hook.GetType().Name, hook, hookAction);
                    }
                }

                if (registryHooksCount > 0)
                {
                    DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                        $"Registry hooks detected: {registryHooksCount}");
                    DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                        $"Executing registry lifecycle hooks for {hookName}");

                    foreach (var hook in registryHooks)
                    {
                        if (hook == null)
                        {
                            DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                                $"{hookName} registry hook é nulo e será ignorado.");
                            continue;
                        }

                        await RunHookAsync(hookName, hook.GetType().Name, hook, hookAction);
                    }
                }
            }
            finally
            {
                phaseWatch.Stop();
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"{hookName} phase duration: {phaseWatch.ElapsedMilliseconds}ms");
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"{hookName} phase completed");
            }
        }

        private static async Task RunHookAsync(
            string hookName,
            string serviceName,
            IWorldLifecycleHook hook,
            Func<IWorldLifecycleHook, Task> hookAction)
        {
            var hookWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{hookName} started: {serviceName}");

            try
            {
                await hookAction(hook);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                    $"{hookName} falhou para {serviceName}: {ex}");
                throw;
            }
            finally
            {
                hookWatch.Stop();
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"{hookName} duration: {serviceName} => {hookWatch.ElapsedMilliseconds}ms");
            }

            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{hookName} completed: {serviceName}");
        }

        private IReadOnlyList<IWorldLifecycleHook> ResolveSceneHooks()
        {
            if (_provider == null || string.IsNullOrWhiteSpace(_sceneName))
            {
                return Array.Empty<IWorldLifecycleHook>();
            }

            var sceneHooks = new List<IWorldLifecycleHook>();
            _provider.GetAllForScene(_sceneName, sceneHooks);

            return sceneHooks.Count == 0 ? Array.Empty<IWorldLifecycleHook>() : sceneHooks;
        }

        private IReadOnlyList<IWorldLifecycleHook> ResolveRegistryHooks()
        {
            if (_hookRegistry == null || _hookRegistry.Hooks.Count == 0)
            {
                return Array.Empty<IWorldLifecycleHook>();
            }

            return _hookRegistry.Hooks;
        }

        private void LogActorRegistryCount(string label)
        {
            if (_actorRegistry == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                    $"ActorRegistry ausente ao logar '{label}'.");
                return;
            }

            DebugUtility.Log(typeof(WorldLifecycleOrchestrator),
                $"ActorRegistry count at '{label}': {_actorRegistry.Count}");
        }
    }

    public static class WorldLifecycleTokens
    {
        public const string WorldResetToken = "WorldLifecycle.WorldReset";
    }
}
