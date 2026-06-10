using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Actors.Semantic.Participation;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
using _ImmersiveGames.NewScripts.Foundation.Platform.Transitions;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;

namespace _ImmersiveGames.NewScripts.SessionOperational.Pipeline
{
    public enum OperationalRouteConsumerEntryResultKind
    {
        Unknown = 0,
        Completed = 1,
        Rejected = 2,
        Failed = 3,
    }

    public readonly struct OperationalRouteConsumerEntryRequest
    {
        public OperationalRouteConsumerEntryRequest(
            string sessionStateId,
            SessionParticipationContext sessionParticipationContext,
            IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> actorMaterializationSeedEntries,
            ActivityEntryObjectSnapshotRestorePayloadContext loadedSnapshotPayloadContext,
            bool hasRouteFadeProfile,
            SceneTransitionProfile routeFadeProfile,
            bool hasRouteLoadingProfile,
            RuntimeLoadingProfileAsset routeLoadingProfile,
            string source,
            string reason)
        {
            SessionStateId = Normalize(sessionStateId);
            SessionParticipationContext = sessionParticipationContext;
            ActorMaterializationSeedEntries = actorMaterializationSeedEntries ?? Array.Empty<PlayerSetDefinitionAsset.PlayerActorResolvedEntry>();
            LoadedSnapshotPayloadContext = loadedSnapshotPayloadContext;
            HasRouteFadeProfile = hasRouteFadeProfile;
            RouteFadeProfile = routeFadeProfile;
            HasRouteLoadingProfile = hasRouteLoadingProfile;
            RouteLoadingProfile = routeLoadingProfile;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public string SessionStateId { get; }
        public SessionParticipationContext SessionParticipationContext { get; }
        public bool HasSessionParticipationContext => SessionParticipationContext is { IsValid: true };
        public IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> ActorMaterializationSeedEntries { get; }
        public ActivityEntryObjectSnapshotRestorePayloadContext LoadedSnapshotPayloadContext { get; }
        public bool HasLoadedSnapshotPayloadContext => LoadedSnapshotPayloadContext.IsValid;
        public bool HasRouteFadeProfile { get; }
        public SceneTransitionProfile RouteFadeProfile { get; }
        public bool HasRouteLoadingProfile { get; }
        public RuntimeLoadingProfileAsset RouteLoadingProfile { get; }
        public string Source { get; }
        public string Reason { get; }
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SessionStateId) &&
            HasSessionParticipationContext &&
            !string.IsNullOrWhiteSpace(Source);

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public readonly struct OperationalRouteConsumerEntryResult
    {
        public OperationalRouteConsumerEntryResult(
            OperationalRouteConsumerEntryResultKind kind,
            string reason,
            string detail)
        {
            Kind = kind;
            Reason = Normalize(reason);
            Detail = Normalize(detail);
        }

        public OperationalRouteConsumerEntryResultKind Kind { get; }
        public string Reason { get; }
        public string Detail { get; }
        public bool IsValid => Kind != OperationalRouteConsumerEntryResultKind.Unknown && !string.IsNullOrWhiteSpace(Reason);
        public bool IsCompleted => Kind == OperationalRouteConsumerEntryResultKind.Completed;
        public bool IsRejected => Kind == OperationalRouteConsumerEntryResultKind.Rejected;
        public bool IsFailed => Kind == OperationalRouteConsumerEntryResultKind.Failed;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IOperationalRouteConsumerEntryPort
    {
        Task<OperationalRouteConsumerEntryResult> RequestEntryAsync(
            OperationalRouteConsumerEntryRequest request,
            CancellationToken cancellationToken);
    }
}
