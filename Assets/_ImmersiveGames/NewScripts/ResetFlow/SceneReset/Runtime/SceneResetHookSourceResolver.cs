using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ImmersiveGames.GameJam2025.Infrastructure.Composition;
using ImmersiveGames.GameJam2025.Game.Gameplay.GameplayReset.Integration;
using ImmersiveGames.GameJam2025.Orchestration.SceneReset.Hooks;
using ImmersiveGames.GameJam2025.Game.Gameplay.Spawn;
namespace ImmersiveGames.GameJam2025.Orchestration.SceneReset.Runtime
{
    internal sealed class SceneResetHookSourceResolver
    {
        private readonly IReadOnlyList<IWorldSpawnService> _spawnServices;
        private readonly IDependencyProvider _provider;
        private readonly string _sceneName;
        private readonly SceneResetHookRegistry _hookRegistry;

        public SceneResetHookSourceResolver(
            IReadOnlyList<IWorldSpawnService> spawnServices,
            IDependencyProvider provider,
            string sceneName,
            SceneResetHookRegistry hookRegistry)
        {
            _spawnServices = spawnServices ?? Array.Empty<IWorldSpawnService>();
            _provider = provider;
            _sceneName = sceneName ?? string.Empty;
            _hookRegistry = hookRegistry;
        }

        public List<(string Label, ISceneResetHook Hook)> CollectWorldHooks(SceneResetHookScopeFilter scopeFilter)
        {
            var hooks = new List<(string Label, ISceneResetHook Hook)>();

            foreach (IWorldSpawnService service in _spawnServices)
            {
                if (service is not ISceneResetHook lifecycleHook)
                {
                    continue;
                }

                if (scopeFilter == null || scopeFilter.ShouldInclude(lifecycleHook))
                {
                    hooks.Add((service.Name, lifecycleHook));
                }
            }

            foreach (ISceneResetHook hook in ResolveSceneHooks())
            {
                if (scopeFilter == null || scopeFilter.ShouldInclude(hook))
                {
                    hooks.Add((hook?.GetType().Name ?? "<null scene hook>", hook));
                }
            }

            foreach (ISceneResetHook hook in ResolveRegistryHooks())
            {
                if (scopeFilter == null || scopeFilter.ShouldInclude(hook))
                {
                    hooks.Add((hook?.GetType().Name ?? "<null registry hook>", hook));
                }
            }

            return hooks;
        }

        public List<IActorGroupGameplayResetWorldParticipant> CollectScopedParticipants()
        {
            var participants = new List<IActorGroupGameplayResetWorldParticipant>();
            var uniques = new HashSet<IActorGroupGameplayResetWorldParticipant>(ResetScopeParticipantReferenceComparer.Instance);

            foreach (IWorldSpawnService service in _spawnServices)
            {
                if (service is IActorGroupGameplayResetWorldParticipant participant && uniques.Add(participant))
                {
                    participants.Add(participant);
                }
            }

            if (_provider != null && !string.IsNullOrWhiteSpace(_sceneName))
            {
                var sceneParticipants = new List<IActorGroupGameplayResetWorldParticipant>();
                _provider.GetAllForScene(_sceneName, sceneParticipants);
                participants.AddRange(sceneParticipants.Where(participant => participant != null && uniques.Add(participant)));
            }

            foreach (ISceneResetHook hook in ResolveSceneHooks())
            {
                if (hook is IActorGroupGameplayResetWorldParticipant participant && uniques.Add(participant))
                {
                    participants.Add(participant);
                }
            }

            foreach (ISceneResetHook hook in ResolveRegistryHooks())
            {
                if (hook is IActorGroupGameplayResetWorldParticipant participant && uniques.Add(participant))
                {
                    participants.Add(participant);
                }
            }

            return participants;
        }

        private IReadOnlyList<ISceneResetHook> ResolveSceneHooks()
        {
            if (_provider == null || string.IsNullOrWhiteSpace(_sceneName))
            {
                return Array.Empty<ISceneResetHook>();
            }

            var sceneHooks = new List<ISceneResetHook>();
            _provider.GetAllForScene(_sceneName, sceneHooks);
            return sceneHooks.Count == 0 ? Array.Empty<ISceneResetHook>() : sceneHooks;
        }

        private IReadOnlyList<ISceneResetHook> ResolveRegistryHooks()
        {
            if (_hookRegistry == null || _hookRegistry.Hooks.Count == 0)
            {
                return Array.Empty<ISceneResetHook>();
            }

            return _hookRegistry.Hooks;
        }

        private sealed class ResetScopeParticipantReferenceComparer : IEqualityComparer<IActorGroupGameplayResetWorldParticipant>
        {
            public static readonly ResetScopeParticipantReferenceComparer Instance = new();

            public bool Equals(IActorGroupGameplayResetWorldParticipant x, IActorGroupGameplayResetWorldParticipant y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(IActorGroupGameplayResetWorldParticipant obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}


