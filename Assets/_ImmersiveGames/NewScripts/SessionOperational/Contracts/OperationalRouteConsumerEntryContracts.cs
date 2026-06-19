using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using _ImmersiveGames.NewScripts.UnityUtils;

namespace _ImmersiveGames.NewScripts.SessionOperational.Contracts
{
    public enum OperationalRouteConsumerEntryResultKind
    {
        Unknown = 0,
        Completed = 1,
        Rejected = 2,
        Failed = 3
    }

    public readonly struct OperationalRouteConsumerEntryRequest
    {
        public OperationalRouteConsumerEntryRequest(
            string sessionStateId,
            SessionParticipationContext sessionParticipationContext,
            IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> actorMaterializationSeedEntries,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext,
            SessionActivityRoutePauseSurfaceContext routePauseSurfaceContext,
            bool hasRouteFadeProfile,
            SceneTransitionProfile routeFadeProfile,
            bool hasRouteLoadingProfile,
            RuntimeLoadingProfileAsset routeLoadingProfile,
            string source,
            string reason)
        {
            SessionStateId = sessionStateId.TrimToEmpty();
            SessionParticipationContext = sessionParticipationContext;
            ActorMaterializationSeedEntries = actorMaterializationSeedEntries ?? Array.Empty<PlayerSetDefinitionAsset.PlayerActorResolvedEntry>();
            LoadedSnapshotPayloadContext = loadedSnapshotPayloadContext;
            RoutePauseSurfaceContext = routePauseSurfaceContext;
            HasRouteFadeProfile = hasRouteFadeProfile;
            RouteFadeProfile = routeFadeProfile;
            HasRouteLoadingProfile = hasRouteLoadingProfile;
            RouteLoadingProfile = routeLoadingProfile;
            Source = source.TrimToEmpty();
            Reason = reason.TrimToEmpty();
        }

        public string SessionStateId { get; }
        public SessionParticipationContext SessionParticipationContext { get; }
        public bool HasSessionParticipationContext => SessionParticipationContext is { IsValid: true };
        public IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> ActorMaterializationSeedEntries { get; }
        public ActivityEntryObjectSnapshotRestorePayloadContext LoadedSnapshotPayloadContext { get; }
        public bool HasLoadedSnapshotPayloadContext => LoadedSnapshotPayloadContext.IsValid;
        public SessionActivityRoutePauseSurfaceContext RoutePauseSurfaceContext { get; }
        public bool HasRoutePauseSurfaceContext => RoutePauseSurfaceContext.HasSurface && RoutePauseSurfaceContext.IsValid;
        public bool HasRouteFadeProfile { get; }
        public SceneTransitionProfile RouteFadeProfile { get; }
        public bool HasRouteLoadingProfile { get; }
        public RuntimeLoadingProfileAsset RouteLoadingProfile { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            HasSessionParticipationContext &&
            RoutePauseSurfaceContext.IsValid &&
            !string.IsNullOrWhiteSpace(Source);
    }

    public readonly struct OperationalRouteConsumerEntryResult
    {
        public OperationalRouteConsumerEntryResult(
            OperationalRouteConsumerEntryResultKind kind,
            string reason,
            string detail)
        {
            Kind = kind;
            Reason = reason.TrimToEmpty();
            Detail = detail.TrimToEmpty();
        }

        public OperationalRouteConsumerEntryResultKind Kind { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsValid => Kind != OperationalRouteConsumerEntryResultKind.Unknown && !string.IsNullOrWhiteSpace(Reason);
        public bool IsCompleted => Kind == OperationalRouteConsumerEntryResultKind.Completed;
        public bool IsRejected => Kind == OperationalRouteConsumerEntryResultKind.Rejected;
        public bool IsFailed => Kind == OperationalRouteConsumerEntryResultKind.Failed;
    }

    public interface IOperationalRouteConsumerEntryPort
    {
        Task<OperationalRouteConsumerEntryResult> RequestEntryAsync(
            OperationalRouteConsumerEntryRequest request,
            CancellationToken cancellationToken);
    }
}
