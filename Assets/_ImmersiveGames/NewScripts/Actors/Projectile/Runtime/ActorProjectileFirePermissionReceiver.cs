using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    public sealed class ActorProjectileFirePermissionReceiver : IActivityCapabilityPermissionReceiver
    {
        private readonly ActorProjectileEmitterEndpoint _endpoint;
        private readonly ActorId _actorId;
        private readonly ActorInstanceRuntimeId _actorInstanceRuntimeId;
        private readonly PlayerActorId _playerActorId;
        private readonly PlayerSlotId _playerSlotId;
        private readonly string _receiverId;

        public ActorProjectileFirePermissionReceiver(
            ActorProjectileEmitterEndpoint endpoint,
            string receiverId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            _endpoint = endpoint ?? throw new InvalidOperationException("ActorProjectileFirePermissionReceiver requires non-null endpoint.");
            _actorId = actorId;
            _actorInstanceRuntimeId = actorInstanceRuntimeId;
            _playerActorId = playerActorId;
            _playerSlotId = playerSlotId;
            _receiverId = string.IsNullOrWhiteSpace(receiverId) ? "actor.projectile.fire.permission.receiver" : receiverId.Trim();
        }

        public string ReceiverId => _receiverId;

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

            DebugUtility.Log(
                typeof(ActorProjectileFirePermissionReceiver),
                $"[OBS][ActorProjectile] event='ActorProjectileFirePermissionReceiverNotified' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' state='{fact.Command.State}' outcome='{fact.Outcome}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'.",
                DebugUtility.Colors.Info);

            switch (fact.Command.State)
            {
                case ActivityCapabilityPermissionState.Allowed:
                    _endpoint.SetProjectileFirePermissionState(ActivityCapabilityPermissionState.Allowed);
                    DebugUtility.Log(
                        typeof(ActorProjectileFirePermissionReceiver),
                        $"[OBS][ActorProjectile] event='ActorProjectileFirePermissionApplied' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' state='Allowed' outcome='{fact.Outcome}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'.",
                        DebugUtility.Colors.Success);
                    break;

                case ActivityCapabilityPermissionState.Blocked:
                    _endpoint.SetProjectileFirePermissionState(ActivityCapabilityPermissionState.Blocked);
                    DebugUtility.Log(
                        typeof(ActorProjectileFirePermissionReceiver),
                        $"[OBS][ActorProjectile] event='ActorProjectileFirePermissionApplied' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' state='Blocked' outcome='{fact.Outcome}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'.",
                        DebugUtility.Colors.Warning);
                    break;

                case ActivityCapabilityPermissionState.Unbound:
                    _endpoint.SetProjectileFirePermissionState(ActivityCapabilityPermissionState.Unbound);
                    DebugUtility.Log(
                        typeof(ActorProjectileFirePermissionReceiver),
                        $"[OBS][ActorProjectile] event='ActorProjectileFirePermissionApplied' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' state='Unbound' outcome='{fact.Outcome}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'.",
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
