using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Foundation;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.PlayerParticipation.Contracts;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Actors.Players.ActivitySetup
{
    public sealed class ActivityPlayerActorRegistry
    {
        private static readonly HashSet<string> LookupResolvedLogKeys = new(StringComparer.Ordinal);
        private readonly Dictionary<SessionParticipantId, PlayerActorRuntimeHandle> _routeHandlesByParticipantId = new();
        private readonly Dictionary<SessionParticipantId, PlayerActorRuntimeHandle> _activeHandlesByParticipantId = new();
        private SessionActivityIdentity _activeScopeIdentity;
        private static int _lookupResolvedLogFrame = -1;

        public SessionActivityIdentity ActiveScopeIdentity => _activeScopeIdentity;

        public void SetActiveScopeIdentity(SessionActivityIdentity scopeIdentity)
        {
            if (!scopeIdentity.IsValid)
            {
                throw new InvalidOperationException("ActivityPlayerActorRegistry requires valid activity scope identity.");
            }

            _activeScopeIdentity = scopeIdentity;
        }

        public void ClearActiveIndexes()
        {
            _activeHandlesByParticipantId.Clear();
        }

        public void IndexActiveHandle(PlayerActorRuntimeHandle handle)
        {
            EnsureActiveScopeOrFail();
            if (!handle.IsValid)
            {
                throw new InvalidOperationException("Cannot index invalid player actor runtime handle.");
            }

            PlayerActorIdentityRecord actorIdentity = handle.ActorIdentity;
            if (_activeHandlesByParticipantId.TryGetValue(actorIdentity.ParticipantId, out PlayerActorRuntimeHandle existingActive) &&
                existingActive.IsValid &&
                existingActive.Instance != handle.Instance)
            {
                throw new InvalidOperationException($"Duplicate player participant registration detected. participantId='{actorIdentity.ParticipantId}'.");
            }

            if (_routeHandlesByParticipantId.TryGetValue(actorIdentity.ParticipantId, out PlayerActorRuntimeHandle existing) &&
                existing.IsValid &&
                existing.Instance != handle.Instance)
            {
                throw new InvalidOperationException($"Incompatible retained player actor for participantId='{actorIdentity.ParticipantId}'.");
            }

            _activeHandlesByParticipantId[actorIdentity.ParticipantId] = handle;
            LogRegistryEvent(
                "ActivityPlayerActorRegistryIndexed",
                "active_index_updated",
                "Indexed",
                "active_index_updated",
                _activeScopeIdentity,
                actorIdentity.Identity,
                handle.ActorId,
                handle.ActorInstanceRuntimeId,
                nameof(IndexActiveHandle),
                nameof(IndexActiveHandle));
        }

        public void IndexRouteScopedHandle(PlayerActorRuntimeHandle handle)
        {
            EnsureActiveScopeOrFail();
            if (!handle.IsValid)
            {
                throw new InvalidOperationException("Cannot index invalid player actor runtime handle.");
            }

            PlayerActorIdentityRecord actorIdentity = handle.ActorIdentity;
            if (_routeHandlesByParticipantId.TryGetValue(actorIdentity.ParticipantId, out PlayerActorRuntimeHandle existing) &&
                existing.IsValid &&
                existing.Instance != handle.Instance)
            {
                throw new InvalidOperationException($"Incompatible retained player actor for participantId='{actorIdentity.ParticipantId}'.");
            }

            _routeHandlesByParticipantId[actorIdentity.ParticipantId] = handle;
            LogRegistryEvent(
                "ActivityPlayerActorRegistryIndexed",
                "route_index_updated",
                "Indexed",
                "route_index_updated",
                _activeScopeIdentity,
                actorIdentity.Identity,
                handle.ActorId,
                handle.ActorInstanceRuntimeId,
                nameof(IndexRouteScopedHandle),
                nameof(IndexRouteScopedHandle));
        }

        public IReadOnlyList<PlayerActorRuntimeHandle> GetIndexedActiveHandles()
        {
            List<PlayerActorRuntimeHandle> handles = new(_activeHandlesByParticipantId.Count);
            foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _activeHandlesByParticipantId)
            {
                if (pair.Value.IsValid)
                {
                    handles.Add(pair.Value);
                }
            }

            return handles;
        }

        public bool TryGetIndexedActiveActorIdentities(out IReadOnlyList<PlayerActorIdentityRecord> records)
        {
            records = Array.Empty<PlayerActorIdentityRecord>();
            if (!_activeScopeIdentity.IsValid)
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
            EmitLookupResolvedIfNeeded(
                "active_actor_identities",
                default,
                _activeScopeIdentity,
                _activeScopeIdentity,
                default,
                default,
                nameof(TryGetIndexedActiveActorIdentities));
            return true;
        }

        public bool TryGetActiveHandleByParticipant(SessionParticipantId participantId, out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!participantId.IsValid)
            {
                throw new InvalidOperationException("participantId is required.");
            }

            if (!_activeHandlesByParticipantId.TryGetValue(participantId, out PlayerActorRuntimeHandle activeHandle) || !activeHandle.IsValid)
            {
                LogRegistryEvent(
                    "ActivityPlayerActorRegistryLookupMissed",
                    "active_handle",
                    "Missed",
                    "active_handle_missing",
                    _activeScopeIdentity,
                    _activeScopeIdentity,
                    default,
                    default,
                    nameof(TryGetActiveHandleByParticipant),
                    nameof(TryGetActiveHandleByParticipant));
                return false;
            }

            handle = activeHandle;
            EmitLookupResolvedIfNeeded(
                "active_handle",
                activeHandle.ActorInstanceRuntimeId,
                _activeScopeIdentity,
                _activeScopeIdentity,
                activeHandle.ActorId,
                activeHandle.ActorInstanceRuntimeId,
                nameof(TryGetActiveHandleByParticipant));
            return true;
        }

        public bool TryGetRouteScopedHandleByParticipant(SessionParticipantId participantId, out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!participantId.IsValid)
            {
                throw new InvalidOperationException("participantId is required.");
            }

            if (!_routeHandlesByParticipantId.TryGetValue(participantId, out PlayerActorRuntimeHandle retained) || !retained.IsValid)
            {
                LogRegistryEvent(
                    "ActivityPlayerActorRegistryLookupMissed",
                    "route_retained_participant",
                    "Missed",
                    "route_retained_handle_missing",
                    _activeScopeIdentity,
                    _activeScopeIdentity,
                    default,
                    default,
                    nameof(TryGetRouteScopedHandleByParticipant),
                    nameof(TryGetRouteScopedHandleByParticipant));
                return false;
            }

            handle = retained;
            EmitLookupResolvedIfNeeded(
                "route_retained_participant",
                retained.ActorInstanceRuntimeId,
                _activeScopeIdentity,
                retained.ActorIdentity.Identity,
                retained.ActorId,
                retained.ActorInstanceRuntimeId,
                nameof(TryGetRouteScopedHandleByParticipant));
            return true;
        }

        public bool TryGetActiveHandleByActorInstance(ActorInstanceRuntimeId actorInstanceRuntimeId, out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!actorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException("actorInstanceRuntimeId is required.");
            }

            foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _activeHandlesByParticipantId)
            {
                PlayerActorRuntimeHandle active = pair.Value;
                if (active.IsValid &&
                    active.ActorInstanceRuntimeId == actorInstanceRuntimeId)
                {
                    handle = active;
                    EmitLookupResolvedIfNeeded(
                        "active_actor_instance_handle",
                        active.ActorInstanceRuntimeId,
                        _activeScopeIdentity,
                        active.ActorIdentity.Identity,
                        active.ActorId,
                        active.ActorInstanceRuntimeId,
                        nameof(TryGetActiveHandleByActorInstance));
                    return true;
                }
            }

            LogRegistryEvent(
                "ActivityPlayerActorRegistryLookupMissed",
                "active_actor_instance_handle",
                "Missed",
                "lookup_missed",
                _activeScopeIdentity,
                _activeScopeIdentity,
                default,
                default,
                nameof(TryGetActiveHandleByActorInstance),
                nameof(TryGetActiveHandleByActorInstance));
            return false;
        }

        public bool TryGetRouteScopedHandleByActorInstance(ActorInstanceRuntimeId actorInstanceRuntimeId, out PlayerActorRuntimeHandle handle)
        {
            handle = default;
            if (!actorInstanceRuntimeId.IsValid)
            {
                throw new InvalidOperationException("actorInstanceRuntimeId is required.");
            }

            foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _routeHandlesByParticipantId)
            {
                PlayerActorRuntimeHandle retained = pair.Value;
                if (!retained.IsValid ||
                    retained.ActorInstanceRuntimeId != actorInstanceRuntimeId)
                {
                    continue;
                }

                handle = retained;
                EmitLookupResolvedIfNeeded(
                    "actor_instance_handle_retained",
                    retained.ActorInstanceRuntimeId,
                    _activeScopeIdentity,
                    retained.ActorIdentity.Identity,
                    retained.ActorId,
                    retained.ActorInstanceRuntimeId,
                    nameof(TryGetRouteScopedHandleByActorInstance));
                return true;
            }

            LogRegistryEvent(
                "ActivityPlayerActorRegistryLookupMissed",
                "actor_instance_handle",
                "Missed",
                "lookup_missed",
                _activeScopeIdentity,
                _activeScopeIdentity,
                default,
                default,
                nameof(TryGetRouteScopedHandleByActorInstance),
                nameof(TryGetRouteScopedHandleByActorInstance));
            return false;
        }

        public IReadOnlyList<PlayerActorIdentityRecord> GetIndexedRouteScopedActorIdentitiesForSession(SessionActivityIdentity expectedIdentity)
        {
            if (!expectedIdentity.IsValid)
            {
                throw new InvalidOperationException("expectedIdentity is invalid.");
            }

            List<PlayerActorIdentityRecord> records = new();
            foreach (KeyValuePair<SessionParticipantId, PlayerActorRuntimeHandle> pair in _routeHandlesByParticipantId)
            {
                PlayerActorRuntimeHandle retained = pair.Value;
                if (!retained.IsValid ||
                    retained.ActorIdentity.Identity.PipelineId != expectedIdentity.PipelineId ||
                    retained.ActorIdentity.Identity.SessionId != expectedIdentity.SessionId)
                {
                    continue;
                }

                records.Add(retained.ActorIdentity);
            }

            EmitLookupResolvedIfNeeded(
                "route_retained_actor_identities",
                default,
                _activeScopeIdentity,
                expectedIdentity,
                default,
                default,
                nameof(GetIndexedRouteScopedActorIdentitiesForSession));
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
            LogRegistryEvent(
                "ActivityPlayerActorRegistryIndexed",
                "clear_all_route_scoped_indexes",
                "Cleared",
                "index_cleared",
                default,
                default,
                default,
                default,
                nameof(ClearAllRouteScopedIndexes),
                nameof(ClearAllRouteScopedIndexes));
        }

        private static void LogRegistryEvent(
            string eventName,
            string decisionKind,
            string outcome,
            string outcomeReason,
            SessionActivityIdentity activeIdentity,
            SessionActivityIdentity targetIdentity,
            ActorId actorId,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            string source,
            string reason)
        {
            DebugUtility.LogVerbose(
                typeof(ActivityPlayerActorRegistry),
                $"[OBS][ActivityPlayerActorRegistry] event='{eventName}' owner='ActivityPlayerActorRegistry' decisionKind='{Normalize(decisionKind)}' outcome='{Normalize(outcome)}' outcomeReason='{Normalize(outcomeReason)}' activePipelineId='{Normalize(activeIdentity.PipelineId)}' activeSessionId='{Normalize(activeIdentity.SessionId)}' activeActivityId='{Normalize(activeIdentity.ActivityId)}' activeEntrySequence='{activeIdentity.EntrySequence}' targetPipelineId='{Normalize(targetIdentity.PipelineId)}' targetSessionId='{Normalize(targetIdentity.SessionId)}' targetActivityId='{Normalize(targetIdentity.ActivityId)}' targetEntrySequence='{targetIdentity.EntrySequence}' actorId='{actorId}' actorInstanceRuntimeId='{actorInstanceRuntimeId}' source='{Normalize(source)}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);
        }

        private static void EmitLookupResolvedIfNeeded(
            string decisionKind,
            ActorInstanceRuntimeId actorInstanceRuntimeId,
            SessionActivityIdentity activeIdentity,
            SessionActivityIdentity targetIdentity,
            ActorId actorId,
            ActorInstanceRuntimeId resolvedActorInstanceRuntimeId,
            string source)
        {
            int currentFrame = Time.frameCount;
            if (_lookupResolvedLogFrame != currentFrame)
            {
                LookupResolvedLogKeys.Clear();
                _lookupResolvedLogFrame = currentFrame;
            }

            string actorInstanceKey = resolvedActorInstanceRuntimeId.IsValid ? resolvedActorInstanceRuntimeId.Value : "<none>";
            string key = $"{Normalize(decisionKind)}|{actorInstanceKey}|{activeIdentity.EntrySequence}";
            if (!LookupResolvedLogKeys.Add(key))
            {
                return;
            }

            LogRegistryEvent(
                "ActivityPlayerActorRegistryLookupResolved",
                decisionKind,
                "Resolved",
                "lookup_resolved",
                activeIdentity,
                targetIdentity,
                actorId,
                actorInstanceRuntimeId,
                source,
                nameof(EmitLookupResolvedIfNeeded));
        }

        private void EnsureActiveScopeOrFail()
        {
            if (!_activeScopeIdentity.IsValid)
            {
                throw new InvalidOperationException("ActivityPlayerActorRegistry active scope is not initialized.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
