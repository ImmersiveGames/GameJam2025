using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Semantic.Preparation;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public readonly struct SessionParticipantId : IEquatable<SessionParticipantId>
    {
        public SessionParticipantId(string value)
        {
            Value = Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(SessionParticipantId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionParticipantId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct SessionActivityPlayerTechnicalPlanEntry : IEquatable<SessionActivityPlayerTechnicalPlanEntry>
    {
        public SessionActivityPlayerTechnicalPlanEntry(
            string participantId,
            bool required,
            GameObject prefab,
            ActorPlacementMode placementMode,
            string placementId,
            Vector3 localPosition,
            Vector3 localEulerAngles)
        {
            ParticipantId = Normalize(participantId);
            Required = required;
            Prefab = prefab;
            PlacementMode = placementMode;
            PlacementId = Normalize(placementId);
            LocalPosition = localPosition;
            LocalEulerAngles = localEulerAngles;
        }

        public string ParticipantId { get; }
        public bool Required { get; }
        public GameObject Prefab { get; }
        public ActorPlacementMode PlacementMode { get; }
        public string PlacementId { get; }
        public Vector3 LocalPosition { get; }
        public Vector3 LocalEulerAngles { get; }
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ParticipantId) &&
            (PlacementMode == ActorPlacementMode.None ||
             PlacementMode == ActorPlacementMode.SceneMarker ||
             PlacementMode == ActorPlacementMode.FixedTransform);
        public bool HasPrefab => Prefab != null;

        public bool Equals(SessionActivityPlayerTechnicalPlanEntry other)
        {
            return string.Equals(ParticipantId, other.ParticipantId, StringComparison.Ordinal) &&
                   Required == other.Required &&
                   Equals(Prefab, other.Prefab) &&
                   PlacementMode == other.PlacementMode &&
                   string.Equals(PlacementId, other.PlacementId, StringComparison.Ordinal) &&
                   LocalPosition.Equals(other.LocalPosition) &&
                   LocalEulerAngles.Equals(other.LocalEulerAngles);
        }

        public override bool Equals(object obj) => obj is SessionActivityPlayerTechnicalPlanEntry other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ParticipantId ?? string.Empty);
                hashCode = (hashCode * 397) ^ (Required ? 1 : 0);
                hashCode = (hashCode * 397) ^ (Prefab != null ? Prefab.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)PlacementMode;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(PlacementId ?? string.Empty);
                hashCode = (hashCode * 397) ^ LocalPosition.GetHashCode();
                hashCode = (hashCode * 397) ^ LocalEulerAngles.GetHashCode();
                return hashCode;
            }
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct SessionActivityRouteTransitionContext : IEquatable<SessionActivityRouteTransitionContext>
    {
        public SessionActivityRouteTransitionContext(
            bool hasRouteFadeProfile,
            SceneTransitionProfile routeFadeProfile,
            bool hasRouteLoadingProfile,
            RuntimeLoadingProfileAsset routeLoadingProfile)
        {
            HasRouteFadeProfile = hasRouteFadeProfile;
            RouteFadeProfile = routeFadeProfile;
            HasRouteLoadingProfile = hasRouteLoadingProfile;
            RouteLoadingProfile = routeLoadingProfile;
        }

        public bool HasRouteFadeProfile { get; }
        public SceneTransitionProfile RouteFadeProfile { get; }
        public bool HasRouteLoadingProfile { get; }
        public RuntimeLoadingProfileAsset RouteLoadingProfile { get; }

        public bool Equals(SessionActivityRouteTransitionContext other)
        {
            return HasRouteFadeProfile == other.HasRouteFadeProfile &&
                   Equals(RouteFadeProfile, other.RouteFadeProfile) &&
                   HasRouteLoadingProfile == other.HasRouteLoadingProfile &&
                   Equals(RouteLoadingProfile, other.RouteLoadingProfile);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionActivityRouteTransitionContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = HasRouteFadeProfile ? 1 : 0;
                hashCode = (hashCode * 397) ^ (RouteFadeProfile != null ? RouteFadeProfile.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (HasRouteLoadingProfile ? 1 : 0);
                hashCode = (hashCode * 397) ^ (RouteLoadingProfile != null ? RouteLoadingProfile.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"hasRouteFadeProfile='{HasRouteFadeProfile}' routeFadeProfile='{(RouteFadeProfile != null ? RouteFadeProfile.name : "<none>")}' hasRouteLoadingProfile='{HasRouteLoadingProfile}' routeLoadingProfile='{(RouteLoadingProfile != null ? RouteLoadingProfile.name : "<none>")}'";
        }
    }
    public readonly struct SessionActivityPlayerPreparationHandoff : IEquatable<SessionActivityPlayerPreparationHandoff>
    {
        public SessionActivityPlayerPreparationHandoff(
            string pipelineId,
            string sessionId,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            string outcome,
            string participationKind,
            int plannedPlayers,
            int requiredPlayers,
            int optionalPlayers,
            int materializedPlayers,
            int skippedPlayers,
            int pendingRequiredPlayers,
            IReadOnlyList<SessionParticipantId> participantIds,
            IReadOnlyList<SessionActivityPlayerTechnicalPlanEntry> technicalPlanEntries)
        {
            PipelineId = Normalize(pipelineId);
            SessionId = Normalize(sessionId);
            RouteIdentity = Normalize(routeIdentity);
            RouteOperationId = Normalize(routeOperationId);
            TransitionId = Normalize(transitionId);
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            Outcome = Normalize(outcome);
            ParticipationKind = Normalize(participationKind);
            PlannedPlayers = plannedPlayers < 0 ? 0 : plannedPlayers;
            RequiredPlayers = requiredPlayers < 0 ? 0 : requiredPlayers;
            OptionalPlayers = optionalPlayers < 0 ? 0 : optionalPlayers;
            MaterializedPlayers = materializedPlayers < 0 ? 0 : materializedPlayers;
            SkippedPlayers = skippedPlayers < 0 ? 0 : skippedPlayers;
            PendingRequiredPlayers = pendingRequiredPlayers < 0 ? 0 : pendingRequiredPlayers;
            ParticipantIds = participantIds ?? Array.Empty<SessionParticipantId>();
            ParticipantIdsDisplay = BuildParticipantIdsDisplay(ParticipantIds);
            TechnicalPlanEntries = technicalPlanEntries ?? Array.Empty<SessionActivityPlayerTechnicalPlanEntry>();
        }

        public string PipelineId { get; }
        public string SessionId { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public string Outcome { get; }
        public string ParticipationKind { get; }
        public int PlannedPlayers { get; }
        public int RequiredPlayers { get; }
        public int OptionalPlayers { get; }
        public int MaterializedPlayers { get; }
        public int SkippedPlayers { get; }
        public int PendingRequiredPlayers { get; }
        public IReadOnlyList<SessionParticipantId> ParticipantIds { get; }
        public string ParticipantIdsDisplay { get; }
        public IReadOnlyList<SessionActivityPlayerTechnicalPlanEntry> TechnicalPlanEntries { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PipelineId) &&
            !string.IsNullOrWhiteSpace(SessionId) &&
            !string.IsNullOrWhiteSpace(RouteIdentity) &&
            !string.IsNullOrWhiteSpace(RouteOperationId) &&
            !string.IsNullOrWhiteSpace(TransitionId) &&
            RouteSequence > 0 &&
            !string.IsNullOrWhiteSpace(Outcome) &&
            !string.IsNullOrWhiteSpace(ParticipationKind) &&
            TechnicalPlanEntries != null;

        public bool Equals(SessionActivityPlayerPreparationHandoff other)
        {
            return string.Equals(PipelineId, other.PipelineId, StringComparison.Ordinal) &&
                   string.Equals(SessionId, other.SessionId, StringComparison.Ordinal) &&
                   string.Equals(RouteIdentity, other.RouteIdentity, StringComparison.Ordinal) &&
                   string.Equals(RouteOperationId, other.RouteOperationId, StringComparison.Ordinal) &&
                   string.Equals(TransitionId, other.TransitionId, StringComparison.Ordinal) &&
                   RouteSequence == other.RouteSequence &&
                   string.Equals(Outcome, other.Outcome, StringComparison.Ordinal) &&
                   string.Equals(ParticipationKind, other.ParticipationKind, StringComparison.Ordinal) &&
                   PlannedPlayers == other.PlannedPlayers &&
                   RequiredPlayers == other.RequiredPlayers &&
                   OptionalPlayers == other.OptionalPlayers &&
                   MaterializedPlayers == other.MaterializedPlayers &&
                   SkippedPlayers == other.SkippedPlayers &&
                   PendingRequiredPlayers == other.PendingRequiredPlayers &&
                   CountParticipantIds(ParticipantIds) == CountParticipantIds(other.ParticipantIds) &&
                   CountTechnicalEntries(TechnicalPlanEntries) == CountTechnicalEntries(other.TechnicalPlanEntries);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionActivityPlayerPreparationHandoff other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(PipelineId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SessionId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(RouteIdentity ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(RouteOperationId ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(TransitionId ?? string.Empty);
                hashCode = (hashCode * 397) ^ RouteSequence;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Outcome ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ParticipationKind ?? string.Empty);
                hashCode = (hashCode * 397) ^ PlannedPlayers;
                hashCode = (hashCode * 397) ^ RequiredPlayers;
                hashCode = (hashCode * 397) ^ OptionalPlayers;
                hashCode = (hashCode * 397) ^ MaterializedPlayers;
                hashCode = (hashCode * 397) ^ SkippedPlayers;
                hashCode = (hashCode * 397) ^ PendingRequiredPlayers;
                hashCode = (hashCode * 397) ^ CountParticipantIds(ParticipantIds);
                hashCode = (hashCode * 397) ^ CountTechnicalEntries(TechnicalPlanEntries);
                return hashCode;
            }
        }

        private static int CountParticipantIds(IReadOnlyList<SessionParticipantId> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < ids.Count; index++)
            {
                if (ids[index].IsValid)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static string BuildParticipantIdsDisplay(IReadOnlyList<SessionParticipantId> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return "<none>";
            }

            List<string> values = new(ids.Count);
            for (int index = 0; index < ids.Count; index++)
            {
                SessionParticipantId id = ids[index];
                if (id.IsValid)
                {
                    values.Add(id.Value);
                }
            }

            return values.Count == 0 ? "<none>" : string.Join(", ", values);
        }

        private static int CountTechnicalEntries(IReadOnlyList<SessionActivityPlayerTechnicalPlanEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].IsValid)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public readonly struct SessionActivityEntryHandoff : IEquatable<SessionActivityEntryHandoff>
    {
        public SessionActivityEntryHandoff(
            string activityId,
            int activityOrdinal,
            int entrySequence,
            string sessionStateId,
            SessionActivityPlayerPreparationHandoff playerPreparation,
            SessionActivityRouteTransitionContext routeTransitionContext,
            string source,
            string reason)
        {
            ActivityId = Normalize(activityId);
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            SessionStateId = Normalize(sessionStateId);
            PlayerPreparation = playerPreparation;
            RouteTransitionContext = routeTransitionContext;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public string SessionStateId { get; }
        public SessionActivityPlayerPreparationHandoff PlayerPreparation { get; }
        public SessionActivityRouteTransitionContext RouteTransitionContext { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasResolvedActivity =>
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0;

        public bool IsEntryOnly =>
            string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal == 0;

        public bool IsValid =>
            EntrySequence >= 0 &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            PlayerPreparation.IsValid &&
            !string.IsNullOrWhiteSpace(Source) &&
            (HasResolvedActivity || IsEntryOnly);

        public bool Equals(SessionActivityEntryHandoff other)
        {
            return string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal) &&
                   ActivityOrdinal == other.ActivityOrdinal &&
                   EntrySequence == other.EntrySequence &&
                   string.Equals(SessionStateId, other.SessionStateId, StringComparison.Ordinal) &&
                   PlayerPreparation.Equals(other.PlayerPreparation) &&
                   RouteTransitionContext.Equals(other.RouteTransitionContext) &&
                   string.Equals(Source, other.Source, StringComparison.Ordinal) &&
                   string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionActivityEntryHandoff other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(ActivityId ?? string.Empty);
                hashCode = (hashCode * 397) ^ ActivityOrdinal;
                hashCode = (hashCode * 397) ^ EntrySequence;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(SessionStateId ?? string.Empty);
                hashCode = (hashCode * 397) ^ PlayerPreparation.GetHashCode();
                hashCode = (hashCode * 397) ^ RouteTransitionContext.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return IsValid
                ? HasResolvedActivity
                    ? $"activityId='{ActivityId}', activityOrdinal='{ActivityOrdinal}', entrySequence='{EntrySequence}', sessionStateId='{SessionStateId}', routeOperationId='{PlayerPreparation.RouteOperationId}', playerPreparationOutcome='{PlayerPreparation.Outcome}', plannedPlayers='{PlayerPreparation.PlannedPlayers}', materializedPlayers='{PlayerPreparation.MaterializedPlayers}', pendingRequiredPlayers='{PlayerPreparation.PendingRequiredPlayers}'"
                    : EntrySequence > 0
                        ? $"activityId='<first-catalog>', activityOrdinal='0', entrySequence='{EntrySequence}', sessionStateId='{SessionStateId}', routeOperationId='{PlayerPreparation.RouteOperationId}', playerPreparationOutcome='{PlayerPreparation.Outcome}', plannedPlayers='{PlayerPreparation.PlannedPlayers}', materializedPlayers='{PlayerPreparation.MaterializedPlayers}', pendingRequiredPlayers='{PlayerPreparation.PendingRequiredPlayers}'"
                        : $"activityId='<first-catalog>', activityOrdinal='0', entrySequence='<pipeline-allocated>', sessionStateId='{SessionStateId}', routeOperationId='{PlayerPreparation.RouteOperationId}', playerPreparationOutcome='{PlayerPreparation.Outcome}', plannedPlayers='{PlayerPreparation.PlannedPlayers}', materializedPlayers='{PlayerPreparation.MaterializedPlayers}', pendingRequiredPlayers='{PlayerPreparation.PendingRequiredPlayers}'"
                : "<none>";
        }

        public static bool operator ==(SessionActivityEntryHandoff left, SessionActivityEntryHandoff right) => left.Equals(right);
        public static bool operator !=(SessionActivityEntryHandoff left, SessionActivityEntryHandoff right) => !left.Equals(right);

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}




