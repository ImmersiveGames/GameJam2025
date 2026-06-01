using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    /// <summary>
    /// Índice técnico de Actors scene-authored descobertos para a entry atual.
    /// Não decide lifecycle; apenas guarda runtime entries resolvidas pelo ActorSceneDiscoveryStage.
    /// </summary>
    public sealed class ActivitySceneActorRegistry
    {
        private readonly Dictionary<ActorInstanceId, SceneAuthoredActorRuntimeEntry> _activeByActorInstanceId = new();
        private readonly Dictionary<string, ActorInstanceId> _activeActorInstanceIdByActorId = new(StringComparer.Ordinal);
        private readonly Dictionary<ActorInstanceId, SceneAuthoredActorRuntimeEntry> _routeRetainedByActorInstanceId = new();
        private readonly Dictionary<string, ActorInstanceId> _routeRetainedActorInstanceIdByActorId = new(StringComparer.Ordinal);
        private SessionActivityIdentity _activeScopeIdentity;

        public void BeginActivityScope(SessionActivityIdentity identity)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("ActivitySceneActorRegistry requires valid scope identity.");
            }

            _activeScopeIdentity = identity;
            _activeByActorInstanceId.Clear();
            _activeActorInstanceIdByActorId.Clear();
        }

        public void ClearAllRouteRetained()
        {
            _activeByActorInstanceId.Clear();
            _activeActorInstanceIdByActorId.Clear();
            _routeRetainedByActorInstanceId.Clear();
            _routeRetainedActorInstanceIdByActorId.Clear();
            _activeScopeIdentity = default;
        }

        public void RegisterDiscovered(
            SceneAuthoredActorIdentityRecord identity,
            Actor actor,
            GameObject actorInstance)
        {
            EnsureScopeOrFail(identity.Identity);
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("Actor scene registration requires valid actor identity.");
            }

            if (actor == null || actorInstance == null)
            {
                throw new InvalidOperationException("Actor scene registration requires actor and actor instance.");
            }

            SceneAuthoredActorRuntimeEntry entry = new(identity, actor, actorInstance, default);
            if (!entry.IsValid)
            {
                throw new InvalidOperationException("Actor scene registration generated invalid runtime entry.");
            }

            if (_activeByActorInstanceId.ContainsKey(identity.ActorInstanceId) ||
                _activeActorInstanceIdByActorId.ContainsKey(identity.ActorId))
            {
                throw new InvalidOperationException($"Duplicate Actor discovered in same entry across sources/scopes. actorId='{identity.ActorId}' actorInstanceId='{identity.ActorInstanceId}'.");
            }

            _activeByActorInstanceId.Add(identity.ActorInstanceId, entry);
            _activeActorInstanceIdByActorId.Add(identity.ActorId, identity.ActorInstanceId);
            if (identity.ActorScope == ActorScope.RouteScoped)
            {
                _routeRetainedByActorInstanceId[identity.ActorInstanceId] = entry;
                _routeRetainedActorInstanceIdByActorId[identity.ActorId] = identity.ActorInstanceId;
            }
        }

        public IReadOnlyList<SceneAuthoredActorRuntimeEntry> GetActiveEntries(SessionActivityIdentity identity)
        {
            EnsureScopeOrFail(identity);
            List<SceneAuthoredActorRuntimeEntry> entries = new(_activeByActorInstanceId.Count);
            foreach (KeyValuePair<ActorInstanceId, SceneAuthoredActorRuntimeEntry> pair in _activeByActorInstanceId)
            {
                entries.Add(pair.Value);
            }

            return entries;
        }

        public bool TryGetActive(
            SessionActivityIdentity identity,
            string actorId,
            out SceneAuthoredActorRuntimeEntry entry)
        {
            entry = default;
            if (!_activeScopeIdentity.IsValid || !IsSameActivityCycle(_activeScopeIdentity, identity))
            {
                return false;
            }

            string normalized = Normalize(actorId);
            return !string.IsNullOrWhiteSpace(normalized) &&
                _activeActorInstanceIdByActorId.TryGetValue(normalized, out ActorInstanceId actorInstanceId) &&
                _activeByActorInstanceId.TryGetValue(actorInstanceId, out entry) &&
                entry.IsValid;
        }

        public bool TryGetRouteRetained(
            SessionActivityIdentity identity,
            string actorId,
            out SceneAuthoredActorRuntimeEntry entry)
        {
            entry = default;
            string normalized = Normalize(actorId);
            return identity.IsValid &&
                !string.IsNullOrWhiteSpace(normalized) &&
                _routeRetainedActorInstanceIdByActorId.TryGetValue(normalized, out ActorInstanceId actorInstanceId) &&
                _routeRetainedByActorInstanceId.TryGetValue(actorInstanceId, out entry) &&
                entry.IsValid &&
                IsSameSessionPipeline(entry.ActorIdentity.Identity, identity);
        }

        public void SetPresentationHandle(
            SessionActivityIdentity identity,
            string actorId,
            ActorPresentationRuntimeHandle handle)
        {
            EnsureScopeOrFail(identity);
            string normalized = Normalize(actorId);
            if (!_activeActorInstanceIdByActorId.TryGetValue(normalized, out ActorInstanceId actorInstanceId) ||
                !_activeByActorInstanceId.TryGetValue(actorInstanceId, out SceneAuthoredActorRuntimeEntry current))
            {
                throw new InvalidOperationException($"Cannot set presentation handle for unknown actorId='{normalized}'.");
            }

            SceneAuthoredActorRuntimeEntry updated = new(current.ActorIdentity, current.Actor, current.ActorInstance, handle);
            _activeByActorInstanceId[actorInstanceId] = updated;
            if (current.ActorIdentity.ActorScope == ActorScope.RouteScoped)
            {
                _routeRetainedByActorInstanceId[actorInstanceId] = updated;
                _routeRetainedActorInstanceIdByActorId[current.ActorIdentity.ActorId] = actorInstanceId;
            }
        }

        public void ClearPresentationHandle(SessionActivityIdentity identity, string actorId)
        {
            string normalized = Normalize(actorId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            if (_activeActorInstanceIdByActorId.TryGetValue(normalized, out ActorInstanceId activeActorInstanceId) &&
                _activeByActorInstanceId.TryGetValue(activeActorInstanceId, out SceneAuthoredActorRuntimeEntry active) &&
                active.IsValid &&
                IsSameSessionPipeline(active.ActorIdentity.Identity, identity))
            {
                _activeByActorInstanceId[activeActorInstanceId] = new SceneAuthoredActorRuntimeEntry(active.ActorIdentity, active.Actor, active.ActorInstance, default);
            }

            if (_routeRetainedActorInstanceIdByActorId.TryGetValue(normalized, out ActorInstanceId retainedActorInstanceId) &&
                _routeRetainedByActorInstanceId.TryGetValue(retainedActorInstanceId, out SceneAuthoredActorRuntimeEntry retained) &&
                retained.IsValid &&
                IsSameSessionPipeline(retained.ActorIdentity.Identity, identity))
            {
                _routeRetainedByActorInstanceId[retainedActorInstanceId] = new SceneAuthoredActorRuntimeEntry(retained.ActorIdentity, retained.Actor, retained.ActorInstance, default);
            }
        }

        public bool TryGetIdentity(SessionActivityIdentity identity, string actorId, out SceneAuthoredActorIdentityRecord actorIdentity)
        {
            actorIdentity = default;
            if (!TryGetActive(identity, actorId, out SceneAuthoredActorRuntimeEntry entry))
            {
                return false;
            }

            actorIdentity = entry.ActorIdentity;
            return actorIdentity.IsValid;
        }

        public void RemoveFromActiveScope(SessionActivityIdentity identity, string actorId)
        {
            EnsureScopeOrFail(identity);
            string normalized = Normalize(actorId);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            if (_activeActorInstanceIdByActorId.TryGetValue(normalized, out ActorInstanceId actorInstanceId))
            {
                _activeByActorInstanceId.Remove(actorInstanceId);
                _activeActorInstanceIdByActorId.Remove(normalized);
            }
        }

        private void EnsureScopeOrFail(SessionActivityIdentity expectedScopeIdentity)
        {
            if (!_activeScopeIdentity.IsValid || !IsSameActivityCycle(_activeScopeIdentity, expectedScopeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_actor_scene_scope: expected scope does not match current activity scope.");
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
