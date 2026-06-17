using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Damage.Runtime
{
    public interface IActorDamageSourceEndpoint
    {
        ActorId SourceActorId { get; }
        ActorInstanceRuntimeId SourceActorInstanceRuntimeId { get; }
        SessionActivityIdentity ActivityIdentity { get; }
        bool IsConfigured { get; }

        bool TryEmitDamageIntent(
            ActorDamageSourceIntent intent,
            IActorDamageableEndpoint target,
            out ActorDamageSourceResult result);
    }
}
