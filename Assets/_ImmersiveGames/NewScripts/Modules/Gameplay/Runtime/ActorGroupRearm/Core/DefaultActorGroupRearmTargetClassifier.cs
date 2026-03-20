using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Core
{
    /// <summary>
    /// Classificador padrão de alvos de reset.
    /// - ByActorKind: usa IActorKindProvider como contrato canônico de grupo.
    /// - ActorIdSet: usa ActorRegistry.TryGetActor para os ids do request.
    /// </summary>
    public sealed class DefaultActorGroupRearmTargetClassifier : IActorGroupRearmTargetClassifier
    {
        public void CollectTargets(ActorGroupRearmRequest request, IActorRegistry actorRegistry, List<IActor> results)
        {
            results.Clear();

            if (actorRegistry == null)
            {
                return;
            }

            switch (request.Target)
            {
                case ActorGroupRearmTarget.ActorIdSet:
                    AddByActorIdSet(request, actorRegistry, results);
                    return;

                case ActorGroupRearmTarget.ByActorKind:
                    AddByActorKind(actorRegistry, results, request.ActorKind);
                    return;

                default:
                    return;
            }
        }

        private static void AddByActorIdSet(ActorGroupRearmRequest request, IActorRegistry actorRegistry, List<IActor> results)
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

                if (ActorKindMatching.MatchesActorKind(actor, kind))
                {
                    results.Add(actor);
                }
            }
        }
    }
}

