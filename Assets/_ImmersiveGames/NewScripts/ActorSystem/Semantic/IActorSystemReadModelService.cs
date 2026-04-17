using _ImmersiveGames.NewScripts.ActorSystem.ReadModel;

namespace _ImmersiveGames.NewScripts.ActorSystem.Semantic
{
    public interface IActorSystemReadModelService
    {
        ActorSystemReadModelSnapshot Current { get; }
        bool TryGetCurrent(out ActorSystemReadModelSnapshot snapshot);
        ActorSystemReadModelSnapshot Refresh();
        void Clear(string reason = null);
    }
}