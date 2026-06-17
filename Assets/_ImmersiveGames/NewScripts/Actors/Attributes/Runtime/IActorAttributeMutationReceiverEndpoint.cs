using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.Runtime
{
    public interface IActorAttributeMutationReceiverEndpoint
    {
        ActorId ActorId { get; }
        ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        SessionActivityIdentity ActivityIdentity { get; }
        bool IsConfigured { get; }

        bool TryReceiveMutationIntent(
            ActorAttributeMutationIntent intent,
            out ActorAttributeMutationResult result);
    }
}
