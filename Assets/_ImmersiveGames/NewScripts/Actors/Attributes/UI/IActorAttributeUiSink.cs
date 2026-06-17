namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    public interface IActorAttributeUiSink
    {
        bool IsReady { get; }

        void Apply(ActorAttributeUiValue value);

        void Clear(string reason);
    }
}
