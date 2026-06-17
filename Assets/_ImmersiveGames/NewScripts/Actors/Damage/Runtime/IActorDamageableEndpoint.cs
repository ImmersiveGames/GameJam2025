using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Damage.Runtime
{
    public interface IActorDamageableEndpoint
    {
        ActorId ActorId { get; }
        ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        SessionActivityIdentity ActivityIdentity { get; }
        ActorAttributeId TargetAttributeId { get; }
        bool IsConfigured { get; }

        bool TryApplyDamageIntent(
            ActorDamageIntent intent,
            out ActorDamageResult result);
    }
}
