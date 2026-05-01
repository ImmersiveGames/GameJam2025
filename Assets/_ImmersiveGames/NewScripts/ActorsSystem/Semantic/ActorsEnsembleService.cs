using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Semantic
{
    /// <summary>
    /// Default policy for deriving axis identity/role/relevance from definitions plus semantic/runtime observations.
    /// </summary>
    public sealed class ActorsDefaultIdentityRolePolicy : IActorsIdentityRolePolicy
    {
        private readonly List<ActorIdentityRecord> _buffer = new(32);

        public ActorIdentityRoleSet Resolve(ActorsEnsembleInput input)
        {
            _buffer.Clear();

            ActorDefinitionRecord[] definitions = input.Definitions.Entries ?? Array.Empty<ActorDefinitionRecord>();
            ActorsSemanticParticipantRecord[] participants = input.SemanticParticipation.Participants ?? Array.Empty<ActorsSemanticParticipantRecord>();
            RuntimeActorObservationRecord[] runtimeActors = input.RuntimeObservation.RuntimeActors ?? Array.Empty<RuntimeActorObservationRecord>();

            for (int index = 0; index < definitions.Length; index += 1)
            {
                ActorDefinitionRecord definition = definitions[index];
                if (!definition.IsValid)
                {
                    continue;
                }

                bool hasSemanticLink = TryFindSemanticParticipant(definition.SemanticParticipantId, participants, out ActorsSemanticParticipantRecord semanticParticipant);
                bool isRuntimeObserved = TryFindRuntimeActor(definition.PreferredRuntimeActorId, runtimeActors, out RuntimeActorObservationRecord runtimeActor);

                ActorRelevance relevance = ResolveRelevance(definition, hasSemanticLink, semanticParticipant, isRuntimeObserved, runtimeActor);

                _buffer.Add(new ActorIdentityRecord(
                    definition.AxisActorId,
                    definition.Role,
                    relevance,
                    hasSemanticLink ? semanticParticipant.ParticipantId : string.Empty,
                    definition.OperationalRecipeKind,
                    isRuntimeObserved ? runtimeActor.RuntimeActorId : RuntimeActorId.None,
                    definition.ActorSpecId,
                    definition.SpawnArchetypeId,
                    definition.ActorSetMemberId,
                    definition.OccurrenceIndex,
                    definition.RealizationMode,
                    definition.ContinuityResetPolicy,
                    definition.ActorSetRef,
                    hasSemanticLink,
                    isRuntimeObserved));
            }

            string signature = BuildSignature(
                input.Definitions.Signature,
                input.SemanticParticipation.ParticipationSignature,
                input.RuntimeObservation.ObservationSignature,
                _buffer.Count);

            return new ActorIdentityRoleSet(signature, _buffer.ToArray());
        }

        private static ActorRelevance ResolveRelevance(
            ActorDefinitionRecord definition,
            bool hasSemanticLink,
            ActorsSemanticParticipantRecord semanticParticipant,
            bool isRuntimeObserved,
            RuntimeActorObservationRecord runtimeActor)
        {
            if (hasSemanticLink)
            {
                if (semanticParticipant.IsPrimary)
                {
                    return ActorRelevance.Primary;
                }

                if (semanticParticipant.IsLocal)
                {
                    return ActorRelevance.Local;
                }
            }

            if (definition.IsRequired)
            {
                return ActorRelevance.Required;
            }

            if (isRuntimeObserved && runtimeActor.IsActive)
            {
                return ActorRelevance.Observed;
            }

            return ActorRelevance.Optional;
        }

        private static bool TryFindSemanticParticipant(
            string semanticParticipantId,
            ActorsSemanticParticipantRecord[] participants,
            out ActorsSemanticParticipantRecord participant)
        {
            participant = default;
            if (string.IsNullOrWhiteSpace(semanticParticipantId) || participants == null || participants.Length == 0)
            {
                return false;
            }

            string normalized = semanticParticipantId.Trim();
            for (int index = 0; index < participants.Length; index += 1)
            {
                if (!participants[index].IsValid)
                {
                    continue;
                }

                if (string.Equals(participants[index].ParticipantId, normalized, StringComparison.Ordinal))
                {
                    participant = participants[index];
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindRuntimeActor(
            RuntimeActorId preferredRuntimeActorId,
            RuntimeActorObservationRecord[] runtimeActors,
            out RuntimeActorObservationRecord runtimeActor)
        {
            runtimeActor = default;
            if (!preferredRuntimeActorId.IsValid || runtimeActors == null || runtimeActors.Length == 0)
            {
                return false;
            }

            for (int index = 0; index < runtimeActors.Length; index += 1)
            {
                if (!runtimeActors[index].IsValid)
                {
                    continue;
                }

                if (runtimeActors[index].RuntimeActorId == preferredRuntimeActorId)
                {
                    runtimeActor = runtimeActors[index];
                    return true;
                }
            }

            return false;
        }

        private static string BuildSignature(string definitionsSignature, string semanticSignature, string runtimeSignature, int count)
        {
            var builder = new StringBuilder(128);
            builder.Append(string.IsNullOrWhiteSpace(definitionsSignature) ? "<no-definitions>" : definitionsSignature.Trim());
            builder.Append('|');
            builder.Append(string.IsNullOrWhiteSpace(semanticSignature) ? "<no-semantic>" : semanticSignature.Trim());
            builder.Append('|');
            builder.Append(string.IsNullOrWhiteSpace(runtimeSignature) ? "<no-runtime>" : runtimeSignature.Trim());
            builder.Append('|');
            builder.Append("count:");
            builder.Append(count);
            return builder.ToString();
        }
    }

    /// <summary>
    /// Slice-1 semantic owner for ActorsSystem ensemble governance.
    /// </summary>
    public sealed class ActorsEnsembleService : IActorsEnsembleService
    {
        private readonly IActorsDefinitionsPort _definitionsPort;
        private readonly IActorsSemanticParticipationInPort _semanticParticipationPort;
        private readonly IActorsRuntimeObservationInPort _runtimeObservationPort;
        private readonly IActorsIdentityRolePolicy _identityRolePolicy;
        private ActorsEnsembleSnapshot _current = ActorsEnsembleSnapshot.Empty;

        public ActorsEnsembleService(
            IActorsDefinitionsPort definitionsPort,
            IActorsSemanticParticipationInPort semanticParticipationPort,
            IActorsRuntimeObservationInPort runtimeObservationPort,
            IActorsIdentityRolePolicy identityRolePolicy)
        {
            _definitionsPort = definitionsPort ?? throw new ArgumentNullException(nameof(definitionsPort));
            _semanticParticipationPort = semanticParticipationPort ?? throw new ArgumentNullException(nameof(semanticParticipationPort));
            _runtimeObservationPort = runtimeObservationPort ?? throw new ArgumentNullException(nameof(runtimeObservationPort));
            _identityRolePolicy = identityRolePolicy ?? throw new ArgumentNullException(nameof(identityRolePolicy));

            DebugUtility.Log(typeof(ActorsEnsembleService),
                "[OBS][ActorsSystem] ActorsEnsembleService registrado (slice1 semantic owner do conjunto).",
                DebugUtility.Colors.Info);
        }

        public ActorsEnsembleSnapshot Current => _current;

        public bool TryGetCurrent(out ActorsEnsembleSnapshot snapshot)
        {
            snapshot = _current;
            return _current.IsValid;
        }

        public ActorsEnsembleSnapshot Refresh()
        {
            if (!_definitionsPort.TryGetCurrent(out ActorsDefinitionsSnapshot definitions) || !definitions.IsValid)
            {
                _current = new ActorsEnsembleSnapshot(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    Array.Empty<ActorIdentityRecord>(),
                    "no-definitions");
                return _current;
            }

            if (!_semanticParticipationPort.TryGetCurrent(out ActorsSemanticParticipationSnapshot semanticParticipation))
            {
                semanticParticipation = ActorsSemanticParticipationSnapshot.Empty;
            }

            if (!_runtimeObservationPort.TryGetCurrent(out ActorsRuntimeObservationSnapshot runtimeObservation))
            {
                runtimeObservation = ActorsRuntimeObservationSnapshot.Empty;
            }

            ActorIdentityRoleSet identitySet = _identityRolePolicy.Resolve(new ActorsEnsembleInput(
                definitions,
                semanticParticipation,
                runtimeObservation));

            string ensembleSignature = BuildEnsembleSignature(
                definitions.Signature,
                semanticParticipation.ParticipationSignature,
                runtimeObservation.ObservationSignature,
                identitySet.Signature,
                identitySet.Count);

            _current = new ActorsEnsembleSnapshot(
                definitions.Signature,
                semanticParticipation.ParticipationSignature,
                runtimeObservation.ObservationSignature,
                ensembleSignature,
                identitySet.Entries,
                identitySet.Count > 0 ? "resolved" : "no-members");

            return _current;
        }

        public void Clear(string reason = null)
        {
            _current = new ActorsEnsembleSnapshot(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                Array.Empty<ActorIdentityRecord>(),
                string.IsNullOrWhiteSpace(reason) ? "cleared" : reason.Trim());
        }

        private static string BuildEnsembleSignature(
            string definitionsSignature,
            string semanticSignature,
            string runtimeSignature,
            string policySignature,
            int count)
        {
            var builder = new StringBuilder(256);
            builder.Append(string.IsNullOrWhiteSpace(definitionsSignature) ? "<no-definitions>" : definitionsSignature.Trim());
            builder.Append('|');
            builder.Append(string.IsNullOrWhiteSpace(semanticSignature) ? "<no-semantic>" : semanticSignature.Trim());
            builder.Append('|');
            builder.Append(string.IsNullOrWhiteSpace(runtimeSignature) ? "<no-runtime>" : runtimeSignature.Trim());
            builder.Append('|');
            builder.Append(string.IsNullOrWhiteSpace(policySignature) ? "<no-policy>" : policySignature.Trim());
            builder.Append('|');
            builder.Append("count:");
            builder.Append(count);
            return builder.ToString();
        }
    }
}
