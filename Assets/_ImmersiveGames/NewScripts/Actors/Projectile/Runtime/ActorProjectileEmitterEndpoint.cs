using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorProjectileEmitterEndpoint : MonoBehaviour, IActorProjectileEmitterEndpoint
    {
        [SerializeField] private string endpointId = "actor.projectile.emitter";

        private ActivityCapabilityPermissionState _firePermissionState = ActivityCapabilityPermissionState.Unbound;
        private ActorProjectileFireCommand? _lastAcceptedCommand;
        private int _acceptedCommandCount;

        public string EndpointId => string.IsNullOrWhiteSpace(endpointId) ? "actor.projectile.emitter" : endpointId;
        public ActorProjectileFireCommand? LastAcceptedCommand => _lastAcceptedCommand;
        public int AcceptedCommandCount => _acceptedCommandCount;
        public ActivityCapabilityPermissionState FirePermissionState => _firePermissionState;
        public bool IsFireAllowed => _firePermissionState == ActivityCapabilityPermissionState.Allowed;

        public void SetProjectileFirePermissionState(ActivityCapabilityPermissionState state)
        {
            if (state == ActivityCapabilityPermissionState.Unknown)
            {
                return;
            }

            _firePermissionState = state;
        }

        public void ClearProjectileFirePermissionState()
        {
            _firePermissionState = ActivityCapabilityPermissionState.Unbound;
        }

        public ActorCommandDispatchResult AcceptCommand(ActorCommandEnvelope command)
        {
            if (!command.IsValid)
            {
                return ActorCommandDispatchResult.RejectedUnsupportedCommand("invalid_command");
            }

            if (command.CommandId.ValueKind != ActorCommandValueKind.FirePrimary ||
                command.CommandId.TriggerKind != ActorCommandTriggerKind.Pressed)
            {
                return ActorCommandDispatchResult.RejectedUnsupportedCommand("unsupported_command");
            }

            ActorProjectileFireCommand fireCommand = new(command);
            ActorProjectileFireResult fireResult = Fire(in fireCommand);
            if (!fireResult.IsValid)
            {
                return ActorCommandDispatchResult.RejectedUnsupportedCommand("invalid_fire_result");
            }

            return fireResult.IsAccepted
                ? ActorCommandDispatchResult.Accepted(fireResult.Reason)
                : ActorCommandDispatchResult.RejectedInactive(fireResult.Reason);
        }

        public ActorProjectileFireResult Fire(in ActorProjectileFireCommand command)
        {
            if (!command.IsValid)
            {
                return ActorProjectileFireResult.RejectedUnsupportedCommand("invalid_fire_command");
            }

            if (!IsFireAllowed)
            {
                string reason = ResolveBlockedReason(_firePermissionState);
                DebugUtility.Log(
                    typeof(ActorProjectileEmitterEndpoint),
                    $"[OBS][ActorProjectile] event='ActorProjectileFireCommandRejected' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='FirePrimary' sourceKind='{command.SourceKind}' valueKind='Button' triggerKind='Pressed' endpointId='{EndpointId}' source='{nameof(ActorProjectileEmitterEndpoint)}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return ActorProjectileFireResult.RejectedInactive(reason);
            }

            _lastAcceptedCommand = command;
            _acceptedCommandCount++;

            DebugUtility.Log(
                typeof(ActorProjectileEmitterEndpoint),
                $"[OBS][ActorProjectile] event='ActorProjectileFireCommandAccepted' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='FirePrimary' sourceKind='{command.SourceKind}' valueKind='Button' triggerKind='Pressed' endpointId='{EndpointId}' source='{nameof(ActorProjectileEmitterEndpoint)}' reason='fire_command_observed'.",
                DebugUtility.Colors.Success);

            return ActorProjectileFireResult.Accepted("fire_command_observed");
        }

        private void OnDisable()
        {
            ClearProjectileFirePermissionState();
        }

        private static string ResolveBlockedReason(ActivityCapabilityPermissionState permissionState)
        {
            return permissionState switch
            {
                ActivityCapabilityPermissionState.Blocked => "activity_gameplay_blocked",
                ActivityCapabilityPermissionState.Unbound => "activity_gameplay_unbound",
                _ => "activity_gameplay_blocked",
            };
        }
    }
}
