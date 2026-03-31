using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Game.Gameplay.Actors.Core;
using _ImmersiveGames.NewScripts.Game.Gameplay.GameplayReset.Integration;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Hooks;
using _ImmersiveGames.NewScripts.Orchestration.SceneReset.Spawn;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Domain;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.SceneReset.Runtime
{
    internal sealed class SceneResetHookCatalog
    {
        private static readonly List<(string Label, IActorLifecycleHook Hook)> EmptyActorHookList = new();
        private readonly Dictionary<Transform, ActorHookCacheEntry> _actorHookCache = new();
        private readonly SceneResetHookScopeFilter _scopeFilter;
        private readonly SceneResetHookSourceResolver _sourceResolver;

        public SceneResetHookCatalog(
            IReadOnlyList<IWorldSpawnService> spawnServices,
            IDependencyProvider provider,
            string sceneName,
            SceneResetHookRegistry hookRegistry,
            WorldResetContext? resetContext)
        {
            _scopeFilter = new SceneResetHookScopeFilter(resetContext);
            _sourceResolver = new SceneResetHookSourceResolver(spawnServices, provider, sceneName, hookRegistry);
        }

        public List<(string Label, ISceneResetHook Hook)> CollectWorldHooks()
        {
            List<(string Label, ISceneResetHook Hook)> hooks = _sourceResolver.CollectWorldHooks(_scopeFilter);
            hooks.Sort((left, right) => SceneResetHookOrdering.CompareHookEntriesWithScope(left, right, _scopeFilter));
            return hooks;
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

                if (!_scopeFilter.ShouldInclude(hook))
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

            actorHooks.Sort((left, right) => SceneResetHookOrdering.CompareHookEntriesWithScope(left, right, _scopeFilter));
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
            return _scopeFilter.ShouldInclude(candidate);
        }

        public List<IActorGroupGameplayResetWorldParticipant> CollectScopedParticipants()
        {
            return _sourceResolver.CollectScopedParticipants();
        }

        public int CompareResetScopeParticipants(IActorGroupGameplayResetWorldParticipant left, IActorGroupGameplayResetWorldParticipant right)
        {
            return _scopeFilter.CompareResetScopeParticipants(left, right);
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
    }
}

