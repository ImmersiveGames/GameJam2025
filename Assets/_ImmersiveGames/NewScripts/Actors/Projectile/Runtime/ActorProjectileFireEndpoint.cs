using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Projectile.Authoring;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorProjectileFireEndpoint : MonoBehaviour, IActorProjectileFireEndpoint, IActorRuntimePoolDependencyProvider, IActorEntryInitializeResetEndpoint, IActorRuntimeLocalResetEndpoint, IActorRuntimeActivityResetEndpoint, IActorRuntimeActivityTransitionResetEndpoint, IActorRuntimeRouteTransitionResetEndpoint, IActorResetContributionProvider, IActorReleaseContributionProvider, IActorCapabilityReleaseEndpoint
    {
        [Header("Projectile Fire Endpoint")]
        [SerializeField, InspectorName("Nome interno do endpoint"), Tooltip("Identificador técnico do endpoint local de fire/projectile. Usado para logs, readiness e correlação interna; não é nome visual do projétil.")]
        private string endpointId = "actor.projectile.fire.endpoint.primary";
        [SerializeField, InspectorName("Perfil de disparo"), Tooltip("Perfil autoral da capability de disparo. O perfil define modos de disparo e aponta para o spawn profile do projectile.")]
        private ActorProjectileFireProfileAsset fireProfile;

        [Header("Readiness / temporário")]
        [SerializeField, InspectorName("Obrigatório por padrão (temporário)"), Tooltip("Indica se este endpoint local bloqueia readiness quando o perfil está ausente. Preferir mover a obrigatoriedade para requirement/binding da Activity em corte futuro.")]
        private bool required;

        [Header("Reset")]
        [SerializeField] private ActivityResetBoundaryEligibility resetBoundaryEligibility = ActivityResetBoundaryEligibility.RuntimeAll;

        private readonly ActorProjectileSpawnRuntimeState _spawnRuntimeState = new();
        private Actor _actor;
        private IActorProjectileSpawnAdapter _spawnAdapter;
        private IActorProjectileFireAudioAdapter _fireAudioAdapter;
        private string _spawnAdapterName = string.Empty;
        private string _fireAudioAdapterName = string.Empty;
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
        public bool HasFireAudioAdapter => _fireAudioAdapter != null;
        public string FireAudioAdapterName => Normalize(_fireAudioAdapterName);
        public int TrackedSpawnCount => _spawnRuntimeState.TrackedSpawnCount;
        public bool HasConfiguredSpawnRuntimePoolService => _spawnRuntimeState.HasConfiguredPoolService;
        public IReadOnlyList<PoolDefinitionAsset> RuntimePoolDefinitions => fireProfile == null
            ? Array.Empty<PoolDefinitionAsset>()
            : fireProfile.GetRuntimePoolDefinitions();

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
                $"event='ActorProjectileSpawnAdapterConfigured' actorId='{ActorId}' actorInstanceRuntimeId='{ActorInstanceRuntimeId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{DefaultFireModeId}' adapter='{SpawnAdapterName}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);
        }

        public void ConfigureSpawnRuntimeStatePoolService(
            IPoolService poolService,
            string source,
            string reason)
        {
            if (poolService == null)
            {
                throw new ArgumentNullException(nameof(poolService));
            }

            _spawnRuntimeState.ConfigurePoolService(
                poolService,
                ActorId,
                ActorInstanceRuntimeId,
                source,
                reason);
        }

        public void ConfigureFireAudioAdapter(
            IActorProjectileFireAudioAdapter fireAudioAdapter,
            string source,
            string reason)
        {
            _fireAudioAdapter = fireAudioAdapter ?? throw new ArgumentNullException(nameof(fireAudioAdapter));
            _fireAudioAdapterName = fireAudioAdapter.AdapterName;

            DebugUtility.LogVerbose(
                typeof(ActorProjectileFireEndpoint),
                $"event='ActorProjectileFireAudioAdapterConfigured' actorId='{ActorId}' actorInstanceRuntimeId='{ActorInstanceRuntimeId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{DefaultFireModeId}' adapter='{FireAudioAdapterName}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
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

                DebugUtility.LogVerbose(
                    typeof(ActorProjectileFireEndpoint),
                    $"event='ActorProjectileFireCommandRejected' actorId='{ActorId}' actorInstanceRuntimeId='{ActorInstanceRuntimeId}' commandId='{command.CommandId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{DefaultFireModeId}' dispatchStatus='RejectedUnsupportedCommand' reason='{invalidReason}' source='{nameof(ActorProjectileFireEndpoint)}'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedUnsupportedCommand(invalidReason);
            }

            if (!_projectileFireEnabled)
            {
                DebugUtility.LogVerbose(
                    typeof(ActorProjectileFireEndpoint),
                    $"event='ActorProjectileFireCommandRejected' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{DefaultFireModeId}' dispatchStatus='RejectedInactive' reason='projectile_fire_endpoint_inactive' source='{nameof(ActorProjectileFireEndpoint)}'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedInactive("projectile_fire_endpoint_inactive");
            }

            if (fireProfile == null)
            {
                const string missingProfileReason = "projectile_fire_profile_missing";

                DebugUtility.LogWarning(
                    typeof(ActorProjectileFireEndpoint),
                    $"event='ActorProjectileFireCommandRejected' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{DefaultFireModeId}' dispatchStatus='RejectedUnsupportedCommand' reason='{missingProfileReason}' source='{nameof(ActorProjectileFireEndpoint)}'.");

                return ActorCommandDispatchResult.RejectedUnsupportedCommand(missingProfileReason);
            }

            if (!fireProfile.TryGetFireMode(DefaultFireModeId, out var fireMode, out string fireModeReason))
            {
                string blockedReason = string.IsNullOrWhiteSpace(fireModeReason)
                    ? "projectile_fire_mode_missing"
                    : fireModeReason;

                DebugUtility.LogVerbose(
                    typeof(ActorProjectileFireEndpoint),
                    $"event='ActorProjectileFireCommandRejected' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{DefaultFireModeId}' dispatchStatus='RejectedUnsupportedCommand' blockedReason='MissingFireMode' reason='{blockedReason}' source='{nameof(ActorProjectileFireEndpoint)}'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedUnsupportedCommand(blockedReason);
            }

            float now = Time.time;
            if (IsCooldownActive(fireMode, now, out float nextAllowedTime, out float remainingSeconds))
            {
                DebugUtility.LogVerbose(
                    typeof(ActorProjectileFireEndpoint),
                    $"event='ActorProjectileFireCooldownBlocked' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireMode.FireModeId}' cooldownSeconds='{fireMode.CooldownSeconds:0.###}' remainingSeconds='{remainingSeconds:0.###}' nextAllowedTime='{nextAllowedTime:0.###}' dispatchStatus='RejectedUnsupportedCommand' reason='projectile_fire_cooldown_active' source='{nameof(ActorProjectileFireEndpoint)}'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedUnsupportedCommand("projectile_fire_cooldown_active");
            }

            if (!TryResolveFireOrigin(
                    command,
                    fireMode,
                    out var resolvedOrigin,
                    out string originSource,
                    out string originFailureReason,
                    out string originFailureMessage))
            {
                LogFireOriginCommandRejected(
                    command,
                    fireMode,
                    originFailureReason,
                    originFailureMessage);

                return ActorCommandDispatchResult.RejectedUnsupportedCommand(originFailureReason);
            }

            if (!TryBuildFireCommand(
                command,
                fireMode.FireModeId,
                resolvedOrigin.Position,
                resolvedOrigin.Direction,
                out var fireCommand,
                out var readiness))
            {
                string blockedReason = string.IsNullOrWhiteSpace(readiness.Reason)
                    ? "projectile_fire_command_build_failed"
                    : readiness.Reason;

                DebugUtility.LogWarning(
                    typeof(ActorProjectileFireEndpoint),
                    $"event='ActorProjectileFireCommandRejected' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireMode.FireModeId}' originId='{fireMode.SpawnOriginId}' resolutionMode='{fireMode.SpawnOriginResolutionMode}' originSource='{originSource}' position='{FormatVector(resolvedOrigin.Position)}' direction='{FormatVector(resolvedOrigin.Direction)}' dispatchStatus='RejectedUnsupportedCommand' reason='{blockedReason}' source='{nameof(ActorProjectileFireEndpoint)}'.");

                return ActorCommandDispatchResult.RejectedUnsupportedCommand(blockedReason);
            }

            DebugUtility.LogVerbose(
                typeof(ActorProjectileFireEndpoint),
                $"event='ActorProjectileFireCommandBuilt' actorId='{fireCommand.ActorId}' actorInstanceRuntimeId='{fireCommand.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' originId='{fireMode.SpawnOriginId}' resolutionMode='{fireMode.SpawnOriginResolutionMode}' originSource='{originSource}' origin='{FormatVector(fireCommand.Origin)}' direction='{FormatVector(fireCommand.Direction)}' motionStrategy='{fireCommand.MotionBootstrap.Strategy}' linearSpeed='{fireCommand.MotionBootstrap.Speed:0.###}' cooldownSeconds='{fireMode.CooldownSeconds:0.###}' source='{nameof(ActorProjectileFireEndpoint)}' reason='spawn_adapter_command_built'.",
                DebugUtility.Colors.Info);

            if (_spawnAdapter == null)
            {
                DebugUtility.LogVerbose(
                    typeof(ActorProjectileFireEndpoint),
                    $"event='ActorProjectileSpawnAdapterMissing' actorId='{fireCommand.ActorId}' actorInstanceRuntimeId='{fireCommand.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' originId='{fireMode.SpawnOriginId}' resolutionMode='{fireMode.SpawnOriginResolutionMode}' originSource='{originSource}' origin='{FormatVector(fireCommand.Origin)}' direction='{FormatVector(fireCommand.Direction)}' motionStrategy='{fireCommand.MotionBootstrap.Strategy}' linearSpeed='{fireCommand.MotionBootstrap.Speed:0.###}' spawnExecuted='False' poolCalled='False' dispatchStatus='RejectedUnsupportedCommand' source='{nameof(ActorProjectileFireEndpoint)}' reason='projectile_spawn_adapter_missing'.",
                    DebugUtility.Colors.Info);

                return ActorCommandDispatchResult.RejectedUnsupportedCommand("projectile_spawn_adapter_missing");
            }

            var adapterResult = _spawnAdapter.Execute(fireCommand);

            DebugUtility.Log(
                typeof(ActorProjectileFireEndpoint),
                $"event='ActorProjectileFireSpawnAdapterCompleted' actorId='{fireCommand.ActorId}' actorInstanceRuntimeId='{fireCommand.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' originId='{fireMode.SpawnOriginId}' resolutionMode='{fireMode.SpawnOriginResolutionMode}' originSource='{originSource}' motionStrategy='{fireCommand.MotionBootstrap.Strategy}' linearSpeed='{fireCommand.MotionBootstrap.Speed:0.###}' adapter='{SpawnAdapterName}' adapterResult='{adapterResult.Kind}' spawnExecuted='{adapterResult.SpawnExecuted}' poolCalled='{adapterResult.PoolCalled}' source='{nameof(ActorProjectileFireEndpoint)}' reason='{adapterResult.Reason}'.",
                adapterResult.IsFailed ? DebugUtility.Colors.Info : DebugUtility.Colors.Success);

            if (adapterResult.IsFailed || !adapterResult.IsAccepted)
            {
                string failureReason = string.IsNullOrWhiteSpace(adapterResult.Reason)
                    ? "projectile_spawn_adapter_failed"
                    : adapterResult.Reason;
                return ActorCommandDispatchResult.RejectedUnsupportedCommand(failureReason);
            }

            TryTrackSpawnedRuntimeObject(adapterResult, command.Source, command.Reason);
            TryPlayFireAudioCue(fireMode, fireCommand, command.Source, command.Reason);

            if (fireMode.HasCooldown)
            {
                float scheduledNextAllowedTime = now + fireMode.CooldownSeconds;
                _nextAllowedFireTimeByMode[fireMode.FireModeId] = scheduledNextAllowedTime;

                DebugUtility.LogVerbose(
                    typeof(ActorProjectileFireEndpoint),
                    $"event='ActorProjectileFireCooldownScheduled' actorId='{fireCommand.ActorId}' actorInstanceRuntimeId='{fireCommand.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' cooldownSeconds='{fireMode.CooldownSeconds:0.###}' nextAllowedTime='{scheduledNextAllowedTime:0.###}' source='{nameof(ActorProjectileFireEndpoint)}' reason='projectile_fire_cooldown_scheduled'.",
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

            if (!TryBuildDescriptor(commandId, out var descriptor, out var descriptorFailureKind, out string descriptorFailureReason))
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

            if (!fireProfile.TryGetFireMode(DefaultFireModeId, out var fireMode, out string fireModeReason))
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

            var resolvedFireModeId = fireModeId.IsValid ? fireModeId : DefaultFireModeId;
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

            if (!fireProfile.TryGetFireMode(resolvedFireModeId, out var fireMode, out string fireModeReason))
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

            var spawnProfile = fireMode.SpawnProfileAsset;
            if (spawnProfile == null)
            {
                const string missingSpawnProfileReason = "projectile_spawn_profile_missing";
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.MissingProfile,
                    BuildDescriptorOrDefault(commandEnvelope.CommandId),
                    commandEnvelope.CommandId,
                    resolvedFireModeId,
                    ActorProjectileFireBlockedReasonKind.MissingSpawnProfile,
                    missingSpawnProfileReason,
                    "ActorProjectileFireEndpoint requires a spawn profile asset to resolve motion bootstrap.");
                return false;
            }

            if (!spawnProfile.TryValidate(out string spawnProfileReason))
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.MissingProfile,
                    BuildDescriptorOrDefault(commandEnvelope.CommandId),
                    commandEnvelope.CommandId,
                    resolvedFireModeId,
                    ActorProjectileFireBlockedReasonKind.MissingSpawnProfile,
                    $"projectile_spawn_profile_invalid:{spawnProfileReason}",
                    spawnProfileReason);
                return false;
            }

            if (fireMode.MotionStrategy != ActorProjectileMotionStrategyKind.Linear)
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.NotExecutable,
                    BuildDescriptorOrDefault(commandEnvelope.CommandId),
                    commandEnvelope.CommandId,
                    resolvedFireModeId,
                    ActorProjectileFireBlockedReasonKind.NotExecutable,
                    "projectile_motion_strategy_linear_required",
                    "ActorProjectileFireEndpoint currently supports only linear projectile motion.");
                return false;
            }

            float linearSpeed = fireMode.LinearSpeed;
            if (linearSpeed <= 0f)
            {
                readiness = ActorProjectileFireEndpointReadiness.Blocked(
                    ActorProjectileFireEndpointReadinessKind.NotExecutable,
                    BuildDescriptorOrDefault(commandEnvelope.CommandId),
                    commandEnvelope.CommandId,
                    resolvedFireModeId,
                    ActorProjectileFireBlockedReasonKind.NotExecutable,
                    "projectile_motion_linear_speed_invalid",
                    "ActorProjectileFireEndpoint requires a positive linearSpeed.");
                return false;
            }

            var motionBootstrap = new ActorProjectileMotionBootstrap(
                direction.normalized,
                linearSpeed,
                fireMode.MotionStrategy,
                nameof(ActorProjectileFireEndpoint),
                "projectile_motion_bootstrap_resolved");

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
                motionBootstrap,
                nameof(ActorProjectileFireEndpoint),
                "projectile_fire_command_built_passively");

            return command.IsValid;
        }

        public void ValidateLocalConfigurationOrThrow(string source)
        {
            if (!TryGetReadiness(ActorCommandId.FirePrimary, out var readiness) && required)
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

        private bool TryResolveFireOrigin(
            ActorCommandEnvelope command,
            ActorProjectileFireMode fireMode,
            out PoolableSpawnOriginResolved resolvedOrigin,
            out string originSource,
            out string failureReason,
            out string failureMessage)
        {
            resolvedOrigin = default;
            originSource = string.Empty;
            failureReason = string.Empty;
            failureMessage = string.Empty;

            LogFireOriginResolutionStarted(command, fireMode);

            var actor = ResolveActor();
            var capabilitySurface = actor?.CapabilitySurface;
            var presentationEndpoint = capabilitySurface?.PresentationEndpoint;
            if (presentationEndpoint == null)
            {
                failureReason = "projectile_fire_presentation_endpoint_missing";
                failureMessage = "ActorProjectileFireEndpoint requires ActorPresentationEndpoint on the owning Actor.";
                LogFireOriginMissing(command, fireMode, failureReason, failureMessage, originSource: "none");
                return false;
            }

            if (!fireMode.SpawnOriginId.IsValid)
            {
                failureReason = "projectile_fire_spawn_origin_id_missing";
                failureMessage = "ActorProjectileFireEndpoint requires a valid spawnOriginId on the fire mode.";
                LogFireOriginMissing(command, fireMode, failureReason, failureMessage, originSource: "none");
                return false;
            }

            if (!presentationEndpoint.TryResolve(
                    fireMode.SpawnOriginId,
                    fireMode.SpawnOriginResolutionMode,
                    out resolvedOrigin))
            {
                failureReason = "projectile_fire_origin_missing";
                failureMessage = $"ActorPresentationEndpoint could not resolve originId='{fireMode.SpawnOriginId}' resolutionMode='{fireMode.SpawnOriginResolutionMode}'.";
                LogFireOriginMissing(command, fireMode, failureReason, failureMessage, originSource: "none");
                return false;
            }

            if (!resolvedOrigin.IsValid || resolvedOrigin.OriginTransform == null)
            {
                failureReason = "projectile_fire_origin_invalid";
                failureMessage = $"Resolved projectile fire origin is invalid originId='{fireMode.SpawnOriginId}' resolutionMode='{fireMode.SpawnOriginResolutionMode}'.";
                LogFireOriginMissing(command, fireMode, failureReason, failureMessage, originSource: "none");
                return false;
            }

            if (resolvedOrigin.Direction.sqrMagnitude <= 0f)
            {
                failureReason = "projectile_fire_origin_direction_invalid";
                failureMessage = $"Resolved projectile fire origin has invalid direction originId='{fireMode.SpawnOriginId}' resolutionMode='{fireMode.SpawnOriginResolutionMode}'.";
                LogFireOriginMissing(command, fireMode, failureReason, failureMessage, originSource: "none");
                return false;
            }

            originSource = resolvedOrigin.UsedFallback ? "EmitterRoot" : "PresentationAnchor";
            if (resolvedOrigin.UsedFallback)
            {
                LogFireOriginFallbackApplied(command, fireMode, resolvedOrigin, originSource);
            }

            LogFireOriginResolved(command, fireMode, resolvedOrigin, originSource);
            return true;
        }

        private void LogFireOriginResolutionStarted(
            ActorCommandEnvelope command,
            ActorProjectileFireMode fireMode)
        {
            DebugUtility.Log(
                typeof(ActorProjectileFireEndpoint),
                $"event='ActorProjectileFireOriginResolutionStarted' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireMode.FireModeId}' originId='{fireMode.SpawnOriginId}' resolutionMode='{fireMode.SpawnOriginResolutionMode}' source='{nameof(ActorProjectileFireEndpoint)}' reason='projectile_fire_origin_resolution_started'.",
                DebugUtility.Colors.Info);
        }

        private void LogFireOriginResolved(
            ActorCommandEnvelope command,
            ActorProjectileFireMode fireMode,
            PoolableSpawnOriginResolved resolvedOrigin,
            string originSource)
        {
            DebugUtility.Log(
                typeof(ActorProjectileFireEndpoint),
                $"event='ActorProjectileFireOriginResolved' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireMode.FireModeId}' originId='{fireMode.SpawnOriginId}' resolutionMode='{fireMode.SpawnOriginResolutionMode}' originSource='{Normalize(originSource)}' position='{FormatVector(resolvedOrigin.Position)}' direction='{FormatVector(resolvedOrigin.Direction)}' usedFallback='{resolvedOrigin.UsedFallback}' source='{nameof(ActorProjectileFireEndpoint)}' reason='projectile_fire_origin_resolved'.",
                DebugUtility.Colors.Success);
        }

        private void LogFireOriginFallbackApplied(
            ActorCommandEnvelope command,
            ActorProjectileFireMode fireMode,
            PoolableSpawnOriginResolved resolvedOrigin,
            string originSource)
        {
            DebugUtility.Log(
                typeof(ActorProjectileFireEndpoint),
                $"event='ActorProjectileFireOriginFallbackApplied' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireMode.FireModeId}' originId='{fireMode.SpawnOriginId}' resolutionMode='{fireMode.SpawnOriginResolutionMode}' originSource='{Normalize(originSource)}' position='{FormatVector(resolvedOrigin.Position)}' direction='{FormatVector(resolvedOrigin.Direction)}' usedFallback='{resolvedOrigin.UsedFallback}' source='{nameof(ActorProjectileFireEndpoint)}' reason='projectile_fire_origin_fallback_applied'.",
                DebugUtility.Colors.Info);
        }

        private void LogFireOriginMissing(
            ActorCommandEnvelope command,
            ActorProjectileFireMode fireMode,
            string reason,
            string message,
            string originSource)
        {
            DebugUtility.LogWarning(
                typeof(ActorProjectileFireEndpoint),
                $"event='ActorProjectileFireOriginMissing' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireMode.FireModeId}' originId='{fireMode.SpawnOriginId}' resolutionMode='{fireMode.SpawnOriginResolutionMode}' originSource='{Normalize(originSource)}' position='{FormatVector(Vector3.zero)}' direction='{FormatVector(Vector3.zero)}' reason='{Normalize(reason)}' message='{Normalize(message)}' source='{nameof(ActorProjectileFireEndpoint)}'.");
        }

        private void LogFireOriginCommandRejected(
            ActorCommandEnvelope command,
            ActorProjectileFireMode fireMode,
            string reason,
            string message)
        {
            DebugUtility.LogWarning(
                typeof(ActorProjectileFireEndpoint),
                $"event='ActorProjectileFireCommandRejected' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' commandId='{command.CommandId}' bindingId='{command.BindingId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireMode.FireModeId}' originId='{fireMode.SpawnOriginId}' resolutionMode='{fireMode.SpawnOriginResolutionMode}' originSource='none' position='{FormatVector(Vector3.zero)}' direction='{FormatVector(Vector3.zero)}' dispatchStatus='RejectedUnsupportedCommand' reason='{Normalize(reason)}' message='{Normalize(message)}' source='{nameof(ActorProjectileFireEndpoint)}'.");
        }

        private void TryPlayFireAudioCue(
            ActorProjectileFireMode fireMode,
            ActorProjectileFireCommand fireCommand,
            string source,
            string reason)
        {
            if (fireMode.FireAudioCue == null)
            {
                return;
            }

            if (_fireAudioAdapter == null)
            {
                DebugUtility.LogWarning(
                    typeof(ActorProjectileFireEndpoint),
                    $"event='ActorProjectileFireAudioCueSkipped' actorId='{fireCommand.ActorId}' actorInstanceRuntimeId='{fireCommand.ActorInstanceRuntimeId}' endpointId='{EndpointId}' profileId='{ProfileId}' fireModeId='{fireCommand.FireModeId}' cue='{fireMode.FireAudioCue.name}' source='{nameof(ActorProjectileFireEndpoint)}' reason='projectile_fire_audio_adapter_missing'.");
                return;
            }

            _fireAudioAdapter.PlayFireCue(
                fireMode.FireAudioCue,
                fireCommand.Origin,
                fireMode.FireAudioVolumeScale,
                source,
                reason);
        }

        private void TryTrackSpawnedRuntimeObject(ActorProjectileSpawnAdapterResult adapterResult, string source, string reason)
        {
            if (!adapterResult.IsAccepted || adapterResult.SpawnedInstance == null || adapterResult.SpawnedActor == null)
            {
                return;
            }

            var spawnedActor = adapterResult.SpawnedActor as RuntimeSpawnedActor;
            if (spawnedActor == null)
            {
                DebugUtility.LogWarning(
                    typeof(ActorProjectileFireEndpoint),
                    $"event='ActorProjectileSpawnTrackSkipped' actorId='{adapterResult.Command.ActorId}' actorInstanceRuntimeId='{adapterResult.Command.ActorInstanceRuntimeId}' spawnedInstanceName='{adapterResult.SpawnedInstance.name}' source='{nameof(ActorProjectileFireEndpoint)}' reason='spawned_actor_not_runtime_spawned_actor'.");
                return;
            }

            var actor = ResolveActor();
            if (actor == null)
            {
                DebugUtility.LogWarning(
                    typeof(ActorProjectileFireEndpoint),
                    $"event='ActorProjectileSpawnTrackSkipped' actorId='{adapterResult.Command.ActorId}' actorInstanceRuntimeId='{adapterResult.Command.ActorInstanceRuntimeId}' spawnedActorId='{spawnedActor.ActorIdValue}' spawnedActorInstanceRuntimeId='{spawnedActor.RuntimeActorInstanceId}' spawnedInstanceName='{adapterResult.SpawnedInstance.name}' source='{nameof(ActorProjectileFireEndpoint)}' reason='owner_actor_missing'.");
                return;
            }

            if (!_spawnRuntimeState.TryTrackSpawnedRuntimeObject(actor, adapterResult.SpawnedInstance, spawnedActor, source, reason))
            {
                DebugUtility.LogWarning(
                    typeof(ActorProjectileFireEndpoint),
                    $"event='ActorProjectileSpawnTrackSkipped' actorId='{adapterResult.Command.ActorId}' actorInstanceRuntimeId='{adapterResult.Command.ActorInstanceRuntimeId}' spawnedActorId='{spawnedActor.ActorIdValue}' spawnedActorInstanceRuntimeId='{spawnedActor.RuntimeActorInstanceId}' spawnedInstanceName='{adapterResult.SpawnedInstance.name}' source='{nameof(ActorProjectileFireEndpoint)}' reason='spawn_tracker_rejected_spawn'.");
            }
        }


        public bool TryCreateResetContribution(
            ActorCapabilityContributionContext context,
            out IActorResetContribution contribution)
        {
            if (!context.IsValid)
            {
                contribution = null;
                return false;
            }

            contribution = new SpawnedRuntimeObjectsResetContribution(context, resetBoundaryEligibility);
            return true;
        }

        public bool TryCreateReleaseContribution(
            ActorCapabilityContributionContext context,
            out IActorReleaseContribution contribution)
        {
            if (!context.IsValid)
            {
                contribution = null;
                return false;
            }

            contribution = new SpawnedRuntimeObjectsReleaseContribution(context, this);
            return true;
        }

        public bool TryRelease(
            ActorCapabilityContributionContext context,
            out ActorCapabilityReleaseResult result)
        {
            return _spawnRuntimeState.TryReleaseSpawnedRuntimeObjects(context, out result);
        }

        public void ApplyEntryInitializeReset(ActorResetContext context)
        {
            _spawnRuntimeState.ApplySpawnedRuntimeObjectsStateProfile(context);
        }

        public void ApplyRuntimeLocalReset(ActorResetContext context)
        {
            _spawnRuntimeState.ApplySpawnedRuntimeObjectsStateProfile(context);
        }

        public void ApplyRuntimeActivityReset(ActorResetContext context)
        {
            _spawnRuntimeState.ApplySpawnedRuntimeObjectsStateProfile(context);
        }

        public void ApplyRuntimeActivityTransitionReset(ActorResetContext context)
        {
            _spawnRuntimeState.ApplySpawnedRuntimeObjectsStateProfile(context);
        }

        public void ApplyRuntimeRouteTransitionReset(ActorResetContext context)
        {
            _spawnRuntimeState.ApplySpawnedRuntimeObjectsStateProfile(context);
        }

        private void OnDestroy()
        {
            _spawnRuntimeState.Clear();
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


        private readonly struct SpawnedRuntimeObjectsReleaseContribution : IActorReleaseContribution
        {
            public SpawnedRuntimeObjectsReleaseContribution(
                ActorCapabilityContributionContext context,
                IActorCapabilityReleaseEndpoint releaseEndpoint)
            {
                Descriptor = new ActorCapabilityContributionDescriptor(
                    new ActorCapabilityId("actor.capability.projectile.spawn_runtime_objects.release"),
                    ActorCapabilityContributionPhase.Release,
                    ActorCapabilityContributionRequirement.Required,
                    context.ActorId,
                    context.ActorInstanceRuntimeId,
                    context.ActorKind,
                    context.ActorRole,
                    context.ActorScope,
                    context.ComponentPath,
                    nameof(ActorProjectileFireEndpoint),
                    "projectile_spawn_runtime_objects_release_contribution");
                ReleaseEndpoint = releaseEndpoint;
            }

            public ActorCapabilityContributionDescriptor Descriptor { get; }
            public IActorCapabilityReleaseEndpoint ReleaseEndpoint { get; }
            public bool IsValid => Descriptor.IsValid && ReleaseEndpoint != null;
        }

        private readonly struct SpawnedRuntimeObjectsResetContribution : IActorResetContribution
        {
            public SpawnedRuntimeObjectsResetContribution(ActorCapabilityContributionContext context, ActivityResetBoundaryEligibility resetBoundaryEligibility)
            {
                Descriptor = new ActorCapabilityContributionDescriptor(
                    new ActorCapabilityId("actor.capability.projectile.spawn_runtime_objects"),
                    ActorCapabilityContributionPhase.Reset,
                    ActorCapabilityContributionRequirement.Optional,
                    context.ActorId,
                    context.ActorInstanceRuntimeId,
                    context.ActorKind,
                    context.ActorRole,
                    context.ActorScope,
                    context.ComponentPath,
                    nameof(ActorProjectileFireEndpoint),
                    "projectile_spawn_runtime_objects_reset_contribution");
                ResetBoundaryEligibility = resetBoundaryEligibility;
            }

            public ActorCapabilityContributionDescriptor Descriptor { get; }
            public ActivityResetBoundaryEligibility ResetBoundaryEligibility { get; }
            public bool IsValid => Descriptor.IsValid;
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
