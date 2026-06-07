using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;

namespace _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime
{
    public sealed class ActorObjectEmissionPermissionReceiver : IActivityCapabilityPermissionReceiver
    {
        private readonly IActorObjectEmissionPermissionStateEndpoint _endpoint;
        private readonly ActorId _actorId;
        private readonly ActorInstanceRuntimeId _actorInstanceRuntimeId;
        private readonly PlayerActorId _playerActorId;
        private readonly PlayerSlotId _playerSlotId;
        private readonly string _receiverId;

        public ActorObjectEmissionPermissionReceiver(
            IActorObjectEmissionPermissionStateEndpoint endpoint,
            string receiverId,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            PlayerActorId playerActorId,
            PlayerSlotId playerSlotId)
        {
            _endpoint = endpoint ?? throw new InvalidOperationException("ActorObjectEmissionPermissionReceiver requires non-null endpoint.");
            _actorId = actorId;
            _actorInstanceRuntimeId = actorInstanceRuntimeId;
            _playerActorId = playerActorId;
            _playerSlotId = playerSlotId;
            _receiverId = string.IsNullOrWhiteSpace(receiverId) ? "actor.object.emission.permission.receiver" : receiverId.Trim();
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
            return $"object_emission.receiver|pipeline={normalizedPipelineId}|session={normalizedSessionStateId}|activity={normalizedActivityId}|entry={identity.EntrySequence}|actorInstance={actorInstanceToken}|slot={slotToken}";
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
                typeof(ActorObjectEmissionPermissionReceiver),
                $"[OBS][ActorObjectEmission] event='ActorObjectEmissionPermissionReceiverNotified' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' state='{fact.Command.State}' outcome='{fact.Outcome}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'.",
                DebugUtility.Colors.Info);

            switch (fact.Command.State)
            {
                case ActivityCapabilityPermissionState.Allowed:
                    _endpoint.SetObjectEmissionPermissionState(ActivityCapabilityPermissionState.Allowed);
                    DebugUtility.Log(
                        typeof(ActorObjectEmissionPermissionReceiver),
                        $"[OBS][ActorObjectEmission] event='ActorObjectEmissionPermissionApplied' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' state='Allowed' outcome='{fact.Outcome}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'.",
                        DebugUtility.Colors.Success);
                    break;

                case ActivityCapabilityPermissionState.Blocked:
                    _endpoint.SetObjectEmissionPermissionState(ActivityCapabilityPermissionState.Blocked);
                    DebugUtility.Log(
                        typeof(ActorObjectEmissionPermissionReceiver),
                        $"[OBS][ActorObjectEmission] event='ActorObjectEmissionPermissionApplied' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' state='Blocked' outcome='{fact.Outcome}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'.",
                        DebugUtility.Colors.Warning);
                    break;

                case ActivityCapabilityPermissionState.Unbound:
                    _endpoint.SetObjectEmissionPermissionState(ActivityCapabilityPermissionState.Unbound);
                    DebugUtility.Log(
                        typeof(ActorObjectEmissionPermissionReceiver),
                        $"[OBS][ActorObjectEmission] event='ActorObjectEmissionPermissionApplied' receiverId='{_receiverId}' actorId='{_actorId}' actorInstanceRuntimeId='{_actorInstanceRuntimeId}' playerActorId='{_playerActorId}' playerSlotId='{_playerSlotId}' state='Unbound' outcome='{fact.Outcome}' source='{fact.Command.Source}' reason='{fact.Command.Reason}'.",
                        DebugUtility.Colors.Warning);
                    break;
            }
        }

        private bool TargetsCurrentActor(ActorInstanceRuntimeId actorInstanceRuntimeId)
        {
            return actorInstanceRuntimeId.IsValid && actorInstanceRuntimeId == _actorInstanceRuntimeId;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool IsGameplayControlPermission(ActivityCapabilityPermissionId permissionId)
        {
            string token = ActivityCapabilityPermissionIds.ToToken(permissionId);
            return string.Equals(token, ActivityCapabilityPermissionIds.ActivityGameplayControl, StringComparison.Ordinal);
        }
    }
}
