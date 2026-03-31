using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.SimulationGate;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Integration;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Hooks;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Spawn;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime
{
    internal sealed class SceneResetContext
    {
        private readonly SceneResetGateLease _gateLease;
        private readonly SceneResetHookCatalog _hookCatalog;
        private readonly string _sceneName;

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
            SpawnServices = spawnServices ?? Array.Empty<IWorldSpawnService>();
            ActorRegistry = actorRegistry;
            _sceneName = string.IsNullOrWhiteSpace(sceneName) ? "<unknown>" : sceneName;
            ResetContext = resetContext;
            StartLog = startLog ?? string.Empty;
            CompletionLog = completionLog ?? string.Empty;

            _gateLease = new SceneResetGateLease(gateService, gateToken);
            _hookCatalog = new SceneResetHookCatalog(SpawnServices, provider, sceneName, hookRegistry, resetContext);
        }

        public const long SlowHookWarningMs = 50;

        public IReadOnlyList<IWorldSpawnService> SpawnServices { get; }
        public IActorRegistry ActorRegistry { get; }
        public WorldResetContext? ResetContext { get; }
        public string StartLog { get; }
        public string CompletionLog { get; }

        public void WarnIfNoSpawnServices()
        {
            if (SpawnServices.Count > 0)
            {
                return;
            }

            DebugUtility.LogWarning(typeof(SceneResetFacade),
                $"Nenhum spawn service disponível para a cena '{_sceneName}'. Reset seguirá apenas com hooks.");
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
            _gateLease.AcquireIfNeeded();
        }

        public void ReleaseGateIfNeeded()
        {
            _gateLease.ReleaseIfNeeded();
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

                if (!_hookCatalog.ShouldIncludeForScopes(service))
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
            return _hookCatalog.CollectWorldHooks();
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
            return _hookCatalog.TryGetCachedActorHooks(transform, out hooks);
        }

        public List<(string Label, IActorLifecycleHook Hook)> CollectActorHooks(Transform transform)
        {
            return _hookCatalog.CollectActorHooks(transform);
        }

        public void CacheActorHooks(Transform transform, List<(string Label, IActorLifecycleHook Hook)> hooks)
        {
            _hookCatalog.CacheActorHooks(transform, hooks);
        }

        public void ClearActorHookCacheForCycle()
        {
            _hookCatalog.ClearActorHookCacheForCycle();
        }

        public List<IActorGroupGameplayResetWorldParticipant> CollectScopedParticipants()
        {
            return _hookCatalog.CollectScopedParticipants();
        }

        public int CompareResetScopeParticipants(IActorGroupGameplayResetWorldParticipant left, IActorGroupGameplayResetWorldParticipant right)
        {
            return _hookCatalog.CompareResetScopeParticipants(left, right);
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

        private static int GetHookOrder(object hook)
        {
            return SceneResetHookOrdering.GetHookOrder(hook);
        }
    }
}

