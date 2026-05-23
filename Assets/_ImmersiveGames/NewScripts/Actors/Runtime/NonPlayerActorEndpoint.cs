using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    [DisallowMultipleComponent]
    public sealed class NonPlayerActorEndpoint : MonoBehaviour
    {
        [SerializeField] private string nonPlayerActorId;
        [SerializeField] private string actorKind = "NonPlayerActor";
        [SerializeField] private NonPlayerActorScope actorScope = NonPlayerActorScope.ActivityScoped;
        [SerializeField] private NonPlayerActorParticipationPolicy participationPolicy = NonPlayerActorParticipationPolicy.ExplicitActivityIds;
        [SerializeField] private List<string> activityIds = new();
        [SerializeField] private ActorPresentationProfileAsset presentationProfile;
        [SerializeField] private ActorPresentationEndpoint presentationEndpoint;

        public string NonPlayerActorId => Normalize(nonPlayerActorId);
        public string ActorKind => Normalize(actorKind);
        public NonPlayerActorScope ActorScope => actorScope;
        public NonPlayerActorParticipationPolicy ParticipationPolicy => participationPolicy;
        public IReadOnlyList<string> ActivityIds => (IReadOnlyList<string>)activityIds ?? Array.Empty<string>();
        public ActorPresentationProfileAsset PresentationProfile => presentationProfile;
        public ActorPresentationEndpoint PresentationEndpoint => presentationEndpoint;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(NonPlayerActorId) &&
            !string.IsNullOrWhiteSpace(ActorKind) &&
            actorScope != NonPlayerActorScope.Unknown &&
            actorScope != NonPlayerActorScope.GlobalScopedUnsupported &&
            participationPolicy != NonPlayerActorParticipationPolicy.Unknown &&
            IsActivityIdsConfigValid() &&
            presentationProfile != null &&
            presentationEndpoint != null;

        public void ValidateOrThrow(string source)
        {
            string origin = string.IsNullOrWhiteSpace(source)
                ? $"{nameof(NonPlayerActorEndpoint)}:{name}"
                : source.Trim();

            if (string.IsNullOrWhiteSpace(NonPlayerActorId))
            {
                throw new InvalidOperationException($"{origin} requires nonPlayerActorId.");
            }

            if (string.IsNullOrWhiteSpace(ActorKind))
            {
                throw new InvalidOperationException($"{origin} requires actorKind.");
            }

            if (actorScope == NonPlayerActorScope.Unknown)
            {
                throw new InvalidOperationException($"{origin} requires explicit actorScope.");
            }

            if (actorScope == NonPlayerActorScope.GlobalScopedUnsupported)
            {
                throw new InvalidOperationException($"{origin} does not support actorScope=GlobalScopedUnsupported in Base 1.2.");
            }

            if (participationPolicy == NonPlayerActorParticipationPolicy.Unknown)
            {
                throw new InvalidOperationException($"{origin} requires explicit participationPolicy.");
            }

            if (!IsActivityIdsConfigValid())
            {
                throw new InvalidOperationException($"{origin} requires at least one activityId when participationPolicy=ExplicitActivityIds.");
            }

            if (presentationProfile == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorPresentationProfileAsset.");
            }

            if (presentationEndpoint == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorPresentationEndpoint.");
            }
        }

        private void OnValidate()
        {
            nonPlayerActorId = Normalize(nonPlayerActorId);
            actorKind = Normalize(actorKind);
            if (activityIds == null)
            {
                activityIds = new List<string>();
                return;
            }

            for (int index = 0; index < activityIds.Count; index++)
            {
                activityIds[index] = Normalize(activityIds[index]);
            }
        }

        public bool ContainsActivityId(string activityId)
        {
            string normalized = Normalize(activityId);
            if (string.IsNullOrWhiteSpace(normalized) || activityIds == null || activityIds.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < activityIds.Count; index++)
            {
                if (string.Equals(Normalize(activityIds[index]), normalized, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsActivityIdsConfigValid()
        {
            if (participationPolicy != NonPlayerActorParticipationPolicy.ExplicitActivityIds)
            {
                return true;
            }

            if (activityIds == null || activityIds.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < activityIds.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(Normalize(activityIds[index])))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
