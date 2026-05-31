using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.Runtime;

namespace _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Inventory
{
    public sealed class PlayerActorCapabilityIdentityResolver : IPlayerActorCapabilityIdentityResolver
    {
        public bool TryResolve(ActorScanTarget target, out PlayerActorCapabilityIdentity identity)
        {
            identity = default;
            if (!target.IsValid || target.ActorRoot == null)
            {
                return false;
            }

            PlayerActorIdentity playerIdentity = target.ActorRoot.GetComponent<PlayerActorIdentity>();
            if (playerIdentity == null)
            {
                return false;
            }

            ActorInstanceRuntimeId actorInstanceRuntimeId = new(target.ActorInstanceId.Value);
            identity = new PlayerActorCapabilityIdentity(new ActorId(target.ActorId), actorInstanceRuntimeId, playerIdentity.PlayerActorId, playerIdentity.PlayerSlotId);
            return identity.IsValid;
        }
    }
}
