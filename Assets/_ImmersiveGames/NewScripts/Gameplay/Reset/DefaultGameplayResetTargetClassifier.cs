using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Reset
{
    /// <summary>
    /// Classificador padrão de alvos de reset.
    /// - PlayersOnly: ator que tenha PlayerActor no mesmo GameObject.
    /// - EaterOnly: procura por componente chamado "EaterActor" (string-based), para não acoplar compile-time.
    /// - ActorIdSet: usa ActorRegistry.TryGetActor para os ids do request.
    /// - AllActorsInScene: usa todos do registry.
    /// </summary>
    public sealed class DefaultGameplayResetTargetClassifier : IGameplayResetTargetClassifier
    {
        public void CollectTargets(GameplayResetRequest request, IActorRegistry actorRegistry, List<IActor> results)
        {
            results.Clear();

            if (actorRegistry == null)
            {
                return;
            }

            switch (request.Target)
            {
                case GameplayResetTarget.AllActorsInScene:
                    AddAllActors(actorRegistry, results);
                    return;

                case GameplayResetTarget.ActorIdSet:
                    AddByActorIdSet(request, actorRegistry, results);
                    return;

                case GameplayResetTarget.PlayersOnly:
                    AddByPredicate(actorRegistry, results, IsPlayerActor);
                    return;

                case GameplayResetTarget.EaterOnly:
                    AddByPredicate(actorRegistry, results, IsEaterActor);
                    return;

                default:
                    AddAllActors(actorRegistry, results);
                    return;
            }
        }

        private static void AddAllActors(IActorRegistry actorRegistry, List<IActor> results)
        {
            if (actorRegistry.Actors == null)
                return;

            foreach (var actor in actorRegistry.Actors)
            {
                if (actor != null)
                    results.Add(actor);
            }
        }

        private static void AddByActorIdSet(GameplayResetRequest request, IActorRegistry actorRegistry, List<IActor> results)
        {
            var ids = request.ActorIds;
            if (ids == null || ids.Count == 0)
                return;

            for (int i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                if (actorRegistry.TryGetActor(id, out var actor) && actor != null)
                {
                    results.Add(actor);
                }
            }
        }

        private static void AddByPredicate(IActorRegistry actorRegistry, List<IActor> results, Func<IActor, bool> predicate)
        {
            if (actorRegistry.Actors == null)
                return;

            foreach (var actor in actorRegistry.Actors)
            {
                if (actor == null)
                    continue;

                if (predicate(actor))
                    results.Add(actor);
            }
        }

        private static bool IsPlayerActor(IActor actor)
        {
            var t = actor.Transform;
            if (t == null)
                return false;

            return t.GetComponent<PlayerActor>() != null;
        }

        private static bool IsEaterActor(IActor actor)
        {
            var t = actor.Transform;
            if (t == null)
                return false;

            // Mantém a feature funcional sem depender de um tipo compile-time.
            // Quando existir um EaterActor concreto, isso passa a classificar corretamente.
            var eater = t.GetComponent("EaterActor");
            return eater != null;
        }
    }
}
