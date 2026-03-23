using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Interop;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Hooks;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Spawn;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime.SceneReset
{
    internal sealed class SceneResetContext
    {
        private static readonly List<(string Label, IActorLifecycleHook Hook)> EmptyActorHookList = new();
        private readonly Dictionary<Transform, ActorHookCacheEntry> _actorHookCache = new();

        public SceneResetContext(
            ISimulationGateService gateService,
            IReadOnlyList<IWorldSpawnService> spawnServices,
            IActorRegistry actorRegistry,
            IDependencyProvider provider,
            string sceneName,
            SceneResetHookRegistry hookRegistry,
            WorldResetContext? resetContext,
            string gateToken,
            string startLog,
            string completionLog)
        {
            GateService = gateService;
            SpawnServices = spawnServices ?? Array.Empty<IWorldSpawnService>();
            ActorRegistry = actorRegistry;
            Provider = provider;
            SceneName = sceneName;
            HookRegistry = hookRegistry;
            ResetContext = resetContext;
            GateToken = gateToken;
            StartLog = startLog ?? string.Empty;
            CompletionLog = completionLog ?? string.Empty;
        }

        public const long SlowHookWarningMs = 50;

        public ISimulationGateService GateService { get; }
        public IReadOnlyList<IWorldSpawnService> SpawnServices { get; }
        public IActorRegistry ActorRegistry { get; }
        public IDependencyProvider Provider { get; }
        public string SceneName { get; }
        public SceneResetHookRegistry HookRegistry { get; }
        public WorldResetContext? ResetContext { get; }
        public string GateToken { get; }
        public string StartLog { get; }
        public string CompletionLog { get; }
        public IDisposable GateHandle { get; private set; }
        public bool GateAcquired { get; private set; }

        public void WarnIfNoSpawnServices()
        {
            if (SpawnServices.Count > 0)
            {
                return;
            }

            DebugUtility.LogWarning(typeof(SceneResetFacade),
                $"Nenhum spawn service disponível para a cena '{SceneName ?? "<unknown>"}'. Reset seguirá apenas com hooks.");
        }

        public void LogActorRegistryCount(string label)
        {
            if (ActorRegistry == null)
            {
                DebugUtility.LogWarning(typeof(SceneResetFacade),
                    $"ActorRegistry ausente ao logar '{label}'.");
                return;
            }

            DebugUtility.Log(typeof(SceneResetFacade),
                $"ActorRegistry count at '{label}': {ActorRegistry.Count}");
        }

        public void AcquireGateIfNeeded()
        {
            GateHandle = TryAcquireGate();
        }

        public void ReleaseGateIfNeeded()
        {
            if (GateHandle != null)
            {
                try
                {
                    GateHandle.Dispose();
                }
                catch (Exception ex)
                {
                    DebugUtility.LogError(typeof(SceneResetFacade),
                        $"Failed to release gate handle: {ex}");
                }
            }

            if (GateAcquired)
            {
                DebugUtility.Log(typeof(SceneResetFacade), "Gate Released");
            }
        }

        private IDisposable TryAcquireGate()
        {
            GateAcquired = false;

            if (GateService == null || string.IsNullOrWhiteSpace(GateToken))
            {
                DebugUtility.LogWarning(typeof(SceneResetFacade),
                    "ISimulationGateService ausente: reset seguirá sem gate.");
                return null;
            }

            IDisposable gateHandle = GateService.Acquire(GateToken);
            GateAcquired = true;
            DebugUtility.Log(typeof(SceneResetFacade), $"Gate Acquired ({GateToken})");
            return gateHandle;
        }

        public async Task RunSpawnServicesStepAsync(string stepName, Func<IWorldSpawnService, Task> stepAction)
        {
            var stepWatch = Stopwatch.StartNew();
            DebugUtility.Log(typeof(SceneResetFacade), $"{stepName} started");

            if (SpawnServices.Count == 0)
            {
                DebugUtility.LogWarning(typeof(SceneResetFacade),
                    $"{stepName} skipped (no spawn services registered).");
                stepWatch.Stop();
                DebugUtility.LogVerbose(typeof(SceneResetFacade),
                    $"{stepName} duration: {stepWatch.ElapsedMilliseconds}ms");
                return;
            }

            foreach (IWorldSpawnService service in SpawnServices)
            {
                if (service == null)
                {
                    DebugUtility.LogError(typeof(SceneResetFacade),
                        $"{stepName} service é nulo e será ignorado.");
                    continue;
                }

                if (!ShouldIncludeForScopes(service))
                {
                    DebugUtility.LogVerbose(typeof(SceneResetFacade),
                        $"{stepName} service skipped by scope filter: {service.Name}");
                    continue;
                }

                DebugUtility.Log(typeof(SceneResetFacade),
                    $"{stepName} service started: {service.Name}");

                var serviceWatch = Stopwatch.StartNew();
                try
                {
                    await stepAction(service);
                }
                finally
                {
                    serviceWatch.Stop();
                    DebugUtility.LogVerbose(typeof(SceneResetFacade),
                        $"{stepName} service duration: {service.Name} => {serviceWatch.ElapsedMilliseconds}ms");
                }

                DebugUtility.Log(typeof(SceneResetFacade),
                    $"{stepName} service completed: {service.Name}");
            }

            stepWatch.Stop();
            DebugUtility.LogVerbose(typeof(SceneResetFacade),
                $"{stepName} duration: {stepWatch.ElapsedMilliseconds}ms");
        }

        public List<(string Label, ISceneResetHook Hook)> CollectWorldHooks()
        {
            var hooks = new List<(string Label, ISceneResetHook Hook)>();

            foreach (IWorldSpawnService service in SpawnServices)
            {
                if (service is not ISceneResetHook lifecycleHook)
                {
                    continue;
                }

                if (ShouldIncludeForScopes(lifecycleHook))
                {
                    hooks.Add((service.Name, lifecycleHook));
                }
            }

            IReadOnlyList<ISceneResetHook> sceneHooks = ResolveSceneHooks();
            hooks.AddRange(from hook in sceneHooks where ShouldIncludeForScopes(hook) select (hook?.GetType().Name ?? "<null scene hook>", hook));

            IReadOnlyList<ISceneResetHook> registryHooks = ResolveRegistryHooks();
            foreach (ISceneResetHook hook in registryHooks)
            {
                if (ShouldIncludeForScopes(hook))
                {
                    hooks.Add((hook?.GetType().Name ?? "<null registry hook>", hook));
                }
            }

            hooks.Sort(CompareHooksWithScope);
            return hooks;
        }

        public List<IActor> SnapshotActors()
        {
            var actors = new List<IActor>();

            if (ActorRegistry == null)
            {
                DebugUtility.LogWarning(typeof(SceneResetFacade),
                    "ActorRegistry ausente ao criar snapshot de atores. Nenhum hook de ator será executado.");
                return actors;
            }

            ActorRegistry.GetActors(actors);
            return actors;
        }

        public bool TryGetCachedActorHooks(Transform transform, out List<(string Label, IActorLifecycleHook Hook)> hooks)
        {
            hooks = null;

            if (transform == null)
            {
                return false;
            }

            if (!_actorHookCache.TryGetValue(transform, out ActorHookCacheEntry entry))
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

        public List<(string Label, IActorLifecycleHook Hook)> CollectActorHooks(Transform transform)
        {
            MonoBehaviour[] components = transform.GetComponentsInChildren<MonoBehaviour>(true);
            if (components == null || components.Length == 0)
            {
                return EmptyActorHookList;
            }

            List<(string Label, IActorLifecycleHook Hook)> actorHooks = null;
            foreach (MonoBehaviour component in components)
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

        public void CacheActorHooks(Transform transform, List<(string Label, IActorLifecycleHook Hook)> hooks)
        {
            if (transform == null || hooks == null || hooks.Count == 0 || ReferenceEquals(hooks, EmptyActorHookList))
            {
                return;
            }

            _actorHookCache[transform] = new ActorHookCacheEntry(transform.root, hooks);
        }

        public void ClearActorHookCacheForCycle()
        {
            _actorHookCache.Clear();
        }

        public bool ShouldIncludeForScopes(object candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (ResetContext == null)
            {
                return true;
            }

            if (candidate is IActorGroupRearmWorldParticipant scopedParticipant)
            {
                return ResetContext.Value.ContainsScope(scopedParticipant.Scope);
            }

            return false;
        }

        public List<IActorGroupRearmWorldParticipant> CollectScopedParticipants()
        {
            var participants = new List<IActorGroupRearmWorldParticipant>();
            var uniques = new HashSet<IActorGroupRearmWorldParticipant>(ResetScopeParticipantReferenceComparer.Instance);

            foreach (IWorldSpawnService service in SpawnServices)
            {
                if (service is IActorGroupRearmWorldParticipant participant && uniques.Add(participant))
                {
                    participants.Add(participant);
                }
            }

            if (Provider != null && !string.IsNullOrWhiteSpace(SceneName))
            {
                var sceneParticipants = new List<IActorGroupRearmWorldParticipant>();
                Provider.GetAllForScene(SceneName, sceneParticipants);
                participants.AddRange(sceneParticipants.Where(participant => participant != null && uniques.Add(participant)));
            }

            foreach (ISceneResetHook hook in ResolveSceneHooks())
            {
                if (hook is IActorGroupRearmWorldParticipant participant && uniques.Add(participant))
                {
                    participants.Add(participant);
                }
            }

            foreach (ISceneResetHook hook in ResolveRegistryHooks())
            {
                if (hook is IActorGroupRearmWorldParticipant participant && uniques.Add(participant))
                {
                    participants.Add(participant);
                }
            }

            return participants;
        }

        public int CompareResetScopeParticipants(IActorGroupRearmWorldParticipant left, IActorGroupRearmWorldParticipant right)
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

        public static string GetActorLabel(IActor actor)
        {
            if (actor == null)
            {
                return "<null actor>";
            }

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

        public static void LogHookOrder<THook>(string hookName, List<(string Label, THook Hook)> collectedHooks)
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

            DebugUtility.LogVerbose(typeof(SceneResetFacade),
                $"{hookName} execution order: {string.Join(", ", orderedLabels)}");
        }

        private IReadOnlyList<ISceneResetHook> ResolveSceneHooks()
        {
            if (Provider == null || string.IsNullOrWhiteSpace(SceneName))
            {
                return Array.Empty<ISceneResetHook>();
            }

            var sceneHooks = new List<ISceneResetHook>();
            Provider.GetAllForScene(SceneName, sceneHooks);
            return sceneHooks.Count == 0 ? Array.Empty<ISceneResetHook>() : sceneHooks;
        }

        private IReadOnlyList<ISceneResetHook> ResolveRegistryHooks()
        {
            if (HookRegistry == null || HookRegistry.Hooks.Count == 0)
            {
                return Array.Empty<ISceneResetHook>();
            }

            return HookRegistry.Hooks;
        }

        private int CompareScope(object left, object right)
        {
            if (ResetContext == null)
            {
                return 0;
            }

            int leftScope = GetParticipantScope(left);
            int rightScope = GetParticipantScope(right);
            return leftScope.CompareTo(rightScope);
        }

        private static int GetParticipantScope(object participant)
        {
            return participant is IActorGroupRearmWorldParticipant scoped ? (int)scoped.Scope : int.MaxValue;
        }

        private int CompareHooksWithScope<THook>((string Label, THook Hook) left, (string Label, THook Hook) right)
            where THook : class
        {
            int scopeComparison = CompareScope(left.Hook, right.Hook);
            if (scopeComparison != 0)
            {
                return scopeComparison;
            }

            return CompareHooks(left, right);
        }

        private static int CompareHooks<THook>((string Label, THook Hook) left, (string Label, THook Hook) right)
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
            if (hook is ISceneResetHookOrdered orderedHook)
            {
                return orderedHook.Order;
            }

            return 0;
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

        private sealed class ResetScopeParticipantReferenceComparer : IEqualityComparer<IActorGroupRearmWorldParticipant>
        {
            public static readonly ResetScopeParticipantReferenceComparer Instance = new();

            public bool Equals(IActorGroupRearmWorldParticipant x, IActorGroupRearmWorldParticipant y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(IActorGroupRearmWorldParticipant obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
