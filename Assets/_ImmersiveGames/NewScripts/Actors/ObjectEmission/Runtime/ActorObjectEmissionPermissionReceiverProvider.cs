using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;

namespace _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime
{
    public sealed class ActorObjectEmissionPermissionReceiverProvider : IActivityPermissionReceiverProvider
    {
        private readonly IActorObjectEmitterEndpoint _endpoint;
        private readonly string _receiverId;
        private readonly ActorId _actorId;
        private readonly ActorInstanceRuntimeId _actorInstanceRuntimeId;
        private readonly PlayerActorId _playerActorId;
        private readonly PlayerSlotId _playerSlotId;

        public ActorObjectEmissionPermissionReceiverProvider(
            IActorObjectEmitterEndpoint endpoint,
            string receiverId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            _endpoint = endpoint ?? throw new InvalidOperationException("ActorObjectEmissionPermissionReceiverProvider requires non-null endpoint.");
            _receiverId = string.IsNullOrWhiteSpace(receiverId) ? "object_emission.receiver" : receiverId.Trim();
            _actorId = actorId;
            _actorInstanceRuntimeId = actorInstanceRuntimeId;
            _playerActorId = playerActorId;
            _playerSlotId = playerSlotId;
        }

        public string ReceiverId => _receiverId;

        public bool TryCreateReceiver(out IActivityCapabilityPermissionReceiver receiver)
        {
            if (!_actorId.IsValid || !_actorInstanceRuntimeId.IsValid || !_playerActorId.IsValid)
            {
                receiver = null;
                return false;
            }

            if (_endpoint is not IActorObjectEmissionPermissionStateEndpoint permissionEndpoint)
            {
                receiver = null;
                return false;
            }

            receiver = new ActorObjectEmissionPermissionReceiver(
                permissionEndpoint,
                _receiverId,
                _actorId,
                _actorInstanceRuntimeId,
                _playerActorId,
                _playerSlotId);
            return true;
        }
    }
}
