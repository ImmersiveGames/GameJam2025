using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    public readonly struct SessionActorRuntimeEntry
    {
        public SessionActorRuntimeEntry(
            SessionActivityIdentity identity,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            ActorId actorId,
            ActorScope actorScope,
            SessionParticipantId participantId,
            GameObject instance,
            Actor actor)
        {
            Identity = identity;
            ActorInstanceRuntimeId = actorInstanceRuntimeId;
            ActorId = actorId;
            ActorScope = actorScope;
            ParticipantId = participantId;
            Instance = instance;
            Actor = actor;
        }

        public SessionActivityIdentity Identity { get; }
        public ActorInstanceRuntimeId ActorInstanceRuntimeId { get; }
        public ActorId ActorId { get; }
        public ActorScope ActorScope { get; }
        public SessionParticipantId ParticipantId { get; }
        public GameObject Instance { get; }
        public Actor Actor { get; }
        public bool HasParticipantId => ParticipantId.IsValid;
        public bool IsValid =>
            Identity.IsValid &&
            ActorInstanceRuntimeId.IsValid &&
            ActorId.IsValid &&
            ActorScope == ActorScope.SessionScoped &&
            Instance != null &&
            Actor != null;
    }

    public sealed class SessionActorRuntimeStore
    {
        private readonly Dictionary<ActorInstanceRuntimeId, SessionActorRuntimeEntry> _entriesByRuntimeId = new();
        private readonly Dictionary<ActorId, ActorInstanceRuntimeId> _runtimeIdByActorId = new();
        private readonly Dictionary<SessionParticipantId, ActorInstanceRuntimeId> _runtimeIdByParticipantId = new();

        public int Count => _entriesByRuntimeId.Count;

        public void Register(SessionActorRuntimeEntry entry)
        {
            if (!entry.IsValid)
            {
                throw new InvalidOperationException("SessionActorRuntimeStore cannot register invalid entry.");
            }

            if (_entriesByRuntimeId.TryGetValue(entry.ActorInstanceRuntimeId, out var existing) &&
                existing.IsValid &&
                existing.Instance != entry.Instance)
            {
                throw new InvalidOperationException($"Duplicate SessionScoped actor runtime id detected. actorInstanceRuntimeId='{entry.ActorInstanceRuntimeId}'.");
            }

            if (_runtimeIdByActorId.TryGetValue(entry.ActorId, out var existingActorRuntimeId) &&
                existingActorRuntimeId.IsValid &&
                existingActorRuntimeId != entry.ActorInstanceRuntimeId)
            {
                throw new InvalidOperationException($"Duplicate SessionScoped actorId detected. actorId='{entry.ActorId}'.");
            }

            if (entry.HasParticipantId &&
                _runtimeIdByParticipantId.TryGetValue(entry.ParticipantId, out var existingParticipantRuntimeId) &&
                existingParticipantRuntimeId.IsValid &&
                existingParticipantRuntimeId != entry.ActorInstanceRuntimeId)
            {
                throw new InvalidOperationException($"Duplicate SessionScoped participant binding detected. participantId='{entry.ParticipantId}'.");
            }

            _entriesByRuntimeId[entry.ActorInstanceRuntimeId] = entry;
            _runtimeIdByActorId[entry.ActorId] = entry.ActorInstanceRuntimeId;
            if (entry.HasParticipantId)
            {
                _runtimeIdByParticipantId[entry.ParticipantId] = entry.ActorInstanceRuntimeId;
            }
        }

        public bool TryGetByRuntimeId(SessionActivityIdentity identity, ActorInstanceRuntimeId actorInstanceRuntimeId, out SessionActorRuntimeEntry entry)
        {
            entry = default;
            return identity.IsValid &&
                actorInstanceRuntimeId.IsValid &&
                _entriesByRuntimeId.TryGetValue(actorInstanceRuntimeId, out entry) &&
                entry.IsValid &&
                IsSameSessionPipeline(entry.Identity, identity);
        }

        public bool TryGetByActorId(SessionActivityIdentity identity, ActorId actorId, out SessionActorRuntimeEntry entry)
        {
            entry = default;
            return identity.IsValid &&
                actorId.IsValid &&
                _runtimeIdByActorId.TryGetValue(actorId, out var runtimeId) &&
                TryGetByRuntimeId(identity, runtimeId, out entry);
        }

        public bool TryGetByParticipantId(SessionActivityIdentity identity, SessionParticipantId participantId, out SessionActorRuntimeEntry entry)
        {
            entry = default;
            return identity.IsValid &&
                participantId.IsValid &&
                _runtimeIdByParticipantId.TryGetValue(participantId, out var runtimeId) &&
                TryGetByRuntimeId(identity, runtimeId, out entry);
        }

        public IReadOnlyList<SessionActorRuntimeEntry> GetEntriesForSession(SessionActivityIdentity identity)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("SessionActorRuntimeStore requires valid identity.");
            }

            List<SessionActorRuntimeEntry> entries = new(_entriesByRuntimeId.Count);
            foreach (var entry in _entriesByRuntimeId.Values)
            {
                if (entry.IsValid && IsSameSessionPipeline(entry.Identity, identity))
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        public IReadOnlyList<SessionActorRuntimeEntry> GetAllEntries()
        {
            List<SessionActorRuntimeEntry> entries = new(_entriesByRuntimeId.Count);
            foreach (var entry in _entriesByRuntimeId.Values)
            {
                if (entry.IsValid)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        public void Remove(ActorInstanceRuntimeId actorInstanceRuntimeId)
        {
            if (!actorInstanceRuntimeId.IsValid ||
                !_entriesByRuntimeId.TryGetValue(actorInstanceRuntimeId, out var existing))
            {
                return;
            }

            _entriesByRuntimeId.Remove(actorInstanceRuntimeId);
            if (_runtimeIdByActorId.TryGetValue(existing.ActorId, out var actorRuntimeId) &&
                actorRuntimeId == actorInstanceRuntimeId)
            {
                _runtimeIdByActorId.Remove(existing.ActorId);
            }

            if (existing.HasParticipantId &&
                _runtimeIdByParticipantId.TryGetValue(existing.ParticipantId, out var participantRuntimeId) &&
                participantRuntimeId == actorInstanceRuntimeId)
            {
                _runtimeIdByParticipantId.Remove(existing.ParticipantId);
            }
        }

        public void Clear()
        {
            _entriesByRuntimeId.Clear();
            _runtimeIdByActorId.Clear();
            _runtimeIdByParticipantId.Clear();
        }

        public static SessionActorRuntimeEntry FromPlayerHandle(PlayerActorRuntimeHandle handle)
        {
            if (!handle.IsValid)
            {
                return default;
            }

            var runtimeActor = handle.Instance != null ? handle.Instance.GetComponent<Actor>() : null;
            if (runtimeActor == null ||
                runtimeActor.ActorScopeMetadata != ActorScope.SessionScoped ||
                !runtimeActor.RuntimeActorInstanceId.IsValid)
            {
                return default;
            }

            return new SessionActorRuntimeEntry(
                handle.ActorIdentity.Identity,
                runtimeActor.RuntimeActorInstanceId,
                new ActorId(runtimeActor.ActorId),
                runtimeActor.ActorScopeMetadata,
                handle.ParticipantId,
                handle.Instance,
                runtimeActor);
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
