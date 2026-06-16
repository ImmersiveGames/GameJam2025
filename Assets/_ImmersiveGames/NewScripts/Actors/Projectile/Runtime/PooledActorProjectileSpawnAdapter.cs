using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    public sealed class PooledActorProjectileSpawnAdapter : IActorProjectileSpawnAdapter
    {
        private const float TransformPositionTolerance = 0.01f;
        private const float TransformRotationTolerance = 0.01f;
        private const string VisualContractOptionalForRuntimeSpawn = "optional_for_runtime_spawn";

        private readonly string _adapterId;
        private readonly Transform _spawnParent;
        private readonly IPoolService _poolService;

        public PooledActorProjectileSpawnAdapter(
            string adapterId,
            IPoolService poolService,
            Transform spawnParent = null)
        {
            _adapterId = Normalize(adapterId);
            _poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));
            _spawnParent = spawnParent;
        }

        public string AdapterId => string.IsNullOrWhiteSpace(_adapterId)
            ? "actor.projectile.spawn.adapter.pooled"
            : _adapterId;

        public ActorProjectileSpawnAdapterResult Execute(ActorProjectileFireCommand command)
        {
            if (!command.IsValid)
            {
                DebugUtility.LogVerbose(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"event='ActorProjectileSpawnAdapterRejected' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' fireModeId='{command.FireModeId}' spawnProfileId='{command.SpawnProfileId}' spawnExecuted='False' poolCalled='False' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='invalid_projectile_fire_command'.",
                    DebugUtility.Colors.Info);

                return ActorProjectileSpawnAdapterResult.Failed(
                    command,
                    "invalid_projectile_fire_command",
                    "Projectile spawn adapter received an invalid command.");
            }

            var poolDefinition = command.PoolDefinition;
            if (poolDefinition == null)
            {
                DebugUtility.LogVerbose(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"event='ActorProjectileSpawnAdapterRejected' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' fireModeId='{command.FireModeId}' spawnProfileId='{command.SpawnProfileId}' spawnExecuted='False' poolCalled='False' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='projectile_pool_definition_missing'.",
                    DebugUtility.Colors.Info);

                return ActorProjectileSpawnAdapterResult.Failed(
                    command,
                    "projectile_pool_definition_missing",
                    "Projectile spawn requires a PoolDefinitionAsset resolved from the projectile spawn profile.");
            }

            GameObject instance = null;
            RuntimeSpawnedActor runtimeSpawnedActor = null;
            bool poolCalled = false;

            try
            {
                DebugUtility.LogVerbose(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"event='ActorProjectileSpawnPoolRentRequested' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' fireModeId='{command.FireModeId}' spawnProfileId='{command.SpawnProfileId}' poolDefinition='{poolDefinition.name}' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='projectile_spawn_pool_rent_requested'.",
                    DebugUtility.Colors.Info);

                instance = _poolService.Rent(poolDefinition, _spawnParent);
                poolCalled = true;

                if (instance == null)
                {
                    throw new InvalidOperationException($"PoolService returned null instance for poolDefinition='{poolDefinition.name}'.");
                }

                var targetRotation = Quaternion.LookRotation(command.Direction.normalized, Vector3.up);
                instance.transform.SetPositionAndRotation(command.Origin, targetRotation);

                runtimeSpawnedActor = instance.GetComponent<RuntimeSpawnedActor>();
                if (runtimeSpawnedActor == null)
                {
                    throw new InvalidOperationException($"Projectile spawn prefab requires RuntimeSpawnedActor. poolDefinition='{poolDefinition.name}' instance='{instance.name}'.");
                }

                var spawnedActorId = BuildSpawnedActorId(command);
                var spawnedRuntimeId = ActorInstanceRuntimeId.FromRuntimeSpawnedActorIdentity(
                    command.ActorInstanceRuntimeId,
                    spawnedActorId.Value,
                    command.CommandEnvelope.Sequence);

                var spawnOrigin = new RuntimeSpawnOriginMetadata(
                    command.ActorId,
                    command.ActorInstanceRuntimeId,
                    new RuntimeSpawnProfileId(command.SpawnProfileId.ToString()),
                    poolDefinition,
                    command.CommandEnvelope.Sequence,
                    nameof(PooledActorProjectileSpawnAdapter),
                    "projectile_spawn_origin_bound");

                runtimeSpawnedActor.BindRuntimeMetadata(
                    spawnedActorId,
                    spawnedRuntimeId,
                    command.SpawnedActorRole,
                    command.SpawnedActorScope,
                    ActorParticipationRecord.ActorParticipationPolicy.None,
                    spawnOrigin,
                    nameof(PooledActorProjectileSpawnAdapter));

                var motionEndpoint = instance.GetComponent<ActorProjectileMotionEndpoint>();
                if (motionEndpoint == null)
                {
                    string motionFailureReason = "projectile_motion_endpoint_missing";
                    string motionFailureMessage = $"Projectile spawn prefab requires ActorProjectileMotionEndpoint. poolDefinition='{poolDefinition.name}' instance='{instance.name}'.";
                    ReturnRentedInstanceIfNeeded(poolDefinition, instance, motionFailureReason);
                    return ActorProjectileSpawnAdapterResult.Failed(
                        command,
                        poolCalled,
                        instance,
                        runtimeSpawnedActor,
                        motionFailureReason,
                        motionFailureMessage);
                }

                if (!motionEndpoint.TryConfigureMotion(
                        command.MotionBootstrap,
                        nameof(PooledActorProjectileSpawnAdapter),
                        "projectile_motion_bootstrap_configured",
                        out string motionConfigurationReason))
                {
                    string motionFailureReason = string.IsNullOrWhiteSpace(motionConfigurationReason)
                        ? "projectile_motion_bootstrap_rejected"
                        : motionConfigurationReason;
                    string motionFailureMessage = $"Projectile motion bootstrap rejected. poolDefinition='{poolDefinition.name}' instance='{instance.name}' reason='{Normalize(motionConfigurationReason)}'.";
                    ReturnRentedInstanceIfNeeded(poolDefinition, instance, motionFailureReason);
                    return ActorProjectileSpawnAdapterResult.Failed(
                        command,
                        poolCalled,
                        instance,
                        runtimeSpawnedActor,
                        motionFailureReason,
                        motionFailureMessage);
                }

                DebugUtility.Log(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"event='ActorProjectileSpawnMotionBootstrapConfigured' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' spawnedActorId='{runtimeSpawnedActor.ActorIdValue}' spawnedActorInstanceRuntimeId='{runtimeSpawnedActor.RuntimeActorInstanceId}' motionStrategy='{command.MotionBootstrap.Strategy}' linearSpeed='{command.MotionBootstrap.Speed:0.###}' direction='{FormatVector(command.MotionBootstrap.Direction)}' poolDefinition='{poolDefinition.name}' instanceName='{instance.name}' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='projectile_motion_bootstrap_configured'.",
                    DebugUtility.Colors.Success);

                if (!TryPrepareSpawnedInstance(
                    command,
                    poolDefinition,
                    instance,
                    runtimeSpawnedActor,
                    motionEndpoint,
                    command.Origin,
                    targetRotation,
                    out string failureReason,
                    out string failureMessage))
                {
                    ReturnRentedInstanceIfNeeded(poolDefinition, instance, failureReason);
                    return ActorProjectileSpawnAdapterResult.Failed(
                        command,
                        poolCalled,
                        instance,
                        runtimeSpawnedActor,
                        failureReason,
                        failureMessage);
                }

                DebugUtility.Log(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"event='ActorProjectileSpawnedFromPool' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' spawnedActorId='{runtimeSpawnedActor.ActorIdValue}' spawnedActorInstanceRuntimeId='{runtimeSpawnedActor.RuntimeActorInstanceId}' ownerActorId='{runtimeSpawnedActor.OwnerActorId}' ownerActorInstanceRuntimeId='{runtimeSpawnedActor.OwnerActorInstanceRuntimeId}' originPoolDefinition='{runtimeSpawnedActor.SpawnOrigin.PoolDefinitionName}' originCommandSequence='{runtimeSpawnedActor.SpawnOrigin.CommandSequence}' fireModeId='{command.FireModeId}' spawnProfileId='{command.SpawnProfileId}' poolDefinition='{poolDefinition.name}' instanceName='{instance.name}' instancePath='{BuildInstancePath(instance.transform)}' spawnExecuted='True' poolCalled='True' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='projectile_spawned_from_pool'.",
                    DebugUtility.Colors.Success);

                return ActorProjectileSpawnAdapterResult.Spawned(
                    command,
                    instance,
                    runtimeSpawnedActor,
                    "projectile_spawned_from_pool",
                    "Projectile actor instance was rented from the canonical pool and validated as materialized.");
            }
            catch (Exception ex)
            {
                string reason = "projectile_spawn_pool_failed";
                ReturnRentedInstanceIfNeeded(poolDefinition, instance, reason);

                DebugUtility.LogError(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"event='ActorProjectileSpawnAdapterFailed' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' fireModeId='{command.FireModeId}' spawnProfileId='{command.SpawnProfileId}' poolDefinition='{poolDefinition.name}' spawnExecuted='False' poolCalled='{poolCalled}' instanceName='{(instance == null ? string.Empty : instance.name)}' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='{reason}' message='{Normalize(ex.Message)}'.");

                return ActorProjectileSpawnAdapterResult.Failed(
                    command,
                    poolCalled,
                    instance,
                    runtimeSpawnedActor,
                    reason,
                    ex.Message);
            }
        }

        private bool TryPrepareSpawnedInstance(
            ActorProjectileFireCommand command,
            PoolDefinitionAsset poolDefinition,
            GameObject instance,
            RuntimeSpawnedActor runtimeSpawnedActor,
            ActorProjectileMotionEndpoint motionEndpoint,
            Vector3 expectedPosition,
            Quaternion expectedRotation,
            out string failureReason,
            out string failureMessage)
        {
            failureReason = string.Empty;
            failureMessage = string.Empty;

            if (instance == null)
            {
                failureReason = "projectile_spawn_instance_missing";
                failureMessage = "Pool returned a missing projectile instance.";
                return false;
            }

            if (runtimeSpawnedActor == null)
            {
                failureReason = "projectile_spawn_runtime_actor_missing";
                failureMessage = "Projectile spawned instance does not contain RuntimeSpawnedActor.";
                return false;
            }

            if (!runtimeSpawnedActor.IsRuntimeMetadataBound || !runtimeSpawnedActor.ActorIdValue.IsValid || !runtimeSpawnedActor.RuntimeActorInstanceId.IsValid || !runtimeSpawnedActor.HasSpawnOrigin)
            {
                failureReason = "projectile_spawn_runtime_actor_metadata_invalid";
                failureMessage = "Projectile RuntimeSpawnedActor metadata/origin is invalid after bind.";
                return false;
            }

            if (motionEndpoint == null || !motionEndpoint.IsMotionConfigured)
            {
                failureReason = "projectile_spawn_motion_endpoint_unconfigured";
                failureMessage = "Projectile spawned instance does not contain a configured ActorProjectileMotionEndpoint.";
                return false;
            }

            if (!instance.activeSelf)
            {
                failureReason = "projectile_spawn_instance_inactive_self";
                failureMessage = "Projectile spawned instance is inactive after rent.";
                return false;
            }

            if (!instance.activeInHierarchy)
            {
                failureReason = "projectile_spawn_instance_inactive_hierarchy";
                failureMessage = "Projectile spawned instance is not active in hierarchy after rent.";
                return false;
            }

            var actualPosition = instance.transform.position;
            var actualRotation = instance.transform.rotation;
            bool positionApplied = Vector3.Distance(actualPosition, expectedPosition) <= TransformPositionTolerance;
            bool rotationApplied = Quaternion.Angle(actualRotation, expectedRotation) <= TransformRotationTolerance;

            if (!positionApplied)
            {
                failureReason = "projectile_spawn_position_not_applied";
                failureMessage = $"Projectile spawned instance position was not applied. expected='{FormatVector(expectedPosition)}' actual='{FormatVector(actualPosition)}'.";
                return false;
            }

            if (!rotationApplied)
            {
                failureReason = "projectile_spawn_rotation_not_applied";
                failureMessage = "Projectile spawned instance rotation was not applied.";
                return false;
            }

            bool presentationEndpointPresent = false;
            string presentationProfileId = "none";
            bool presentationVisualRootPresent = false;

            var capabilitySurface = runtimeSpawnedActor.CapabilitySurface;
            var presentationEndpoint = capabilitySurface?.PresentationEndpoint;
            if (presentationEndpoint != null)
            {
                presentationEndpointPresent = true;

                string resolvedProfileId = Normalize(presentationEndpoint.Profile?.ProfileId);
                if (!string.IsNullOrWhiteSpace(resolvedProfileId))
                {
                    presentationProfileId = resolvedProfileId;
                }

                if (presentationEndpoint.TryGetContainer(
                        ActorPresentationSlotKind.VisualRoot,
                        "visual.root",
                        out var visualRootContainer) &&
                    visualRootContainer != null &&
                    visualRootContainer.HasContainerTransform)
                {
                    presentationVisualRootPresent = true;
                }
            }

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(includeInactive: true);
            int rendererCount = renderers?.Length ?? 0;
            int enabledRendererCount = 0;
            int materialCount = 0;
            int validMaterialCount = 0;
            string rendererNames = string.Empty;
            string materialNames = string.Empty;

            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(rendererNames))
                    {
                        rendererNames += ",";
                    }

                    rendererNames += renderer.name;

                    if (renderer.enabled)
                    {
                        enabledRendererCount++;
                    }

                    Material[] sharedMaterials = renderer.sharedMaterials;
                    if (sharedMaterials == null)
                    {
                        continue;
                    }

                    materialCount += sharedMaterials.Length;
                    for (int j = 0; j < sharedMaterials.Length; j++)
                    {
                        var material = sharedMaterials[j];
                        if (material == null)
                        {
                            continue;
                        }

                        validMaterialCount++;
                        if (!string.IsNullOrEmpty(materialNames))
                        {
                            materialNames += ",";
                        }

                        materialNames += material.name;
                    }
                }
            }

            DebugUtility.LogVerbose(
                typeof(PooledActorProjectileSpawnAdapter),
                $"event='ActorProjectileSpawnVisualObserved' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' spawnedActorId='{runtimeSpawnedActor.ActorIdValue}' spawnedActorInstanceRuntimeId='{runtimeSpawnedActor.RuntimeActorInstanceId}' ownerActorId='{runtimeSpawnedActor.OwnerActorId}' ownerActorInstanceRuntimeId='{runtimeSpawnedActor.OwnerActorInstanceRuntimeId}' originPoolDefinition='{runtimeSpawnedActor.SpawnOrigin.PoolDefinitionName}' originCommandSequence='{runtimeSpawnedActor.SpawnOrigin.CommandSequence}' fireModeId='{command.FireModeId}' spawnProfileId='{command.SpawnProfileId}' poolDefinition='{poolDefinition.name}' instanceName='{instance.name}' activeSelf='{instance.activeSelf}' activeInHierarchy='{instance.activeInHierarchy}' rendererCount='{rendererCount}' enabledRendererCount='{enabledRendererCount}' materialCount='{materialCount}' validMaterialCount='{validMaterialCount}' presentationEndpointPresent='{presentationEndpointPresent}' presentationProfileId='{presentationProfileId}' presentationVisualRootPresent='{presentationVisualRootPresent}' visualContract='{VisualContractOptionalForRuntimeSpawn}' rendererNames='{Normalize(rendererNames)}' materialNames='{Normalize(materialNames)}' position='{FormatVector(actualPosition)}' rotation='{FormatQuaternion(actualRotation)}' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='projectile_spawn_visual_observed'.",
                DebugUtility.Colors.Info);

            DebugUtility.Log(
                typeof(PooledActorProjectileSpawnAdapter),
                    $"event='ActorProjectileSpawnInstancePrepared' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' spawnedActorId='{runtimeSpawnedActor.ActorIdValue}' spawnedActorInstanceRuntimeId='{runtimeSpawnedActor.RuntimeActorInstanceId}' ownerActorId='{runtimeSpawnedActor.OwnerActorId}' ownerActorInstanceRuntimeId='{runtimeSpawnedActor.OwnerActorInstanceRuntimeId}' originPoolDefinition='{runtimeSpawnedActor.SpawnOrigin.PoolDefinitionName}' originCommandSequence='{runtimeSpawnedActor.SpawnOrigin.CommandSequence}' fireModeId='{command.FireModeId}' spawnProfileId='{command.SpawnProfileId}' poolDefinition='{poolDefinition.name}' instanceName='{instance.name}' instancePath='{BuildInstancePath(instance.transform)}' activeSelf='{instance.activeSelf}' activeInHierarchy='{instance.activeInHierarchy}' positionApplied='{positionApplied}' rotationApplied='{rotationApplied}' rendererCount='{rendererCount}' enabledRendererCount='{enabledRendererCount}' materialCount='{materialCount}' validMaterialCount='{validMaterialCount}' presentationEndpointPresent='{presentationEndpointPresent}' presentationProfileId='{presentationProfileId}' presentationVisualRootPresent='{presentationVisualRootPresent}' visualContract='{VisualContractOptionalForRuntimeSpawn}' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='projectile_spawn_instance_prepared'.",
                    DebugUtility.Colors.Success);

            return true;
        }

        private void ReturnRentedInstanceIfNeeded(PoolDefinitionAsset poolDefinition, GameObject instance, string reason)
        {
            if (_poolService == null || poolDefinition == null || instance == null)
            {
                return;
            }

            try
            {
                _poolService.Return(poolDefinition, instance);
                DebugUtility.Log(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"event='ActorProjectileSpawnFailedInstanceReturnedToPool' adapterId='{AdapterId}' poolDefinition='{poolDefinition.name}' instanceName='{instance.name}' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='{Normalize(reason)}'.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"event='ActorProjectileSpawnFailedInstanceReturnToPoolFailed' adapterId='{AdapterId}' poolDefinition='{poolDefinition.name}' instanceName='{instance.name}' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='{Normalize(reason)}' message='{Normalize(ex.Message)}'.");
            }
        }

        private static ActorId BuildSpawnedActorId(ActorProjectileFireCommand command)
        {
            // Mantém a identidade semântica do projectile neutra.
            // O owner já é registrado separadamente em RuntimeSpawnOriginMetadata
            // e no ActorInstanceRuntimeId do spawned actor.
            int sequence = command.CommandEnvelope.Sequence < 0 ? 0 : command.CommandEnvelope.Sequence;
            return new ActorId($"actor.projectile.runtime.spawn.{sequence}");
        }

        private static string BuildInstancePath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            System.Collections.Generic.Stack<string> segments = new();
            var current = transform;
            while (current != null)
            {
                segments.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", segments);
        }

        private static string FormatVector(Vector3 value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###}";
        }

        private static string FormatQuaternion(Quaternion value)
        {
            return $"{value.x:0.###},{value.y:0.###},{value.z:0.###},{value.w:0.###}";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
