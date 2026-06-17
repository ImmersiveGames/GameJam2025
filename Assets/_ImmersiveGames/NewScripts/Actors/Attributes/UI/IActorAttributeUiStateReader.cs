namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    public interface IActorAttributeUiStateReader
    {
        bool TryRead(
            ActorAttributeUiBindingTarget target,
            out ActorAttributeUiValue value,
            out string failureReason);
    }
}
