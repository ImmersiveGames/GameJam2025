using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Semantic.Preparation;
using _ImmersiveGames.NewScripts.Players.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Players.ActivitySetup
{
    public readonly struct PlayerActorSetupResult
    {
        public PlayerActorSetupResult(
            PlayerActorReadyStage readyStage,
            IReadOnlyList<PlayerActorEntryPlan> entryPlans,
            IReadOnlyList<PlayerActorResetPlan> resetPlans,
            IReadOnlyList<PlayerActorReleasePlan> releasePlans,
            IReadOnlyList<PlayerActorMaterializationRecord> records,
            IReadOnlyList<PlayerActorIdentityRecord> retainedActors)
        {
            ReadyStage = readyStage;
            EntryPlans = entryPlans ?? Array.Empty<PlayerActorEntryPlan>();
            ResetPlans = resetPlans ?? Array.Empty<PlayerActorResetPlan>();
            ReleasePlans = releasePlans ?? Array.Empty<PlayerActorReleasePlan>();
            Records = records ?? Array.Empty<PlayerActorMaterializationRecord>();
            RetainedActors = retainedActors ?? Array.Empty<PlayerActorIdentityRecord>();
        }

        public PlayerActorReadyStage ReadyStage { get; }
        public IReadOnlyList<PlayerActorEntryPlan> EntryPlans { get; }
        public IReadOnlyList<PlayerActorResetPlan> ResetPlans { get; }
        public IReadOnlyList<PlayerActorReleasePlan> ReleasePlans { get; }
        public IReadOnlyList<PlayerActorMaterializationRecord> Records { get; }
        public IReadOnlyList<PlayerActorIdentityRecord> RetainedActors { get; }

        public bool IsMaterializedOnlyReady => ReadyStage == PlayerActorReadyStage.MaterializedOnly;
        public bool IsRetainedForActivityReady => ReadyStage == PlayerActorReadyStage.RetainedForActivity;
        public bool HasMaterialization => Records.Count > 0;
        public bool HasRetainedReentry => RetainedActors.Count > 0;
    }

    public static class PlayerActorSetupStage
    {
        public static PlayerActorSetupResult Execute(
            SessionActivityIdentity identity,
            PlayerSelectionSnapshot selectionSnapshot,
            PlayerSetDefinitionAsset playerSetDefinition,
            IPlayerActorMaterializationAdapter materializationAdapter,
            IPlayerActorParticipationAdapter participationAdapter,
            ActivityPlayerActorRegistry registry,
            string source,
            string reason)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("PlayerActorSetupStage requires valid SessionActivity identity.");
            }

            if (!selectionSnapshot.IsValid)
            {
                throw new InvalidOperationException("PlayerActorSetupStage requires valid PlayerSelectionSnapshot.");
            }

            if (!selectionSnapshot.Identity.Equals(identity))
            {
                throw new InvalidOperationException("stale_or_foreign_player_selection_snapshot: selection snapshot identity does not match active identity.");
            }

            if (playerSetDefinition == null)
            {
                throw new InvalidOperationException("PlayerActorSetupStage requires playerSetDefinition when player actor setup is enabled.");
            }

            if (materializationAdapter == null)
            {
                throw new InvalidOperationException("PlayerActorSetupStage requires materialization adapter.");
            }

            if (participationAdapter == null)
            {
                throw new InvalidOperationException("PlayerActorSetupStage requires participation adapter.");
            }

            if (registry == null)
            {
                throw new InvalidOperationException("PlayerActorSetupStage requires activity player registry.");
            }

            registry.BeginActivityScope(identity);

            IReadOnlyList<PlayerSetDefinitionAsset.PlayerActorResolvedEntry> definitionEntries =
                playerSetDefinition.ResolvePlayerActorEntriesOrFail("SessionActivity/PlayerActorSetupStage");

            Dictionary<string, PlayerSetDefinitionAsset.PlayerActorResolvedEntry> byId = new(StringComparer.Ordinal);
            for (int index = 0; index < definitionEntries.Count; index++)
            {
                PlayerSetDefinitionAsset.PlayerActorResolvedEntry entry = definitionEntries[index];
                if (!entry.IsValid)
                {
                    throw new InvalidOperationException($"Invalid player set entry at index '{index}'.");
                }

                if (!byId.TryAdd(entry.PlayerId, entry))
                {
                    throw new InvalidOperationException($"Duplicate playerId in playerSetDefinition. playerId='{entry.PlayerId}'.");
                }
            }

            List<PlayerActorEntryPlan> entryPlans = new();
            List<PlayerActorResetPlan> resetPlans = new();
            List<PlayerActorReleasePlan> releasePlans = new();
            HashSet<string> selected = new(StringComparer.Ordinal);

            for (int index = 0; index < selectionSnapshot.Entries.Count; index++)
            {
                PlayerSelectionEntry selectedEntry = selectionSnapshot.Entries[index];
                if (!selectedEntry.IsValid)
                {
                    throw new InvalidOperationException($"PlayerSelectionSnapshot entry at index '{index}' is invalid.");
                }

                if (!selected.Add(selectedEntry.PlayerId))
                {
                    throw new InvalidOperationException($"Duplicate selected playerId in PlayerSelectionSnapshot. playerId='{selectedEntry.PlayerId}'.");
                }

                if (!byId.TryGetValue(selectedEntry.PlayerId, out PlayerSetDefinitionAsset.PlayerActorResolvedEntry definitionEntry))
                {
                    throw new InvalidOperationException($"Selected playerId is missing from playerSetDefinition. playerId='{selectedEntry.PlayerId}'.");
                }

                if (definitionEntry.Prefab == null)
                {
                    throw new InvalidOperationException($"Player actor prefab is required for selected player. playerId='{selectedEntry.PlayerId}'.");
                }

                PlayerActorIdentityRecord actorIdentity = new(identity, definitionEntry.PlayerId, BuildRouteScopedPlayerActorId(identity, definitionEntry.PlayerId));

                Vector3 localPosition = definitionEntry.PlacementMode == ActorPlacementMode.FixedTransform
                    ? definitionEntry.LocalPosition
                    : Vector3.zero;
                Vector3 localEuler = definitionEntry.PlacementMode == ActorPlacementMode.FixedTransform
                    ? definitionEntry.LocalRotation
                    : Vector3.zero;

                entryPlans.Add(new PlayerActorEntryPlan(actorIdentity, definitionEntry.Prefab, localPosition, localEuler));
                resetPlans.Add(new PlayerActorResetPlan(actorIdentity));
                releasePlans.Add(new PlayerActorReleasePlan(actorIdentity));
            }

            for (int index = 0; index < definitionEntries.Count; index++)
            {
                PlayerSetDefinitionAsset.PlayerActorResolvedEntry definitionEntry = definitionEntries[index];
                if (definitionEntry.Required && !selected.Contains(definitionEntry.PlayerId))
                {
                    throw new InvalidOperationException($"Required player is missing from PlayerSelectionSnapshot. playerId='{definitionEntry.PlayerId}'.");
                }
            }

            List<PlayerActorEntryPlan> toMaterialize = new();
            List<PlayerActorIdentityRecord> toReenter = new();
            for (int index = 0; index < entryPlans.Count; index++)
            {
                PlayerActorEntryPlan plan = entryPlans[index];
                if (registry.TryGetRetainedForPlayer(identity, plan.ActorIdentity.PlayerId, out GameObject retainedInstance, out PlayerActorIdentityRecord retainedIdentity))
                {
                    EnsureRetainedCompatibilityOrFail(identity, plan, retainedIdentity, retainedInstance);
                    PlayerActorIdentity identityComponent = retainedInstance.GetComponent<PlayerActorIdentity>();
                    if (identityComponent == null)
                    {
                        throw new InvalidOperationException($"Retained player actor missing PlayerActorIdentity component. playerId='{plan.ActorIdentity.PlayerId}'.");
                    }

                    identityComponent.Bind(
                        identity.PipelineId,
                        identity.SessionId,
                        identity.ActivityId,
                        identity.ActivityOrdinal,
                        identity.EntrySequence,
                        plan.ActorIdentity.PlayerId,
                        plan.ActorIdentity.PlayerActorId);

                    registry.RegisterRetainedParticipation(identity, plan.ActorIdentity, retainedInstance);
                    toReenter.Add(plan.ActorIdentity);
                    continue;
                }

                toMaterialize.Add(plan);
            }

            List<PlayerActorMaterializationRecord> records = new();
            if (toMaterialize.Count > 0)
            {
                PlayerActorMaterializationCommand materializationCommand = new(identity, toMaterialize, source, reason);
                IReadOnlyList<PlayerActorMaterializationRecord> produced = materializationAdapter.Execute(materializationCommand, identity);
                for (int index = 0; index < produced.Count; index++)
                {
                    PlayerActorMaterializationRecord record = produced[index];
                    if (!record.IsValid)
                    {
                        throw new InvalidOperationException($"PlayerActor materialization record at index '{index}' is invalid.");
                    }

                    registry.RegisterMaterialized(record.ActorIdentity, record.Instance);
                    records.Add(record);
                }
            }

            if (toReenter.Count > 0)
            {
                PlayerActorParticipationEnterCommand enterCommand = new(identity, toReenter, source, reason);
                IReadOnlyList<PlayerActorParticipationEnterRecord> enterRecords = participationAdapter.Execute(enterCommand, identity, registry);
                for (int index = 0; index < enterRecords.Count; index++)
                {
                    if (!enterRecords[index].IsValid)
                    {
                        throw new InvalidOperationException($"PlayerActor participation enter record at index '{index}' is invalid.");
                    }
                }
            }

            return new PlayerActorSetupResult(
                toMaterialize.Count > 0 ? PlayerActorReadyStage.MaterializedOnly : PlayerActorReadyStage.RetainedForActivity,
                entryPlans,
                resetPlans,
                releasePlans,
                records,
                toReenter);
        }

        private static string BuildRouteScopedPlayerActorId(SessionActivityIdentity identity, string playerId)
        {
            if (!identity.IsValid)
            {
                throw new InvalidOperationException("Cannot build route-scoped player actor id with invalid identity.");
            }

            string normalized = string.IsNullOrWhiteSpace(playerId) ? string.Empty : playerId.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidOperationException("Cannot build route-scoped player actor id without playerId.");
            }

            return $"{identity.SessionId}|{normalized}";
        }

        private static void EnsureRetainedCompatibilityOrFail(
            SessionActivityIdentity identity,
            PlayerActorEntryPlan expectedPlan,
            PlayerActorIdentityRecord retainedIdentity,
            GameObject retainedInstance)
        {
            if (!retainedIdentity.IsValid)
            {
                throw new InvalidOperationException($"Retained player identity is invalid. playerId='{expectedPlan.ActorIdentity.PlayerId}'.");
            }

            if (!string.Equals(retainedIdentity.PlayerId, expectedPlan.ActorIdentity.PlayerId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Retained player actor is incompatible with expected player. expectedPlayerId='{expectedPlan.ActorIdentity.PlayerId}' retainedPlayerId='{retainedIdentity.PlayerId}'.");
            }

            if (!string.Equals(retainedIdentity.PlayerActorId, expectedPlan.ActorIdentity.PlayerActorId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Retained player actor id mismatch. expected='{expectedPlan.ActorIdentity.PlayerActorId}' retained='{retainedIdentity.PlayerActorId}'.");
            }

            if (!string.Equals(retainedIdentity.Identity.PipelineId, identity.PipelineId, StringComparison.Ordinal) ||
                !string.Equals(retainedIdentity.Identity.SessionId, identity.SessionId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"stale_or_foreign_retained_player_actor_identity: playerId='{expectedPlan.ActorIdentity.PlayerId}'.");
            }

            if (retainedInstance == null)
            {
                throw new InvalidOperationException(
                    $"Retained player actor instance is null. playerId='{expectedPlan.ActorIdentity.PlayerId}'.");
            }
        }
    }
}
