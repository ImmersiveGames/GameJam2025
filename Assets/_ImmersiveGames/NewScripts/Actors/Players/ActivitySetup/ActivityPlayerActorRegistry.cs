using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class ActivityPlayerActorRegistry
    {
        private readonly Dictionary<SessionParticipantId, PlayerActorRuntimeHandle> _routeHandlesByParticipantId = new();
        private readonly Dictionary<SessionParticipantId, PlayerActorRuntimeHandle> _activeHandlesByParticipantId = new();
        private readonly Dictionary<PlayerActorId, PlayerActorRuntimeHandle> _activeHandlesByPlayerActorId = new();
        private SessionActivityIdentity _activeScopeIdentity;

        public SessionActivityIdentity ActiveScopeIdentity => _activeScopeIdentity;

        public void BeginActivityScope(SessionActivityIdentity scopeIdentity)
        {
            if (!scopeIdentity.IsValid)
            {
                throw new InvalidOperationException("ActivityPlayerActorRegistry requires valid activity scope identity.");
            }

            _activeScopeIdentity = scopeIdentity;
            _activeHandlesByParticipantId.Clear();
            _activeHandlesByPlayerActorId.Clear();
        }

        public void RegisterMaterialized(PlayerActorRuntimeHandle handle)
        {
            EnsureActiveScopeOrFail();
            if (!handle.IsValid)
            {
                throw new InvalidOperationException("Cannot register invalid player actor runtime handle.");
            }

            PlayerActorIdentityRecord actorIdentity = handle.ActorIdentity;
            EnsureIdentityMatchesActiveScopeOrFail(actorIdentity.Identity, "stale_or_foreign_player_actor_registration");

            if (_activeHandlesByParticipantId.ContainsKey(actorIdentity.ParticipantId))
            {
                throw new InvalidOperationException($"Duplicate player participant registration detected. participantId='{actorIdentity.ParticipantId}'.");
            }

            if (_activeHandlesByPlayerActorId.ContainsKey(actorIdentity.PlayerActorId))
            {
                throw new InvalidOperationException($"Duplicate playerActorId registration detected. playerActorId='{actorIdentity.PlayerActorId}'.");
            }

            if (_routeHandlesByParticipantId.TryGetValue(actorIdentity.ParticipantId, out PlayerActorRuntimeHandle existing) &&
                existing.IsValid &&
                existing.Instance != handle.Instance)
            {
                throw new InvalidOperationException($"Incompatible retained player actor for participantId='{actorIdentity.ParticipantId}'.");
            }

            _activeHandlesByParticipantId.Add(actorIdentity.ParticipantId, handle);
            _activeHandlesByPlayerActorId.Add(actorIdentity.PlayerActorId, handle);
            _routeHandlesByParticipantId[actorIdentity.ParticipantId] = handle;
        }

        public bool TryGetRetainedForParticipant(SessionActivityIdentity scopeIdentity, SessionParticipantId participantId, out PlayerActorRuntimeHandle handle)
        {
            EnsureScopeMatchesOrFail(scopeIdentity);
            handle = default;
            if (!participantId.IsValid)
            {
                throw new InvalidOperationException("participantId is required.");
            }

            if (!_routeHandlesByParticipantId.TryGetValue(participantId, out PlayerActorRuntimeHandle retained) || !retained.IsValid)
            {
                return false;
            }

            if (!IsSameSessionPipeline(retained.ActorIdentity.Identity, scopeIdentity))
            {
                throw new InvalidOperationException($"stale_or_foreign_retained_player_actor: participantId='{participantId}'.");
            }

            handle = retained;
            return true;
        }

        public void RegisterRetainedParticipation(SessionActivityIdentity scopeIdentity, PlayerActorRuntimeHandle handle)
        {
            EnsureScopeMatchesOrFail(scopeIdentity);
            if (!handle.IsValid)
            {
                throw new InvalidOperationException("Cannot register retained invalid player actor runtime handle.");
            }

            PlayerActorIdentityRecord actorIdentity = handle.ActorIdentity;
            EnsureIdentityMatchesActiveScopeOrFail(actorIdentity.Identity, "stale_or_foreign_player_actor_reenter_registration");
            _activeHandlesByParticipantId[actorIdentity.ParticipantId] = handle;
            _activeHandlesByPlayerActorId[actorIdentity.PlayerActorId] = handle;
            _routeHandlesByParticipantId[actorIdentity.ParticipantId] = handle;
        }

        public IReadOnlyList<PlayerActorIdentityRecord> GetActiveActorIdentitiesOrFail(SessionActivityIdentity expectedScopeIdentity)
        {
            EnsureScopeMatchesOrFail(expectedScopeIdentity);
            List<PlayerActorIdentityRecord> records = new(_activeHandlesByParticipantId.Count);
            foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _activeHandlesByParticipantId)
            {
                if (pair.Value.IsValid)
                {
                    records.Add(pair.Value.ActorIdentity);
                }
            }

            return records;
        }

        public bool TryGetActiveActorIdentities(SessionActivityIdentity expectedScopeIdentity, out IReadOnlyList<PlayerActorIdentityRecord> records)
        {
            records = Array.Empty<PlayerActorIdentityRecord>();
            if (!_activeScopeIdentity.IsValid || !IsSameActivityCycle(_activeScopeIdentity, expectedScopeIdentity))
            {
                return false;
            }

            List<PlayerActorIdentityRecord> active = new(_activeHandlesByParticipantId.Count);
            foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _activeHandlesByParticipantId)
            {
                if (pair.Value.IsValid)
                {
                    active.Add(pair.Value.ActorIdentity);
                }
            }

            records = active;
            return true;
        }

        public PlayerActorRuntimeHandle ResolveActiveHandleOrFail(SessionActivityIdentity expectedScopeIdentity, SessionParticipantId participantId)
        {
            EnsureScopeMatchesOrFail(expectedScopeIdentity);
            if (!participantId.IsValid)
            {
                throw new InvalidOperationException("participantId is required.");
            }

            if (!_activeHandlesByParticipantId.TryGetValue(participantId, out PlayerActorRuntimeHandle handle) || !handle.IsValid)
            {
                throw new InvalidOperationException($"Active PlayerActor handle not found in registry. participantId='{participantId}'.");
            }

            return handle;
        }

        public PlayerActorRuntimeHandle ResolveActiveHandleOrFail(SessionActivityIdentity expectedScopeIdentity, PlayerActorId playerActorId)
        {
            EnsureScopeMatchesOrFail(expectedScopeIdentity);
            if (!playerActorId.IsValid)
            {
                throw new InvalidOperationException("playerActorId is required.");
            }

            if (!_activeHandlesByPlayerActorId.TryGetValue(playerActorId, out PlayerActorRuntimeHandle handle) || !handle.IsValid)
            {
                throw new InvalidOperationException($"Active PlayerActor handle not found in registry. playerActorId='{playerActorId}'.");
            }

            return handle;
        }

        public bool TryResolveHandleForControl(SessionActivityIdentity expectedIdentity, PlayerActorId playerActorId, out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!expectedIdentity.IsValid || !playerActorId.IsValid)
            {
                return false;
            }

            if (_activeScopeIdentity.IsValid &&
                IsSameActivityCycle(_activeScopeIdentity, expectedIdentity) &&
                _activeHandlesByPlayerActorId.TryGetValue(playerActorId, out PlayerActorRuntimeHandle activeHandle) &&
                activeHandle.IsValid)
            {
                handle = activeHandle;
                return true;
            }

            foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _routeHandlesByParticipantId)
            {
                PlayerActorRuntimeHandle retained = pair.Value;
                if (!retained.IsValid || retained.PlayerActorId != playerActorId)
                {
                    continue;
                }

                if (!IsSameSessionPipeline(retained.ActorIdentity.Identity, expectedIdentity))
                {
                    continue;
                }

                handle = retained;
                return true;
            }

            return false;
        }

        public IReadOnlyList<PlayerActorIdentityRecord> GetRouteRetainedActorIdentitiesForSession(SessionActivityIdentity expectedIdentity)
        {
            if (!expectedIdentity.IsValid)
            {
                throw new InvalidOperationException("expectedIdentity is invalid.");
            }

            List<PlayerActorIdentityRecord> records = new();
            foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _routeHandlesByParticipantId)
            {
                PlayerActorRuntimeHandle retained = pair.Value;
                if (!retained.IsValid || !IsSameSessionPipeline(retained.ActorIdentity.Identity, expectedIdentity))
                {
                    continue;
                }

                records.Add(retained.ActorIdentity);
            }

            return records;
        }

        public void ClearAllRouteRetained()
        {
            foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _routeHandlesByParticipantId)
            {
                if (pair.Value.Instance != null)
                {
                    UnityEngine.Object.Destroy(pair.Value.Instance);
                }
            }

            _routeHandlesByParticipantId.Clear();
            _activeHandlesByParticipantId.Clear();
            _activeHandlesByPlayerActorId.Clear();
            _activeScopeIdentity = default;
        }

        private void EnsureActiveScopeOrFail()
        {
            if (!_activeScopeIdentity.IsValid)
            {
                throw new InvalidOperationException("ActivityPlayerActorRegistry active scope is not initialized.");
            }
        }

        private void EnsureScopeMatchesOrFail(SessionActivityIdentity expectedScopeIdentity)
        {
            EnsureActiveScopeOrFail();
            if (!IsSameActivityCycle(_activeScopeIdentity, expectedScopeIdentity))
            {
                throw new InvalidOperationException("stale_or_foreign_player_actor_scope: expected scope does not match current activity scope.");
            }
        }

        private void EnsureIdentityMatchesActiveScopeOrFail(SessionActivityIdentity identity, string errorToken)
        {
            if (!IsSameActivityCycle(_activeScopeIdentity, identity))
            {
                throw new InvalidOperationException($"{errorToken}: actor identity does not match activity scope.");
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
    }
}
