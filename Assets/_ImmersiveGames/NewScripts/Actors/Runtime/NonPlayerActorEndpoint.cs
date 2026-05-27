using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Presentation.Authoring;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    [DisallowMultipleComponent]
    public sealed class NonPlayerActorEndpoint : Actor
    {
        [SerializeField] private string nonPlayerActorId;
        [SerializeField] private string actorKind = "NonPlayerActor";
        [SerializeField] private NonPlayerActorScope actorScope = NonPlayerActorScope.ActivityScoped;
        [SerializeField] private NonPlayerActorParticipationPolicy participationPolicy = NonPlayerActorParticipationPolicy.ExplicitActivityIds;
        [SerializeField] private List<ActivityAsset> participatingActivities = new();
        [SerializeField] private ActorPresentationProfileAsset presentationProfile;
        [SerializeField] private ActorPresentationEndpoint presentationEndpoint;

        public string NonPlayerActorId => Normalize(nonPlayerActorId);
        public override string ActorId => NonPlayerActorId;
        public override ActorRole ActorRoleMetadata => ActorRole.SceneAuthoredNonPlayer;
        public override ActorScope ActorScopeMetadata => actorScope == NonPlayerActorScope.RouteScoped
            ? _ImmersiveGames.NewScripts.Actors.Foundation.ActorScope.RouteScoped
            : _ImmersiveGames.NewScripts.Actors.Foundation.ActorScope.ActivityScoped;
        public string ActorKind => Normalize(actorKind);
        public NonPlayerActorScope ActorScope => actorScope;
        public NonPlayerActorParticipationPolicy ParticipationPolicy => participationPolicy;
        public IReadOnlyList<ActivityAsset> ParticipatingActivities => (IReadOnlyList<ActivityAsset>)participatingActivities ?? Array.Empty<ActivityAsset>();
        public ActorPresentationProfileAsset PresentationProfile => presentationProfile;
        public ActorPresentationEndpoint PresentationEndpoint => presentationEndpoint;

        public IReadOnlyList<string> ResolveParticipatingActivityIdsOrFail(string source)
        {
            string origin = string.IsNullOrWhiteSpace(source)
                ? $"{nameof(NonPlayerActorEndpoint)}:{name}"
                : source.Trim();

            if (participationPolicy != NonPlayerActorParticipationPolicy.ExplicitActivityIds)
            {
                return Array.Empty<string>();
            }

            if (participatingActivities == null || participatingActivities.Count == 0)
            {
                throw new InvalidOperationException($"{origin} requires at least one ActivityAsset when participationPolicy=ExplicitActivityIds.");
            }

            List<string> resolved = new(participatingActivities.Count);
            HashSet<string> dedupe = new(StringComparer.Ordinal);
            for (int index = 0; index < participatingActivities.Count; index++)
            {
                ActivityAsset activity = participatingActivities[index];
                if (activity == null)
                {
                    throw new InvalidOperationException($"{origin} has null participatingActivities[{index}] with participationPolicy=ExplicitActivityIds.");
                }

                string activityId = Normalize(activity.ActivityId);
                if (string.IsNullOrWhiteSpace(activityId))
                {
                    throw new InvalidOperationException($"{origin} has participatingActivities[{index}]='{activity.name}' with empty ActivityId.");
                }

                if (!dedupe.Add(activityId))
                {
                    throw new InvalidOperationException($"{origin} has duplicate participating activityId='{activityId}'.");
                }

                resolved.Add(activityId);
            }

            return resolved;
        }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(NonPlayerActorId) &&
            !string.IsNullOrWhiteSpace(ActorKind) &&
            actorScope != NonPlayerActorScope.Unknown &&
            actorScope != NonPlayerActorScope.GlobalScopedUnsupported &&
            participationPolicy != NonPlayerActorParticipationPolicy.Unknown &&
            IsParticipatingActivitiesConfigValid($"{nameof(NonPlayerActorEndpoint)}:{nameof(IsValid)}:{name}") &&
            presentationProfile != null &&
            presentationEndpoint != null;

        public override void ValidateLocalConfigurationOrThrow(string source)
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

            if (!IsParticipatingActivitiesConfigValid(origin))
            {
                throw new InvalidOperationException($"{origin} has invalid participatingActivities when participationPolicy=ExplicitActivityIds.");
            }

            if (presentationProfile == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorPresentationProfileAsset.");
            }

            if (presentationEndpoint == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorPresentationEndpoint.");
            }

            if (CapabilitySurface == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorCapabilitySurface.");
            }
        }

        public void ValidateOrThrow(string source)
        {
            ValidateLocalConfigurationOrThrow(source);
        }

        private void OnValidate()
        {
            base.OnValidate();
            nonPlayerActorId = Normalize(nonPlayerActorId);
            actorKind = Normalize(actorKind);
            if (participatingActivities == null)
            {
                participatingActivities = new List<ActivityAsset>();
            }
        }

        public bool ContainsActivityId(string activityId)
        {
            string normalized = Normalize(activityId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            IReadOnlyList<string> resolvedActivityIds = ResolveParticipatingActivityIdsOrFail($"{nameof(ContainsActivityId)}:{name}");
            for (int index = 0; index < resolvedActivityIds.Count; index++)
            {
                if (string.Equals(Normalize(resolvedActivityIds[index]), normalized, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsParticipatingActivitiesConfigValid(string origin)
        {
            if (participationPolicy != NonPlayerActorParticipationPolicy.ExplicitActivityIds)
            {
                return true;
            }

            if (participatingActivities == null || participatingActivities.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < participatingActivities.Count; index++)
            {
                ActivityAsset activity = participatingActivities[index];
                if (activity == null)
                {
                    return false;
                }

                string activityId = Normalize(activity.ActivityId);
                if (string.IsNullOrWhiteSpace(activityId))
                {
                    return false;
                }
            }

            try
            {
                ResolveParticipatingActivityIdsOrFail(origin);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
