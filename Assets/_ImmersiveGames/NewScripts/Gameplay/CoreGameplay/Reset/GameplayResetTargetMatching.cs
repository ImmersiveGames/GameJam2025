using _ImmersiveGames.NewScripts.Runtime.Actors;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Reset
{
    internal static class GameplayResetTargetMatching
    {
        public static bool TryGetActorKind(IActor actor, out ActorKind kind)
        {
            kind = ActorKind.Unknown;
            if (actor is not IActorKindProvider provider)
            {
                return false;
            }

            kind = provider.Kind;
            return true;
        }

        public static bool MatchesActorKind(IActor actor, ActorKind kind)
        {
            if (actor == null || kind == ActorKind.Unknown)
            {
                return false;
            }

            return TryGetActorKind(actor, out var actorKind) && actorKind == kind;
        }

        public static bool MatchesEaterKindFirstWithFallback(IActor actor, out bool fallbackUsed)
        {
            fallbackUsed = false;

            if (actor == null)
            {
                return false;
            }

            if (TryGetActorKind(actor, out var actorKind) && actorKind == ActorKind.Eater)
            {
                return true;
            }

            var t = actor.Transform;
            if (t == null)
            {
                return false;
            }

            if (t.GetComponent<EaterActor>() == null)
            {
                return false;
            }

            fallbackUsed = true;
            return true;
        }
    }
}
