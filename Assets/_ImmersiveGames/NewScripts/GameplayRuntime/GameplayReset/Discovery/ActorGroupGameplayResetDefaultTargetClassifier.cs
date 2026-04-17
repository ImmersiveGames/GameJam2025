using System.Collections.Generic;
using _ImmersiveGames.NewScripts.GameplayRuntime.ActorRegistry;
using _ImmersiveGames.NewScripts.GameplayRuntime.Authoring.Actors.Core;
using _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Core;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.GameplayReset.Discovery
{
    /// <summary>
    /// Classificador padr�o de alvos de reset.
    /// - ByActorKind: usa IActorKindProvider como contrato can�nico de grupo.
    /// - ActorIdSet: usa ActorRegistry.TryGetActor para os ids do request.
    /// </summary>
    public sealed class ActorGroupGameplayResetDefaultTargetClassifier : IActorGroupGameplayResetTargetClassifier
    {
        public void CollectTargets(ActorGroupGameplayResetRequest request, IActorRegistry actorRegistry, List<IActor> results)
        {
            results.Clear();

            if (actorRegistry == null)
            {
                return;
            }

            switch (request.Target)
            {
                case ActorGroupGameplayResetTarget.ActorIdSet:
                    AddByActorIdSet(request, actorRegistry, results);
                    return;

                case ActorGroupGameplayResetTarget.ByActorKind:
                    AddByActorKind(actorRegistry, results, request.ActorKind);
                    return;

                default:
                    return;
            }
        }

        private static void AddByActorIdSet(ActorGroupGameplayResetRequest request, IActorRegistry actorRegistry, List<IActor> results)
        {
            IReadOnlyList<string> ids = request.ActorIds;
            if (ids == null || ids.Count == 0)
            {
                return;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (actorRegistry.TryGetActor(id, out var actor) && actor != null)
                {
                    results.Add(actor);
                }
            }
        }

        private static void AddByActorKind(IActorRegistry actorRegistry, List<IActor> results, ActorKind kind)
        {
            if (actorRegistry.Actors == null || kind == ActorKind.Unknown)
            {
                return;
            }

            foreach (var actor in actorRegistry.Actors)
            {
                if (actor == null)
                {
                    continue;
                }

                if (ActorKindMatchRules.MatchesActorKind(actor, kind))
                {
                    results.Add(actor);
                }
            }
        }
    }
}



