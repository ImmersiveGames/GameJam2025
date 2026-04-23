using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorSystem.Contracts.Outbound;
using _ImmersiveGames.NewScripts.ActorSystem.ReadModel;

namespace _ImmersiveGames.NewScripts.ActorSystem.Semantic
{
    public interface IActorSystemRelevantActorResolver
    {
        string Resolve(
            ActorSystemSemanticContext context,
            List<ActorRuntimePresenceSnapshot> runtimeActors);
    }

    public interface IActorSystemReadModelService
    {
        ActorSystemReadModelSnapshot Current { get; }
        bool TryGetCurrent(out ActorSystemReadModelSnapshot snapshot);
        ActorSystemReadModelSnapshot Refresh();
        void Clear(string reason = null);
    }

    public sealed class ActorSystemDefaultRelevantActorResolver : IActorSystemRelevantActorResolver
    {
        public string Resolve(
            ActorSystemSemanticContext context,
            List<ActorRuntimePresenceSnapshot> runtimeActors)
        {
            if (runtimeActors == null || runtimeActors.Count == 0)
            {
                return string.Empty;
            }

            if (context.HasPrimaryParticipant)
            {
                for (int index = 0; index < runtimeActors.Count; index += 1)
                {
                    ActorRuntimePresenceSnapshot candidate = runtimeActors[index];
                    if (candidate.IsValid && string.Equals(candidate.ActorId, context.PrimaryParticipantId, StringComparison.Ordinal))
                    {
                        return candidate.ActorId;
                    }
                }
            }

            if (context.HasLocalParticipant)
            {
                for (int index = 0; index < runtimeActors.Count; index += 1)
                {
                    ActorRuntimePresenceSnapshot candidate = runtimeActors[index];
                    if (candidate.IsValid && string.Equals(candidate.ActorId, context.LocalParticipantId, StringComparison.Ordinal))
                    {
                        return candidate.ActorId;
                    }
                }
            }

            for (int index = 0; index < runtimeActors.Count; index += 1)
            {
                ActorRuntimePresenceSnapshot candidate = runtimeActors[index];
                if (candidate.IsValid && candidate.IsActive)
                {
                    return candidate.ActorId;
                }
            }

            for (int index = 0; index < runtimeActors.Count; index += 1)
            {
                if (runtimeActors[index].IsValid)
                {
                    return runtimeActors[index].ActorId;
                }
            }

            return string.Empty;
        }
    }
}
