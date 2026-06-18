using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    public sealed class ActorProjectileFirePermissionReceiverProvider : IActivityPermissionReceiverProvider
    {
        private readonly IActorProjectileFireEndpoint _projectileFireEndpoint;
        private readonly ActivityCapabilityPermissionReceiverId _receiverId;
        private readonly ActorId _actorId;
        private readonly ActorInstanceRuntimeId _actorInstanceRuntimeId;
        private readonly PlayerActorId _playerActorId;
        private readonly PlayerSlotId _playerSlotId;

        public ActorProjectileFirePermissionReceiverProvider(
            IActorProjectileFireEndpoint projectileFireEndpoint,
            ActivityCapabilityPermissionReceiverId receiverId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            _projectileFireEndpoint = projectileFireEndpoint;
            _receiverId = receiverId;
            _actorId = actorId;
            _actorInstanceRuntimeId = actorInstanceRuntimeId;
            _playerActorId = playerActorId;
            _playerSlotId = playerSlotId;
        }

        public ActivityCapabilityPermissionReceiverId ReceiverId => _receiverId;

        public bool TryCreateReceiver(out IActivityCapabilityPermissionReceiver receiver)
        {
            if (_projectileFireEndpoint == null ||
                !_receiverId.IsValid ||
                !_actorId.IsValid ||
                !_actorInstanceRuntimeId.IsValid ||
                !_playerActorId.IsValid)
            {
                receiver = null;
                return false;
            }

            receiver = new ActorProjectileFirePermissionReceiver(
                _projectileFireEndpoint,
                _receiverId,
                _actorId,
                _actorInstanceRuntimeId,
                _playerActorId,
                _playerSlotId);
            return true;
        }

    }

    public sealed class ActorProjectileFirePermissionReceiver : IActorPermissionReceiver
    {
        private readonly IActorProjectileFireEndpoint _projectileFireEndpoint;
        private readonly ActivityCapabilityPermissionReceiverId _receiverId;
        private readonly ActorId _actorId;
        private readonly ActorInstanceRuntimeId _actorInstanceRuntimeId;
        private readonly PlayerActorId _playerActorId;
        private readonly PlayerSlotId _playerSlotId;

        public ActorProjectileFirePermissionReceiver(
            IActorProjectileFireEndpoint projectileFireEndpoint,
            ActivityCapabilityPermissionReceiverId receiverId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            _projectileFireEndpoint = projectileFireEndpoint ?? throw new InvalidOperationException("ActorProjectileFirePermissionReceiver requires non-null IActorProjectileFireEndpoint.");
            _receiverId = receiverId;
            _actorId = actorId;
            _actorInstanceRuntimeId = actorInstanceRuntimeId;
            _playerActorId = playerActorId;
            _playerSlotId = playerSlotId;

            if (!_receiverId.IsValid)
            {
                throw new InvalidOperationException("ActorProjectileFirePermissionReceiver requires non-empty receiverId.");
            }

            if (!_actorId.IsValid)
            {
                throw new InvalidOperationException("ActorProjectileFirePermissionReceiver requires valid ActorId.");
            }

            if (!_actorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException("ActorProjectileFirePermissionReceiver requires valid ActorInstanceRuntimeId.");
            }

            if (!_playerActorId.IsValid)
            {
                throw new InvalidOperationException("ActorProjectileFirePermissionReceiver requires valid PlayerActorId.");
            }
        }

        public ActivityCapabilityPermissionReceiverId ReceiverId => _receiverId;

        public static ActivityCapabilityPermissionReceiverId CreateReceiverId(ActivityCapabilityPermissionReceiverIdentity identity)
        {
            string normalizedPipelineId = identity.PipelineId.TrimToEmpty();
            string normalizedSessionStateId = identity.SessionStateId.TrimToEmpty();
            string normalizedActivityId = identity.ActivityId.TrimToEmpty();
            string normalizedActorInstanceRuntimeId = identity.ActorInstanceRuntimeId.IsValid ? identity.ActorInstanceRuntimeId.Value : string.Empty;
            string actorInstanceToken = string.IsNullOrWhiteSpace(normalizedActorInstanceRuntimeId) ? "actor.instance.unbound" : normalizedActorInstanceRuntimeId;
            return ActivityCapabilityPermissionReceiverId.FromString($"projectile_fire.receiver|pipeline={normalizedPipelineId}|session={normalizedSessionStateId}|activity={normalizedActivityId}|entry={identity.EntrySequence}|actorInstance={actorInstanceToken}");
        }

        public void OnPermissionChanged(ActivityCapabilityPermissionFact fact)
        {
            if (!fact.IsValid || !fact.Command.IsValid)
            {
                return;
            }

            if (!IsGameplayControlPermission(fact.Command.PermissionId))
            {
                return;
            }

            if (!TargetsCurrentActor(fact.Command.ActorInstanceRuntimeId))
            {
                return;
            }

            DebugUtility.LogVerbose(
                typeof(ActorProjectileFirePermissionReceiver),
                $"event='ActivityCapabilityPermissionReceiverNotified' permissionId='{fact.Command.PermissionId}' state='{fact.Command.State}' outcome='{fact.Outcome}' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                DebugUtility.Colors.Info);

            switch (fact.Command.State)
            {
                case ActivityCapabilityPermissionState.Allowed:
                    _projectileFireEndpoint.SetProjectileFireEnabled(true);
                    DebugUtility.Log(
                        typeof(ActorProjectileFirePermissionReceiver),
                        $"event='ActorProjectileFirePermissionApplied' permissionId='{fact.Command.PermissionId}' state='Allowed' outcome='{fact.Outcome}' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' endpointId='{_projectileFireEndpoint.EndpointId}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                        DebugUtility.Colors.Success);
                    break;

                case ActivityCapabilityPermissionState.Blocked:
                    _projectileFireEndpoint.SetProjectileFireEnabled(false);
                    DebugUtility.Log(
                        typeof(ActorProjectileFirePermissionReceiver),
                        $"event='ActorProjectileFirePermissionApplied' permissionId='{fact.Command.PermissionId}' state='Blocked' outcome='{fact.Outcome}' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' endpointId='{_projectileFireEndpoint.EndpointId}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                        DebugUtility.Colors.Warning);
                    break;

                case ActivityCapabilityPermissionState.Unbound:
                    _projectileFireEndpoint.SetProjectileFireEnabled(false);
                    DebugUtility.Log(
                        typeof(ActorProjectileFirePermissionReceiver),
                        $"event='ActorProjectileFirePermissionApplied' permissionId='{fact.Command.PermissionId}' state='Unbound' outcome='{fact.Outcome}' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' endpointId='{_projectileFireEndpoint.EndpointId}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                        DebugUtility.Colors.Warning);
                    break;
            }
        }

        private bool TargetsCurrentActor(ActorInstanceRuntimeId actorInstanceRuntimeId)
        {
            return actorInstanceRuntimeId.IsValid && actorInstanceRuntimeId == _actorInstanceRuntimeId;
        }

        private static bool IsGameplayControlPermission(ActivityCapabilityPermissionId permissionId)
        {
            string token = ActivityCapabilityPermissionIds.ToToken(permissionId);
            return string.Equals(token, ActivityCapabilityPermissionIds.ActivityGameplayControl, StringComparison.Ordinal);
        }
    }
}
