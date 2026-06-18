using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.SessionActivity.Authoring;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.Runtime
{
    [DisallowMultipleComponent]
    public sealed class SceneAuthoredActor : Actor, ISceneAuthoredActor
    {
        [SerializeField] private string actorId = string.Empty;
        [SerializeField] private ActorScope actorScope = ActorScope.Unknown;
        [SerializeField] private ActorParticipationRecord.ActorParticipationPolicy participationPolicy = ActorParticipationRecord.ActorParticipationPolicy.None;
        [SerializeField] private List<ActivityAsset> participatingActivities = new();

        public override ActorId ActorIdValue => new(actorId.TrimToEmpty());
        public override ActorRole ActorRoleMetadata => ActorRole.SceneActor;
        public override ActorScope ActorScopeMetadata => actorScope;
        public override ActorParticipationRecord.ActorParticipationPolicy ActorParticipationPolicy => participationPolicy;
        public ActorScope SceneActorScope => ActorScopeMetadata;
        public ActorParticipationRecord.ActorParticipationPolicy SceneActorParticipationPolicy => ActorParticipationPolicy;

        private IReadOnlyList<string> ResolveParticipatingActivityIdsOrFail(string source)
        {
            string origin = string.IsNullOrWhiteSpace(source)
                ? $"SceneActor:{name}"
                : source.Trim();

            if (ActorParticipationPolicy != ActorParticipationRecord.ActorParticipationPolicy.ExplicitActivityIds)
            {
                return Array.Empty<string>();
            }

            if (participatingActivities == null || participatingActivities.Count == 0)
            {
                throw new InvalidOperationException($"{origin} requires at least one ActivityAsset when ActorParticipationPolicy=ExplicitActivityIds.");
            }

            List<string> resolved = new(participatingActivities.Count);
            HashSet<string> dedupe = new(StringComparer.Ordinal);
            for (int index = 0; index < participatingActivities.Count; index++)
            {
                var activity = participatingActivities[index];
                if (activity == null)
                {
                    throw new InvalidOperationException($"{origin} has null participatingActivities[{index}] with ActorParticipationPolicy=ExplicitActivityIds.");
                }

                string activityId = activity.ActivityId.TrimToEmpty();
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

        public IReadOnlyList<string> ResolveExplicitParticipationActivityIdsOrFail(string source)
        {
            return ActorParticipationPolicy == ActorParticipationRecord.ActorParticipationPolicy.ExplicitActivityIds
                ? ResolveParticipatingActivityIdsOrFail(source)
                : Array.Empty<string>();
        }

        public void ValidateSceneAuthoredConfigurationOrThrow(string source)
        {
            ValidateLocalConfigurationOrThrow(source);
        }

        public override void ValidateLocalConfigurationOrThrow(string source)
        {
            string origin = string.IsNullOrWhiteSpace(source)
                ? $"SceneActor:{name}"
                : source.Trim();

            if (string.IsNullOrWhiteSpace(ActorId))
            {
                throw new InvalidOperationException($"{origin} requires actorId.");
            }

            if (ActorScopeMetadata == ActorScope.Unknown)
            {
                throw new InvalidOperationException($"{origin} requires explicit actorScope.");
            }

            if (!Enum.IsDefined(typeof(ActorParticipationRecord.ActorParticipationPolicy), ActorParticipationPolicy))
            {
                throw new InvalidOperationException($"{origin} requires valid ActorParticipationPolicy.");
            }

            if (!IsParticipatingActivitiesConfigValid(origin))
            {
                throw new InvalidOperationException($"{origin} has invalid participatingActivities when ActorParticipationPolicy=ExplicitActivityIds.");
            }

            var surface = CapabilitySurface;
            if (surface == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorCapabilitySurface.");
            }

            if (surface.PresentationEndpoint == null)
            {
                throw new InvalidOperationException($"{origin} requires ActorCapabilitySurface.PresentationEndpoint.");
            }

            surface.PresentationEndpoint.ValidateOrThrow($"{origin}/{nameof(ActorCapabilitySurface)}.{nameof(ActorCapabilitySurface.PresentationEndpoint)}");
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            actorId = actorId.TrimToEmpty();
            if (participatingActivities == null)
            {
                participatingActivities = new List<ActivityAsset>();
            }
        }

        private bool IsParticipatingActivitiesConfigValid(string origin)
        {
            if (ActorParticipationPolicy != ActorParticipationRecord.ActorParticipationPolicy.ExplicitActivityIds)
            {
                return true;
            }

            if (participatingActivities == null || participatingActivities.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < participatingActivities.Count; index++)
            {
                var activity = participatingActivities[index];
                if (activity == null)
                {
                    return false;
                }

                string activityId = activity.ActivityId.TrimToEmpty();
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

    }
}
