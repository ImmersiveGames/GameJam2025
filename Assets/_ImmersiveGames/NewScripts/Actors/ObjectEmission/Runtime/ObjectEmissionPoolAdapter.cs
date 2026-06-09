using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ObjectEmission.Authoring;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime
{
    public sealed class ObjectEmissionPoolAdapter
    {
        private IPoolService _poolService;

        public void AttachPoolService(IPoolService poolService)
        {
            if (poolService == null)
            {
                throw new ArgumentNullException(nameof(poolService), "ObjectEmissionPoolAdapter requires non-null IPoolService.");
            }

            if (ReferenceEquals(_poolService, poolService))
            {
                return;
            }

            _poolService = poolService;
        }

        public IObjectEmissionReturnSink CreateReturnSink(PoolDefinitionAsset poolDefinition, in ObjectEmissionRuntimePayload payload)
        {
            if (_poolService == null)
            {
                throw new InvalidOperationException("ObjectEmissionPoolAdapter requires IPoolService before creating a return sink.");
            }

            if (poolDefinition == null)
            {
                throw new ArgumentNullException(nameof(poolDefinition));
            }

            if (!payload.IsValid)
            {
                throw new InvalidOperationException("ObjectEmissionPoolAdapter requires a valid ObjectEmissionRuntimePayload before creating a return sink.");
            }

            return new ObjectEmissionPoolReturnSink(_poolService, poolDefinition, payload);
        }

        public void PrepareObjectEmissionPool(in ActorObjectEmissionCommand command)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ObjectEmissionPoolAdapter requires a valid ActorObjectEmissionCommand.");
            }

            ActorObjectEmissionProfile profile = command.Profile ?? throw new InvalidOperationException("ObjectEmissionPoolAdapter requires non-null ActorObjectEmissionProfile.");
            PoolDefinitionAsset poolDefinition = profile.PoolDefinition ?? throw new InvalidOperationException($"ObjectEmissionPoolAdapter requires pool definition for profileId='{profile.ProfileId}'.");

            DebugUtility.Log(
                typeof(ObjectEmissionPoolAdapter),
                $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolProfileResolved' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' poolLabel='{poolDefinition.PoolLabel}' emissionOriginPolicy='{profile.EmissionOriginPolicy}' objectSpeed='{profile.ObjectSpeed:0.###}' objectLifetime='{profile.ObjectLifetime:0.###}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            if (_poolService == null)
            {
                DebugUtility.Log(
                    typeof(ObjectEmissionPoolAdapter),
                    $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolRegistrationSkipped' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' reason='pool_service_unavailable' source='{command.Source}' commandSource='{command.SourceIdentity}' commandReason='{command.Reason}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            _poolService.EnsureRegistered(poolDefinition);
            DebugUtility.Log(
                typeof(ObjectEmissionPoolAdapter),
                $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolRegistered' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            if (!poolDefinition.Prewarm)
            {
                DebugUtility.Log(
                    typeof(ObjectEmissionPoolAdapter),
                    $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolPrewarmSkipped' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' reason='prewarm_disabled_by_pool_definition' source='{command.Source}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            DebugUtility.Log(
                typeof(ObjectEmissionPoolAdapter),
                $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolPrewarmRequested' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);
            _poolService.Prewarm(poolDefinition);
            DebugUtility.Log(
                typeof(ObjectEmissionPoolAdapter),
                $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolPrewarmCompleted' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);
        }

        public ObjectEmissionPooledObject RentAndInitialize(
            in ActorObjectEmissionCommand command,
            in ObjectEmissionRuntimePayload payload,
            IObjectEmissionReturnSink returnSink)
        {
            if (!command.IsValid)
            {
                throw new InvalidOperationException("ObjectEmissionPoolAdapter requires a valid ActorObjectEmissionCommand.");
            }

            if (!payload.IsValid)
            {
                throw new InvalidOperationException("ObjectEmissionPoolAdapter requires a valid ObjectEmissionRuntimePayload.");
            }

            if (returnSink == null)
            {
                throw new ArgumentNullException(nameof(returnSink), "ObjectEmissionPoolAdapter requires a non-null return sink.");
            }

            if (_poolService == null)
            {
                throw new InvalidOperationException("ObjectEmissionPoolAdapter requires IPoolService before renting.");
            }

            ActorObjectEmissionProfile profile = command.Profile ?? throw new InvalidOperationException("ObjectEmissionPoolAdapter requires non-null ActorObjectEmissionProfile.");
            PoolDefinitionAsset poolDefinition = profile.PoolDefinition ?? throw new InvalidOperationException($"ObjectEmissionPoolAdapter requires pool definition for profileId='{profile.ProfileId}'.");

            if (!string.Equals(payload.ProfileId, profile.ProfileId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"ObjectEmissionPoolAdapter payload profileId mismatch. expected='{profile.ProfileId}' actual='{payload.ProfileId}'.");
            }

            DebugUtility.Log(
                typeof(ObjectEmissionPoolAdapter),
                $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolRentRequested' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Info);

            GameObject instance = _poolService.Rent(poolDefinition, null);
            if (instance == null)
            {
                throw new InvalidOperationException($"ObjectEmissionPoolAdapter failed to rent instance for poolDefinition='{poolDefinition.name}'.");
            }

            ObjectEmissionPooledObject pooledObject = instance.GetComponent<ObjectEmissionPooledObject>();
            if (pooledObject == null)
            {
                _poolService.Return(poolDefinition, instance);
                throw new InvalidOperationException($"ObjectEmissionPoolAdapter requires ObjectEmissionPooledObject on prefab for poolDefinition='{poolDefinition.name}' instance='{instance.name}'.");
            }

            pooledObject.Initialize(payload, returnSink);

            DebugUtility.Log(
                typeof(ObjectEmissionPoolAdapter),
                $"[OBS][ObjectEmissionPool] event='ObjectEmissionPoolRentCompleted' actorId='{command.ActorId}' actorInstanceRuntimeId='{command.ActorInstanceRuntimeId}' profileId='{profile.ProfileId}' poolDefinition='{poolDefinition.name}' instanceName='{instance.name}' instancePath='{BuildInstancePath(instance.transform)}' activeSelf='{instance.activeSelf}' activeInHierarchy='{instance.activeInHierarchy}' parentName='{GetParentName(instance.transform)}' source='{command.Source}' reason='{command.Reason}'.",
                DebugUtility.Colors.Success);

            return pooledObject;
        }

        private static string BuildInstancePath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            Stack<string> segments = new();
            Transform current = transform;
            while (current != null)
            {
                segments.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", segments);
        }

        private static string GetParentName(Transform transform)
        {
            return transform != null && transform.parent != null ? transform.parent.name : "<none>";
        }
    }
}
