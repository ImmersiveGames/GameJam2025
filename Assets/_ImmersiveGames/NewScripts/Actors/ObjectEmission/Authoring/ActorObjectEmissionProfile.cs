using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Config;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ObjectEmission.Authoring
{
    public enum ActorObjectEmissionOriginPolicy
    {
        Unknown = 0,
        EmitterTransform = 1,
        MuzzleTransform = 2,
        CustomOffset = 3,
    }

    [CreateAssetMenu(
        fileName = "ActorObjectEmissionProfile",
        menuName = "ImmersiveGames/Actors/ObjectEmission/Object Emission Profile",
        order = 21)]
    public sealed class ActorObjectEmissionProfile : ScriptableObject
    {
        [SerializeField] private string profileId = "object.emission.default";
        [SerializeField] private PoolDefinitionAsset poolDefinition;
        [SerializeField] private ActorObjectEmissionOriginPolicy emissionOriginPolicy = ActorObjectEmissionOriginPolicy.EmitterTransform;
        [SerializeField, Min(0.01f)] private float objectSpeed = 12f;
        [SerializeField, Min(0.01f)] private float objectLifetime = 2f;
        [SerializeField] private LayerMask objectLayerMask = ~0;

        public string ProfileId => Normalize(profileId);
        public PoolDefinitionAsset PoolDefinition => poolDefinition;
        public ActorObjectEmissionOriginPolicy EmissionOriginPolicy => emissionOriginPolicy;
        public float ObjectSpeed => objectSpeed;
        public float ObjectLifetime => objectLifetime;
        public LayerMask ObjectLayerMask => objectLayerMask;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ProfileId) &&
            poolDefinition != null &&
            emissionOriginPolicy != ActorObjectEmissionOriginPolicy.Unknown &&
            objectSpeed > 0f &&
            objectLifetime > 0f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (IsValid)
            {
                return;
            }

            FailFast("ActorObjectEmissionProfile invalid: profileId, poolDefinition, emissionOriginPolicy, objectSpeed and objectLifetime must be valid.");
        }
#endif

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private void FailFast(string message)
        {
            DebugUtility.LogError(typeof(ActorObjectEmissionProfile), $"[FATAL][ObjectEmission] {message} asset='{name}'.");
            throw new InvalidOperationException(message);
        }
    }
}
