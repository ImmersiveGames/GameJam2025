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
        internal ObjectEmissionPoolAdapter PoolAdapter => _objectEmissionPoolAdapter;

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

        private void OnEnable()
        {
            if (ObjectEmissionPoolRuntimeBridge.TryAttachEndpoint(this))
            {
                return;
            }

            DebugUtility.LogWarning(
                typeof(ActorObjectEmitterEndpoint),
                $"[OBS][ObjectEmission] event='ObjectEmissionPoolServiceAttachDeferred' endpointId='{EndpointId}' reason='runtime_bridge_unavailable'.");
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
            ObjectEmissionRuntimePayload payload = BuildRuntimePayload(command, ResolveEmissionPoseOrFail(command.Profile));
            IObjectEmissionReturnSink returnSink = _objectEmissionPoolAdapter.CreateReturnSink(command.PoolDefinition, payload);
            ObjectEmissionPooledObject pooledObject = _objectEmissionPoolAdapter.RentAndInitialize(command, payload, returnSink);

            DebugUtility.Log(
                typeof(ActorObjectEmitterEndpoint),
                $"[OBS][ObjectEmission] event='ObjectEmissionSpawned' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='FirePrimary' profileId='{payload.ProfileId}' instanceName='{pooledObject.name}' instancePath='{BuildInstancePath(pooledObject.transform)}' activeSelf='{pooledObject.gameObject.activeSelf}' activeInHierarchy='{pooledObject.gameObject.activeInHierarchy}' endpointId='{EndpointId}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

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

        private static ObjectEmissionRuntimePayload BuildRuntimePayload(
            in ActorObjectEmissionCommand command,
            (Vector3 position, Quaternion rotation) spawnPose)
        {
            return new ObjectEmissionRuntimePayload(
                command.ActorId,
                command.ActorInstanceRuntimeId,
                command.ProfileId,
                command.ObjectSpeed,
                command.ObjectLifetime,
                spawnPose.position,
                spawnPose.rotation,
                command.Source,
                command.Reason);
        }

        private (Vector3 position, Quaternion rotation) ResolveEmissionPoseOrFail(ActorObjectEmissionProfile profile)
        {
            if (profile == null)
            {
                throw new InvalidOperationException($"ActorObjectEmitterEndpoint requires non-null profile for endpointId='{EndpointId}'.");
            }

            return profile.EmissionOriginPolicy switch
            {
                ActorObjectEmissionOriginPolicy.EmitterTransform => (transform.position, transform.rotation),
                _ => throw new InvalidOperationException($"ActorObjectEmitterEndpoint does not support emission origin policy '{profile.EmissionOriginPolicy}' for endpointId='{EndpointId}'."),
            };
        }

        private static string BuildInstancePath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            System.Collections.Generic.Stack<string> segments = new();
            Transform current = transform;
            while (current != null)
            {
                segments.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", segments);
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
