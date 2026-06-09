using System;
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
        [SerializeField, Tooltip("Identificador técnico do endpoint de fire/projectile. O endpoint não executa spawn neste corte.")]
        private string endpointId = "actor.projectile.fire.endpoint.player.primary";
        [SerializeField, Tooltip("Profile autoral tipado da capability de fire/projectile.")]
        private ActorProjectileFireProfileAsset fireProfile;
        [SerializeField] private ActorCommandKind acceptedCommandKind = ActorCommandKind.FirePrimary;
        [SerializeField] private bool required;
        [SerializeField, Tooltip("Fronteira explícita de spawn. Este adapter não executa spawn neste corte.")]
        private ActorProjectileSpawnAdapterBoundary spawnAdapter;

        private Actor actor;

        public ActorProjectileFireEndpointId EndpointId => new(Normalize(endpointId));
        public ActorId ActorId => ResolveActor()?.ActorIdValue ?? default;
        public ActorInstanceRuntimeId ActorInstanceRuntimeId => ResolveActor()?.RuntimeActorInstanceId ?? default;
        public ActorProjectileProfileId ProfileId => fireProfile == null ? default : fireProfile.ProfileId;
        public ActorProjectileFireModeId DefaultFireModeId => fireProfile == null ? default : fireProfile.DefaultFireModeId;
        public bool IsRequired => required;



        public ActorCommandDispatchResult AcceptCommand(ActorCommandEnvelope command)
        {
            if (!command.IsValid || command.CommandId != ActorCommandId.FirePrimary)
            {
                string invalidReason = !command.IsValid
                    ? "invalid_projectile_fire_envelope"
                    : "unsupported_projectile_fire_command";

                DebugUtility.Log(
                    typeof(ActorProjectileFireEndpoint),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileFireCommandRejected' actorId='{ActorId}' actorInstanceRuntimeId='{ActorInstanceRuntimeId}' commandId='{command.CommandId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{DefaultFireModeId}' dispatchStatus='RejectedUnsupportedCommand' reason='{invalidReason}' source='{nameof(ActorProjectileFireEndpoint)}'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedUnsupportedCommand(invalidReason);
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

            DebugUtility.Log(
                typeof(ActorProjectileFireEndpoint),
                $"[OBS][ActorProjectileFire] event='ActorProjectileFireCommandBuilt' actorId='{fireCommand.ActorId}' actorInstanceRuntimeId='{fireCommand.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' origin='{FormatVector(fireCommand.Origin)}' direction='{FormatVector(fireCommand.Direction)}' source='{nameof(ActorProjectileFireEndpoint)}' reason='spawn_adapter_command_built'.",
                DebugUtility.Colors.Info);

            if (spawnAdapter == null)
            {
                DebugUtility.Log(
                    typeof(ActorProjectileFireEndpoint),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnAdapterMissing' actorId='{fireCommand.ActorId}' actorInstanceRuntimeId='{fireCommand.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' spawnExecuted='False' poolCalled='False' dispatchStatus='RejectedUnsupportedCommand' source='{nameof(ActorProjectileFireEndpoint)}' reason='projectile_spawn_adapter_missing'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedUnsupportedCommand("projectile_spawn_adapter_missing");
            }

            ActorProjectileSpawnAdapterResult adapterResult = spawnAdapter.Execute(fireCommand);

            DebugUtility.Log(
                typeof(ActorProjectileFireEndpoint),
                $"[OBS][ActorProjectileFire] event='ActorProjectileFireSpawnAdapterCompleted' actorId='{fireCommand.ActorId}' actorInstanceRuntimeId='{fireCommand.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' adapter='{spawnAdapter.GetType().Name}' adapterResult='{adapterResult.Kind}' spawnExecuted='{adapterResult.SpawnExecuted}' poolCalled='{adapterResult.PoolCalled}' source='{nameof(ActorProjectileFireEndpoint)}' reason='{adapterResult.Reason}'.",
                adapterResult.IsFailed ? DebugUtility.Colors.Info : DebugUtility.Colors.Success);

            if (adapterResult.IsFailed)
            {
                string failureReason = string.IsNullOrWhiteSpace(adapterResult.Reason)
                    ? "projectile_spawn_adapter_failed"
                    : adapterResult.Reason;
                return ActorCommandDispatchResult.RejectedUnsupportedCommand(failureReason);
            }

            string acceptedReason = string.IsNullOrWhiteSpace(adapterResult.Reason)
                ? "projectile_spawn_adapter_boundary_accepted"
                : adapterResult.Reason;
            return ActorCommandDispatchResult.Accepted(acceptedReason);
        }

        public bool TryGetReadiness(
            ActorCommandId commandId,
            out ActorProjectileFireEndpointReadiness readiness)
        {
            readiness = default;

            if (commandId != ActorCommandId.FirePrimary || acceptedCommandKind != ActorCommandKind.FirePrimary)
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.InvalidCommand,
                    BuildDescriptorOrDefault(),
                    DefaultFireModeId,
                    ActorProjectileFireBlockedReasonKind.InvalidCommand,
                    "invalid_projectile_fire_command",
                    "ActorProjectileFireEndpoint currently accepts only FirePrimary.");
                return false;
            }

            if (!TryBuildDescriptor(out ActorProjectileFireEndpointDescriptor descriptor, out ActorProjectileFireEndpointReadinessKind descriptorFailureKind, out string descriptorFailureReason))
            {
                if (!required && descriptorFailureKind == ActorProjectileFireEndpointReadinessKind.MissingProfile)
                {
                    readiness = ActorProjectileFireEndpointReadiness.SkippedOptional(
                        descriptor,
                        descriptorFailureReason);
                    return true;
                }

                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    descriptorFailureKind,
                    descriptor,
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
                        DefaultFireModeId,
                        ActorProjectileFireBlockedReasonKind.MissingSpawnability,
                        "projectile_fire_profile_missing",
                        "ActorProjectileFireEndpoint requires ActorProjectileFireProfileAsset.")
                    : ActorProjectileFireEndpointReadiness.SkippedOptional(
                        descriptor,
                        "optional_projectile_fire_profile_missing");
                return !required;
            }

            if (!fireProfile.TryGetFireMode(DefaultFireModeId, out ActorProjectileFireMode fireMode, out string fireModeReason))
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.MissingFireMode,
                    descriptor,
                    DefaultFireModeId,
                    ActorProjectileFireBlockedReasonKind.MissingFireMode,
                    fireModeReason,
                    fireModeReason);
                return false;
            }

            if (fireMode.AcceptedCommandId != ActorCommandId.FirePrimary)
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.InvalidCommand,
                    descriptor,
                    fireMode.FireModeId,
                    ActorProjectileFireBlockedReasonKind.InvalidCommand,
                    "projectile_fire_mode_command_mismatch",
                    "Projectile fire mode does not accept FirePrimary.");
                return false;
            }

            readiness = ActorProjectileFireEndpointReadiness.Prepared(
                descriptor,
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

            if (!commandEnvelope.IsValid || commandEnvelope.CommandId != ActorCommandId.FirePrimary)
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.InvalidCommand,
                    BuildDescriptorOrDefault(),
                    fireModeId,
                    ActorProjectileFireBlockedReasonKind.InvalidCommand,
                    "invalid_fire_command_envelope",
                    "ActorProjectileFireEndpoint.TryBuildFireCommand requires a valid FirePrimary envelope.");
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
                    BuildDescriptorOrDefault(),
                    resolvedFireModeId,
                    ActorProjectileFireBlockedReasonKind.MissingSpawnability,
                    missingProfileReason,
                    missingProfileReason);
                return false;
            }

            if (!fireProfile.TryGetFireMode(resolvedFireModeId, out _, out string fireModeReason))
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.MissingFireMode,
                    BuildDescriptorOrDefault(),
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
                    BuildDescriptorOrDefault(),
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
            out ActorProjectileFireEndpointDescriptor descriptor,
            out ActorProjectileFireEndpointReadinessKind failureKind,
            out string reason)
        {
            descriptor = BuildDescriptorOrDefault();
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
                ActorCommandId.FirePrimary,
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

        private ActorProjectileFireEndpointDescriptor BuildDescriptorOrDefault()
        {
            return new ActorProjectileFireEndpointDescriptor(
                EndpointId,
                ActorId,
                ActorInstanceRuntimeId,
                ProfileId,
                DefaultFireModeId,
                acceptedCommandKind == ActorCommandKind.FirePrimary ? ActorCommandId.FirePrimary : default,
                required,
                nameof(ActorProjectileFireEndpoint),
                "projectile_fire_endpoint_descriptor_snapshot");
        }

        private Actor ResolveActor()
        {
            if (actor != null)
            {
                return actor;
            }

            actor = GetComponentInParent<Actor>(includeInactive: true);
            return actor;
        }

        private static ActorProjectileFireBlockedReasonKind ToBlockedReason(ActorProjectileFireEndpointReadinessKind readinessKind)
        {
            return readinessKind switch
            {
                ActorProjectileFireEndpointReadinessKind.InvalidCommand => ActorProjectileFireBlockedReasonKind.InvalidCommand,
                ActorProjectileFireEndpointReadinessKind.MissingFireMode => ActorProjectileFireBlockedReasonKind.MissingFireMode,
                ActorProjectileFireEndpointReadinessKind.MissingProfile => ActorProjectileFireBlockedReasonKind.MissingSpawnability,
                ActorProjectileFireEndpointReadinessKind.MissingMuzzle => ActorProjectileFireBlockedReasonKind.MissingMuzzle,
                ActorProjectileFireEndpointReadinessKind.NotExecutable => ActorProjectileFireBlockedReasonKind.NotExecutable,
                _ => ActorProjectileFireBlockedReasonKind.Unknown,
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            endpointId = Normalize(endpointId);
            if (acceptedCommandKind != ActorCommandKind.FirePrimary)
            {
                acceptedCommandKind = ActorCommandKind.FirePrimary;
            }
        }
#endif


        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
