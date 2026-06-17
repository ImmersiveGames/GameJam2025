using _ImmersiveGames.NewScripts.Actors.Attributes.Runtime;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Attributes.UI
{
    public sealed class ActorAttributeEndpointUiStateReader : IActorAttributeUiStateReader
    {
        private const float NormalizedEpsilon = 0.0001f;
        private readonly ActorAttributeEndpoint _endpoint;

        public ActorAttributeEndpointUiStateReader(ActorAttributeEndpoint endpoint)
        {
            _endpoint = endpoint;
        }

        public bool TryRead(
            ActorAttributeUiBindingTarget target,
            out ActorAttributeUiValue value,
            out string failureReason)
        {
            value = default;
            failureReason = string.Empty;

            if (!target.IsValid)
            {
                failureReason = "invalid_target";
                return false;
            }

            if (_endpoint == null)
            {
                failureReason = "endpoint_missing";
                return false;
            }

            if (!_endpoint.IsInitialized)
            {
                failureReason = "endpoint_not_initialized";
                return false;
            }

            if (_endpoint.CurrentActorInstanceRuntimeId != target.ActorInstanceRuntimeId)
            {
                failureReason = "actor_instance_mismatch";
                return false;
            }

            if (!_endpoint.TryGetState(target.AttributeId, out ActorAttributeState state) || state == null)
            {
                failureReason = "attribute_state_missing";
                return false;
            }

            if (!IsFinite(state.CurrentValue) || !IsFinite(state.MinValue) || !IsFinite(state.MaxValue))
            {
                failureReason = "attribute_state_invalid_values";
                return false;
            }

            if (!TryNormalize(state.CurrentValue, state.MinValue, state.MaxValue, out float normalizedValue))
            {
                failureReason = "attribute_state_invalid_range";
                return false;
            }

            value = new ActorAttributeUiValue(
                target.ActorId,
                target.ActorInstanceRuntimeId,
                target.AttributeId,
                state.CurrentValue,
                state.MinValue,
                state.MaxValue,
                true,
                normalizedValue,
                string.Empty,
                string.Empty);
            return true;
        }

        private static bool TryNormalize(float currentValue, float minValue, float maxValue, out float normalizedValue)
        {
            normalizedValue = 0f;
            float denominator = maxValue - minValue;
            if (!IsFinite(denominator) || denominator <= NormalizedEpsilon)
            {
                return false;
            }

            normalizedValue = Mathf.Clamp01((currentValue - minValue) / denominator);
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
