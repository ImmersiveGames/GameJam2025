using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    public sealed class ActorProjectileSpawnRuntimeState
    {
        private readonly List<TrackedSpawnedRuntimeObject> _trackedSpawns = new();
        private IPoolService _poolService;

        public int TrackedSpawnCount => _trackedSpawns.Count;
        public bool HasConfiguredPoolService => _poolService != null;

        public void ConfigurePoolService(
            IPoolService poolService,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string source,
            string reason)
        {
            _poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));

            DebugUtility.LogVerbose(
                typeof(ActorProjectileSpawnRuntimeState),
                $"event='ActorProjectileSpawnRuntimeStatePoolServiceConfigured' actorId='{actorId}' actorInstanceRuntimeId='{actorInstanceRuntimeId}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Info);
        }

        public void ApplySpawnedRuntimeObjectsStateProfile(ActorResetContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("ActorProjectileSpawnRuntimeState received invalid reset context.");
            }

            PruneTrackedSpawns("reset_prune_stale_entries");

            int trackedCountBefore = _trackedSpawns.Count;
            string profileSource = ResolveSpawnedRuntimeObjectsProfileSource(context.ResetIntent, context.StateProfileKind);

            if (trackedCountBefore == 0)
            {
                DebugUtility.Log(
                    typeof(ActorProjectileSpawnRuntimeState),
                    $"event='ActorProjectileSpawnedRuntimeObjectsStateProfileSkipped' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' resetIntent='{context.ResetIntent}' resetStateProfile='{context.StateProfileKind}' runtimeObjectsProfileKind='{context.StateProfileKind}' runtimeObjectsProfileSource='{profileSource}' trackedCountBefore='0' returnedCount='0' skippedCount='0' trackedCountAfter='0' reason='no_tracked_runtime_objects' source='{context.Source.TrimToEmpty()}' trigger='reset' detailSource='{nameof(ActorProjectileSpawnRuntimeState)}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            int returnedCount = 0;
            int skippedCount = 0;
            TrackedSpawnedRuntimeObject[] snapshot = _trackedSpawns.ToArray();
            for (int index = 0; index < snapshot.Length; index++)
            {
                if (TryReturnTrackedSpawnedRuntimeObject(snapshot[index], context.Source, context.Reason, "reset"))
                {
                    returnedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }

            ClearTrackedSpawns();

            DebugUtility.Log(
                typeof(ActorProjectileSpawnRuntimeState),
                $"event='ActorProjectileSpawnedRuntimeObjectsStateProfileApplied' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' resetIntent='{context.ResetIntent}' resetStateProfile='{context.StateProfileKind}' runtimeObjectsProfileKind='{context.StateProfileKind}' runtimeObjectsProfileSource='{profileSource}' trackedCountBefore='{trackedCountBefore}' returnedCount='{returnedCount}' skippedCount='{skippedCount}' trackedCountAfter='{_trackedSpawns.Count}' source='{context.Source.TrimToEmpty()}' trigger='reset' reason='{context.Reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Success);
        }

        public bool TryReleaseSpawnedRuntimeObjects(
            ActorCapabilityContributionContext context,
            out ActorCapabilityReleaseResult result)
        {
            if (!context.IsValid)
            {
                result = new ActorCapabilityReleaseResult(
                    false,
                    "invalid_release_context",
                    nameof(ActorProjectileSpawnRuntimeState),
                    "runtime_spawned_objects_release");
                return false;
            }

            PruneTrackedSpawns("release_prune_stale_entries");

            int trackedCountBefore = _trackedSpawns.Count;
            if (trackedCountBefore == 0)
            {
                DebugUtility.Log(
                    typeof(ActorProjectileSpawnRuntimeState),
                    $"event='ActorProjectileSpawnedRuntimeObjectsReleaseSkipped' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' trackedCountBefore='0' returnedCount='0' skippedCount='0' trackedCountAfter='0' source='{context.Source.TrimToEmpty()}' trigger='owner_release' reason='no_tracked_runtime_objects'.",
                    DebugUtility.Colors.Info);

                result = new ActorCapabilityReleaseResult(
                    true,
                    "no_tracked_runtime_objects",
                    context.Source,
                    context.Reason);
                return true;
            }

            int returnedCount = 0;
            int skippedCount = 0;
            TrackedSpawnedRuntimeObject[] snapshot = _trackedSpawns.ToArray();
            for (int index = 0; index < snapshot.Length; index++)
            {
                if (TryReturnTrackedSpawnedRuntimeObject(snapshot[index], context.Source, context.Reason, "owner_release"))
                {
                    returnedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }

            ClearTrackedSpawns();

            bool released = skippedCount == 0;
            string outcomeReason = released
                ? "runtime_spawned_objects_released"
                : "runtime_spawned_objects_release_incomplete";

            DebugUtility.Log(
                typeof(ActorProjectileSpawnRuntimeState),
                $"event='ActorProjectileSpawnedRuntimeObjectsReleased' actorId='{context.ActorId}' actorInstanceRuntimeId='{context.ActorInstanceRuntimeId}' trackedCountBefore='{trackedCountBefore}' returnedCount='{returnedCount}' skippedCount='{skippedCount}' trackedCountAfter='{_trackedSpawns.Count}' releaseMandatory='true' source='{context.Source.TrimToEmpty()}' trigger='owner_release' reason='{context.Reason.TrimToEmpty()}' outcomeReason='{outcomeReason}'.",
                released ? DebugUtility.Colors.Success : DebugUtility.Colors.Error);

            result = new ActorCapabilityReleaseResult(
                released,
                outcomeReason,
                context.Source,
                context.Reason);
            return released;
        }

        public bool TryTrackSpawnedRuntimeObject(
            Actor ownerActor,
            GameObject spawnedInstance,
            RuntimeSpawnedActor spawnedActor,
            string source,
            string reason)
        {
            if (ownerActor == null || !ownerActor.ActorIdValue.IsValid || !ownerActor.RuntimeActorInstanceId.IsValid)
            {
                LogTrackSkipped(
                    default,
                    default,
                    spawnedActor,
                    spawnedInstance,
                    source,
                    reason,
                    "owner_actor_missing");
                return false;
            }

            if (!TryBuildTrackedSpawnedRuntimeObject(
                ownerActor,
                spawnedInstance,
                spawnedActor,
                source,
                reason,
                out var trackedSpawnedRuntimeObject,
                out string skippedReason))
            {
                LogTrackSkipped(
                    ownerActor.ActorIdValue,
                    ownerActor.RuntimeActorInstanceId,
                    spawnedActor,
                    spawnedInstance,
                    source,
                    skippedReason,
                    "spawn_tracking_rejected");
                return false;
            }

            PruneTrackedSpawns("track_prune_stale_entries");
            RemoveTrackedSpawnedRuntimeObject(spawnedActor, "spawn_replaced", "replace_existing_spawn_registration", false);

            spawnedActor.PoolReturned += HandleSpawnedActorPoolReturned;
            spawnedActor.PoolDestroyed += HandleSpawnedActorPoolDestroyed;
            _trackedSpawns.Add(trackedSpawnedRuntimeObject);

            DebugUtility.LogVerbose(
                typeof(ActorProjectileSpawnRuntimeState),
                $"event='ActorProjectileSpawnTracked' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
                DebugUtility.Colors.Success);

            return true;
        }

        public bool TryReturnSpawnedRuntimeObject(
            RuntimeSpawnedActor spawnedActor,
            string source,
            string reason,
            string trigger,
            out string outcomeReason)
        {
            outcomeReason = string.Empty;
            PruneTrackedSpawns("single_return_prune_stale_entries");

            if (spawnedActor == null)
            {
                outcomeReason = "spawned_actor_missing";
                DebugUtility.LogWarning(
                    typeof(ActorProjectileSpawnRuntimeState),
                    $"event='ActorProjectileSpawnedRuntimeObjectReturnSkipped' actorId='' actorInstanceRuntimeId='' spawnedActorId='' spawnedActorInstanceRuntimeId='' originPoolDefinition='' spawnProfileId='' commandSequence='0' trackedCount='{_trackedSpawns.Count}' source='{source.TrimToEmpty()}' trigger='{trigger.TrimToEmpty()}' reason='{outcomeReason}'.");
                return false;
            }

            if (!TryGetTrackedSpawnedRuntimeObject(spawnedActor, out var trackedSpawnedRuntimeObject))
            {
                outcomeReason = "tracked_spawn_missing";
                DebugUtility.LogWarning(
                    typeof(ActorProjectileSpawnRuntimeState),
                    $"event='ActorProjectileSpawnedRuntimeObjectReturnSkipped' actorId='{spawnedActor.OwnerActorId}' actorInstanceRuntimeId='{spawnedActor.OwnerActorInstanceRuntimeId}' spawnedActorId='{spawnedActor.ActorIdValue}' spawnedActorInstanceRuntimeId='{spawnedActor.RuntimeActorInstanceId}' originPoolDefinition='{(spawnedActor.OriginPoolDefinition == null ? string.Empty : spawnedActor.OriginPoolDefinition.name)}' spawnProfileId='{spawnedActor.SpawnProfileId}' commandSequence='{(spawnedActor.HasSpawnOrigin ? spawnedActor.SpawnOrigin.CommandSequence : 0)}' trackedCount='{_trackedSpawns.Count}' source='{source.TrimToEmpty()}' trigger='{trigger.TrimToEmpty()}' reason='{outcomeReason}'.");
                return false;
            }

            bool returned = TryReturnTrackedSpawnedRuntimeObject(
                trackedSpawnedRuntimeObject,
                source,
                reason,
                trigger);

            outcomeReason = returned
                ? "runtime_spawned_object_returned"
                : "runtime_spawned_object_return_failed";
            return returned;
        }

        public void Clear()
        {
            ClearTrackedSpawns();
        }

        private void HandleSpawnedActorPoolReturned(RuntimeSpawnedActor spawnedActor)
        {
            if (spawnedActor == null)
            {
                return;
            }

            TryRemoveTrackedSpawnedRuntimeObject(spawnedActor, out _);
        }

        private void HandleSpawnedActorPoolDestroyed(RuntimeSpawnedActor spawnedActor)
        {
            if (spawnedActor == null)
            {
                return;
            }

            if (TryRemoveTrackedSpawnedRuntimeObject(spawnedActor, out var trackedSpawnedRuntimeObject))
            {
                DebugUtility.LogWarning(
                    typeof(ActorProjectileSpawnRuntimeState),
                    $"event='ActorProjectileSpawnedRuntimeObjectReturnSkipped' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{nameof(ActorProjectileSpawnRuntimeState)}' reason='pool_destroyed'.");
            }
        }

        private bool TryBuildTrackedSpawnedRuntimeObject(
            Actor ownerActor,
            GameObject spawnedInstance,
            RuntimeSpawnedActor spawnedActor,
            string source,
            string reason,
            out TrackedSpawnedRuntimeObject trackedSpawnedRuntimeObject,
            out string skippedReason)
        {
            trackedSpawnedRuntimeObject = null;
            skippedReason = string.Empty;

            if (ownerActor == null || !ownerActor.ActorIdValue.IsValid || !ownerActor.RuntimeActorInstanceId.IsValid)
            {
                skippedReason = "owner_actor_invalid";
                return false;
            }

            if (spawnedInstance == null)
            {
                skippedReason = "spawned_instance_missing";
                return false;
            }

            if (spawnedActor == null)
            {
                skippedReason = "spawned_actor_missing";
                return false;
            }

            if (spawnedActor.gameObject != spawnedInstance)
            {
                skippedReason = "spawned_instance_actor_mismatch";
                return false;
            }

            if (!spawnedActor.IsRuntimeMetadataBound || !spawnedActor.HasSpawnOrigin)
            {
                skippedReason = "spawned_actor_metadata_invalid";
                return false;
            }

            if (spawnedActor.OwnerActorId != ownerActor.ActorIdValue ||
                spawnedActor.OwnerActorInstanceRuntimeId != ownerActor.RuntimeActorInstanceId)
            {
                skippedReason = "spawned_actor_owner_mismatch";
                return false;
            }

            var originPoolDefinition = spawnedActor.OriginPoolDefinition;
            if (originPoolDefinition == null)
            {
                skippedReason = "spawned_actor_origin_pool_definition_missing";
                return false;
            }

            trackedSpawnedRuntimeObject = new TrackedSpawnedRuntimeObject(
                spawnedInstance,
                spawnedActor,
                spawnedActor.ActorIdValue,
                spawnedActor.RuntimeActorInstanceId,
                spawnedActor.OwnerActorId,
                spawnedActor.OwnerActorInstanceRuntimeId,
                spawnedActor.SpawnOrigin,
                source.TrimToEmpty(),
                reason.TrimToEmpty());

            return true;
        }

        private bool TryReturnTrackedSpawnedRuntimeObject(
            TrackedSpawnedRuntimeObject trackedSpawnedRuntimeObject,
            string source,
            string reason,
            string trigger)
        {
            if (trackedSpawnedRuntimeObject == null)
            {
                return false;
            }

            if (!IsTrackedSpawnedRuntimeObjectReturnable(trackedSpawnedRuntimeObject, out string staleReason))
            {
                DebugUtility.LogWarning(
                    typeof(ActorProjectileSpawnRuntimeState),
                    $"event='ActorProjectileSpawnedRuntimeObjectReturnSkipped' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{source.TrimToEmpty()}' trigger='{trigger.TrimToEmpty()}' reason='{staleReason.TrimToEmpty()}'.");
                RemoveTrackedSpawnedRuntimeObject(trackedSpawnedRuntimeObject.SpawnedActor, "stale_return_skip", staleReason, false);
                return false;
            }

            if (_poolService == null)
            {
                DebugUtility.LogWarning(
                    typeof(ActorProjectileSpawnRuntimeState),
                    $"event='ActorProjectileSpawnedRuntimeObjectReturnSkipped' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{source.TrimToEmpty()}' trigger='{trigger.TrimToEmpty()}' reason='pool_service_not_configured'.");
                RemoveTrackedSpawnedRuntimeObject(trackedSpawnedRuntimeObject.SpawnedActor, "pool_service_not_configured", reason, false);
                return false;
            }

            try
            {
                int trackedCountBefore = _trackedSpawns.Count;
                _poolService.Return(trackedSpawnedRuntimeObject.SpawnOrigin.PoolDefinition, trackedSpawnedRuntimeObject.SpawnedInstance);

                DebugUtility.Log(
                    typeof(ActorProjectileSpawnRuntimeState),
                    $"event='ActorProjectileSpawnedRuntimeObjectReturned' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCountBefore='{trackedCountBefore}' trackedCountAfter='{_trackedSpawns.Count}' source='{source.TrimToEmpty()}' trigger='{trigger.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.",
                    DebugUtility.Colors.Success);
                return true;
            }
            catch (Exception exception)
            {
                DebugUtility.LogWarning(
                    typeof(ActorProjectileSpawnRuntimeState),
                    $"event='ActorProjectileSpawnedRuntimeObjectReturnSkipped' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{source.TrimToEmpty()}' trigger='{trigger.TrimToEmpty()}' reason='pool_return_failed' message='{exception.Message.TrimToEmpty()}'.");
                RemoveTrackedSpawnedRuntimeObject(trackedSpawnedRuntimeObject.SpawnedActor, "pool_return_failed", exception.Message, false);
                return false;
            }
        }

        private bool IsTrackedSpawnedRuntimeObjectReturnable(
            TrackedSpawnedRuntimeObject trackedSpawnedRuntimeObject,
            out string reason)
        {
            reason = string.Empty;

            if (trackedSpawnedRuntimeObject == null)
            {
                reason = "tracked_entry_missing";
                return false;
            }

            if (trackedSpawnedRuntimeObject.SpawnedInstance == null || trackedSpawnedRuntimeObject.SpawnedActor == null)
            {
                reason = "tracked_runtime_object_missing";
                return false;
            }

            var spawnedActor = trackedSpawnedRuntimeObject.SpawnedActor;
            if (spawnedActor.gameObject != trackedSpawnedRuntimeObject.SpawnedInstance)
            {
                reason = "spawned_instance_actor_mismatch";
                return false;
            }

            if (!spawnedActor.IsRuntimeMetadataBound || !spawnedActor.HasSpawnOrigin)
            {
                reason = "runtime_metadata_invalid";
                return false;
            }

            if (spawnedActor.ActorIdValue != trackedSpawnedRuntimeObject.SpawnedActorId ||
                spawnedActor.RuntimeActorInstanceId != trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId)
            {
                reason = "spawned_actor_identity_mismatch";
                return false;
            }

            if (spawnedActor.OwnerActorId != trackedSpawnedRuntimeObject.OwnerActorId ||
                spawnedActor.OwnerActorInstanceRuntimeId != trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId)
            {
                reason = "owner_identity_mismatch";
                return false;
            }

            if (spawnedActor.OriginPoolDefinition == null || spawnedActor.OriginPoolDefinition != trackedSpawnedRuntimeObject.SpawnOrigin.PoolDefinition)
            {
                reason = "origin_pool_definition_mismatch";
                return false;
            }

            if (!spawnedActor.gameObject.activeInHierarchy)
            {
                reason = "spawned_object_inactive";
                return false;
            }

            return true;
        }

        private void ClearTrackedSpawns()
        {
            for (int index = 0; index < _trackedSpawns.Count; index++)
            {
                UnsubscribeTrackedSpawn(_trackedSpawns[index]);
            }

            _trackedSpawns.Clear();
        }

        private void PruneTrackedSpawns(string reason)
        {
            for (int index = _trackedSpawns.Count - 1; index >= 0; index--)
            {
                var trackedSpawnedRuntimeObject = _trackedSpawns[index];
                if (trackedSpawnedRuntimeObject is { IsStillValid: true })
                {
                    continue;
                }

                RemoveTrackedSpawnedRuntimeObject(trackedSpawnedRuntimeObject != null ? trackedSpawnedRuntimeObject.SpawnedActor : null, "prune_stale", reason);
            }
        }

        private bool TryGetTrackedSpawnedRuntimeObject(
            RuntimeSpawnedActor spawnedActor,
            out TrackedSpawnedRuntimeObject trackedSpawnedRuntimeObject)
        {
            trackedSpawnedRuntimeObject = null;

            if (spawnedActor == null)
            {
                return false;
            }

            for (int index = _trackedSpawns.Count - 1; index >= 0; index--)
            {
                var candidate = _trackedSpawns[index];
                if (candidate == null || !ReferenceEquals(candidate.SpawnedActor, spawnedActor))
                {
                    continue;
                }

                trackedSpawnedRuntimeObject = candidate;
                return true;
            }

            return false;
        }

        private bool TryRemoveTrackedSpawnedRuntimeObject(
            RuntimeSpawnedActor spawnedActor,
            out TrackedSpawnedRuntimeObject trackedSpawnedRuntimeObject)
        {
            trackedSpawnedRuntimeObject = null;

            if (spawnedActor == null)
            {
                return false;
            }

            for (int index = _trackedSpawns.Count - 1; index >= 0; index--)
            {
                var candidate = _trackedSpawns[index];
                if (candidate == null || !ReferenceEquals(candidate.SpawnedActor, spawnedActor))
                {
                    continue;
                }

                _trackedSpawns.RemoveAt(index);
                UnsubscribeTrackedSpawn(candidate);
                trackedSpawnedRuntimeObject = candidate;
                return true;
            }

            return false;
        }

        private void RemoveTrackedSpawnedRuntimeObject(
            RuntimeSpawnedActor spawnedActor,
            string source,
            string reason,
            bool logSkip = true)
        {
            if (TryRemoveTrackedSpawnedRuntimeObject(spawnedActor, out var trackedSpawnedRuntimeObject))
            {
                if (!logSkip)
                {
                    return;
                }

                DebugUtility.LogWarning(
                    typeof(ActorProjectileSpawnRuntimeState),
                    $"event='ActorProjectileSpawnedRuntimeObjectReturnSkipped' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}'.");
            }
        }

        private void UnsubscribeTrackedSpawn(TrackedSpawnedRuntimeObject trackedSpawnedRuntimeObject)
        {
            if (trackedSpawnedRuntimeObject == null || trackedSpawnedRuntimeObject.SpawnedActor == null)
            {
                return;
            }

            trackedSpawnedRuntimeObject.SpawnedActor.PoolReturned -= HandleSpawnedActorPoolReturned;
            trackedSpawnedRuntimeObject.SpawnedActor.PoolDestroyed -= HandleSpawnedActorPoolDestroyed;
        }

        private void LogTrackSkipped(
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            RuntimeSpawnedActor spawnedActor,
            GameObject spawnedInstance,
            string source,
            string reason,
            string outcomeReason)
        {
            string spawnedActorId = spawnedActor != null ? spawnedActor.ActorIdValue.ToString() : string.Empty;
            string spawnedActorInstanceRuntimeId = spawnedActor != null ? spawnedActor.RuntimeActorInstanceId.ToString() : string.Empty;
            string spawnedInstanceName = spawnedInstance != null ? spawnedInstance.name : string.Empty;

            DebugUtility.LogWarning(
                typeof(ActorProjectileSpawnRuntimeState),
                $"event='ActorProjectileSpawnTrackSkipped' actorId='{actorId}' actorInstanceRuntimeId='{actorInstanceRuntimeId}' spawnedActorId='{spawnedActorId}' spawnedActorInstanceRuntimeId='{spawnedActorInstanceRuntimeId}' spawnedInstanceName='{spawnedInstanceName}' source='{source.TrimToEmpty()}' reason='{reason.TrimToEmpty()}' outcomeReason='{outcomeReason.TrimToEmpty()}'.");
        }

        private static string ResolveSpawnedRuntimeObjectsProfileSource(
            ActivityResetIntent resetIntent,
            ActivityResetStateProfileKind stateProfileKind)
        {
            return resetIntent switch
            {
                ActivityResetIntent.EntryInitialize when stateProfileKind == ActivityResetStateProfileKind.InitialState =>
                    "entry_initialize_spawned_runtime_objects_return",
                ActivityResetIntent.RuntimeLocalReset when stateProfileKind == ActivityResetStateProfileKind.RuntimeLocalState =>
                    "runtime_local_spawned_runtime_objects_return",
                ActivityResetIntent.RuntimeActivityReset when stateProfileKind == ActivityResetStateProfileKind.RuntimeActivityState =>
                    "runtime_activity_spawned_runtime_objects_return",
                ActivityResetIntent.RuntimeActivityTransitionReset when stateProfileKind == ActivityResetStateProfileKind.RuntimeActivityTransitionState =>
                    "runtime_activity_transition_spawned_runtime_objects_return",
                ActivityResetIntent.RuntimeRouteTransitionReset when stateProfileKind == ActivityResetStateProfileKind.RuntimeRouteTransitionState =>
                    "runtime_route_transition_spawned_runtime_objects_return",
                _ => "unknown_spawned_runtime_objects_profile"
            };
        }
        private sealed class TrackedSpawnedRuntimeObject
        {
            public TrackedSpawnedRuntimeObject(
                GameObject spawnedInstance,
                RuntimeSpawnedActor spawnedActor,
                ActorId spawnedActorId,
                ActorInstanceRuntimeId spawnedActorInstanceRuntimeId,
                ActorId ownerActorId,
                ActorInstanceRuntimeId ownerActorInstanceRuntimeId,
                RuntimeSpawnOriginMetadata spawnOrigin,
                string source,
                string reason)
            {
                SpawnedInstance = spawnedInstance;
                SpawnedActor = spawnedActor;
                SpawnedActorId = spawnedActorId;
                SpawnedActorInstanceRuntimeId = spawnedActorInstanceRuntimeId;
                OwnerActorId = ownerActorId;
                OwnerActorInstanceRuntimeId = ownerActorInstanceRuntimeId;
                SpawnOrigin = spawnOrigin;
                Source = source;
                Reason = reason;
            }

            public GameObject SpawnedInstance { get; }
            public RuntimeSpawnedActor SpawnedActor { get; }
            public ActorId SpawnedActorId { get; }
            public ActorInstanceRuntimeId SpawnedActorInstanceRuntimeId { get; }
            public ActorId OwnerActorId { get; }
            public ActorInstanceRuntimeId OwnerActorInstanceRuntimeId { get; }
            public RuntimeSpawnOriginMetadata SpawnOrigin { get; }
            public string Source { get; }
            public string Reason { get; }
            public string OriginPoolDefinitionName => SpawnOrigin.PoolDefinitionName;
            public RuntimeSpawnProfileId SpawnProfileId => SpawnOrigin.SpawnProfileId;
            public int CommandSequence => SpawnOrigin.CommandSequence;
            public bool IsStillValid =>
                SpawnedInstance != null &&
                SpawnedActor != null &&
                SpawnedActor.IsRuntimeMetadataBound &&
                SpawnedActor.HasSpawnOrigin &&
                SpawnedActor.ActorIdValue == SpawnedActorId &&
                SpawnedActor.RuntimeActorInstanceId == SpawnedActorInstanceRuntimeId &&
                SpawnedActor.OwnerActorId == OwnerActorId &&
                SpawnedActor.OwnerActorInstanceRuntimeId == OwnerActorInstanceRuntimeId &&
                SpawnedActor.OriginPoolDefinition != null &&
                SpawnedActor.OriginPoolDefinition == SpawnOrigin.PoolDefinition;
        }
    }
}
