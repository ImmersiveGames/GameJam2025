using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
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
        private readonly Dictionary<ActorInstanceRuntimeId, SceneAuthoredActorRuntimeEntry> _activeByActorInstanceRuntimeId = new();
        private readonly Dictionary<string, ActorInstanceRuntimeId> _activeActorInstanceRuntimeIdByActorId = new(StringComparer.Ordinal);
        private SessionActivityIdentity _activeScopeIdentity;

        public void BeginActivityScope(SessionActivityIdentity identity)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("ActivitySceneActorRegistry requires valid scope identity.");
            }

            _activeScopeIdentity = identity;
            _activeByActorInstanceRuntimeId.Clear();
            _activeActorInstanceRuntimeIdByActorId.Clear();
        }

        public void ClearAllRouteRetained()
        {
            _activeByActorInstanceRuntimeId.Clear();
            _activeActorInstanceRuntimeIdByActorId.Clear();
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

            SceneAuthoredActorRuntimeEntry entry = new(identity, actor, actorInstance);
            if (!entry.IsValid)
            {
                throw new InvalidOperationException("Actor scene registration generated invalid runtime entry.");
            }

            if (_activeByActorInstanceRuntimeId.ContainsKey(identity.ActorInstanceRuntimeId) ||
                _activeActorInstanceRuntimeIdByActorId.ContainsKey(identity.ActorId))
            {
                throw new InvalidOperationException($"Duplicate Actor discovered in same entry across sources/scopes. actorId='{identity.ActorId}' actorInstanceRuntimeId='{identity.ActorInstanceRuntimeId}'.");
            }

            _activeByActorInstanceRuntimeId.Add(identity.ActorInstanceRuntimeId, entry);
            _activeActorInstanceRuntimeIdByActorId.Add(identity.ActorId, identity.ActorInstanceRuntimeId);
        }

        public IReadOnlyList<SceneAuthoredActorRuntimeEntry> GetActiveEntries(SessionActivityIdentity identity)
        {
            EnsureScopeOrFail(identity);
            List<SceneAuthoredActorRuntimeEntry> entries = new(_activeByActorInstanceRuntimeId.Count);
            foreach (KeyValuePair<ActorInstanceRuntimeId, SceneAuthoredActorRuntimeEntry> pair in _activeByActorInstanceRuntimeId)
            {
                entries.Add(pair.Value);
            }

            return entries;
        }

        private void EnsureScopeOrFail(SessionActivityIdentity expectedScopeIdentity)
        {
            if (!_activeScopeIdentity.IsValid || !IsSameActivityCycle(_activeScopeIdentity, expectedScopeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_actor_scene_scope: expected scope does not match current activity scope.");
            }
        }

        private static bool IsSameActivityCycle(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return IsSameSessionPipeline(left, right) &&
                string.Equals(left.ActivityId, right.ActivityId, StringComparison.Ordinal) &&
                left.ActivityOrdinal == right.ActivityOrdinal &&
                left.EntrySequence == right.EntrySequence;
        }

        private static bool IsSameSessionPipeline(SessionActivityIdentity left, SessionActivityIdentity right)
        {
            return left.IsValid &&
                right.IsValid &&
                string.Equals(left.PipelineId, right.PipelineId, StringComparison.Ordinal) &&
                string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal);
        }

    }
}
