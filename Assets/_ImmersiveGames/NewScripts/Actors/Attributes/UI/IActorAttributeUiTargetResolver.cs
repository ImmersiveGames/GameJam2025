using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    public interface IActorAttributeUiTargetResolver
    {
        ActorAttributeUiTargetResolveResult Resolve(
            ActorAttributeUiBindingRequest request,
            ActivityParticipationContext participationContext,
            ActivityPlayerActorRegistry registry,
            string source,
            string reason);
    }
}
