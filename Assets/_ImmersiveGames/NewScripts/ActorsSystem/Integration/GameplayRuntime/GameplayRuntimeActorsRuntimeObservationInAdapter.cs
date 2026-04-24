using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Integration.GameplayRuntime
{
    /// <summary>
    /// Read-only runtime observation adapter. This does not define axis ownership.
    /// </summary>
    public sealed class GameplayRuntimeActorsRuntimeObservationInAdapter : IActorsRuntimeObservationInPort
    {
        private readonly IDependencyProvider _dependencyProvider;

        public GameplayRuntimeActorsRuntimeObservationInAdapter(IDependencyProvider dependencyProvider)
        {
            _dependencyProvider = dependencyProvider ?? throw new ArgumentNullException(nameof(dependencyProvider));
        }

        public bool TryGetCurrent(out ActorsRuntimeObservationSnapshot snapshot)
        {
            snapshot = ActorsRuntimeObservationSnapshot.Empty;

            if (!_dependencyProvider.TryGet<IActorRegistry>(out IActorRegistry actorRegistry) || actorRegistry == null)
            {
                return false;
            }

            var records = new List<RuntimeActorObservationRecord>(actorRegistry.Count);
            var signatureBuilder = new StringBuilder(128);
            signatureBuilder.Append("runtime-observation|");

            foreach (IActor actor in actorRegistry.Actors)
            {
                if (actor == null || string.IsNullOrWhiteSpace(actor.ActorId))
                {
                    continue;
                }

                ActorRole role = ResolveRole(actor);
                ActorOperationalRecipeKind recipeKind = ResolveRecipeKind(actor);
                var record = new RuntimeActorObservationRecord(
                    new RuntimeActorId(actor.ActorId),
                    actor.DisplayName,
                    role,
                    recipeKind,
                    actor.IsActive);

                records.Add(record);
                signatureBuilder.Append(record.RuntimeActorId.Value);
                signatureBuilder.Append(':');
                signatureBuilder.Append((int)record.ObservedRole);
                signatureBuilder.Append(':');
                signatureBuilder.Append((int)record.ObservedRecipeKind);
                signatureBuilder.Append(':');
                signatureBuilder.Append(record.IsActive ? '1' : '0');
                signatureBuilder.Append('|');
            }

            signatureBuilder.Append("count:");
            signatureBuilder.Append(records.Count);

            snapshot = new ActorsRuntimeObservationSnapshot(signatureBuilder.ToString(), records.ToArray());
            return snapshot.IsValid;
        }

        private static ActorRole ResolveRole(IActor actor)
        {
            if (actor is IActorKindProvider kindProvider)
            {
                switch (kindProvider.Kind)
                {
                    case ActorKind.Player:
                        return ActorRole.Player;
                    case ActorKind.Dummy:
                    case ActorKind.Eater:
                        return ActorRole.Actor;
                    default:
                        return ActorRole.Unknown;
                }
            }

            return ActorRole.Unknown;
        }

        private static ActorOperationalRecipeKind ResolveRecipeKind(IActor actor)
        {
            if (actor is not IActorKindProvider kindProvider)
            {
                return ActorOperationalRecipeKind.Unknown;
            }

            return kindProvider.Kind switch
            {
                ActorKind.Player => ActorOperationalRecipeKind.Player,
                ActorKind.Dummy => ActorOperationalRecipeKind.Dummy,
                ActorKind.Eater => ActorOperationalRecipeKind.Eater,
                _ => ActorOperationalRecipeKind.Unknown
            };
        }
    }
}
