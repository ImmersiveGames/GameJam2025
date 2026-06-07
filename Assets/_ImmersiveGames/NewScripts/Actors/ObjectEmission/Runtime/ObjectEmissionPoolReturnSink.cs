using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;

namespace _ImmersiveGames.NewScripts.Actors.ObjectEmission.Runtime
{
    /// <summary>
    /// Explicit return bridge from the runtime object back to the canonical pool service.
    /// </summary>
    public sealed class ObjectEmissionPoolReturnSink : IObjectEmissionReturnSink
    {
        private readonly IPoolService _poolService;
        private readonly PoolDefinitionAsset _poolDefinition;
        private readonly ObjectEmissionRuntimePayload _correlation;

        public ObjectEmissionPoolReturnSink(IPoolService poolService, PoolDefinitionAsset poolDefinition, in ObjectEmissionRuntimePayload correlation)
        {
            _poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));
            _poolDefinition = poolDefinition ?? throw new ArgumentNullException(nameof(poolDefinition));
            if (!correlation.IsValid)
            {
                throw new ArgumentException("ObjectEmissionPoolReturnSink requires a valid correlation payload.", nameof(correlation));
            }

            _correlation = correlation;
        }

        public void RequestReturn(ObjectEmissionPooledObject pooledObject, in ObjectEmissionRuntimePayload payload, string reason)
        {
            if (pooledObject == null)
            {
                throw new ArgumentNullException(nameof(pooledObject));
            }

            string normalizedReason = Normalize(reason);
            _poolService.Return(_poolDefinition, pooledObject.gameObject);

            DebugUtility.Log(
                typeof(ObjectEmissionPoolReturnSink),
                $"[OBS][ObjectEmission] event='ObjectEmissionReturnedToPool' instanceName='{pooledObject.name}' instancePath='{BuildInstancePath(pooledObject.transform)}' poolDefinition='{_poolDefinition.name}' reason='{normalizedReason}' actorId='{_correlation.ActorId}' actorInstanceRuntimeId='{_correlation.ActorInstanceRuntimeId}' profileId='{_correlation.ProfileId}' source='{_correlation.Source}' commandSource='{payload.Source}' commandReason='{payload.Reason}'.",
                DebugUtility.Colors.Success);
        }

        private static string BuildInstancePath(UnityEngine.Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            System.Collections.Generic.Stack<string> segments = new();
            UnityEngine.Transform current = transform;
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
