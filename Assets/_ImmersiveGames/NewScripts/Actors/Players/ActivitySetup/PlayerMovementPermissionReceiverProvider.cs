using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;

namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class PlayerMovementPermissionReceiverProvider : IActivityPermissionReceiverProvider
    {
        private readonly IActorMovementEndpoint _movementEndpoint;
        private readonly ActivityCapabilityPermissionReceiverId _receiverId;
        private readonly ActorId _actorId;
        private readonly ActorInstanceRuntimeId _actorInstanceRuntimeId;
        private readonly PlayerActorId _playerActorId;
        private readonly PlayerSlotId _playerSlotId;

        public PlayerMovementPermissionReceiverProvider(
            IActorMovementEndpoint movementEndpoint,
            ActivityCapabilityPermissionReceiverId receiverId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            _movementEndpoint = movementEndpoint;
            _receiverId = receiverId;
            _actorId = actorId;
            _actorInstanceRuntimeId = actorInstanceRuntimeId;
            _playerActorId = playerActorId;
            _playerSlotId = playerSlotId;
        }

        public ActivityCapabilityPermissionReceiverId ReceiverId => _receiverId;

        public bool TryCreateReceiver(out IActivityCapabilityPermissionReceiver receiver)
        {
            if (_movementEndpoint == null ||
                !_receiverId.IsValid)
            {
                receiver = null;
                return false;
            }

            if (!_actorId.IsValid || !_actorInstanceRuntimeId.IsValid || !_playerActorId.IsValid)
            {
                receiver = null;
                return false;
            }

            receiver = new PlayerMovementPermissionReceiver(
                _movementEndpoint,
                _receiverId,
                _actorId,
                _actorInstanceRuntimeId,
                _playerActorId,
                _playerSlotId);
            return true;
        }
    }
}
