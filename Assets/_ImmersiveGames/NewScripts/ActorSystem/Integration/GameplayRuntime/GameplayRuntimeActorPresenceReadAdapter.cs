using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorSystem.Contracts.Outbound;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;

namespace _ImmersiveGames.NewScripts.ActorSystem.Integration.GameplayRuntime
{
    /// <summary>
    /// Read-only adapter from GameplayRuntime ActorRegistry into ActorSystem presence port.
    /// </summary>
    public sealed class GameplayRuntimeActorPresenceReadAdapter : IActorPresenceReadPort
    {
        private readonly IDependencyProvider _dependencyProvider;
        private bool _missingRegistryLogged;
        private bool _resolvedRegistryLogged;

        public GameplayRuntimeActorPresenceReadAdapter(IDependencyProvider dependencyProvider)
        {
            _dependencyProvider = dependencyProvider ?? throw new ArgumentNullException(nameof(dependencyProvider));
        }

        public bool TryGetAll(List<ActorRuntimePresenceSnapshot> target)
        {
            if (target == null)
            {
                return false;
            }

            target.Clear();
            if (!TryResolveActorRegistry(out IActorRegistry actorRegistry))
            {
                return false;
            }

            foreach (IActor actor in actorRegistry.Actors)
            {
                if (actor == null || string.IsNullOrWhiteSpace(actor.ActorId))
                {
                    continue;
                }

                ActorKind kind = actor is IActorKindProvider kindProvider ? kindProvider.Kind : ActorKind.Unknown;
                target.Add(new ActorRuntimePresenceSnapshot(actor.ActorId, actor.DisplayName, kind, actor.IsActive));
            }

            return target.Count > 0;
        }

        public bool TryGetByActorId(string actorId, out ActorRuntimePresenceSnapshot snapshot)
        {
            snapshot = default;
            if (string.IsNullOrWhiteSpace(actorId))
            {
                return false;
            }

            if (!TryResolveActorRegistry(out IActorRegistry actorRegistry))
            {
                return false;
            }

            if (!actorRegistry.TryGetActor(actorId.Trim(), out IActor actor) || actor == null)
            {
                return false;
            }

            ActorKind kind = actor is IActorKindProvider kindProvider ? kindProvider.Kind : ActorKind.Unknown;
            snapshot = new ActorRuntimePresenceSnapshot(actor.ActorId, actor.DisplayName, kind, actor.IsActive);
            return snapshot.IsValid;
        }

        private bool TryResolveActorRegistry(out IActorRegistry actorRegistry)
        {
            actorRegistry = null;

            if (!_dependencyProvider.TryGet<IActorRegistry>(out actorRegistry) || actorRegistry == null)
            {
                if (!_missingRegistryLogged)
                {
                    _missingRegistryLogged = true;
                    _resolvedRegistryLogged = false;
                    DebugUtility.LogVerbose(typeof(GameplayRuntimeActorPresenceReadAdapter),
                        "[OBS][ActorSystem][GameplayRuntime] IActorRegistry indisponivel no escopo atual; leitura de presenca retornara vazia.",
                        DebugUtility.Colors.Info);
                }

                return false;
            }

            if (!_resolvedRegistryLogged)
            {
                _resolvedRegistryLogged = true;
                _missingRegistryLogged = false;
                DebugUtility.LogVerbose(typeof(GameplayRuntimeActorPresenceReadAdapter),
                    "[OBS][ActorSystem][GameplayRuntime] IActorRegistry resolvido para leitura read-only de presenca.",
                    DebugUtility.Colors.Info);
            }

            return true;
        }
    }
}
