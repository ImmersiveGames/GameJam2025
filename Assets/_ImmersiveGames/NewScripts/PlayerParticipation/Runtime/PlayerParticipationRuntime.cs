using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;

namespace _ImmersiveGames.NewScripts.PlayerParticipation.Runtime
{
    public enum PlayerParticipationRuntimeContextResolutionKind
    {
        Unknown = 0,
        Created = 1,
        Reused = 2,
        ReplacedInvalid = 3,
    }

    public readonly struct PlayerParticipationRuntimeContextResult
    {
        public PlayerParticipationRuntimeContextResult(
            PlayerParticipationRuntimeContextResolutionKind kind,
            string sessionId,
            int revision,
            SessionParticipationContext context,
            string source,
            string reason)
        {
            Kind = kind;
            SessionId = Normalize(sessionId);
            Revision = revision;
            Context = context;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public PlayerParticipationRuntimeContextResolutionKind Kind { get; }
        public string SessionId { get; }
        public int Revision { get; }
        public SessionParticipationContext Context { get; }
        public string Source { get; }
        public string Reason { get; }

        public bool IsCreated => Kind == PlayerParticipationRuntimeContextResolutionKind.Created;
        public bool IsReused => Kind == PlayerParticipationRuntimeContextResolutionKind.Reused;
        public bool IsReplacedInvalid => Kind == PlayerParticipationRuntimeContextResolutionKind.ReplacedInvalid;
        public bool IsAccepted =>
            Kind == PlayerParticipationRuntimeContextResolutionKind.Created ||
            Kind == PlayerParticipationRuntimeContextResolutionKind.Reused ||
            Kind == PlayerParticipationRuntimeContextResolutionKind.ReplacedInvalid;
        public bool IsValid =>
            IsAccepted &&
            !string.IsNullOrWhiteSpace(SessionId) &&
            Revision > 0 &&
            Context != null &&
            Context.IsValid;

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public interface IPlayerParticipationRuntime
    {
        PlayerParticipationRuntimeContextResult ResolveOrStoreSessionContext(
            string sessionId,
            SessionParticipationContext candidateContext,
            string routeIdentity,
            string routeOperationId,
            string source,
            string reason);

        bool TryGetSessionContext(
            string sessionId,
            string routeIdentity,
            string routeOperationId,
            string source,
            string reason,
            out PlayerParticipationRuntimeContextResult result);

        bool ClearSession(string sessionId, string source, string reason);
    }

    public sealed class PlayerParticipationRuntime : IPlayerParticipationRuntime
    {
        private readonly Dictionary<string, PlayerParticipationRuntimeState> _statesBySessionId = new(StringComparer.Ordinal);

        public PlayerParticipationRuntimeContextResult ResolveOrStoreSessionContext(
            string sessionId,
            SessionParticipationContext candidateContext,
            string routeIdentity,
            string routeOperationId,
            string source,
            string reason)
        {
            string normalizedSessionId = NormalizeRequired(sessionId, nameof(sessionId));
            string normalizedRouteIdentity = NormalizeRequired(routeIdentity, nameof(routeIdentity));
            string normalizedRouteOperationId = NormalizeRequired(routeOperationId, nameof(routeOperationId));
            string normalizedSource = NormalizeRequired(source, nameof(source));
            string normalizedReason = NormalizeRequired(reason, nameof(reason));

            if (candidateContext == null || !candidateContext.IsValid)
            {
                throw new InvalidOperationException("candidate SessionParticipationContext is invalid.");
            }

            if (_statesBySessionId.TryGetValue(normalizedSessionId, out PlayerParticipationRuntimeState existingState) &&
                existingState != null)
            {
                if (existingState.Context != null &&
                    existingState.Context.IsValid &&
                    SatisfiesRequirement(existingState.Context, candidateContext.RequirementKind))
                {
                    SessionParticipationContext reusedSnapshot = BuildRouteSnapshot(
                        normalizedSessionId,
                        existingState.Revision,
                        existingState.Context,
                        candidateContext.RequirementKind,
                        candidateContext.RuntimeJoinPolicy,
                        normalizedRouteIdentity,
                        normalizedRouteOperationId,
                        normalizedSource,
                        normalizedReason);

                    return new PlayerParticipationRuntimeContextResult(
                        PlayerParticipationRuntimeContextResolutionKind.Reused,
                        normalizedSessionId,
                        existingState.Revision,
                        reusedSnapshot,
                        normalizedSource,
                        normalizedReason);
                }

                int replacementRevision = existingState.Revision + 1;
                SessionParticipationContext replacementSnapshot = BuildRouteSnapshot(
                    normalizedSessionId,
                    replacementRevision,
                    candidateContext,
                    candidateContext.RequirementKind,
                    candidateContext.RuntimeJoinPolicy,
                    normalizedRouteIdentity,
                    normalizedRouteOperationId,
                    normalizedSource,
                    normalizedReason);

                _statesBySessionId[normalizedSessionId] = new PlayerParticipationRuntimeState(
                    normalizedSessionId,
                    replacementRevision,
                    replacementSnapshot,
                    normalizedSource,
                    normalizedReason);

                return new PlayerParticipationRuntimeContextResult(
                    PlayerParticipationRuntimeContextResolutionKind.ReplacedInvalid,
                    normalizedSessionId,
                    replacementRevision,
                    replacementSnapshot,
                    normalizedSource,
                    normalizedReason);
            }

            const int initialRevision = 1;
            SessionParticipationContext createdSnapshot = BuildRouteSnapshot(
                normalizedSessionId,
                initialRevision,
                candidateContext,
                candidateContext.RequirementKind,
                candidateContext.RuntimeJoinPolicy,
                normalizedRouteIdentity,
                normalizedRouteOperationId,
                normalizedSource,
                normalizedReason);

            _statesBySessionId[normalizedSessionId] = new PlayerParticipationRuntimeState(
                normalizedSessionId,
                initialRevision,
                createdSnapshot,
                normalizedSource,
                normalizedReason);

            return new PlayerParticipationRuntimeContextResult(
                PlayerParticipationRuntimeContextResolutionKind.Created,
                normalizedSessionId,
                initialRevision,
                createdSnapshot,
                normalizedSource,
                normalizedReason);
        }

        public bool TryGetSessionContext(
            string sessionId,
            string routeIdentity,
            string routeOperationId,
            string source,
            string reason,
            out PlayerParticipationRuntimeContextResult result)
        {
            result = default;

            string normalizedSessionId = Normalize(sessionId);
            if (string.IsNullOrWhiteSpace(normalizedSessionId) ||
                !_statesBySessionId.TryGetValue(normalizedSessionId, out PlayerParticipationRuntimeState state) ||
                state == null ||
                state.Context == null ||
                !state.Context.IsValid)
            {
                return false;
            }

            SessionParticipationContext snapshot = BuildRouteSnapshot(
                normalizedSessionId,
                state.Revision,
                state.Context,
                state.Context.RequirementKind,
                state.Context.RuntimeJoinPolicy,
                routeIdentity,
                routeOperationId,
                source,
                reason);

            result = new PlayerParticipationRuntimeContextResult(
                PlayerParticipationRuntimeContextResolutionKind.Reused,
                normalizedSessionId,
                state.Revision,
                snapshot,
                source,
                reason);
            return result.IsValid;
        }

        public bool ClearSession(string sessionId, string source, string reason)
        {
            string normalizedSessionId = Normalize(sessionId);
            if (string.IsNullOrWhiteSpace(normalizedSessionId))
            {
                return false;
            }

            return _statesBySessionId.Remove(normalizedSessionId);
        }

        private static SessionParticipationContext BuildRouteSnapshot(
            string sessionId,
            int revision,
            SessionParticipationContext sourceContext,
            RouteParticipationRequirementKind requirementKind,
            RuntimePlayerJoinPolicyKind runtimeJoinPolicy,
            string routeIdentity,
            string routeOperationId,
            string source,
            string reason)
        {
            if (sourceContext == null)
            {
                throw new ArgumentNullException(nameof(sourceContext));
            }

            return new SessionParticipationContext(
                sessionId,
                revision,
                routeIdentity,
                routeOperationId,
                requirementKind,
                sourceContext.SlotReservations,
                sourceContext.Selections,
                sourceContext.Participants,
                runtimeJoinPolicy,
                source,
                reason);
        }

        private static bool SatisfiesRequirement(
            SessionParticipationContext context,
            RouteParticipationRequirementKind requirementKind)
        {
            if (context == null || !context.IsValid)
            {
                return false;
            }

            if (requirementKind == RouteParticipationRequirementKind.None ||
                requirementKind == RouteParticipationRequirementKind.Optional)
            {
                return true;
            }

            return context.HasParticipants;
        }

        private static string NormalizeRequired(string value, string argumentName)
        {
            string normalized = Normalize(value);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException($"{argumentName} is required.", argumentName);
            }

            return normalized;
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        private sealed class PlayerParticipationRuntimeState
        {
            public PlayerParticipationRuntimeState(
                string sessionId,
                int revision,
                SessionParticipationContext context,
                string source,
                string reason)
            {
                SessionId = NormalizeRequired(sessionId, nameof(sessionId));
                Revision = revision;
                Context = context ?? throw new ArgumentNullException(nameof(context));
                Source = Normalize(source);
                Reason = Normalize(reason);
            }

            public string SessionId { get; }
            public int Revision { get; }
            public SessionParticipationContext Context { get; }
            public string Source { get; }
            public string Reason { get; }
        }
    }
}
