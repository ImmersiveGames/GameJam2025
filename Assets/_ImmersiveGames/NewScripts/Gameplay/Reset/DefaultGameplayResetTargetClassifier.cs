using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.Actors;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Reset
{
    /// <summary>
    /// Classificador padrão de alvos de reset.
    /// - PlayersOnly: alias para ActorKind.Player.
    /// - EaterOnly: procura por componente chamado "EaterActor" (string-based), para não acoplar compile-time.
    /// - ActorIdSet: usa ActorRegistry.TryGetActor para os ids do request.
    /// - AllActorsInScene: usa todos do registry.
    /// - ByActorKind: filtra via IActorKindProvider.
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
                    AddByActorKind(actorRegistry, results, ActorKind.Player);
                    return;

                case GameplayResetTarget.ByActorKind:
                    AddByActorKind(actorRegistry, results, request.ActorKind);
                    return;

                case GameplayResetTarget.EaterOnly:
                    AddEatersKindFirstWithFallback(actorRegistry, results, out bool fallbackUsed);
                    if (fallbackUsed)
                    {
                        DebugUtility.LogWarning(typeof(DefaultGameplayResetTargetClassifier),
                            "GameplayResetTarget.EaterOnly using string-based fallback (EaterActor).");
                    }
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

        private static void AddByActorKind(IActorRegistry actorRegistry, List<IActor> results, ActorKind kind)
        {
            if (actorRegistry.Actors == null)
                return;

            if (kind == ActorKind.Unknown)
                return;

            foreach (var actor in actorRegistry.Actors)
            {
                if (actor == null)
                    continue;

                if (TryGetActorKind(actor, out var actorKind) && actorKind == kind)
                {
                    results.Add(actor);
                }
            }
        }

        private static bool TryGetActorKind(IActor actor, out ActorKind kind)
        {
            kind = ActorKind.Unknown;

            if (actor is not IActorKindProvider provider)
            {
                return false;
            }

            kind = provider.Kind;
            return true;
        }

        private static void AddEatersKindFirstWithFallback(
            IActorRegistry actorRegistry,
            List<IActor> results,
            out bool fallbackUsed)
        {
            fallbackUsed = false;

            if (actorRegistry.Actors == null)
                return;

            foreach (var actor in actorRegistry.Actors)
            {
                if (actor == null)
                    continue;

                if (TryGetActorKind(actor, out var actorKind) && actorKind == ActorKind.Eater)
                {
                    results.Add(actor);
                    continue;
                }

                var t = actor.Transform;
                if (t == null)
                    continue;

                // Mantém a feature funcional sem depender de um tipo compile-time.
                // Quando existir um EaterActor concreto, isso passa a classificar corretamente.
                if (t.GetComponent("EaterActor") != null)
                {
                    results.Add(actor);
                    fallbackUsed = true;
                }
            }
        }
    }
}
