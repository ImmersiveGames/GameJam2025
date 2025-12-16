using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Orquestra o ciclo de reset do mundo de forma determinística.
    /// Responsável por garantir a ordem: Acquire Gate → Despawn → Spawn → Release Gate.
    /// </summary>
    public sealed class WorldLifecycleOrchestrator
    {
        private readonly ISimulationGateService _gateService;
        private readonly IReadOnlyList<IWorldSpawnService> _spawnServices;
        private readonly IActorRegistry _actorRegistry;

        public WorldLifecycleOrchestrator(
            ISimulationGateService gateService,
            IReadOnlyList<IWorldSpawnService> spawnServices,
            IActorRegistry actorRegistry)
        {
            _gateService = gateService;
            _spawnServices = spawnServices ?? Array.Empty<IWorldSpawnService>();
            _actorRegistry = actorRegistry;
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

                await RunPhaseAsync("Despawn", service => service.DespawnAsync());
                LogActorRegistryCount("After Despawn");

                await RunPhaseAsync("Spawn", service => service.SpawnAsync());
                LogActorRegistryCount("After Spawn");

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
