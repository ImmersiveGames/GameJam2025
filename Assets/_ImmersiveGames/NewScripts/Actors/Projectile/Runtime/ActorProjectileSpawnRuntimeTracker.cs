using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Contracts;
using _ImmersiveGames.NewScripts.Actors.Capabilities.Reset;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ActorProjectileSpawnRuntimeTracker : MonoBehaviour, IActorEntryInitializeResetEndpoint, IActorRuntimeLocalResetEndpoint, IActorRuntimeActivityResetEndpoint, IActorRuntimeActivityTransitionResetEndpoint, IActorRuntimeRouteTransitionResetEndpoint, IActorResetContributionProvider
    {
        [Header("Reset")]
        [SerializeField] private ActivityResetBoundaryEligibility resetBoundaryEligibility = ActivityResetBoundaryEligibility.All;

        private readonly List<TrackedSpawnedRuntimeObject> _trackedSpawns = new();
        private Actor _ownerActor;
        private IPoolService _poolService;

        public int TrackedSpawnCount => _trackedSpawns.Count;

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
        private void ApplySpawnedRuntimeObjectsStateProfile(ActorResetContext context)
        {
            if (!context.IsValid)
            {
                throw new InvalidOperationException("ActorProjectileSpawnRuntimeTracker received invalid reset context.");
            }

            RefreshOwnerActor();
            PruneTrackedSpawns("reset_prune_stale_entries");

            int trackedCountBefore = _trackedSpawns.Count;
            string profileSource = ResolveSpawnedRuntimeObjectsProfileSource(context.ResetIntent, context.StateProfileKind);

            if (trackedCountBefore == 0)
            {
                DebugUtility.Log(
                    typeof(ActorProjectileSpawnRuntimeTracker),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnedRuntimeObjectsStateProfileSkipped' actorId='{OwnerActorId}' actorInstanceRuntimeId='{OwnerActorInstanceRuntimeId}' resetIntent='{context.ResetIntent}' resetStateProfile='{context.StateProfileKind}' runtimeObjectsProfileKind='{context.StateProfileKind}' runtimeObjectsProfileSource='{profileSource}' trackedCountBefore='0' returnedCount='0' skippedCount='0' trackedCountAfter='0' reason='no_tracked_runtime_objects' source='{Normalize(context.Source)}' trigger='reset' detailSource='{nameof(ActorProjectileSpawnRuntimeTracker)}'.",
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
                typeof(ActorProjectileSpawnRuntimeTracker),
                $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnedRuntimeObjectsStateProfileApplied' actorId='{OwnerActorId}' actorInstanceRuntimeId='{OwnerActorInstanceRuntimeId}' resetIntent='{context.ResetIntent}' resetStateProfile='{context.StateProfileKind}' runtimeObjectsProfileKind='{context.StateProfileKind}' runtimeObjectsProfileSource='{profileSource}' trackedCountBefore='{trackedCountBefore}' returnedCount='{returnedCount}' skippedCount='{skippedCount}' trackedCountAfter='{_trackedSpawns.Count}' source='{Normalize(context.Source)}' trigger='reset' reason='{Normalize(context.Reason)}'.",
                DebugUtility.Colors.Success);
        }


        public void ApplyEntryInitializeReset(ActorResetContext context)
        {
            ApplySpawnedRuntimeObjectsStateProfile(context);
        }

        public void ApplyRuntimeLocalReset(ActorResetContext context)
        {
            ApplySpawnedRuntimeObjectsStateProfile(context);
        }

        public void ApplyRuntimeActivityReset(ActorResetContext context)
        {
            ApplySpawnedRuntimeObjectsStateProfile(context);
        }

        public void ApplyRuntimeActivityTransitionReset(ActorResetContext context)
        {
            ApplySpawnedRuntimeObjectsStateProfile(context);
        }

        public void ApplyRuntimeRouteTransitionReset(ActorResetContext context)
        {
            ApplySpawnedRuntimeObjectsStateProfile(context);
        }

        public bool TryTrackSpawnedRuntimeObject(
            GameObject spawnedInstance,
            RuntimeSpawnedActor spawnedActor,
            string source,
            string reason)
        {
            RefreshOwnerActor();

            if (!TryResolveOwnerActor(out Actor ownerActor))
            {
                LogTrackSkipped(
                    ActorIdValue,
                    RuntimeActorInstanceId,
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
                    out TrackedSpawnedRuntimeObject trackedSpawnedRuntimeObject,
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
            RemoveTrackedSpawnedRuntimeObject(spawnedActor, "spawn_replaced", "replace_existing_spawn_registration", logSkip: false);

            spawnedActor.PoolReturned += HandleSpawnedActorPoolReturned;
            spawnedActor.PoolDestroyed += HandleSpawnedActorPoolDestroyed;
            _trackedSpawns.Add(trackedSpawnedRuntimeObject);

            DebugUtility.Log(
                typeof(ActorProjectileSpawnRuntimeTracker),
                $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnTracked' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Success);

            return true;
        }

        private void OnDestroy()
        {
            ClearTrackedSpawns();
        }

        public ActorId ActorIdValue => OwnerActorId;
        public ActorInstanceRuntimeId RuntimeActorInstanceId => OwnerActorInstanceRuntimeId;

        private ActorId OwnerActorId => _ownerActor != null ? _ownerActor.ActorIdValue : default;
        private ActorInstanceRuntimeId OwnerActorInstanceRuntimeId => _ownerActor != null ? _ownerActor.RuntimeActorInstanceId : default;

        private void HandleSpawnedActorPoolReturned(RuntimeSpawnedActor spawnedActor)
        {
            if (spawnedActor == null)
            {
                return;
            }

            if (TryRemoveTrackedSpawnedRuntimeObject(spawnedActor, out TrackedSpawnedRuntimeObject trackedSpawnedRuntimeObject))
            {
                DebugUtility.Log(
                    typeof(ActorProjectileSpawnRuntimeTracker),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnedRuntimeObjectReturnedToPool' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{nameof(ActorProjectileSpawnRuntimeTracker)}' reason='pool_return_callback'.",
                    DebugUtility.Colors.Info);
            }
        }

        private void HandleSpawnedActorPoolDestroyed(RuntimeSpawnedActor spawnedActor)
        {
            if (spawnedActor == null)
            {
                return;
            }

            if (TryRemoveTrackedSpawnedRuntimeObject(spawnedActor, out TrackedSpawnedRuntimeObject trackedSpawnedRuntimeObject))
            {
                DebugUtility.LogWarning(
                    typeof(ActorProjectileSpawnRuntimeTracker),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnedRuntimeObjectReturnSkipped' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{nameof(ActorProjectileSpawnRuntimeTracker)}' reason='pool_destroyed'.");
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

            PoolDefinitionAsset originPoolDefinition = spawnedActor.OriginPoolDefinition;
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
                Normalize(source),
                Normalize(reason));

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
                    typeof(ActorProjectileSpawnRuntimeTracker),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnedRuntimeObjectReturnSkipped' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{Normalize(source)}' trigger='{Normalize(trigger)}' reason='{Normalize(staleReason)}'.");
                RemoveTrackedSpawnedRuntimeObject(trackedSpawnedRuntimeObject.SpawnedActor, "stale_return_skip", staleReason, logSkip: false);
                return false;
            }

            if (!TryResolvePoolService(out IPoolService poolService))
            {
                DebugUtility.LogWarning(
                    typeof(ActorProjectileSpawnRuntimeTracker),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnedRuntimeObjectReturnSkipped' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{Normalize(source)}' trigger='{Normalize(trigger)}' reason='pool_service_unavailable'.");
                RemoveTrackedSpawnedRuntimeObject(trackedSpawnedRuntimeObject.SpawnedActor, "pool_service_unavailable", reason, logSkip: false);
                return false;
            }

            DebugUtility.Log(
                typeof(ActorProjectileSpawnRuntimeTracker),
                $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnedRuntimeObjectReturnRequested' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{Normalize(source)}' trigger='{Normalize(trigger)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            try
            {
                poolService.Return(trackedSpawnedRuntimeObject.SpawnOrigin.PoolDefinition, trackedSpawnedRuntimeObject.SpawnedInstance);
                return true;
            }
            catch (Exception exception)
            {
                DebugUtility.LogWarning(
                    typeof(ActorProjectileSpawnRuntimeTracker),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnedRuntimeObjectReturnSkipped' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{Normalize(source)}' trigger='{Normalize(trigger)}' reason='pool_return_failed' message='{Normalize(exception.Message)}'.");
                RemoveTrackedSpawnedRuntimeObject(trackedSpawnedRuntimeObject.SpawnedActor, "pool_return_failed", exception.Message, logSkip: false);
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

            RuntimeSpawnedActor spawnedActor = trackedSpawnedRuntimeObject.SpawnedActor;
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
                TrackedSpawnedRuntimeObject trackedSpawnedRuntimeObject = _trackedSpawns[index];
                if (trackedSpawnedRuntimeObject != null && trackedSpawnedRuntimeObject.IsStillValid)
                {
                    continue;
                }

                RemoveTrackedSpawnedRuntimeObject(trackedSpawnedRuntimeObject != null ? trackedSpawnedRuntimeObject.SpawnedActor : null, "prune_stale", reason);
            }
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
                TrackedSpawnedRuntimeObject candidate = _trackedSpawns[index];
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
            if (TryRemoveTrackedSpawnedRuntimeObject(spawnedActor, out TrackedSpawnedRuntimeObject trackedSpawnedRuntimeObject))
            {
                if (!logSkip)
                {
                    return;
                }

                DebugUtility.LogWarning(
                    typeof(ActorProjectileSpawnRuntimeTracker),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnedRuntimeObjectReturnSkipped' actorId='{trackedSpawnedRuntimeObject.OwnerActorId}' actorInstanceRuntimeId='{trackedSpawnedRuntimeObject.OwnerActorInstanceRuntimeId}' spawnedActorId='{trackedSpawnedRuntimeObject.SpawnedActorId}' spawnedActorInstanceRuntimeId='{trackedSpawnedRuntimeObject.SpawnedActorInstanceRuntimeId}' originPoolDefinition='{trackedSpawnedRuntimeObject.OriginPoolDefinitionName}' spawnProfileId='{trackedSpawnedRuntimeObject.SpawnProfileId}' commandSequence='{trackedSpawnedRuntimeObject.CommandSequence}' trackedCount='{_trackedSpawns.Count}' source='{Normalize(source)}' reason='{Normalize(reason)}'.");
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

        private bool TryResolveOwnerActor(out Actor ownerActor)
        {
            ownerActor = ResolveOwnerActor();
            return ownerActor != null &&
                ownerActor.ActorIdValue.IsValid &&
                ownerActor.RuntimeActorInstanceId.IsValid;
        }

        private void RefreshOwnerActor()
        {
            _ownerActor = GetComponentInParent<Actor>(includeInactive: true);
        }

        private Actor ResolveOwnerActor()
        {
            if (_ownerActor != null)
            {
                return _ownerActor;
            }

            _ownerActor = GetComponentInParent<Actor>(includeInactive: true);
            return _ownerActor;
        }

        private bool TryResolvePoolService(out IPoolService poolService)
        {
            if (_poolService != null)
            {
                poolService = _poolService;
                return true;
            }

            if (DependencyManager.Provider != null &&
                DependencyManager.Provider.TryGetGlobal<IPoolService>(out IPoolService resolvedPoolService) &&
                resolvedPoolService != null)
            {
                _poolService = resolvedPoolService;
                poolService = resolvedPoolService;
                return true;
            }

            poolService = null;
            return false;
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
                typeof(ActorProjectileSpawnRuntimeTracker),
                $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnTrackSkipped' actorId='{actorId}' actorInstanceRuntimeId='{actorInstanceRuntimeId}' spawnedActorId='{spawnedActorId}' spawnedActorInstanceRuntimeId='{spawnedActorInstanceRuntimeId}' spawnedInstanceName='{spawnedInstanceName}' source='{Normalize(source)}' reason='{Normalize(reason)}' outcomeReason='{Normalize(outcomeReason)}'.");
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
                _ => "unknown_spawned_runtime_objects_profile",
            };
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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
                    nameof(ActorProjectileSpawnRuntimeTracker),
                    "projectile_spawn_runtime_objects_reset_contribution");
                ResetBoundaryEligibility = resetBoundaryEligibility;
            }

            public ActorCapabilityContributionDescriptor Descriptor { get; }
            public ActivityResetBoundaryEligibility ResetBoundaryEligibility { get; }
            public bool IsValid => Descriptor.IsValid;
        }
    }
}
