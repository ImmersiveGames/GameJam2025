using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Servicos owner de Spawn material/despawn/respawn, apenas sequenciados por SceneReset.
        /// </summary>
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

            // Snapshot de observabilidade para hooks e participacao de reset.
            // Nao altera ownership nem readiness: isso continua no trilho de Spawn.
            ActorRegistry.GetActors(actors);
            return actors;
        }

        public bool TryGetCachedActorHooks(Transform transform, out List<(string Label, IActorLifecycleHook Hook)> hooks)
        {
            return _hookCatalog.TryGetCachedActorHooks(transform, out hooks);
        }

        public bool ShouldIncludeForScopes(object candidate)
        {
            return _hookCatalog.ShouldIncludeForScopes(candidate);
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
            // Participantes de reset sao bridges de execucao, nao owners do actor.
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

