using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    public sealed class ActorProjectileFirePermissionReceiverProvider : IActivityPermissionReceiverProvider
    {
        private readonly IActorProjectileFireEndpoint _projectileFireEndpoint;
        private readonly string _receiverId;
        private readonly ActorId _actorId;
        private readonly ActorInstanceRuntimeId _actorInstanceRuntimeId;
        private readonly PlayerActorId _playerActorId;
        private readonly PlayerSlotId _playerSlotId;

        public ActorProjectileFirePermissionReceiverProvider(
            IActorProjectileFireEndpoint projectileFireEndpoint,
            string receiverId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            _projectileFireEndpoint = projectileFireEndpoint;
            _receiverId = Normalize(receiverId);
            _actorId = actorId;
            _actorInstanceRuntimeId = actorInstanceRuntimeId;
            _playerActorId = playerActorId;
            _playerSlotId = playerSlotId;
        }

        public string ReceiverId => _receiverId;

        public bool TryCreateReceiver(out IActivityCapabilityPermissionReceiver receiver)
        {
            if (_projectileFireEndpoint == null ||
                string.IsNullOrWhiteSpace(_receiverId) ||
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public sealed class ActorProjectileFirePermissionReceiver : IActorPermissionReceiver
    {
        private readonly IActorProjectileFireEndpoint _projectileFireEndpoint;
        private readonly string _receiverId;
        private readonly ActorId _actorId;
        private readonly ActorInstanceRuntimeId _actorInstanceRuntimeId;
        private readonly PlayerActorId _playerActorId;
        private readonly PlayerSlotId _playerSlotId;

        public ActorProjectileFirePermissionReceiver(
            IActorProjectileFireEndpoint projectileFireEndpoint,
            string receiverId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            _projectileFireEndpoint = projectileFireEndpoint ?? throw new InvalidOperationException("ActorProjectileFirePermissionReceiver requires non-null IActorProjectileFireEndpoint.");
            _receiverId = Normalize(receiverId);
            _actorId = actorId;
            _actorInstanceRuntimeId = actorInstanceRuntimeId;
            _playerActorId = playerActorId;
            _playerSlotId = playerSlotId;

            if (string.IsNullOrWhiteSpace(_receiverId))
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

        public string ReceiverId => _receiverId;

        public static string CreateReceiverId(ActivityCapabilityPermissionReceiverIdentity identity)
        {
            string normalizedPipelineId = Normalize(identity.PipelineId);
            string normalizedSessionStateId = Normalize(identity.SessionStateId);
            string normalizedActivityId = Normalize(identity.ActivityId);
            string normalizedActorInstanceRuntimeId = identity.ActorInstanceRuntimeId.IsValid ? identity.ActorInstanceRuntimeId.Value : string.Empty;
            string normalizedPlayerSlotId = identity.PlayerSlotId.IsValid ? identity.PlayerSlotId.Value : string.Empty;
            string actorInstanceToken = string.IsNullOrWhiteSpace(normalizedActorInstanceRuntimeId) ? "actor.instance.unbound" : normalizedActorInstanceRuntimeId;
            string slotToken = string.IsNullOrWhiteSpace(normalizedPlayerSlotId) ? "slot.unbound" : normalizedPlayerSlotId;
            return $"projectile_fire.receiver|pipeline={normalizedPipelineId}|session={normalizedSessionStateId}|activity={normalizedActivityId}|entry={identity.EntrySequence}|actorInstance={actorInstanceToken}|slot={slotToken}";
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

            DebugUtility.Log(
                typeof(ActorProjectileFirePermissionReceiver),
                $"[OBS][ActivityCapabilityPermission] event='ActivityCapabilityPermissionReceiverNotified' permissionId='{fact.Command.PermissionId}' state='{fact.Command.State}' outcome='{fact.Outcome}' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                DebugUtility.Colors.Info);

            switch (fact.Command.State)
            {
                case ActivityCapabilityPermissionState.Allowed:
                    _projectileFireEndpoint.SetProjectileFireEnabled(true);
                    DebugUtility.Log(
                        typeof(ActorProjectileFirePermissionReceiver),
                        $"[OBS][ActivityCapabilityPermission] event='ActorProjectileFirePermissionApplied' permissionId='{fact.Command.PermissionId}' state='Allowed' outcome='{fact.Outcome}' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' endpointId='{_projectileFireEndpoint.EndpointId}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                        DebugUtility.Colors.Success);
                    break;

                case ActivityCapabilityPermissionState.Blocked:
                    _projectileFireEndpoint.SetProjectileFireEnabled(false);
                    DebugUtility.Log(
                        typeof(ActorProjectileFirePermissionReceiver),
                        $"[OBS][ActivityCapabilityPermission] event='ActorProjectileFirePermissionApplied' permissionId='{fact.Command.PermissionId}' state='Blocked' outcome='{fact.Outcome}' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' endpointId='{_projectileFireEndpoint.EndpointId}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
                        DebugUtility.Colors.Warning);
                    break;

                case ActivityCapabilityPermissionState.Unbound:
                    _projectileFireEndpoint.SetProjectileFireEnabled(false);
                    DebugUtility.Log(
                        typeof(ActorProjectileFirePermissionReceiver),
                        $"[OBS][ActivityCapabilityPermission] event='ActorProjectileFirePermissionApplied' permissionId='{fact.Command.PermissionId}' state='Unbound' outcome='{fact.Outcome}' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' endpointId='{_projectileFireEndpoint.EndpointId}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'",
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
