using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Players.ActivitySetup
{
    public sealed class ActivityPlayerActorRegistry
    {
        private readonly Dictionary<string, GameObject> _routeInstancesByPlayerSlotId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PlayerActorIdentityRecord> _routeIdentityByPlayerSlotId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, GameObject> _activeInstancesByPlayerActorId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PlayerActorIdentityRecord> _activeIdentityByPlayerActorId = new(StringComparer.Ordinal);
        private SessionActivityIdentity _activeScopeIdentity;

        public SessionActivityIdentity ActiveScopeIdentity => _activeScopeIdentity;

        public void BeginActivityScope(SessionActivityIdentity scopeIdentity)
        {
            if (!scopeIdentity.IsValid)
            {
                throw new InvalidOperationException("ActivityPlayerActorRegistry requires valid activity scope identity.");
            }

            _activeScopeIdentity = scopeIdentity;
            _activeInstancesByPlayerActorId.Clear();
            _activeIdentityByPlayerActorId.Clear();
        }

        public void RegisterMaterialized(PlayerActorIdentityRecord actorIdentity, GameObject instance)
        {
            EnsureActiveScopeOrFail();
            EnsureIdentityMatchesActiveScopeOrFail(actorIdentity.Identity, "stale_or_foreign_player_actor_registration");

            if (instance == null)
            {
                throw new InvalidOperationException("Cannot register null player actor instance.");
            }

            if (_activeInstancesByPlayerActorId.ContainsKey(actorIdentity.PlayerActorId))
            {
                throw new InvalidOperationException($"Duplicate playerActorId registration detected. playerActorId='{actorIdentity.PlayerActorId}'.");
            }

            _activeInstancesByPlayerActorId.Add(actorIdentity.PlayerActorId, instance);
            _activeIdentityByPlayerActorId.Add(actorIdentity.PlayerActorId, actorIdentity);

            if (_routeInstancesByPlayerSlotId.TryGetValue(actorIdentity.PlayerSlotId, out GameObject existing) && existing != null && existing != instance)
            {
                throw new InvalidOperationException($"Incompatible retained player actor for playerSlotId='{actorIdentity.PlayerSlotId}'.");
            }

            _routeInstancesByPlayerSlotId[actorIdentity.PlayerSlotId] = instance;
            _routeIdentityByPlayerSlotId[actorIdentity.PlayerSlotId] = actorIdentity;
        }

        public bool TryGetRetainedForPlayerSlot(SessionActivityIdentity scopeIdentity, string playerSlotId, out GameObject instance, out PlayerActorIdentityRecord retainedIdentity)
        {
            EnsureScopeMatchesOrFail(scopeIdentity);
            string normalizedPlayerSlotId = Normalize(playerSlotId);
            if (string.IsNullOrWhiteSpace(normalizedPlayerSlotId))
            {
                throw new InvalidOperationException("playerSlotId is required.");
            }

            if (!_routeInstancesByPlayerSlotId.TryGetValue(normalizedPlayerSlotId, out instance) || instance == null)
            {
                retainedIdentity = default;
                return false;
            }

            if (!_routeIdentityByPlayerSlotId.TryGetValue(normalizedPlayerSlotId, out retainedIdentity) || !retainedIdentity.IsValid)
            {
                throw new InvalidOperationException($"Retained identity missing for playerSlotId='{normalizedPlayerSlotId}'.");
            }

            if (!IsSameSessionPipeline(retainedIdentity.Identity, scopeIdentity))
            {
                throw new InvalidOperationException($"stale_or_foreign_retained_player_actor: playerSlotId='{normalizedPlayerSlotId}'.");
            }

            return true;
        }

        public void RegisterRetainedParticipation(SessionActivityIdentity scopeIdentity, PlayerActorIdentityRecord actorIdentity, GameObject instance)
        {
            EnsureScopeMatchesOrFail(scopeIdentity);
            EnsureIdentityMatchesActiveScopeOrFail(actorIdentity.Identity, "stale_or_foreign_player_actor_reenter_registration");
            if (instance == null)
            {
                throw new InvalidOperationException($"Cannot register retained null player actor. playerSlotId='{actorIdentity.PlayerSlotId}'.");
            }

            _activeInstancesByPlayerActorId[actorIdentity.PlayerActorId] = instance;
            _activeIdentityByPlayerActorId[actorIdentity.PlayerActorId] = actorIdentity;
            _routeInstancesByPlayerSlotId[actorIdentity.PlayerSlotId] = instance;
            _routeIdentityByPlayerSlotId[actorIdentity.PlayerSlotId] = actorIdentity;
        }

        public IReadOnlyList<PlayerActorIdentityRecord> GetActiveActorIdentitiesOrFail(SessionActivityIdentity expectedScopeIdentity)
        {
            EnsureScopeMatchesOrFail(expectedScopeIdentity);
            List<PlayerActorIdentityRecord> records = new(_activeIdentityByPlayerActorId.Count);
            foreach (KeyValuePair<string, PlayerActorIdentityRecord> pair in _activeIdentityByPlayerActorId)
            {
                records.Add(pair.Value);
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

            List<PlayerActorIdentityRecord> active = new(_activeIdentityByPlayerActorId.Count);
            foreach (KeyValuePair<string, PlayerActorIdentityRecord> pair in _activeIdentityByPlayerActorId)
            {
                active.Add(pair.Value);
            }

            records = active;
            return true;
        }

        public GameObject ResolveActiveInstanceOrFail(SessionActivityIdentity expectedScopeIdentity, string playerActorId)
        {
            EnsureScopeMatchesOrFail(expectedScopeIdentity);
            string normalizedId = Normalize(playerActorId);
            if (string.IsNullOrWhiteSpace(normalizedId))
            {
                throw new InvalidOperationException("playerActorId is required.");
            }

            if (!_activeInstancesByPlayerActorId.TryGetValue(normalizedId, out GameObject instance) || instance == null)
            {
                throw new InvalidOperationException($"Active PlayerActor instance not found in registry. playerActorId='{normalizedId}'.");
            }

            return instance;
        }

        public bool TryResolveInstanceForControl(SessionActivityIdentity expectedIdentity, string playerActorId, out GameObject instance, out PlayerActorIdentityRecord actorIdentity)
        {
            instance = null;
            actorIdentity = default;
            string normalizedId = Normalize(playerActorId);
            if (!expectedIdentity.IsValid || string.IsNullOrWhiteSpace(normalizedId))
            {
                return false;
            }

            if (_activeScopeIdentity.IsValid &&
                IsSameActivityCycle(_activeScopeIdentity, expectedIdentity) &&
                _activeInstancesByPlayerActorId.TryGetValue(normalizedId, out GameObject activeInstance) &&
                activeInstance != null &&
                _activeIdentityByPlayerActorId.TryGetValue(normalizedId, out PlayerActorIdentityRecord activeIdentity) &&
                activeIdentity.IsValid)
            {
                instance = activeInstance;
                actorIdentity = activeIdentity;
                return true;
            }

            foreach (KeyValuePair<string, PlayerActorIdentityRecord> pair in _routeIdentityByPlayerSlotId)
            {
                PlayerActorIdentityRecord retained = pair.Value;
                if (!retained.IsValid ||
                    !string.Equals(retained.PlayerActorId, normalizedId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!IsSameSessionPipeline(retained.Identity, expectedIdentity))
                {
                    continue;
                }

                if (!_routeInstancesByPlayerSlotId.TryGetValue(retained.PlayerSlotId, out GameObject retainedInstance) || retainedInstance == null)
                {
                    continue;
                }

                instance = retainedInstance;
                actorIdentity = retained;
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
            foreach (KeyValuePair<string, PlayerActorIdentityRecord> pair in _routeIdentityByPlayerSlotId)
            {
                PlayerActorIdentityRecord retained = pair.Value;
                if (!retained.IsValid || !IsSameSessionPipeline(retained.Identity, expectedIdentity))
                {
                    continue;
                }

                if (_routeInstancesByPlayerSlotId.TryGetValue(retained.PlayerSlotId, out GameObject retainedInstance) && retainedInstance != null)
                {
                    records.Add(retained);
                }
            }

            return records;
        }

        public void ClearAllRouteRetained()
        {
            foreach (KeyValuePair<string, GameObject> pair in _routeInstancesByPlayerSlotId)
            {
                if (pair.Value != null)
                {
                    UnityEngine.Object.Destroy(pair.Value);
                }
            }

            _routeInstancesByPlayerSlotId.Clear();
            _routeIdentityByPlayerSlotId.Clear();
            _activeInstancesByPlayerActorId.Clear();
            _activeIdentityByPlayerActorId.Clear();
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

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
