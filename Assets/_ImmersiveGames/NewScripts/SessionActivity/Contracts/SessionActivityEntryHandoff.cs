using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.SessionActivity.Contracts
{
    public readonly struct SessionActivityActorMaterializationPlanEntry : IEquatable<SessionActivityActorMaterializationPlanEntry>
    {
        public SessionActivityActorMaterializationPlanEntry(
            SessionParticipantId participantId,
            bool required,
            GameObject prefab,
            ActorPlacementMode placementMode,
            string placementId,
            Vector3 localPosition,
            Vector3 localEulerAngles)
        {
            ParticipantId = participantId;
            Required = required;
            Prefab = prefab;
            PlacementMode = placementMode;
            PlacementId = placementId.TrimToEmpty();
            LocalPosition = localPosition;
            LocalEulerAngles = localEulerAngles;
        }

        public SessionParticipantId ParticipantId { get; }
        public bool Required { get; }
        public GameObject Prefab { get; }
        public ActorPlacementMode PlacementMode { get; }
        public string PlacementId { get; }
        public Vector3 LocalPosition { get; }
        public Vector3 LocalEulerAngles { get; }
        public bool IsValid =>
            ParticipantId.IsValid &&
            (PlacementMode == ActorPlacementMode.None ||
                PlacementMode == ActorPlacementMode.SceneMarker ||
                PlacementMode == ActorPlacementMode.FixedTransform);
        public bool HasPrefab => Prefab != null;

        public bool Equals(SessionActivityActorMaterializationPlanEntry other)
        {
            return ParticipantId.Equals(other.ParticipantId) &&
                Required == other.Required &&
                Equals(Prefab, other.Prefab) &&
                PlacementMode == other.PlacementMode &&
                string.Equals(PlacementId, other.PlacementId, StringComparison.Ordinal) &&
                LocalPosition.Equals(other.LocalPosition) &&
                LocalEulerAngles.Equals(other.LocalEulerAngles);
        }

        public override bool Equals(object obj)
        {
            return obj is SessionActivityActorMaterializationPlanEntry other && Equals(other);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ParticipantId.GetHashCode();
                hashCode = hashCode * 397 ^ (Required ? 1 : 0);
                hashCode = hashCode * 397 ^ (Prefab != null ? Prefab.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (int)PlacementMode;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(PlacementId ?? string.Empty);
                hashCode = hashCode * 397 ^ LocalPosition.GetHashCode();
                hashCode = hashCode * 397 ^ LocalEulerAngles.GetHashCode();
                return hashCode;
            }
        }
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
                hashCode = hashCode * 397 ^ (RouteFadeProfile != null ? RouteFadeProfile.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (HasRouteLoadingProfile ? 1 : 0);
                hashCode = hashCode * 397 ^ (RouteLoadingProfile != null ? RouteLoadingProfile.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"hasRouteFadeProfile='{HasRouteFadeProfile}' routeFadeProfile='{(RouteFadeProfile != null ? RouteFadeProfile.name : "<none>")}' hasRouteLoadingProfile='{HasRouteLoadingProfile}' routeLoadingProfile='{(RouteLoadingProfile != null ? RouteLoadingProfile.name : "<none>")}'";
        }
    }


    public readonly struct SessionActivityRoutePauseSurfaceContext : IEquatable<SessionActivityRoutePauseSurfaceContext>
    {
        public SessionActivityRoutePauseSurfaceContext(
            bool hasSurface,
            string surfaceId,
            string sceneName,
            string overlayRootId,
            string activityContentRootId,
            string routeIdentity,
            string routeOperationId,
            string transitionId,
            int routeSequence,
            SessionActivityInputModeKind inputModeOnPause,
            SessionActivityInputModeKind inputModeOnResume,
            string source,
            string reason)
        {
            HasSurface = hasSurface;
            SurfaceId = surfaceId.TrimToEmpty();
            SceneName = sceneName.TrimToEmpty();
            OverlayRootId = overlayRootId.TrimToEmpty();
            ActivityContentRootId = activityContentRootId.TrimToEmpty();
            RouteIdentity = routeIdentity.TrimToEmpty();
            RouteOperationId = routeOperationId.TrimToEmpty();
            TransitionId = transitionId.TrimToEmpty();
            RouteSequence = routeSequence < 0 ? 0 : routeSequence;
            InputModeOnPause = inputModeOnPause;
            InputModeOnResume = inputModeOnResume;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public bool HasSurface { get; }
        public string SurfaceId { get; }
        public string SceneName { get; }
        public string OverlayRootId { get; }
        public string ActivityContentRootId { get; }
        public string RouteIdentity { get; }
        public string RouteOperationId { get; }
        public string TransitionId { get; }
        public int RouteSequence { get; }
        public SessionActivityInputModeKind InputModeOnPause { get; }
        public SessionActivityInputModeKind InputModeOnResume { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsValid =>
            !HasSurface ||
            (!string.IsNullOrWhiteSpace(SurfaceId) &&
             !string.IsNullOrWhiteSpace(SceneName) &&
             !string.IsNullOrWhiteSpace(OverlayRootId) &&
             !string.IsNullOrWhiteSpace(ActivityContentRootId) &&
             !string.IsNullOrWhiteSpace(RouteIdentity) &&
             !string.IsNullOrWhiteSpace(RouteOperationId) &&
             !string.IsNullOrWhiteSpace(TransitionId) &&
             RouteSequence > 0 &&
             InputModeOnPause == SessionActivityInputModeKind.PauseOverlay &&
             InputModeOnResume == SessionActivityInputModeKind.ActivityGameplay &&
             !string.IsNullOrWhiteSpace(Source));

        public static SessionActivityRoutePauseSurfaceContext None(string source, string reason)
        {
            return new SessionActivityRoutePauseSurfaceContext(
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                SessionActivityInputModeKind.Unknown,
                SessionActivityInputModeKind.Unknown,
                source,
                reason);
        }

        public bool Equals(SessionActivityRoutePauseSurfaceContext other)
        {
            return HasSurface == other.HasSurface &&
                string.Equals(SurfaceId, other.SurfaceId, StringComparison.Ordinal) &&
                string.Equals(SceneName, other.SceneName, StringComparison.Ordinal) &&
                string.Equals(OverlayRootId, other.OverlayRootId, StringComparison.Ordinal) &&
                string.Equals(ActivityContentRootId, other.ActivityContentRootId, StringComparison.Ordinal) &&
                string.Equals(RouteIdentity, other.RouteIdentity, StringComparison.Ordinal) &&
                string.Equals(RouteOperationId, other.RouteOperationId, StringComparison.Ordinal) &&
                string.Equals(TransitionId, other.TransitionId, StringComparison.Ordinal) &&
                RouteSequence == other.RouteSequence &&
                InputModeOnPause == other.InputModeOnPause &&
                InputModeOnResume == other.InputModeOnResume;
        }

        public override bool Equals(object obj)
        {
            return obj is SessionActivityRoutePauseSurfaceContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = HasSurface ? 1 : 0;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(SurfaceId ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(SceneName ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(OverlayRootId ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(ActivityContentRootId ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(RouteIdentity ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(RouteOperationId ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(TransitionId ?? string.Empty);
                hashCode = hashCode * 397 ^ RouteSequence;
                hashCode = hashCode * 397 ^ (int)InputModeOnPause;
                hashCode = hashCode * 397 ^ (int)InputModeOnResume;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return HasSurface && IsValid
                ? $"surfaceId='{SurfaceId}', sceneName='{SceneName}', overlayRootId='{OverlayRootId}', activityContentRootId='{ActivityContentRootId}', routeIdentity='{RouteIdentity}', routeOperationId='{RouteOperationId}', transitionId='{TransitionId}', routeSequence='{RouteSequence}', inputModeOnPause='{InputModeOnPause}', inputModeOnResume='{InputModeOnResume}'"
                : "<none>";
        }
    }
    public readonly struct SessionActivityEntryHandoff : IEquatable<SessionActivityEntryHandoff>
    {
        public SessionActivityEntryHandoff(
            string activityId,
            int activityOrdinal,
            int entrySequence,
            string sessionStateId,
            SessionParticipationContext sessionParticipationContext,
            IReadOnlyList<SessionActivityActorMaterializationPlanEntry> actorMaterializationPlanEntries,
            SessionActivityRouteTransitionContext routeTransitionContext,
            SessionActivityRoutePauseSurfaceContext routePauseSurfaceContext,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext,
            string source,
            string reason)
        {
            ActivityId = activityId.TrimToEmpty();
            ActivityOrdinal = activityOrdinal < 0 ? 0 : activityOrdinal;
            EntrySequence = entrySequence < 0 ? 0 : entrySequence;
            SessionStateId = sessionStateId.TrimToEmpty();
            SessionParticipationContext = sessionParticipationContext;
            ActorMaterializationPlanEntries = actorMaterializationPlanEntries ?? Array.Empty<SessionActivityActorMaterializationPlanEntry>();
            RouteTransitionContext = routeTransitionContext;
            RoutePauseSurfaceContext = routePauseSurfaceContext;
            LoadedSnapshotPayloadContext = loadedSnapshotPayloadContext;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public string ActivityId { get; }
        public int ActivityOrdinal { get; }
        public int EntrySequence { get; }
        public string SessionStateId { get; }
        public SessionParticipationContext SessionParticipationContext { get; }
        public IReadOnlyList<SessionActivityActorMaterializationPlanEntry> ActorMaterializationPlanEntries { get; }
        public SessionActivityRouteTransitionContext RouteTransitionContext { get; }
        public SessionActivityRoutePauseSurfaceContext RoutePauseSurfaceContext { get; }
        public ActivityEntryObjectSnapshotRestorePayloadContext LoadedSnapshotPayloadContext { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool HasSessionParticipationContext => SessionParticipationContext is { IsValid: true };
        public int SessionParticipationRevision => SessionParticipationContext?.Revision ?? 0;
        public int SessionParticipationSlotReservationCount => SessionParticipationContext?.SlotReservationCount ?? 0;
        public int SessionParticipationSelectionCount => SessionParticipationContext?.SelectionCount ?? 0;
        public int SessionParticipationParticipantCount => SessionParticipationContext?.ParticipantCount ?? 0;
        public int ActorMaterializationPlanEntryCount => CountMaterializationPlanEntries(ActorMaterializationPlanEntries);
        public bool HasRoutePauseSurfaceContext => RoutePauseSurfaceContext.HasSurface && RoutePauseSurfaceContext.IsValid;
        public bool HasLoadedSnapshotPayloadContext => LoadedSnapshotPayloadContext.IsValid;

        public bool HasResolvedActivity =>
            !string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal > 0;

        public bool IsEntryOnly =>
            string.IsNullOrWhiteSpace(ActivityId) &&
            ActivityOrdinal == 0;

        public bool IsValid =>
            EntrySequence >= 0 &&
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            HasSessionParticipationContext &&
            !string.IsNullOrWhiteSpace(Source) &&
            RoutePauseSurfaceContext.IsValid &&
            (HasResolvedActivity || IsEntryOnly);

        public bool Equals(SessionActivityEntryHandoff other)
        {
            return string.Equals(ActivityId, other.ActivityId, StringComparison.Ordinal) &&
                ActivityOrdinal == other.ActivityOrdinal &&
                EntrySequence == other.EntrySequence &&
                string.Equals(SessionStateId, other.SessionStateId, StringComparison.Ordinal) &&
                string.Equals(SessionParticipationContext?.RouteOperationId, other.SessionParticipationContext?.RouteOperationId, StringComparison.Ordinal) &&
                SessionParticipationRevision == other.SessionParticipationRevision &&
                ActorMaterializationPlanEntryCount == other.ActorMaterializationPlanEntryCount &&
                RouteTransitionContext.Equals(other.RouteTransitionContext) &&
                RoutePauseSurfaceContext.Equals(other.RoutePauseSurfaceContext) &&
                LoadedSnapshotPayloadContext.HasPayload == other.LoadedSnapshotPayloadContext.HasPayload &&
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
                hashCode = hashCode * 397 ^ ActivityOrdinal;
                hashCode = hashCode * 397 ^ EntrySequence;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(SessionStateId ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(SessionParticipationContext?.RouteOperationId ?? string.Empty);
                hashCode = hashCode * 397 ^ SessionParticipationRevision;
                hashCode = hashCode * 397 ^ ActorMaterializationPlanEntryCount;
                hashCode = hashCode * 397 ^ RouteTransitionContext.GetHashCode();
                hashCode = hashCode * 397 ^ RoutePauseSurfaceContext.GetHashCode();
                hashCode = hashCode * 397 ^ (LoadedSnapshotPayloadContext.HasPayload ? 1 : 0);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            if (!IsValid)
            {
                return "<none>";
            }

            string activity = HasResolvedActivity
                ? $"activityId='{ActivityId}', activityOrdinal='{ActivityOrdinal}', entrySequence='{EntrySequence}'"
                : EntrySequence > 0
                    ? $"activityId='<first-catalog>', activityOrdinal='0', entrySequence='{EntrySequence}'"
                    : "activityId='<first-catalog>', activityOrdinal='0', entrySequence='<pipeline-allocated>'";

            return
                $"{activity}, sessionStateId='{SessionStateId}', routeOperationId='{SessionParticipationContext.RouteOperationId}', sessionParticipationContext='present', sessionParticipationRevision='{SessionParticipationRevision}', sessionParticipants='{SessionParticipationParticipantCount}', actorMaterializationPlanEntries='{ActorMaterializationPlanEntryCount}', routePauseSurface='{(HasRoutePauseSurfaceContext ? "present" : "absent")}', routePauseSurfaceScene='{(HasRoutePauseSurfaceContext ? RoutePauseSurfaceContext.SceneName : "<none>")}', loadedSnapshotPayload='{(HasLoadedSnapshotPayloadContext ? "present" : "absent")}'";
        }

        public static bool operator ==(SessionActivityEntryHandoff left, SessionActivityEntryHandoff right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(SessionActivityEntryHandoff left, SessionActivityEntryHandoff right)
        {
            return !left.Equals(right);
        }

        private static int CountMaterializationPlanEntries(IReadOnlyList<SessionActivityActorMaterializationPlanEntry> entries)
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
    }
}
