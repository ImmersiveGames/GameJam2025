using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Presentation.Adapters
{
    public interface IActorPresentationMaterializationAdapter
    {
        ActorPresentationResult Materialize(ActorPresentationMaterializationCommand command);
        ActorPresentationResult Release(ActorPresentationReleaseCommand command);
    }
}
