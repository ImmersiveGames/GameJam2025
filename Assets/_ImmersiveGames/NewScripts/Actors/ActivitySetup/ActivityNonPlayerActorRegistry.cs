using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    public sealed class ActivityNonPlayerActorRegistry
    {
        private readonly Dictionary<string, NonPlayerActorRuntimeEntry> _activeByActorId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, NonPlayerActorRuntimeEntry> _routeRetainedByActorId = new(StringComparer.Ordinal);
        private readonly HashSet<string> _activeParticipationByActorId = new(StringComparer.Ordinal);
        private SessionActivityIdentity _activeScopeIdentity;

        public void BeginActivityScope(SessionActivityIdentity scopeIdentity)
        {
            if (!scopeIdentity.IsValid)
            {
                throw new InvalidOperationException("ActivityNonPlayerActorRegistry requires valid scope identity.");
            }

            _activeScopeIdentity = scopeIdentity;
            _activeByActorId.Clear();
            _activeParticipationByActorId.Clear();
        }

        public void RegisterDiscovered(NonPlayerActorIdentityRecord identity, NonPlayerActorEndpoint endpoint, GameObject actorInstance)
        {
            EnsureScopeOrFail(identity.Identity);
            if (endpoint == null || actorInstance == null)
            {
                throw new InvalidOperationException("NonPlayer actor registration requires endpoint and actor instance.");
            }

            NonPlayerActorRuntimeEntry entry = new(identity, endpoint, actorInstance, default);
            if (!entry.IsValid)
            {
                throw new InvalidOperationException("NonPlayer actor registration generated invalid runtime entry.");
            }

            if (_activeByActorId.ContainsKey(identity.NonPlayerActorId))
            {
                throw new InvalidOperationException($"Duplicate nonPlayerActorId discovered in same entry. nonPlayerActorId='{identity.NonPlayerActorId}'.");
            }

            _activeByActorId.Add(identity.NonPlayerActorId, entry);
            _routeRetainedByActorId[identity.NonPlayerActorId] = entry;
        }

        public bool TryGetActive(SessionActivityIdentity expectedScopeIdentity, string nonPlayerActorId, out NonPlayerActorRuntimeEntry entry)
        {
            entry = default;
            if (!_activeScopeIdentity.IsValid || !IsSameActivityCycle(_activeScopeIdentity, expectedScopeIdentity))
            {
                return false;
            }

            string normalized = Normalize(nonPlayerActorId);
            return !string.IsNullOrWhiteSpace(normalized) &&
                _activeByActorId.TryGetValue(normalized, out entry) &&
                entry.IsValid;
        }

        public bool TryGetRouteRetained(SessionActivityIdentity expectedIdentity, string nonPlayerActorId, out NonPlayerActorRuntimeEntry entry)
        {
            entry = default;
            string normalized = Normalize(nonPlayerActorId);
            return expectedIdentity.IsValid &&
                !string.IsNullOrWhiteSpace(normalized) &&
                _routeRetainedByActorId.TryGetValue(normalized, out entry) &&
                entry.IsValid &&
                IsSameSessionPipeline(entry.ActorIdentity.Identity, expectedIdentity);
        }

        public IReadOnlyList<NonPlayerActorRuntimeEntry> GetActiveEntries(SessionActivityIdentity expectedScopeIdentity)
        {
            EnsureScopeOrFail(expectedScopeIdentity);
            List<NonPlayerActorRuntimeEntry> entries = new(_activeByActorId.Count);
            foreach (KeyValuePair<string, NonPlayerActorRuntimeEntry> pair in _activeByActorId)
            {
                entries.Add(pair.Value);
            }

            return entries;
        }

        public void SetPresentationHandle(SessionActivityIdentity expectedScopeIdentity, string nonPlayerActorId, ActorPresentationRuntimeHandle handle)
        {
            EnsureScopeOrFail(expectedScopeIdentity);
            string normalized = Normalize(nonPlayerActorId);
            if (!_activeByActorId.TryGetValue(normalized, out NonPlayerActorRuntimeEntry current))
            {
                throw new InvalidOperationException($"Cannot set presentation handle for unknown nonPlayerActorId='{normalized}'.");
            }

            NonPlayerActorRuntimeEntry updated = new(current.ActorIdentity, current.Endpoint, current.ActorInstance, handle);
            _activeByActorId[normalized] = updated;
            _routeRetainedByActorId[normalized] = updated;
        }

        public void MarkParticipationEntered(SessionActivityIdentity expectedScopeIdentity, string nonPlayerActorId)
        {
            EnsureScopeOrFail(expectedScopeIdentity);
            string normalized = Normalize(nonPlayerActorId);
            if (!_activeByActorId.ContainsKey(normalized))
            {
                throw new InvalidOperationException($"Cannot mark participation for unknown nonPlayerActorId='{normalized}'.");
            }

            _activeParticipationByActorId.Add(normalized);
        }

        public void MarkParticipationExited(SessionActivityIdentity expectedScopeIdentity, string nonPlayerActorId)
        {
            EnsureScopeOrFail(expectedScopeIdentity);
            string normalized = Normalize(nonPlayerActorId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            _activeParticipationByActorId.Remove(normalized);
        }

        public bool IsParticipationActive(SessionActivityIdentity expectedScopeIdentity, string nonPlayerActorId)
        {
            EnsureScopeOrFail(expectedScopeIdentity);
            string normalized = Normalize(nonPlayerActorId);
            return !string.IsNullOrWhiteSpace(normalized) && _activeParticipationByActorId.Contains(normalized);
        }

        public void ClearPresentationHandle(SessionActivityIdentity expectedIdentity, string nonPlayerActorId)
        {
            string normalized = Normalize(nonPlayerActorId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            if (_activeByActorId.TryGetValue(normalized, out NonPlayerActorRuntimeEntry active) &&
                active.IsValid &&
                IsSameSessionPipeline(active.ActorIdentity.Identity, expectedIdentity))
            {
                _activeByActorId[normalized] = new NonPlayerActorRuntimeEntry(active.ActorIdentity, active.Endpoint, active.ActorInstance, default);
            }

            if (_routeRetainedByActorId.TryGetValue(normalized, out NonPlayerActorRuntimeEntry retained) &&
                retained.IsValid &&
                IsSameSessionPipeline(retained.ActorIdentity.Identity, expectedIdentity))
            {
                _routeRetainedByActorId[normalized] = new NonPlayerActorRuntimeEntry(retained.ActorIdentity, retained.Endpoint, retained.ActorInstance, default);
            }
        }

        public void RemoveFromActiveScope(SessionActivityIdentity expectedIdentity, string nonPlayerActorId)
        {
            EnsureScopeOrFail(expectedIdentity);
            string normalized = Normalize(nonPlayerActorId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            _activeByActorId.Remove(normalized);
            _activeParticipationByActorId.Remove(normalized);
        }

        public void ClearAllRouteRetained()
        {
            _activeByActorId.Clear();
            _routeRetainedByActorId.Clear();
            _activeParticipationByActorId.Clear();
            _activeScopeIdentity = default;
        }

        private void EnsureScopeOrFail(SessionActivityIdentity expectedScopeIdentity)
        {
            if (!_activeScopeIdentity.IsValid || !IsSameActivityCycle(_activeScopeIdentity, expectedScopeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_non_player_actor_scope: expected scope does not match current activity scope.");
            }
        }

        private static bool IsSameSessionPipeline(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return left.IsValid &&
                right.IsValid &&
                string.Equals(left.PipelineId, right.PipelineId, StringComparison.Ordinal) &&
                string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal);
        }

        private static bool IsSameActivityCycle(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return IsSameSessionPipeline(left, right) &&
                string.Equals(left.ActivityId, right.ActivityId, StringComparison.Ordinal) &&
                left.ActivityOrdinal == right.ActivityOrdinal &&
                left.EntrySequence == right.EntrySequence;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
