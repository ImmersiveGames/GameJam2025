using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Projectile.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Projectile.Runtime
{
    public sealed class PooledActorProjectileSpawnAdapter : IActorProjectileSpawnAdapter
    {
        private readonly string _adapterId;
        private readonly Transform _spawnParent;
        private IPoolService _poolService;

        public PooledActorProjectileSpawnAdapter(
            string adapterId,
            Transform spawnParent = null)
        {
            _adapterId = Normalize(adapterId);
            _spawnParent = spawnParent;
        }

        public string AdapterId => string.IsNullOrWhiteSpace(_adapterId)
            ? "actor.projectile.spawn.adapter.pooled"
            : _adapterId;

        public ActorProjectileSpawnAdapterResult Execute(ActorProjectileFireCommand command)
        {
            if (!command.IsValid)
            {
                DebugUtility.Log(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnAdapterRejected' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' fireModeId='{command.FireModeId}' spawnExecuted='False' poolCalled='False' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='invalid_projectile_fire_command'.",
                    DebugUtility.Colors.Info);

                return ActorProjectileSpawnAdapterResult.Failed(
                    command,
                    "invalid_projectile_fire_command",
                    "Projectile spawn adapter received an invalid command.");
            }

            PoolDefinitionAsset poolDefinition = command.PoolDefinition;
            if (poolDefinition == null)
            {
                DebugUtility.Log(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnAdapterRejected' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' fireModeId='{command.FireModeId}' spawnExecuted='False' poolCalled='False' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='projectile_pool_definition_missing'.",
                    DebugUtility.Colors.Info);

                return ActorProjectileSpawnAdapterResult.Failed(
                    command,
                    "projectile_pool_definition_missing",
                    "Projectile spawn requires a PoolDefinitionAsset resolved from the fire profile.");
            }

            if (!TryResolvePoolService(nameof(Execute)))
            {
                DebugUtility.Log(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnAdapterRejected' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' fireModeId='{command.FireModeId}' poolDefinition='{poolDefinition.name}' spawnExecuted='False' poolCalled='False' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='pool_service_unavailable'.",
                    DebugUtility.Colors.Info);

                return ActorProjectileSpawnAdapterResult.Failed(
                    command,
                    "pool_service_unavailable",
                    "Canonical IPoolService is unavailable for projectile spawn.");
            }

            try
            {
                DebugUtility.Log(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnPoolRentRequested' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' fireModeId='{command.FireModeId}' poolDefinition='{poolDefinition.name}' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='projectile_spawn_pool_rent_requested'.",
                    DebugUtility.Colors.Info);

                _poolService.EnsureRegistered(poolDefinition);
                if (poolDefinition.Prewarm)
                {
                    _poolService.Prewarm(poolDefinition);
                }

                GameObject instance = _poolService.Rent(poolDefinition, _spawnParent);
                if (instance == null)
                {
                    throw new InvalidOperationException($"PoolService returned null instance for poolDefinition='{poolDefinition.name}'.");
                }

                instance.transform.SetPositionAndRotation(command.Origin, Quaternion.LookRotation(command.Direction.normalized, Vector3.up));

                RuntimeSpawnedActor runtimeSpawnedActor = instance.GetComponent<RuntimeSpawnedActor>();
                if (runtimeSpawnedActor == null)
                {
                    _poolService.Return(poolDefinition, instance);
                    throw new InvalidOperationException($"Projectile spawn prefab requires RuntimeSpawnedActor. poolDefinition='{poolDefinition.name}' instance='{instance.name}'.");
                }

                ActorId spawnedActorId = BuildSpawnedActorId(command);
                ActorInstanceRuntimeId spawnedRuntimeId = ActorInstanceRuntimeId.FromRuntimeSpawnedActorIdentity(
                    command.ActorInstanceRuntimeId,
                    spawnedActorId.Value,
                    command.CommandEnvelope.Sequence);

                runtimeSpawnedActor.BindRuntimeMetadata(
                    spawnedActorId,
                    spawnedRuntimeId,
                    ActorRole.RuntimeSpawnedActor,
                    ActorScope.ActivityScoped,
                    ActorParticipationRecord.ActorParticipationPolicy.None,
                    nameof(PooledActorProjectileSpawnAdapter));

                DebugUtility.Log(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnedFromPool' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' spawnedActorId='{runtimeSpawnedActor.ActorIdValue}' spawnedActorInstanceRuntimeId='{runtimeSpawnedActor.RuntimeActorInstanceId}' fireModeId='{command.FireModeId}' poolDefinition='{poolDefinition.name}' instanceName='{instance.name}' instancePath='{BuildInstancePath(instance.transform)}' spawnExecuted='True' poolCalled='True' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='projectile_spawned_from_pool'.",
                    DebugUtility.Colors.Success);

                return ActorProjectileSpawnAdapterResult.Spawned(
                    command,
                    instance,
                    runtimeSpawnedActor,
                    "projectile_spawned_from_pool",
                    "Projectile actor instance was rented from the canonical pool.");
            }
            catch (Exception ex)
            {
                DebugUtility.LogError(
                    typeof(PooledActorProjectileSpawnAdapter),
                    $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnAdapterFailed' adapterId='{AdapterId}' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' fireModeId='{command.FireModeId}' poolDefinition='{poolDefinition.name}' spawnExecuted='False' poolCalled='True' source='{nameof(PooledActorProjectileSpawnAdapter)}' reason='projectile_spawn_pool_failed' message='{Normalize(ex.Message)}'.");

                return ActorProjectileSpawnAdapterResult.Failed(
                    command,
                    "projectile_spawn_pool_failed",
                    ex.Message);
            }
        }

        private bool TryResolvePoolService(string source)
        {
            if (_poolService != null)
            {
                return true;
            }

            if (DependencyManager.Provider == null || !DependencyManager.Provider.TryGetGlobal<IPoolService>(out var resolved) || resolved == null)
            {
                return false;
            }

            _poolService = resolved;
            DebugUtility.LogVerbose(
                typeof(PooledActorProjectileSpawnAdapter),
                $"[OBS][ActorProjectileFire] event='ActorProjectileSpawnPoolServiceResolved' adapterId='{AdapterId}' source='{Normalize(source)}' reason='canonical_pool_service_resolved'.",
                DebugUtility.Colors.Info);
            return true;
        }

        private static ActorId BuildSpawnedActorId(ActorProjectileFireCommand command)
        {
            string ownerActor = command.ActorId.IsValid ? command.ActorId.Value : "actor";
            string fireMode = command.FireModeId.IsValid ? command.FireModeId.Value : "fire";
            int sequence = command.CommandEnvelope.Sequence < 0 ? 0 : command.CommandEnvelope.Sequence;
            return new ActorId($"actor.projectile.{ownerActor}.{fireMode}.{sequence}");
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
