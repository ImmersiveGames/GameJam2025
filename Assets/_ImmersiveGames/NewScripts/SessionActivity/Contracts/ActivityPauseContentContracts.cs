using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public readonly struct ActivityPauseContentSlotId : IEquatable<ActivityPauseContentSlotId>
    {
        public ActivityPauseContentSlotId(string value)
        {
            Value = value.TrimToEmpty();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActivityPauseContentSlotId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityPauseContentSlotId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public readonly struct ActivityPauseContentContribution
    {
        public ActivityPauseContentContribution(
            ActivityPauseContentSlotId slotId,
            GameObject prefab,
            bool required)
        {
            SlotId = slotId;
            Prefab = prefab;
            Required = required;
        }

        public ActivityPauseContentSlotId SlotId { get; }
        public GameObject Prefab { get; }
        public bool Required { get; }

        public bool IsValid => SlotId.IsValid && Prefab != null;
    }

    public readonly struct ActivityPauseContentProfile
    {
        public ActivityPauseContentProfile(
            string profileId,
            IReadOnlyList<ActivityPauseContentContribution> contributions)
        {
            ProfileId = profileId.TrimToEmpty();
            Contributions = contributions ?? Array.Empty<ActivityPauseContentContribution>();
        }

        public string ProfileId { get; }
        public IReadOnlyList<ActivityPauseContentContribution> Contributions { get; }
        public bool HasContributions => Contributions != null && Contributions.Count > 0;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ProfileId) &&
            Contributions != null &&
            AreContributionsValid(Contributions);

        private static bool AreContributionsValid(IReadOnlyList<ActivityPauseContentContribution> contributions)
        {
            if (contributions == null)
            {
                return false;
            }

            HashSet<string> slotIds = new(StringComparer.Ordinal);
            for (int i = 0; i < contributions.Count; i++)
            {
                var contribution = contributions[i];
                if (!contribution.IsValid)
                {
                    return false;
                }

                if (!slotIds.Add(contribution.SlotId.Value))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public readonly struct ActivityPauseContentBindingCommand
    {
        public ActivityPauseContentBindingCommand(
            SessionActivityIdentity identity,
            SessionActivityRoutePauseSurfaceContext routePauseSurfaceContext,
            ActivityPauseContentProfile profile,
            string source,
            string reason)
        {
            Identity = identity;
            RoutePauseSurfaceContext = routePauseSurfaceContext;
            Profile = profile;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityRoutePauseSurfaceContext RoutePauseSurfaceContext { get; }
        public ActivityPauseContentProfile Profile { get; }
        public string Source { get; }
        public string Reason { get; }
        public string ActivityId => Identity.ActivityId;
        public int ActivityOrdinal => Identity.ActivityOrdinal;
        public int EntrySequence => Identity.EntrySequence;

        public bool IsValid =>
            Identity.IsValid &&
            RoutePauseSurfaceContext.HasSurface &&
            RoutePauseSurfaceContext.IsValid &&
            Profile.IsValid &&
            !string.IsNullOrWhiteSpace(Source);
    }

    public readonly struct ActivityPauseContentReleaseCommand
    {
        public ActivityPauseContentReleaseCommand(
            SessionActivityIdentity identity,
            SessionActivityRoutePauseSurfaceContext routePauseSurfaceContext,
            ActivityPauseContentBindingResult currentBinding,
            string source,
            string reason)
        {
            Identity = identity;
            RoutePauseSurfaceContext = routePauseSurfaceContext;
            CurrentBinding = currentBinding;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public SessionActivityIdentity Identity { get; }
        public SessionActivityRoutePauseSurfaceContext RoutePauseSurfaceContext { get; }
        public ActivityPauseContentBindingResult CurrentBinding { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            Identity.IsValid &&
            RoutePauseSurfaceContext.HasSurface &&
            RoutePauseSurfaceContext.IsValid &&
            CurrentBinding.IsValid &&
            !string.IsNullOrWhiteSpace(Source);
    }

    public readonly struct ActivityPauseContentBindingResult
    {
        public ActivityPauseContentBindingResult(
            bool completed,
            bool skipped,
            SessionActivityIdentity identity,
            string profileId,
            int requestedCount,
            int boundCount,
            int skippedCount,
            string reason)
        {
            Completed = completed;
            Skipped = skipped;
            Identity = identity;
            ProfileId = profileId.TrimToEmpty();
            RequestedCount = requestedCount < 0 ? 0 : requestedCount;
            BoundCount = boundCount < 0 ? 0 : boundCount;
            SkippedCount = skippedCount < 0 ? 0 : skippedCount;
            Reason = reason.TrimToEmpty();
        }

        public bool Completed { get; }
        public bool Skipped { get; }
        public SessionActivityIdentity Identity { get; }
        public string ProfileId { get; }
        public int RequestedCount { get; }
        public int BoundCount { get; }
        public int SkippedCount { get; }
        public string Reason { get; }
        public bool HasBoundContent => Completed && !Skipped && BoundCount > 0;

        public bool IsValid =>
            Identity.IsValid &&
            !string.IsNullOrWhiteSpace(Reason) &&
            (Skipped || !string.IsNullOrWhiteSpace(ProfileId));

        public static ActivityPauseContentBindingResult SkippedNoContribution(
            SessionActivityIdentity identity,
            string reason)
        {
            return new ActivityPauseContentBindingResult(
                true,
                true,
                identity,
                string.Empty,
                0,
                0,
                0,
                reason);
        }
    }

    public readonly struct ActivityPauseContentReleaseResult
    {
        public ActivityPauseContentReleaseResult(
            bool completed,
            bool skipped,
            SessionActivityIdentity identity,
            string profileId,
            int releasedCount,
            string reason)
        {
            Completed = completed;
            Skipped = skipped;
            Identity = identity;
            ProfileId = profileId.TrimToEmpty();
            ReleasedCount = releasedCount < 0 ? 0 : releasedCount;
            Reason = reason.TrimToEmpty();
        }

        public bool Completed { get; }
        public bool Skipped { get; }
        public SessionActivityIdentity Identity { get; }
        public string ProfileId { get; }
        public int ReleasedCount { get; }
        public string Reason { get; }

        public bool IsValid => Identity.IsValid && !string.IsNullOrWhiteSpace(Reason);
    }

    public interface ISessionActivityPauseContentAdapter
    {
        ActivityPauseContentBindingResult Bind(ActivityPauseContentBindingCommand command);
        ActivityPauseContentReleaseResult Release(ActivityPauseContentReleaseCommand command);
    }
}
