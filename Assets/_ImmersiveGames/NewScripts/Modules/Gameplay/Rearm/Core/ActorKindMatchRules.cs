using _ImmersiveGames.NewScripts.Modules.Gameplay.Actors.Core;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Rearm.Core
{
    internal static class ActorKindMatchRules
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
    }
}

