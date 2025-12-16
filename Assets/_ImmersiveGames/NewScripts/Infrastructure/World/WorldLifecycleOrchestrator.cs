using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.Scripts.GameplaySystems.Execution;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.World
{
    /// <summary>
    /// Orquestra o ciclo de reset do mundo de forma determinística.
    /// Responsável por garantir a ordem: Acquire Gate → (Hooks) → Despawn → (Hooks) → Spawn → (Hooks) → Release Gate.
    /// </summary>
    public sealed class WorldLifecycleOrchestrator
    {
        // Guardrail de QA: manter a ordem das fases exatamente como descrita aqui.
        // Não reorganizar o fluxo do reset ou mover responsabilidades entre classes.
        private readonly ISimulationGateService _gateService;
        private readonly IReadOnlyList<IWorldSpawnService> _spawnServices;
        private readonly IActorRegistry _actorRegistry;
        private readonly IDependencyProvider _provider;
        private readonly string _sceneName;
        private readonly WorldLifecycleHookRegistry _hookRegistry;

        private const long SlowHookWarningMs = 50;

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
            // Guardrail: o registry deve vir do bootstrapper (escopo de cena), nunca ser criado aqui.
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

                if (_spawnServices == null || _spawnServices.Count == 0)
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                        $"Nenhum spawn service disponível para a cena '{_sceneName ?? "<unknown>"}'. Reset seguirá apenas com hooks.");
                }

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

                // Ordem fixa do reset: hooks pré-despawn → hooks de atores → despawn →
                // hooks pós-despawn/pré-spawn → spawn → hooks de atores → hooks finais.
                await RunHookPhaseAsync("OnBeforeDespawn", hook => hook.OnBeforeDespawnAsync());
                await RunActorHooksBeforeDespawnAsync();

                await RunPhaseAsync("Despawn", service => service.DespawnAsync());
                LogActorRegistryCount("After Despawn");

                await RunHookPhaseAsync("OnAfterDespawn", hook => hook.OnAfterDespawnAsync());
                await RunHookPhaseAsync("OnBeforeSpawn", hook => hook.OnBeforeSpawnAsync());

                await RunPhaseAsync("Spawn", service => service.SpawnAsync());
                LogActorRegistryCount("After Spawn");

                await RunActorHooksAfterSpawnAsync();
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
                    try
                    {
                        gateHandle.Dispose();
                    }
                    catch (Exception ex)
                    {
                        DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                            $"Failed to release gate handle: {ex}");
                    }
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
            var collectedHooks = CollectHooks();
            if (collectedHooks.Count == 0)
            {
                return;
            }

            var totalHooks = collectedHooks.Count;

            var phaseWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{hookName} phase started (hooks={totalHooks})");

            try
            {
                LogHookOrder(hookName, collectedHooks);

                foreach (var hookEntry in collectedHooks)
                {
                    if (hookEntry.Hook == null)
                    {
                        DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                            $"{hookName} hook '{hookEntry.Label}' é nulo e será ignorado.");
                        continue;
                    }

                    await RunHookAsync(hookName, hookEntry.Label, hookEntry.Hook, hookAction);
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

        private async Task RunActorHooksBeforeDespawnAsync()
        {
            var actors = SnapshotActors();
            if (actors.Count == 0)
            {
                return;
            }

            await RunActorHooksAsync("OnBeforeActorDespawn", actors, hook => hook.OnBeforeActorDespawnAsync());
        }

        private async Task RunActorHooksAfterSpawnAsync()
        {
            var actors = SnapshotActors();
            if (actors.Count == 0)
            {
                return;
            }

            await RunActorHooksAsync("OnAfterActorSpawn", actors, hook => hook.OnAfterActorSpawnAsync());
        }

        private async Task RunActorHooksAsync(
            string hookName,
            List<IActor> actors,
            Func<IActorLifecycleHook, Task> hookAction)
        {
            var phaseWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{hookName} actor hooks phase started (actors={actors.Count})");

            foreach (var actor in actors)
            {
                if (actor == null)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                        $"{hookName} actor é nulo e será ignorado.");
                    continue;
                }

                var actorLabel = GetActorLabel(actor);
                var actorWatch = Stopwatch.StartNew();
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"{hookName} actor started: {actorLabel}");

                try
                {
                    var transform = actor.Transform;
                    if (transform == null)
                    {
                        DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                            $"{hookName} ignorado para {actorLabel}: Transform ausente.");
                        continue;
                    }

                    var components = transform.GetComponentsInChildren<MonoBehaviour>(true);
                    if (components == null || components.Length == 0)
                    {
                        continue;
                    }

                    foreach (var component in components)
                    {
                        if (component is not IActorLifecycleHook hook)
                        {
                            continue;
                        }

                        await RunActorHookAsync(hookName, actorLabel, component.GetType().Name, hook, hookAction);
                    }
                }
                finally
                {
                    actorWatch.Stop();
                    DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                        $"{hookName} actor duration: {actorLabel} => {actorWatch.ElapsedMilliseconds}ms");
                    DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                        $"{hookName} actor completed: {actorLabel}");
                }
            }

            phaseWatch.Stop();
            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{hookName} actor hooks phase duration: {phaseWatch.ElapsedMilliseconds}ms");
        }

        private static async Task RunActorHookAsync(
            string hookName,
            string actorLabel,
            string hookLabel,
            IActorLifecycleHook hook,
            Func<IActorLifecycleHook, Task> hookAction)
        {
            var hookWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{hookName} started: {hookLabel} (actor={actorLabel})");

            try
            {
                await hookAction(hook);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                    $"{hookName} falhou para {hookLabel} (actor={actorLabel}): {ex}");
                throw;
            }
            finally
            {
                hookWatch.Stop();
                if (hookWatch.ElapsedMilliseconds > SlowHookWarningMs)
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                        $"{hookName} lento: {hookLabel} (actor={actorLabel}) levou {hookWatch.ElapsedMilliseconds}ms.");
                }
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"{hookName} duration: {hookLabel} (actor={actorLabel}) => {hookWatch.ElapsedMilliseconds}ms");
            }

            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{hookName} completed: {hookLabel} (actor={actorLabel})");
        }

        private List<IActor> SnapshotActors()
        {
            var actors = new List<IActor>();

            if (_actorRegistry == null)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                    "ActorRegistry ausente ao criar snapshot de atores. Nenhum hook de ator será executado.");
                return actors;
            }

            _actorRegistry.GetActors(actors);
            return actors;
        }

        private static string GetActorLabel(IActor actor)
        {
            if (!string.IsNullOrWhiteSpace(actor.ActorId))
            {
                return actor.ActorId;
            }

            if (!string.IsNullOrWhiteSpace(actor.DisplayName))
            {
                return actor.DisplayName;
            }

            return actor.GetType().Name;
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
                if (hookWatch.ElapsedMilliseconds > SlowHookWarningMs)
                {
                    DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                        $"{hookName} lento: {serviceName} levou {hookWatch.ElapsedMilliseconds}ms.");
                }
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

        private List<(string Label, IWorldLifecycleHook Hook)> CollectHooks()
        {
            var hooks = new List<(string Label, IWorldLifecycleHook Hook)>();

            if (_spawnServices != null)
            {
                foreach (var service in _spawnServices)
                {
                    if (service == null)
                    {
                        continue;
                    }

                    if (service is not IWorldLifecycleHook lifecycleHook)
                    {
                        continue;
                    }

                    hooks.Add((service.Name, lifecycleHook));
                }
            }

            var sceneHooks = ResolveSceneHooks();
            foreach (var hook in sceneHooks)
            {
                hooks.Add((hook?.GetType().Name ?? "<null scene hook>", hook));
            }

            var registryHooks = ResolveRegistryHooks();
            foreach (var hook in registryHooks)
            {
                hooks.Add((hook?.GetType().Name ?? "<null registry hook>", hook));
            }

            hooks.Sort(CompareHooks);

            return hooks;
        }

        private static int CompareHooks(
            (string Label, IWorldLifecycleHook Hook) left,
            (string Label, IWorldLifecycleHook Hook) right)
        {
            var leftOrder = GetHookOrder(left.Hook);
            var rightOrder = GetHookOrder(right.Hook);

            var orderComparison = leftOrder.CompareTo(rightOrder);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            var leftTypeName = left.Hook?.GetType().FullName ?? string.Empty;
            var rightTypeName = right.Hook?.GetType().FullName ?? string.Empty;

            return string.Compare(leftTypeName, rightTypeName, StringComparison.Ordinal);
        }

        private static int GetHookOrder(IWorldLifecycleHook hook)
        {
            if (hook is IOrderedLifecycleHook orderedHook)
            {
                return orderedHook.Order;
            }

            return 0;
        }

        private static void LogHookOrder(string hookName, List<(string Label, IWorldLifecycleHook Hook)> collectedHooks)
        {
            if (collectedHooks == null || collectedHooks.Count == 0)
            {
                return;
            }

            var orderedLabels = collectedHooks
                .Select(entry =>
                {
                    var order = GetHookOrder(entry.Hook);
                    var typeName = entry.Hook?.GetType().Name ?? entry.Label;
                    return $"{typeName}(order={order})";
                })
                .ToArray();

            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{hookName} execution order: {string.Join(", ", orderedLabels)}");
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
