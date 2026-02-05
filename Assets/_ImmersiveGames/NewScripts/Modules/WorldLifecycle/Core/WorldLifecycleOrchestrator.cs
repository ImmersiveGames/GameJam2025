using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors;
using _ImmersiveGames.NewScripts.Modules.Gameplay.GameplayReset;
using _ImmersiveGames.NewScripts.Modules.Gates;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Hooks;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Spawn;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldReset;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Core
{
    /// <summary>
    /// Orquestra o ciclo de reset do mundo de forma determinística.
    /// Responsável por garantir a ordem: Acquire Gate → (Hooks) → Despawn → (Hooks) → Spawn → (Hooks) → Release Gate.
    /// </summary>
    public sealed class WorldLifecycleOrchestrator
    {
        // Guardrail de QA: manter a ordem das fases como exatamente descrita aqui.
        // Não reorganizar o fluxo do reset ou mover responsabilidades entre classes.
        private readonly ISimulationGateService _gateService;
        private readonly IReadOnlyList<IWorldSpawnService> _spawnServices;
        private readonly IActorRegistry _actorRegistry;
        private readonly IDependencyProvider _provider;
        private readonly string _sceneName;
        private readonly WorldLifecycleHookRegistry _hookRegistry;
        private readonly Dictionary<Transform, ActorHookCacheEntry> _actorHookCache = new();
        // Sentinel compartilhado para "nenhum hook encontrado" — nunca deve ser mutado.
        private static readonly List<(string Label, IActorLifecycleHook Hook)> EmptyActorHookList = new();

        private const long SlowHookWarningMs = 50;

        private WorldResetContext? _currentResetContext;

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
            await ResetInternalAsync(
                context: null,
                gateToken: WorldLifecycleTokens.WorldResetToken,
                startLog: "World Reset Started",
                completionLog: "World Reset Completed");
        }

        public async Task ResetScopesAsync(IReadOnlyList<WorldResetScope> scopes, string reason)
        {
            if (scopes == null || scopes.Count == 0)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                    "Scoped reset ignored: scopes vazios ou nulos.");
                return;
            }

            if (scopes.Any(t => t == WorldResetScope.World))
            {
                DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                    "WorldResetScope.World não é suportado em soft reset. Utilize ResetWorldAsync para hard reset.");
                return;
            }

            var context = new WorldResetContext(reason, scopes, WorldResetFlags.SoftReset);
            await ResetInternalAsync(
                context,
                SimulationGateTokens.SoftReset,
                startLog: $"Scoped Reset Started ({context})",
                completionLog: "Scoped Reset Completed");
        }

        private async Task RunStepAsync(string stepName, Func<IWorldSpawnService, Task> stepAction)
        {
            var stepWatch = Stopwatch.StartNew();
            DebugUtility.Log(typeof(WorldLifecycleOrchestrator), $"{stepName} started");

            if (_spawnServices == null || _spawnServices.Count == 0)
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                    $"{stepName} skipped (no spawn services registered).");
                stepWatch.Stop();
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"{stepName} duration: {stepWatch.ElapsedMilliseconds}ms");
                return;
            }

            foreach (var service in _spawnServices)
            {
                if (service == null)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                        $"{stepName} service é nulo e será ignorado.");
                    continue;
                }

                if (!ShouldIncludeForScopes(service))
                {
                    DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                        $"{stepName} service skipped by scope filter: {service.Name}");
                    continue;
                }

                DebugUtility.Log(typeof(WorldLifecycleOrchestrator),
                    $"{stepName} service started: {service.Name}");

                var serviceWatch = Stopwatch.StartNew();
                try
                {
                    await stepAction(service);
                }
                finally
                {
                    serviceWatch.Stop();
                    DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                        $"{stepName} service duration: {service.Name} => {serviceWatch.ElapsedMilliseconds}ms");
                }

                DebugUtility.Log(typeof(WorldLifecycleOrchestrator),
                    $"{stepName} service completed: {service.Name}");
            }

            stepWatch.Stop();
            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{stepName} duration: {stepWatch.ElapsedMilliseconds}ms");
        }

        private async Task RunHookStepAsync(string hookName, Func<IWorldLifecycleHook, Task> hookAction)
        {
            List<(string Label, IWorldLifecycleHook Hook)> collectedHooks = CollectHooks();
            if (collectedHooks.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"{hookName} step skipped (hooks=0)");
                return;
            }

            int totalHooks = collectedHooks.Count;

            var stepWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{hookName} step started (hooks={totalHooks})");

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
                stepWatch.Stop();
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"{hookName} step duration: {stepWatch.ElapsedMilliseconds}ms");
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"{hookName} step completed");
            }
        }

        private async Task RunActorHooksBeforeDespawnAsync()
        {
            List<IActor> actors = SnapshotActors();
            if (actors.Count == 0)
            {
                return;
            }

            await RunActorHooksAsync("OnBeforeActorDespawn", actors, hook => hook.OnBeforeActorDespawnAsync());
        }

        private async Task RunActorHooksAfterSpawnAsync()
        {
            List<IActor> actors = SnapshotActors();
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
            var stepWatch = Stopwatch.StartNew();
            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{hookName} actor hooks step started (actors={actors.Count})");

            foreach (var actor in actors)
            {
                if (actor == null)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                        $"{hookName} actor é nulo e será ignorado.");
                    continue;
                }

                string actorLabel = GetActorLabel(actor);
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

                    if (!TryGetCachedActorHooks(transform, out List<(string Label, IActorLifecycleHook Hook)> actorHooks))
                    {
                        actorHooks = CollectActorHooks(transform);
                        CacheActorHooks(transform, actorHooks);
                    }

                    if (actorHooks.Count == 0)
                    {
                        continue;
                    }

                    LogHookOrder($"{hookName} ({actorLabel})", actorHooks);

                    foreach (var actorHook in actorHooks)
                    {
                        await RunActorHookAsync(hookName, actorLabel, actorHook.Label, actorHook.Hook, hookAction);
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

            stepWatch.Stop();
            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"{hookName} actor hooks step duration: {stepWatch.ElapsedMilliseconds}ms");
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

                    if (ShouldIncludeForScopes(lifecycleHook))
                    {
                        hooks.Add((service.Name, lifecycleHook));
                    }
                }
            }

            IReadOnlyList<IWorldLifecycleHook> sceneHooks = ResolveSceneHooks();
            hooks.AddRange(from hook in sceneHooks where ShouldIncludeForScopes(hook) select (hook?.GetType().Name ?? "<null scene hook>", hook));

            IReadOnlyList<IWorldLifecycleHook> registryHooks = ResolveRegistryHooks();
            foreach (var hook in registryHooks)
            {
                if (ShouldIncludeForScopes(hook))
                {
                    hooks.Add((hook?.GetType().Name ?? "<null registry hook>", hook));
                }
            }

            hooks.Sort(CompareHooksWithScope);

            return hooks;
        }

        private static int CompareHooks<THook>(
            (string Label, THook Hook) left,
            (string Label, THook Hook) right)
            where THook : class
        {
            int leftOrder = GetHookOrder(left.Hook);
            int rightOrder = GetHookOrder(right.Hook);

            int orderComparison = leftOrder.CompareTo(rightOrder);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            string leftTypeName = left.Hook?.GetType().FullName ?? string.Empty;
            string rightTypeName = right.Hook?.GetType().FullName ?? string.Empty;

            return string.Compare(leftTypeName, rightTypeName, StringComparison.Ordinal);
        }

        private static int GetHookOrder(object hook)
        {
            if (hook is IWorldLifecycleHookOrdered orderedHook)
            {
                return orderedHook.Order;
            }

            return 0;
        }

        private static void LogHookOrder<THook>(string hookName, List<(string Label, THook Hook)> collectedHooks)
            where THook : class
        {
            if (collectedHooks == null || collectedHooks.Count == 0)
            {
                return;
            }

            string[] orderedLabels = collectedHooks
                .Select(entry =>
                {
                    int order = GetHookOrder(entry.Hook);
                    string typeName = entry.Hook?.GetType().Name ?? entry.Label;
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

        private bool TryGetCachedActorHooks(
            Transform transform,
            out List<(string Label, IActorLifecycleHook Hook)> hooks)
        {
            hooks = null;

            if (transform == null)
            {
                return false;
            }

            if (!_actorHookCache.TryGetValue(transform, out var entry))
            {
                return false;
            }

            if (entry.Root == null || entry.Root != transform.root)
            {
                _actorHookCache.Remove(transform);
                return false;
            }

            hooks = entry.Hooks;
            return hooks != null;
        }

        private List<(string Label, IActorLifecycleHook Hook)> CollectActorHooks(Transform transform)
        {
            MonoBehaviour[] components = transform.GetComponentsInChildren<MonoBehaviour>(true);
            if (components == null || components.Length == 0)
            {
                return EmptyActorHookList;
            }

            List<(string Label, IActorLifecycleHook Hook)> actorHooks = null;

            foreach (var component in components)
            {
                if (component is not IActorLifecycleHook hook)
                {
                    continue;
                }

                if (!ShouldIncludeForScopes(hook))
                {
                    continue;
                }

                actorHooks ??= new List<(string Label, IActorLifecycleHook Hook)>();
                actorHooks.Add((component.GetType().Name, hook));
            }

            if (actorHooks == null)
            {
                return EmptyActorHookList;
            }

            actorHooks.Sort(CompareHooksWithScope);
            return actorHooks;
        }

        private void CacheActorHooks(
            Transform transform,
            List<(string Label, IActorLifecycleHook Hook)> hooks)
        {
            if (transform == null || hooks == null || hooks.Count == 0)
            {
                return;
            }

            if (ReferenceEquals(hooks, EmptyActorHookList))
            {
                return;
            }

            var root = transform.root;
            _actorHookCache[transform] = new ActorHookCacheEntry(root, hooks);
        }

        /// <summary>
        /// Limpa o cache de hooks por ator ao final de cada ciclo de reset.
        /// Mantém a semântica de cache por ciclo, mesmo em caso de falhas.
        /// </summary>
        private void ClearActorHookCacheForCycle()
        {
            _actorHookCache.Clear();
        }

        private sealed class ActorHookCacheEntry
        {
            public ActorHookCacheEntry(Transform root, List<(string Label, IActorLifecycleHook Hook)> hooks)
            {
                Root = root;
                Hooks = hooks;
            }

            public Transform Root { get; }

            public List<(string Label, IActorLifecycleHook Hook)> Hooks { get; }
        }

        private void WarnIfNoSpawnServices()
        {
            if (_spawnServices != null && _spawnServices.Count > 0)
            {
                return;
            }

            DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                $"Nenhum spawn service disponível para a cena '{_sceneName ?? "<unknown>"}'. Reset seguirá apenas com hooks.");
        }

        private IDisposable TryAcquireGate(string gateToken, out bool gateAcquired)
        {
            gateAcquired = false;

            if (_gateService == null || string.IsNullOrWhiteSpace(gateToken))
            {
                DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                    "ISimulationGateService ausente: reset seguirá sem gate.");
                return null;
            }

            var gateHandle = _gateService.Acquire(gateToken);
            gateAcquired = true;
            DebugUtility.Log(typeof(WorldLifecycleOrchestrator), $"Gate Acquired ({gateToken})");
            return gateHandle;
        }

        private static void ReleaseGate(IDisposable gateHandle, bool gateAcquired)
        {
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
        }

        private async Task RunResetPipelineAsync(WorldResetContext? context)
        {
            // Ordem fixa do reset: hooks pré-despawn → hooks de atores → despawn →
            // hooks pós-despawn/pré-spawn → (scoped participants) → hooks pré-spawn → spawn → hooks de atores → hooks finais.
            await RunHookStepAsync("OnBeforeDespawn", hook => hook.OnBeforeDespawnAsync());
            await RunActorHooksBeforeDespawnAsync();

            await RunStepAsync("Despawn", service => service.DespawnAsync());
            LogActorRegistryCount("After Despawn");

            await RunHookStepAsync("OnAfterDespawn", hook => hook.OnAfterDespawnAsync());

            if (context != null)
            {
                await RunScopedParticipantsResetAsync(context.Value);
            }

            await RunHookStepAsync("OnBeforeSpawn", hook => hook.OnBeforeSpawnAsync());

            await RunStepAsync("Spawn", service => service.SpawnAsync());
            LogActorRegistryCount("After Spawn");

            await RunActorHooksAfterSpawnAsync();
            await RunHookStepAsync("OnAfterSpawn", hook => hook.OnAfterSpawnAsync());
        }
        private async Task ResetInternalAsync(
            WorldResetContext? context,
            string gateToken,
            string startLog,
            string completionLog)
        {
            WorldResetContext? previousContext = _currentResetContext;
            _currentResetContext = context;

            var resetWatch = Stopwatch.StartNew();
            DebugUtility.Log(typeof(WorldLifecycleOrchestrator), startLog);

            IDisposable gateHandle = null;
            bool gateAcquired = false;
            bool completed = false;

            try
            {
                LogActorRegistryCount("Reset start");

                WarnIfNoSpawnServices();
                gateHandle = TryAcquireGate(gateToken, out gateAcquired);

                await RunResetPipelineAsync(context);

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

                ClearActorHookCacheForCycle();

                ReleaseGate(gateHandle, gateAcquired);

                if (completed)
                {
                    DebugUtility.Log(typeof(WorldLifecycleOrchestrator), completionLog);
                }

                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"Reset duration: {resetWatch.ElapsedMilliseconds}ms");

                _currentResetContext = previousContext;
            }
        }

        private bool ShouldIncludeForScopes(object candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (_currentResetContext == null)
            {
                return true;
            }

            var context = _currentResetContext.Value;
            if (candidate is IGameplayResetParticipant scopedParticipant)
            {
                return context.ContainsScope(scopedParticipant.Scope);
            }

            return false;
        }

        private int CompareScope(object left, object right)
        {
            if (_currentResetContext == null)
            {
                return 0;
            }

            int leftScope = GetParticipantScope(left);
            int rightScope = GetParticipantScope(right);

            return leftScope.CompareTo(rightScope);
        }

        private static int GetParticipantScope(object participant)
        {
            return participant is IGameplayResetParticipant scoped ? (int)scoped.Scope : int.MaxValue;
        }

        private int CompareHooksWithScope<THook>(
            (string Label, THook Hook) left,
            (string Label, THook Hook) right)
            where THook : class
        {
            int scopeComparison = CompareScope(left.Hook, right.Hook);
            if (scopeComparison != 0)
            {
                return scopeComparison;
            }

            return CompareHooks(left, right);
        }

        private async Task RunScopedParticipantsResetAsync(WorldResetContext context)
        {
            // Soft reset executa apenas participantes que declaram escopo via IGameplayResetParticipant.
            // Caso seja necessário um hook global no futuro, isso deve acontecer por meio de um escopo/interface explícitos.
            List<IGameplayResetParticipant> participants = CollectScopedParticipants();
            if (participants.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    "Scoped reset participants step skipped (participants=0)");
                return;
            }

            var filtered = new List<IGameplayResetParticipant>();
            foreach (var participant in participants)
            {
                if (participant == null)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                        "Scoped reset participant é nulo e será ignorado.");
                    continue;
                }

                if (!context.ContainsScope(participant.Scope))
                {
                    DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                        $"Scoped reset participant skipped by scope: {participant.GetType().Name}");
                    continue;
                }

                filtered.Add(participant);
            }

            if (filtered.Count == 0)
            {
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    "Scoped reset participants step skipped (filtered=0)");
                return;
            }

            filtered.Sort(CompareResetScopeParticipants);
            LogScopedParticipantOrder(filtered);

            foreach (var participant in filtered)
            {
                string participantType = participant.GetType().FullName ?? participant.GetType().Name;
                var watch = Stopwatch.StartNew();
                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"Scoped reset started: {participantType}");

                try
                {
                    await participant.ResetAsync(context);
                }
                catch (Exception ex)
                {
                    DebugUtility.LogError(typeof(WorldLifecycleOrchestrator),
                        $"Scoped reset falhou para {participantType}: {ex}");
                    throw;
                }
                finally
                {
                    watch.Stop();
                    if (watch.ElapsedMilliseconds > SlowHookWarningMs)
                    {
                        DebugUtility.LogWarning(typeof(WorldLifecycleOrchestrator),
                            $"Scoped reset lento: {participantType} levou {watch.ElapsedMilliseconds}ms.");
                    }

                    DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                        $"Scoped reset duration: {participantType} => {watch.ElapsedMilliseconds}ms");
                }

                DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                    $"Scoped reset completed: {participantType}");
            }
        }

        private List<IGameplayResetParticipant> CollectScopedParticipants()
        {
            var participants = new List<IGameplayResetParticipant>();
            var uniques = new HashSet<IGameplayResetParticipant>(ResetScopeParticipantReferenceComparer.Instance);

            if (_spawnServices != null)
            {
                foreach (var service in _spawnServices)
                {
                    if (service is IGameplayResetParticipant participant)
                    {
                        if (uniques.Add(participant))
                        {
                            participants.Add(participant);
                        }
                    }
                }
            }

            if (_provider != null && !string.IsNullOrWhiteSpace(_sceneName))
            {
                var sceneParticipants = new List<IGameplayResetParticipant>();
                _provider.GetAllForScene(_sceneName, sceneParticipants);

                participants.AddRange(sceneParticipants.Where(participant => participant != null).Where(participant => uniques.Add(participant)));
            }

            IReadOnlyList<IWorldLifecycleHook> sceneHooks = ResolveSceneHooks();
            for (int i = 0; i < sceneHooks.Count; i++)
            {
                if (sceneHooks[i] is IGameplayResetParticipant participant)
                {
                    if (uniques.Add(participant))
                    {
                        participants.Add(participant);
                    }
                }
            }

            IReadOnlyList<IWorldLifecycleHook> registryHooks = ResolveRegistryHooks();
            foreach (var t in registryHooks)
            {
                if (t is IGameplayResetParticipant participant)
                {
                    if (uniques.Add(participant))
                    {
                        participants.Add(participant);
                    }
                }
            }

            return participants;
        }

        private int CompareResetScopeParticipants(IGameplayResetParticipant left, IGameplayResetParticipant right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int scopeComparison = left.Scope.CompareTo(right.Scope);
            if (scopeComparison != 0)
            {
                return scopeComparison;
            }

            int orderComparison = left.Order.CompareTo(right.Order);
            if (orderComparison != 0)
            {
                return orderComparison;
            }

            string leftType = left.GetType().FullName ?? left.GetType().Name;
            string rightType = right.GetType().FullName ?? right.GetType().Name;

            return string.Compare(leftType, rightType, StringComparison.Ordinal);
        }

        private void LogScopedParticipantOrder(List<IGameplayResetParticipant> participants)
        {
            string[] orderedLabels = participants
                .Select(participant =>
                {
                    string typeName = participant?.GetType().Name ?? "<null participant>";
                    return $"{typeName}(scope={participant?.Scope}, order={participant?.Order})";
                })
                .ToArray();

            DebugUtility.LogVerbose(typeof(WorldLifecycleOrchestrator),
                $"Scoped reset execution order: {string.Join(", ", orderedLabels)}");
        }

        private sealed class ResetScopeParticipantReferenceComparer : IEqualityComparer<IGameplayResetParticipant>
        {
            public static readonly ResetScopeParticipantReferenceComparer Instance = new();

            public bool Equals(IGameplayResetParticipant x, IGameplayResetParticipant y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(IGameplayResetParticipant obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}


