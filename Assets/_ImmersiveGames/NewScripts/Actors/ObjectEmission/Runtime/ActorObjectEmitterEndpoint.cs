using System;
using _ImmersiveGames.NewScripts.Actors.ObjectEmission.Authoring;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.SessionActivity.Capabilities.Permissions;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorObjectEmitterEndpoint : MonoBehaviour, IActorObjectEmitterEndpoint, IActorObjectEmissionPermissionStateEndpoint
    {
        [SerializeField] private string endpointId = "actor.object.emitter";
        [SerializeField] private ActorObjectEmissionProfile objectEmissionProfile;

        private ActivityCapabilityPermissionState _firePermissionState = ActivityCapabilityPermissionState.Unbound;
        private ActorObjectEmissionCommand? _lastAcceptedCommand;
        private int _acceptedCommandCount;
        private readonly ObjectEmissionPoolAdapter _objectEmissionPoolAdapter = new();

        public string EndpointId => string.IsNullOrWhiteSpace(endpointId) ? "actor.object.emitter" : endpointId;
        public ActorObjectEmissionCommand? LastAcceptedCommand => _lastAcceptedCommand;
        public int AcceptedCommandCount => _acceptedCommandCount;
        public ActivityCapabilityPermissionState FirePermissionState => _firePermissionState;
        public bool IsFireAllowed => _firePermissionState == ActivityCapabilityPermissionState.Allowed;
        public ActorObjectEmissionProfile ObjectEmissionProfile => objectEmissionProfile;

        public void SetObjectEmissionPermissionState(ActivityCapabilityPermissionState state)
        {
            if (state == ActivityCapabilityPermissionState.Unknown)
            {
                return;
            }

            _firePermissionState = state;
        }

        public void ClearObjectEmissionPermissionState()
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

            if (!IsFireAllowed)
            {
                string reason = ResolveBlockedReason(_firePermissionState);
                LogFireRejected(command, reason);
                return ActorCommandDispatchResult.RejectedInactive(reason);
            }

            ActorObjectEmissionCommand emissionCommand = new(command, ResolveObjectEmissionProfileOrFail());
            ActorObjectEmissionResult fireResult = Fire(in emissionCommand);
            if (!fireResult.IsValid)
            {
                return ActorCommandDispatchResult.RejectedUnsupportedCommand("invalid_fire_result");
            }

            return fireResult.IsAccepted
                ? ActorCommandDispatchResult.Accepted(fireResult.Reason)
                : ActorCommandDispatchResult.RejectedInactive(fireResult.Reason);
        }

        public ActorObjectEmissionResult Fire(in ActorObjectEmissionCommand command)
        {
            if (!command.IsValid)
            {
                return ActorObjectEmissionResult.RejectedUnsupportedCommand("invalid_emission_command");
            }

            if (!IsFireAllowed)
            {
                string reason = ResolveBlockedReason(_firePermissionState);
                DebugUtility.Log(
                    typeof(ActorObjectEmitterEndpoint),
                    $"[OBS][ActorObjectEmission] event='ActorObjectEmissionCommandRejected' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='FirePrimary' sourceKind='{command.SourceKind}' valueKind='Button' triggerKind='Pressed' endpointId='{EndpointId}' source='{nameof(ActorObjectEmitterEndpoint)}' reason='{reason}'.",
                    DebugUtility.Colors.Info);
                return ActorObjectEmissionResult.RejectedInactive(reason);
            }

            DebugUtility.Log(
                typeof(ActorObjectEmitterEndpoint),
                $"[OBS][ActorObjectEmission] event='ActorObjectEmissionProfileResolved' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='FirePrimary' profileId='{command.ProfileId}' poolDefinition='{command.PoolDefinition?.name ?? "<none>"}' emissionOriginPolicy='{command.EmissionOriginPolicy}' objectSpeed='{command.ObjectSpeed:0.###}' objectLifetime='{command.ObjectLifetime:0.###}' endpointId='{EndpointId}' source='{nameof(ActorObjectEmitterEndpoint)}' reason='object_emission_profile_resolved'.",
                DebugUtility.Colors.Info);

            _objectEmissionPoolAdapter.PrepareObjectEmissionPool(command);

            _lastAcceptedCommand = command;
            _acceptedCommandCount++;

            DebugUtility.Log(
                typeof(ActorObjectEmitterEndpoint),
                $"[OBS][ActorObjectEmission] event='ActorObjectEmissionCommandAccepted' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='FirePrimary' sourceKind='{command.SourceKind}' valueKind='Button' triggerKind='Pressed' endpointId='{EndpointId}' source='{nameof(ActorObjectEmitterEndpoint)}' reason='object_emission_command_observed'.",
                DebugUtility.Colors.Success);

            return ActorObjectEmissionResult.Accepted("object_emission_command_observed");
        }

        private void OnDisable()
        {
            ClearObjectEmissionPermissionState();
        }

        private void LogFireRejected(ActorCommandEnvelope command, string reason)
        {
            DebugUtility.Log(
                typeof(ActorObjectEmitterEndpoint),
                $"[OBS][ActorObjectEmission] event='ActorObjectEmissionCommandRejected' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='FirePrimary' sourceKind='{command.SourceKind}' valueKind='Button' triggerKind='Pressed' endpointId='{EndpointId}' source='{nameof(ActorObjectEmitterEndpoint)}' reason='{reason}'.",
                DebugUtility.Colors.Info);
        }

        private ActorObjectEmissionProfile ResolveObjectEmissionProfileOrFail()
        {
            if (objectEmissionProfile == null)
            {
                throw new InvalidOperationException($"ActorObjectEmitterEndpoint requires ActorObjectEmissionProfile for endpointId='{EndpointId}'.");
            }

            if (!objectEmissionProfile.IsValid)
            {
                throw new InvalidOperationException($"ActorObjectEmitterEndpoint found invalid ActorObjectEmissionProfile asset='{objectEmissionProfile.name}' endpointId='{EndpointId}'.");
            }

            return objectEmissionProfile;
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
