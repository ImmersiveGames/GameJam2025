using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class ActivityPlayerActorRegistry
    {
        private readonly Dictionary<SessionParticipantId, PlayerActorRuntimeHandle> _routeHandlesByParticipantId = new();
        private readonly Dictionary<SessionParticipantId, PlayerActorRuntimeHandle> _activeHandlesByParticipantId = new();
        private SessionActivityIdentity _activeScopeIdentity;

        public SessionActivityIdentity ActiveScopeIdentity => _activeScopeIdentity;

        public void BeginActivityScope(SessionActivityIdentity scopeIdentity)
        {
            if (!scopeIdentity.IsValid)
            {
                throw new InvalidOperationException("ActivityPlayerActorRegistry requires valid activity scope identity.");
            }

            _activeScopeIdentity = scopeIdentity;
            if (_activeHandlesByParticipantId.Count == 0)
            {
                return;
            }

            List<KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle>> retained = new(_activeHandlesByParticipantId.Count);
            foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _activeHandlesByParticipantId)
            {
                PlayerActorRuntimeHandle handle = pair.Value;
                if (handle.IsValid &&
                    handle.Actor != null &&
                    handle.Actor.ActorScopeMetadata == ActorScope.SessionScoped)
                {
                    retained.Add(pair);
                }
            }

            _activeHandlesByParticipantId.Clear();
            for (int index = 0; index < retained.Count; index++)
            {
                KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair = retained[index];
                _activeHandlesByParticipantId[pair.Key] = pair.Value;
            }
        }

        public void RegisterMaterialized(PlayerActorRuntimeHandle handle)
        {
            EnsureActiveScopeOrFail();
            if (!handle.IsValid)
            {
                throw new InvalidOperationException("Cannot register invalid player actor runtime handle.");
            }

            PlayerActorIdentityRecord actorIdentity = handle.ActorIdentity;
            EnsureIdentityMatchesActiveScopeOrFail(actorIdentity.Identity, "stale_or_foreign_player_actor_registration", handle.Actor.ActorScopeMetadata);

            if (_activeHandlesByParticipantId.ContainsKey(actorIdentity.ParticipantId))
            {
                throw new InvalidOperationException($"Duplicate player participant registration detected. participantId='{actorIdentity.ParticipantId}'.");
            }

            if (_routeHandlesByParticipantId.TryGetValue(actorIdentity.ParticipantId, out PlayerActorRuntimeHandle existing) &&
                existing.IsValid &&
                existing.Instance != handle.Instance)
            {
                throw new InvalidOperationException($"Incompatible retained player actor for participantId='{actorIdentity.ParticipantId}'.");
            }

            _activeHandlesByParticipantId.Add(actorIdentity.ParticipantId, handle);
            if (handle.Actor != null &&
                handle.Actor.ActorScopeMetadata == ActorScope.RouteScoped)
            {
                _routeHandlesByParticipantId[actorIdentity.ParticipantId] = handle;
            }
        }

        public bool TryGetRetainedForParticipant(SessionActivityIdentity scopeIdentity, SessionParticipantId participantId, out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!participantId.IsValid)
            {
                throw new InvalidOperationException("participantId is required.");
            }

            if (!_routeHandlesByParticipantId.TryGetValue(participantId, out PlayerActorRuntimeHandle retained) || !retained.IsValid)
            {
                return false;
            }

            if (!IsScopeCompatible(retained.ActorIdentity.Identity, scopeIdentity, retained.Actor.ActorScopeMetadata))
            {
                throw new InvalidOperationException($"stale_or_foreign_retained_player_actor: participantId='{participantId}'.");
            }

            handle = retained;
            return true;
        }

        public void RegisterRetainedParticipation(SessionActivityIdentity scopeIdentity, PlayerActorRuntimeHandle handle)
        {
            if (!handle.IsValid)
            {
                throw new InvalidOperationException("Cannot register retained invalid player actor runtime handle.");
            }

            PlayerActorIdentityRecord actorIdentity = handle.ActorIdentity;
            EnsureScopeMatchesOrFail(scopeIdentity, handle.Actor.ActorScopeMetadata);
            EnsureIdentityMatchesActiveScopeOrFail(actorIdentity.Identity, "stale_or_foreign_player_actor_reenter_registration", handle.Actor.ActorScopeMetadata);
            _activeHandlesByParticipantId[actorIdentity.ParticipantId] = handle;
            if (handle.Actor != null &&
                handle.Actor.ActorScopeMetadata == ActorScope.RouteScoped)
            {
                _routeHandlesByParticipantId[actorIdentity.ParticipantId] = handle;
            }
        }

        public IReadOnlyList<PlayerActorIdentityRecord> GetActiveActorIdentitiesOrFail(SessionActivityIdentity expectedScopeIdentity)
        {
            EnsureScopeMatchesOrFail(expectedScopeIdentity, ActorScope.SessionScoped);
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
            if (!participantId.IsValid)
            {
                throw new InvalidOperationException("participantId is required.");
            }

            if (!_activeHandlesByParticipantId.TryGetValue(participantId, out PlayerActorRuntimeHandle handle) || !handle.IsValid)
            {
                throw new InvalidOperationException($"Active PlayerActor handle not found in registry. participantId='{participantId}'.");
            }

            EnsureScopeCompatibleOrFail(handle.ActorIdentity.Identity, expectedScopeIdentity, handle.Actor.ActorScopeMetadata, "stale_or_foreign_player_actor_scope");
            return handle;
        }

        public bool TryResolveHandleForParticipant(SessionActivityIdentity expectedIdentity, SessionParticipantId participantId, out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!expectedIdentity.IsValid || !participantId.IsValid)
            {
                return false;
            }

            if (_activeHandlesByParticipantId.TryGetValue(participantId, out PlayerActorRuntimeHandle activeHandle) &&
                activeHandle.IsValid &&
                IsScopeCompatible(activeHandle.ActorIdentity.Identity, expectedIdentity, activeHandle.Actor.ActorScopeMetadata))
            {
                handle = activeHandle;
                return true;
            }

            if (_routeHandlesByParticipantId.TryGetValue(participantId, out PlayerActorRuntimeHandle retained) &&
                retained.IsValid &&
                IsScopeCompatible(retained.ActorIdentity.Identity, expectedIdentity, retained.Actor.ActorScopeMetadata))
            {
                handle = retained;
                return true;
            }

            return false;
        }

        public bool TryResolveHandleForActorInstance(SessionActivityIdentity expectedIdentity, ActorInstanceRuntimeId actorInstanceRuntimeId, out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!expectedIdentity.IsValid || !actorInstanceRuntimeId.IsValid)
            {
                return false;
            }

            if (_activeScopeIdentity.IsValid)
            {
                foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _activeHandlesByParticipantId)
                {
                    PlayerActorRuntimeHandle active = pair.Value;
                    if (active.IsValid &&
                        active.ActorInstanceRuntimeId == actorInstanceRuntimeId &&
                        IsScopeCompatible(active.ActorIdentity.Identity, expectedIdentity, active.Actor.ActorScopeMetadata))
                    {
                        handle = active;
                        return true;
                    }
                }
            }

            foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _routeHandlesByParticipantId)
            {
                PlayerActorRuntimeHandle retained = pair.Value;
                if (!retained.IsValid ||
                    retained.ActorInstanceRuntimeId != actorInstanceRuntimeId ||
                    !IsScopeCompatible(retained.ActorIdentity.Identity, expectedIdentity, retained.Actor.ActorScopeMetadata))
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

        public IReadOnlyList<PlayerActorRuntimeHandle> GetIndexedRouteScopedHandles()
        {
            List<PlayerActorRuntimeHandle> handles = new(_routeHandlesByParticipantId.Count);
            foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _routeHandlesByParticipantId)
            {
                if (pair.Value.IsValid)
                {
                    handles.Add(pair.Value);
                }
            }

            return handles;
        }

        public void ClearAllRouteScopedIndexes()
        {
            _routeHandlesByParticipantId.Clear();
            _activeHandlesByParticipantId.Clear();
            _activeScopeIdentity = default;
        }

        private void EnsureActiveScopeOrFail()
        {
            if (!_activeScopeIdentity.IsValid)
            {
                throw new InvalidOperationException("ActivityPlayerActorRegistry active scope is not initialized.");
            }
        }

        private void EnsureScopeMatchesOrFail(SessionActivityIdentity expectedScopeIdentity, ActorScope actorScope)
        {
            EnsureActiveScopeOrFail();
            if (!IsScopeCompatible(_activeScopeIdentity, expectedScopeIdentity, actorScope))
            {
                throw new InvalidOperationException("stale_or_foreign_player_actor_scope: expected scope does not match current activity scope.");
            }
        }

        private void EnsureIdentityMatchesActiveScopeOrFail(SessionActivityIdentity identity, string errorToken, ActorScope actorScope)
        {
            if (!IsScopeCompatible(_activeScopeIdentity, identity, actorScope))
            {
                throw new InvalidOperationException($"{errorToken}: actor identity does not match activity scope.");
            }
        }

        private static void EnsureScopeCompatibleOrFail(
            SessionActivityIdentity left,
            SessionActivityIdentity right,
            ActorScope actorScope,
            string errorToken)
        {
            if (!IsScopeCompatible(left, right, actorScope))
            {
                throw new InvalidOperationException(errorToken);
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

        private static bool IsScopeCompatible(SessionActivityIdentity left, SessionActivityIdentity right, ActorScope actorScope)
        {
            return actorScope == ActorScope.SessionScoped ||
                   actorScope == ActorScope.RouteScoped
                ? IsSameSessionPipeline(left, right)
                : IsSameActivityCycle(left, right);
        }
    }
}
