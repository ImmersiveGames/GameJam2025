using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Authoring;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorProjectileFireEndpoint : MonoBehaviour, IActorProjectileFireEndpoint
    {
        [SerializeField, Tooltip("Identificador técnico do endpoint de fire/projectile.")]
        private string endpointId = "actor.projectile.fire.endpoint.player.primary";
        [SerializeField, Tooltip("Profile autoral tipado da capability de fire/projectile.")]
        private ActorProjectileFireProfileAsset fireProfile;
        [SerializeField] private bool required;

        private Actor _actor;
        private IActorProjectileSpawnAdapter _spawnAdapter;
        private string _spawnAdapterName = string.Empty;
        private bool _projectileFireEnabled;
        private readonly Dictionary<ActorProjectileFireModeId, float> _nextAllowedFireTimeByMode = new();

        public ActorProjectileFireEndpointId EndpointId => new(Normalize(endpointId));
        public ActorId ActorId => ResolveActor()?.ActorIdValue ?? default;
        public ActorInstanceRuntimeId ActorInstanceRuntimeId => ResolveActor()?.RuntimeActorInstanceId ?? default;
        public ActorProjectileProfileId ProfileId => fireProfile == null ? default : fireProfile.ProfileId;
        public ActorProjectileFireModeId DefaultFireModeId => fireProfile == null ? default : fireProfile.DefaultFireModeId;
        public bool IsRequired => required;
        public bool IsProjectileFireEnabled => _projectileFireEnabled;
        public bool HasSpawnAdapter => _spawnAdapter != null;
        public string SpawnAdapterName => Normalize(_spawnAdapterName);

        public void ConfigureSpawnAdapter(
            IActorProjectileSpawnAdapter spawnAdapter,
            string source,
            string reason)
        {
            if (spawnAdapter == null)
            {
                throw new InvalidOperationException("ActorProjectileFireEndpoint.ConfigureSpawnAdapter requires non-null spawn adapter.");
            }

            _spawnAdapter = spawnAdapter;
            _spawnAdapterName = spawnAdapter.GetType().Name;

            DebugUtility.Log(
                typeof(ActorProjectileFireEndpoint),
                $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnAdapterConfigured' actorId='{ActorId}' actorInstanceRuntimeId='{ActorInstanceRuntimeId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{DefaultFireModeId}' adapter='{SpawnAdapterName}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);
        }

        public void SetProjectileFireEnabled(bool enabled)
        {
            _projectileFireEnabled = enabled;
        }

        public ActorCommandDispatchResult AcceptCommand(ActorCommandEnvelope command)
        {
            if (!command.IsValid)
            {
                const string invalidReason = "invalid_projectile_fire_envelope";

                DebugUtility.Log(
                    typeof(ActorProjectileFireEndpoint),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileFireCommandRejected' actorId='{ActorId}' actorInstanceRuntimeId='{ActorInstanceRuntimeId}' commandId='{command.CommandId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{DefaultFireModeId}' dispatchStatus='RejectedUnsupportedCommand' reason='{invalidReason}' source='{nameof(ActorProjectileFireEndpoint)}'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedUnsupportedCommand(invalidReason);
            }

            if (!_projectileFireEnabled)
            {
                DebugUtility.Log(
                    typeof(ActorProjectileFireEndpoint),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileFireCommandRejected' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{DefaultFireModeId}' dispatchStatus='RejectedInactive' reason='projectile_fire_endpoint_inactive' source='{nameof(ActorProjectileFireEndpoint)}'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedInactive("projectile_fire_endpoint_inactive");
            }

            Vector3 direction = transform.forward;
            if (direction.sqrMagnitude <= 0f)
            {
                direction = Vector3.forward;
            }

            if (!TryBuildFireCommand(
                command,
                DefaultFireModeId,
                transform.position,
                direction,
                out ActorProjectileFireCommand fireCommand,
                out ActorProjectileFireEndpointReadiness readiness))
            {
                string blockedReason = string.IsNullOrWhiteSpace(readiness.Reason)
                    ? "projectile_fire_command_build_failed"
                    : readiness.Reason;

                DebugUtility.Log(
                    typeof(ActorProjectileFireEndpoint),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileFireCommandRejected' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{DefaultFireModeId}' readinessState='{readiness.Kind}' blockedReason='{readiness.BlockedReason}' dispatchStatus='RejectedUnsupportedCommand' reason='{blockedReason}' source='{nameof(ActorProjectileFireEndpoint)}'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedUnsupportedCommand(blockedReason);
            }

            if (!fireProfile.TryGetFireMode(fireCommand.FireModeId, out ActorProjectileFireMode fireMode, out string fireModeReason))
            {
                string blockedReason = string.IsNullOrWhiteSpace(fireModeReason)
                    ? "projectile_fire_mode_missing"
                    : fireModeReason;

                DebugUtility.Log(
                    typeof(ActorProjectileFireEndpoint),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileFireCommandRejected' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' dispatchStatus='RejectedUnsupportedCommand' blockedReason='MissingFireMode' reason='{blockedReason}' source='{nameof(ActorProjectileFireEndpoint)}'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedUnsupportedCommand(blockedReason);
            }

            float now = Time.time;
            if (IsCooldownActive(fireMode, now, out float nextAllowedTime, out float remainingSeconds))
            {
                DebugUtility.Log(
                    typeof(ActorProjectileFireEndpoint),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileFireCooldownBlocked' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' cooldownSeconds='{fireMode.CooldownSeconds:0.###}' remainingSeconds='{remainingSeconds:0.###}' nextAllowedTime='{nextAllowedTime:0.###}' dispatchStatus='RejectedUnsupportedCommand' reason='projectile_fire_cooldown_active' source='{nameof(ActorProjectileFireEndpoint)}'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedUnsupportedCommand("projectile_fire_cooldown_active");
            }

            DebugUtility.Log(
                typeof(ActorProjectileFireEndpoint),
                $"[OBS][ActorProjectileFire] event='ActorProjectileFireCommandBuilt' actorId='{fireCommand.ActorId}' actorInstanceRuntimeId='{fireCommand.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' origin='{FormatVector(fireCommand.Origin)}' direction='{FormatVector(fireCommand.Direction)}' cooldownSeconds='{fireMode.CooldownSeconds:0.###}' source='{nameof(ActorProjectileFireEndpoint)}' reason='spawn_adapter_command_built'.",
                DebugUtility.Colors.Info);

            if (_spawnAdapter == null)
            {
                DebugUtility.Log(
                    typeof(ActorProjectileFireEndpoint),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnAdapterMissing' actorId='{fireCommand.ActorId}' actorInstanceRuntimeId='{fireCommand.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' spawnExecuted='False' poolCalled='False' dispatchStatus='RejectedUnsupportedCommand' source='{nameof(ActorProjectileFireEndpoint)}' reason='projectile_spawn_adapter_missing'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedUnsupportedCommand("projectile_spawn_adapter_missing");
            }

            ActorProjectileSpawnAdapterResult adapterResult = _spawnAdapter.Execute(fireCommand);

            DebugUtility.Log(
                typeof(ActorProjectileFireEndpoint),
                $"[OBS][ActorProjectileFire] event='ActorProjectileFireSpawnAdapterCompleted' actorId='{fireCommand.ActorId}' actorInstanceRuntimeId='{fireCommand.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' adapter='{SpawnAdapterName}' adapterResult='{adapterResult.Kind}' spawnExecuted='{adapterResult.SpawnExecuted}' poolCalled='{adapterResult.PoolCalled}' source='{nameof(ActorProjectileFireEndpoint)}' reason='{adapterResult.Reason}'.",
                adapterResult.IsFailed ? DebugUtility.Colors.Info : DebugUtility.Colors.Success);

            if (adapterResult.IsFailed || !adapterResult.IsAccepted)
            {
                string failureReason = string.IsNullOrWhiteSpace(adapterResult.Reason)
                    ? "projectile_spawn_adapter_failed"
                    : adapterResult.Reason;
                return ActorCommandDispatchResult.RejectedUnsupportedCommand(failureReason);
            }

            if (fireMode.HasCooldown)
            {
                float scheduledNextAllowedTime = now + fireMode.CooldownSeconds;
                _nextAllowedFireTimeByMode[fireMode.FireModeId] = scheduledNextAllowedTime;

                DebugUtility.Log(
                    typeof(ActorProjectileFireEndpoint),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileFireCooldownScheduled' actorId='{fireCommand.ActorId}' actorInstanceRuntimeId='{fireCommand.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' cooldownSeconds='{fireMode.CooldownSeconds:0.###}' nextAllowedTime='{scheduledNextAllowedTime:0.###}' source='{nameof(ActorProjectileFireEndpoint)}' reason='projectile_fire_cooldown_scheduled'.",
                    DebugUtility.Colors.Info);
            }

            string acceptedReason = string.IsNullOrWhiteSpace(adapterResult.Reason)
                ? "projectile_spawn_adapter_accepted"
                : adapterResult.Reason;
            return ActorCommandDispatchResult.Accepted(acceptedReason);
        }

        public bool TryGetReadiness(
            ActorCommandId commandId,
            out ActorProjectileFireEndpointReadiness readiness)
        {
            readiness = default;

            if (!commandId.IsValid)
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.InvalidCommand,
                    BuildDescriptorOrDefault(commandId),
                    commandId,
                    DefaultFireModeId,
                    ActorProjectileFireBlockedReasonKind.InvalidCommand,
                    "invalid_projectile_fire_command",
                    "ActorProjectileFireEndpoint requires a valid bound command.");
                return false;
            }

            if (!TryBuildDescriptor(commandId, out ActorProjectileFireEndpointDescriptor descriptor, out ActorProjectileFireEndpointReadinessKind descriptorFailureKind, out string descriptorFailureReason))
            {
                if (!required && descriptorFailureKind == ActorProjectileFireEndpointReadinessKind.MissingProfile)
                {
                    readiness = ActorProjectileFireEndpointReadiness.SkippedOptional(
                        descriptor,
                        commandId,
                        descriptorFailureReason);
                    return true;
                }

                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    descriptorFailureKind,
                    descriptor,
                    commandId,
                    DefaultFireModeId,
                    ToBlockedReason(descriptorFailureKind),
                    descriptorFailureReason,
                    descriptorFailureReason);
                return false;
            }

            if (fireProfile == null)
            {
                readiness = required
                    ? ActorProjectileFireEndpointReadiness.Blocked(
                        ActorProjectileFireEndpointReadinessKind.MissingProfile,
                        descriptor,
                        commandId,
                        DefaultFireModeId,
                        ActorProjectileFireBlockedReasonKind.MissingSpawnProfile,
                        "projectile_fire_profile_missing",
                        "ActorProjectileFireEndpoint requires ActorProjectileFireProfileAsset.")
                    : ActorProjectileFireEndpointReadiness.SkippedOptional(
                        descriptor,
                        commandId,
                        "optional_projectile_fire_profile_missing");
                return !required;
            }

            if (!fireProfile.TryGetFireMode(DefaultFireModeId, out ActorProjectileFireMode fireMode, out string fireModeReason))
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.MissingFireMode,
                    descriptor,
                    commandId,
                    DefaultFireModeId,
                    ActorProjectileFireBlockedReasonKind.MissingFireMode,
                    fireModeReason,
                    fireModeReason);
                return false;
            }
            readiness = ActorProjectileFireEndpointReadiness.Prepared(
                descriptor,
                commandId,
                fireMode.FireModeId,
                "projectile_fire_endpoint_ready");
            return true;
        }

        public bool TryBuildFireCommand(
            ActorCommandEnvelope commandEnvelope,
            ActorProjectileFireModeId fireModeId,
            Vector3 origin,
            Vector3 direction,
            out ActorProjectileFireCommand command,
            out ActorProjectileFireEndpointReadiness readiness)
        {
            command = default;

            if (!commandEnvelope.IsValid)
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.InvalidCommand,
                    BuildDescriptorOrDefault(commandEnvelope.CommandId),
                    commandEnvelope.CommandId,
                    fireModeId,
                    ActorProjectileFireBlockedReasonKind.InvalidCommand,
                    "invalid_fire_command_envelope",
                    "ActorProjectileFireEndpoint.TryBuildFireCommand requires a valid command envelope.");
                return false;
            }

            ActorProjectileFireModeId resolvedFireModeId = fireModeId.IsValid ? fireModeId : DefaultFireModeId;
            if (!TryGetReadiness(commandEnvelope.CommandId, out readiness) || !readiness.IsPrepared)
            {
                return false;
            }

            if (fireProfile == null)
            {
                const string missingProfileReason = "projectile_fire_profile_missing";
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.MissingProfile,
                    BuildDescriptorOrDefault(commandEnvelope.CommandId),
                    commandEnvelope.CommandId,
                    resolvedFireModeId,
                    ActorProjectileFireBlockedReasonKind.MissingSpawnProfile,
                    missingProfileReason,
                    missingProfileReason);
                return false;
            }

            if (!fireProfile.TryGetFireMode(resolvedFireModeId, out ActorProjectileFireMode fireMode, out string fireModeReason))
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.MissingFireMode,
                    BuildDescriptorOrDefault(commandEnvelope.CommandId),
                    commandEnvelope.CommandId,
                    resolvedFireModeId,
                    ActorProjectileFireBlockedReasonKind.MissingFireMode,
                    fireModeReason,
                    fireModeReason);
                return false;
            }

            if (direction.sqrMagnitude <= 0f)
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.MissingMuzzle,
                    BuildDescriptorOrDefault(commandEnvelope.CommandId),
                    commandEnvelope.CommandId,
                    resolvedFireModeId,
                    ActorProjectileFireBlockedReasonKind.MissingMuzzle,
                    "projectile_fire_direction_missing",
                    "ActorProjectileFireCommand requires non-zero direction.");
                return false;
            }

            command = new ActorProjectileFireCommand(
                commandEnvelope.ActorId,
                commandEnvelope.ActorInstanceRuntimeId,
                commandEnvelope,
                resolvedFireModeId,
                fireMode.SpawnProfileId,
                fireMode.PoolDefinition,
                fireMode.SpawnedActorRole,
                fireMode.SpawnedActorScope,
                origin,
                direction.normalized,
                nameof(ActorProjectileFireEndpoint),
                "projectile_fire_command_built_passively");

            return command.IsValid;
        }

        public void ValidateLocalConfigurationOrThrow(string source)
        {
            if (!TryGetReadiness(ActorCommandId.FirePrimary, out ActorProjectileFireEndpointReadiness readiness) && required)
            {
                string origin = string.IsNullOrWhiteSpace(source) ? nameof(ActorProjectileFireEndpoint) : source.Trim();
                throw new InvalidOperationException($"{origin} invalid projectile fire endpoint: kind='{readiness.Kind}' reason='{readiness.Reason}' message='{readiness.Message}'.");
            }
        }

        private bool TryBuildDescriptor(
            ActorCommandId boundCommandId,
            out ActorProjectileFireEndpointDescriptor descriptor,
            out ActorProjectileFireEndpointReadinessKind failureKind,
            out string reason)
        {
            descriptor = BuildDescriptorOrDefault(boundCommandId);
            failureKind = ActorProjectileFireEndpointReadinessKind.Unknown;
            reason = string.Empty;

            if (!EndpointId.IsValid)
            {
                failureKind = ActorProjectileFireEndpointReadinessKind.MissingRequired;
                reason = "projectile_fire_endpoint_id_missing";
                return false;
            }

            if (!ActorId.IsValid)
            {
                failureKind = ActorProjectileFireEndpointReadinessKind.MissingRequired;
                reason = "actor_id_missing";
                return false;
            }

            if (!ActorInstanceRuntimeId.IsValid)
            {
                failureKind = ActorProjectileFireEndpointReadinessKind.MissingRequired;
                reason = "actor_instance_runtime_id_missing";
                return false;
            }

            if (fireProfile == null)
            {
                failureKind = ActorProjectileFireEndpointReadinessKind.MissingProfile;
                reason = "projectile_fire_profile_missing";
                return false;
            }

            if (!fireProfile.TryValidate(out string profileReason))
            {
                failureKind = ActorProjectileFireEndpointReadinessKind.MissingProfile;
                reason = $"projectile_fire_profile_invalid:{profileReason}";
                return false;
            }

            descriptor = new ActorProjectileFireEndpointDescriptor(
                EndpointId,
                ActorId,
                ActorInstanceRuntimeId,
                fireProfile.ProfileId,
                fireProfile.DefaultFireModeId,
                boundCommandId,
                required,
                nameof(ActorProjectileFireEndpoint),
                "projectile_fire_endpoint_descriptor");

            if (!descriptor.IsValid)
            {
                failureKind = ActorProjectileFireEndpointReadinessKind.Failed;
                reason = "projectile_fire_endpoint_descriptor_invalid";
                return false;
            }

            return true;
        }

        private ActorProjectileFireEndpointDescriptor BuildDescriptorOrDefault(ActorCommandId boundCommandId)
        {
            return new ActorProjectileFireEndpointDescriptor(
                EndpointId,
                ActorId,
                ActorInstanceRuntimeId,
                ProfileId,
                DefaultFireModeId,
                boundCommandId,
                required,
                nameof(ActorProjectileFireEndpoint),
                "projectile_fire_endpoint_descriptor_snapshot");
        }

        private Actor ResolveActor()
        {
            if (_actor != null)
            {
                return _actor;
            }

            _actor = GetComponentInParent<Actor>(includeInactive: true);
            return _actor;
        }

        private static ActorProjectileFireBlockedReasonKind ToBlockedReason(ActorProjectileFireEndpointReadinessKind readinessKind)
        {
            return readinessKind switch
            {
                ActorProjectileFireEndpointReadinessKind.InvalidCommand => ActorProjectileFireBlockedReasonKind.InvalidCommand,
                ActorProjectileFireEndpointReadinessKind.MissingFireMode => ActorProjectileFireBlockedReasonKind.MissingFireMode,
                ActorProjectileFireEndpointReadinessKind.MissingProfile => ActorProjectileFireBlockedReasonKind.MissingSpawnProfile,
                ActorProjectileFireEndpointReadinessKind.MissingMuzzle => ActorProjectileFireBlockedReasonKind.MissingMuzzle,
                ActorProjectileFireEndpointReadinessKind.NotExecutable => ActorProjectileFireBlockedReasonKind.NotExecutable,
                _ => ActorProjectileFireBlockedReasonKind.Unknown,
            };
        }

        private bool IsCooldownActive(ActorProjectileFireMode fireMode, float now, out float nextAllowedTime, out float remainingSeconds)
        {
            nextAllowedTime = 0f;
            remainingSeconds = 0f;

            if (!fireMode.HasCooldown)
            {
                return false;
            }

            if (!_nextAllowedFireTimeByMode.TryGetValue(fireMode.FireModeId, out nextAllowedTime))
            {
                return false;
            }

            remainingSeconds = nextAllowedTime - now;
            if (remainingSeconds <= 0f)
            {
                _nextAllowedFireTimeByMode.Remove(fireMode.FireModeId);
                remainingSeconds = 0f;
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            endpointId = Normalize(endpointId);
        }
#endif

        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
