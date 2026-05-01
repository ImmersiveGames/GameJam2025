using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.GameplayRuntime.Integration.ActorsExecution;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionTransition.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;

namespace _ImmersiveGames.NewScripts.ActorsSystem.Integration.SessionFlow
{
    /// <summary>
    /// Canonical adapter: projects session semantic participation plus route actor set
    /// into ActorsSystem inbound contracts.
    ///
    /// DEPENDÊNCIAS CRÍTICAS (injetadas explicitamente, sem service locator):
    /// - IGameplayParticipationFlowService: owned by SessionFlow.Semantic
    /// - ISceneFlowRouteActorSetRefContext: composed in SceneFlowBootstrap.EnsureRouteActorSetRefContext
    /// - IActorSetSelectionService: composed via ActorsSystem bootstrap
    /// </summary>
    public sealed class SessionFlowActorsSemanticPortsAdapter :
        IActorsSemanticParticipationInPort,
        IActorsDefinitionsPort
    {
        private readonly IGameplayParticipationFlowService _participationFlowService;
        private readonly ISceneFlowRouteActorSetRefContext _routeActorSetContext;
        private readonly IActorSetSelectionService _actorSetSelectionService;
        private readonly object _canonicalGameplayEntrySync = new();
        private CanonicalGameplayEntryContext _canonicalGameplayEntryContext;
        private bool _hasCanonicalGameplayEntryContext;
        private CanonicalActorResolutionSet _currentResolutionSet;

        public SessionFlowActorsSemanticPortsAdapter(
            IGameplayParticipationFlowService participationFlowService,
            ISceneFlowRouteActorSetRefContext routeActorSetContext,
            IActorSetSelectionService actorSetSelectionService)
        {
            _participationFlowService = participationFlowService ?? throw new ArgumentNullException(nameof(participationFlowService));
            _routeActorSetContext = routeActorSetContext ?? throw new ArgumentNullException(nameof(routeActorSetContext));
            _actorSetSelectionService = actorSetSelectionService ?? throw new ArgumentNullException(nameof(actorSetSelectionService));
        }

        public void PrimeCanonicalGameplayEntry(SessionTransitionPhaseLocalEntryReadyEvent evt)
        {
            if (!evt.HasCanonicalPayload)
            {
                throw new InvalidOperationException(
                    "[FATAL][Config][ActorsSystem] SessionTransitionPhaseLocalEntryReadyEvent sem payload canonico nao pode primar o refresh semantico gameplay.");
            }

            ActorSetRef actorSetRef = new ActorSetRef(evt.ActorSetRef);
            if (!evt.RouteId.IsValid || evt.RouteKind != SceneRouteKind.Gameplay || !actorSetRef.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Canonical gameplay entry invalido ao primar refresh. routeId='{evt.RouteId}' routeKind='{evt.RouteKind}' actorSetRef='{AsText(evt.ActorSetRef)}'.");
            }

            lock (_canonicalGameplayEntrySync)
            {
                _canonicalGameplayEntryContext = new CanonicalGameplayEntryContext(
                    evt.RouteId,
                    evt.RouteKind,
                    evt.SceneName,
                    evt.Reason,
                    evt.SessionSignature,
                    evt.PhaseSignature,
                    evt.ParticipationSignature,
                    evt.ActorSetRef,
                    evt.CycleSignature,
                    evt.Source,
                    evt.PhaseEntryIdentity);
                _hasCanonicalGameplayEntryContext = true;
            }
        }

        public void ClearCanonicalGameplayEntryContext()
        {
            lock (_canonicalGameplayEntrySync)
            {
                _hasCanonicalGameplayEntryContext = false;
                _canonicalGameplayEntryContext = default;
            }
        }

        public bool TryGetCurrentCanonicalGameplayEntry(out CanonicalGameplayEntrySnapshot snapshot)
        {
            snapshot = default;

            lock (_canonicalGameplayEntrySync)
            {
                if (!_hasCanonicalGameplayEntryContext)
                {
                    return false;
                }

                snapshot = new CanonicalGameplayEntrySnapshot(_canonicalGameplayEntryContext);
                return snapshot.IsValid;
            }
        }

        public bool TryGetCurrent(out ActorsSemanticParticipationSnapshot snapshot)
        {
            snapshot = ActorsSemanticParticipationSnapshot.Empty;

            if (!_participationFlowService.TryGetCurrent(out ParticipationSnapshot participationSnapshot) || !participationSnapshot.IsValid)
            {
                return false;
            }

            ActorsSemanticParticipantRecord[] participants = BuildSemanticParticipants(participationSnapshot);

            snapshot = new ActorsSemanticParticipationSnapshot(
                participationSnapshot.SessionSignature,
                participationSnapshot.PhaseSignature,
                participationSnapshot.Signature.Value,
                participants);
            return snapshot.IsValid;
        }

        public bool TryGetCurrent(out ActorsDefinitionsSnapshot snapshot)
        {
            snapshot = ActorsDefinitionsSnapshot.Empty;

            if (TryGetCanonicalGameplayDefinitionsSnapshot(out snapshot))
            {
                return snapshot.IsValid;
            }

            bool hasParticipation =
                _participationFlowService.TryGetCurrent(out ParticipationSnapshot participationSnapshot) &&
                participationSnapshot.IsValid;

            // ✅ Dependencies are now injected explicitly in constructor
            if (!_routeActorSetContext.TryGetCurrent(out ActorSetRef actorSetRef, out SceneRouteKind routeKind, out string source))
            {
                if (routeKind == SceneRouteKind.Gameplay)
                {
                    throw new InvalidOperationException(
                        "[FATAL][Config][ActorsSystem] Gameplay route context unresolved while deriving actor definitions.");
                }

                snapshot = BuildEmptyDefinitionsSnapshot(routeKind, source, hasParticipation ? participationSnapshot.Signature.Value : string.Empty, "route-context-unresolved");
                return snapshot.IsValid;
            }

            if (!actorSetRef.IsValid)
            {
                if (routeKind == SceneRouteKind.Gameplay)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsSystem] Missing ActorSetRef in gameplay route context while deriving definitions. source='{AsText(source)}'.");
                }

                snapshot = BuildEmptyDefinitionsSnapshot(routeKind, source, hasParticipation ? participationSnapshot.Signature.Value : string.Empty, "non-gameplay-without-actor-set");
                return snapshot.IsValid;
            }

            if (!_actorSetSelectionService.TryResolve(actorSetRef, out ActorSetResolvedSelection selection) || !selection.HasEntries)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] ActorSetRef unresolved while deriving definitions actorSetRef='{actorSetRef.Value}' routeKind='{routeKind}' source='{AsText(source)}'.");
            }

            CanonicalActorResolutionSet resolutionSet = BuildCanonicalResolutionSetOrFail(
                selection,
                hasParticipation,
                participationSnapshot,
                actorSetRef,
                routeKind,
                source,
                cycleSignature: string.Empty);
            _currentResolutionSet = resolutionSet;
            ActorDefinitionRecord[] entries = BuildDefinitionsFromResolutionSet(resolutionSet);

            snapshot = new ActorsDefinitionsSnapshot(
                BuildDefinitionsSignature(actorSetRef, routeKind, source, hasParticipation ? participationSnapshot.Signature.Value : string.Empty, string.Empty, entries.Length),
                entries);
            return snapshot.IsValid;
        }
        // ...existing code...
        private static ActorsSemanticParticipantRecord[] BuildSemanticParticipants(ParticipationSnapshot participationSnapshot)
        {
            ParticipantSnapshot[] source = participationSnapshot.Participants;
            if (source == null || source.Length == 0)
            {
                return Array.Empty<ActorsSemanticParticipantRecord>();
            }

            var participants = new List<ActorsSemanticParticipantRecord>(source.Length);
            for (int index = 0; index < source.Length; index += 1)
            {
                ParticipantSnapshot participant = source[index];
                if (!participant.IsValid)
                {
                    continue;
                }

                participants.Add(new ActorsSemanticParticipantRecord(
                    participant.ParticipantId.Value,
                    MapRole(participant.Kind),
                    participant.IsPrimary,
                    participant.IsLocal));
            }

            return participants.ToArray();
        }

        public bool TryGetCurrentCanonicalResolution(out CanonicalActorResolutionSet resolutionSet)
        {
            resolutionSet = _currentResolutionSet;
            return resolutionSet.IsValid;
        }

        private static CanonicalActorResolutionSet BuildCanonicalResolutionSetOrFail(
            ActorSetResolvedSelection selection,
            bool hasParticipation,
            ParticipationSnapshot participationSnapshot,
            ActorSetRef actorSetRef,
            SceneRouteKind routeKind,
            string source,
            string cycleSignature)
        {
            var entries = new List<CanonicalActorResolution>(selection.Count);
            for (int index = 0; index < selection.Members.Length; index += 1)
            {
                ActorSetResolvedMember member = selection.Members[index];
                if (!member.IsValid)
                {
                    continue;
                }

                entries.Add(BuildResolutionFromMemberOrFail(
                    member,
                    hasParticipation,
                    participationSnapshot,
                    actorSetRef,
                    routeKind,
                    source));
            }

            string signature = BuildResolutionSignature(actorSetRef, routeKind, source, cycleSignature, entries.Count);
            return new CanonicalActorResolutionSet(signature, entries.ToArray());
        }

        private static CanonicalActorResolution BuildResolutionFromMemberOrFail(
            ActorSetResolvedMember member,
            bool hasParticipation,
            ParticipationSnapshot participationSnapshot,
            ActorSetRef actorSetRef,
            SceneRouteKind routeKind,
            string source)
        {
            ActorSpecRecord spec = member.Spec;
            if (!spec.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Invalid ActorSpec while deriving canonical resolution actorSetRef='{actorSetRef.Value}' routeKind='{routeKind}' memberId='{member.ActorSetMemberId}'.");
            }

            ActorRole role = MapRole(spec.RoleGroup);
            if (role == ActorRole.Unknown)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Unsupported roleGroup='{spec.RoleGroup}' while deriving definition actorSpecId='{spec.ActorSpecId}'.");
            }

            var occurrences = new List<CanonicalActorOccurrence>();
            bool isDefaultRequired = spec.IntegrationStage != ActorSpecIntegrationStage.RuntimeDynamic;

            if (spec.SourceKind == ActorSpecSourceKind.ParticipationDerived)
            {
                if (!hasParticipation)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsSystem] Participation snapshot required for participation-derived actorSpecId='{spec.ActorSpecId}' actorSetRef='{actorSetRef.Value}' routeKind='{routeKind}'.");
                }

                List<ParticipantSnapshot> participants = ResolveParticipantsForRoleGroup(spec.RoleGroup, participationSnapshot);
                if (participants.Count == 0 && member.Cardinality.Kind != ActorCardinalityKind.ZeroOrOne)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsSystem] Missing semantic participants for participation-derived actorSpecId='{spec.ActorSpecId}' roleGroup='{spec.RoleGroup}' actorSetRef='{actorSetRef.Value}' memberId='{member.ActorSetMemberId}' routeKind='{routeKind}' participationSignature='{participationSnapshot.Signature}'.");
                }

                ValidateParticipantCardinalityOrFail(member.Cardinality, participants.Count, spec, actorSetRef, routeKind);
                for (int index = 0; index < participants.Count; index += 1)
                {
                    ParticipantSnapshot participant = participants[index];
                    string participantId = participant.ParticipantId.Value;
                    AxisActorId axisActorId = AxisActorId.FromCanonicalParticipation(actorSetRef, member.ActorSetMemberId, participantId);
                    if (!axisActorId.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"[FATAL][Config][ActorsSystem] Invalid AxisActorId for participation-derived actorSpecId='{spec.ActorSpecId}' actorSetRef='{actorSetRef.Value}' memberId='{member.ActorSetMemberId}' participantId='{participantId}'.");
                    }

                    bool isRequired = isDefaultRequired || participant.IsPrimary || participant.IsLocal;
                    occurrences.Add(new CanonicalActorOccurrence(axisActorId, index, participantId, isRequired));
                }
            }
            else
            {
                int occurrenceCount = ResolveNonParticipationOccurrenceCount(member.Cardinality);
                for (int occurrenceIndex = 0; occurrenceIndex < occurrenceCount; occurrenceIndex += 1)
                {
                    AxisActorId axisActorId = AxisActorId.FromCanonicalOccurrence(actorSetRef, member.ActorSetMemberId, occurrenceIndex);
                    if (!axisActorId.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"[FATAL][Config][ActorsSystem] Invalid AxisActorId for non-participation actorSpecId='{spec.ActorSpecId}' actorSetRef='{actorSetRef.Value}' memberId='{member.ActorSetMemberId}' occurrenceIndex='{occurrenceIndex}'.");
                    }

                    bool isRequired = IsRequiredOccurrence(member.Cardinality, occurrenceIndex, isDefaultRequired);
                    occurrences.Add(new CanonicalActorOccurrence(axisActorId, occurrenceIndex, string.Empty, isRequired));
                }
            }

            if (occurrences.Count == 0)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Canonical resolution sem ocorrencias actorSpecId='{spec.ActorSpecId}' actorSetRef='{actorSetRef.Value}' memberId='{member.ActorSetMemberId}' routeKind='{routeKind}'.");
            }

            return new CanonicalActorResolution(
                actorSetRef,
                member.ActorSetMemberId,
                spec.ActorSpecId,
                spec.SpawnArchetypeId,
                role,
                spec.OperationalRecipeKind,
                spec.RealizationMode,
                spec.ContinuityResetPolicy,
                occurrences.ToArray(),
                $"ActorSet/{actorSetRef.Value}/Member/{member.ActorSetMemberId.Value}/ActorSpec/{spec.ActorSpecId}");
        }

        private static ActorDefinitionRecord[] BuildDefinitionsFromResolutionSet(CanonicalActorResolutionSet resolutionSet)
        {
            if (!resolutionSet.IsValid || resolutionSet.Entries == null || resolutionSet.Entries.Length == 0)
            {
                return Array.Empty<ActorDefinitionRecord>();
            }

            var definitions = new List<ActorDefinitionRecord>(resolutionSet.Entries.Length);
            for (int entryIndex = 0; entryIndex < resolutionSet.Entries.Length; entryIndex += 1)
            {
                CanonicalActorResolution resolution = resolutionSet.Entries[entryIndex];
                if (!resolution.IsValid)
                {
                    continue;
                }

                CanonicalActorOccurrence[] occurrences = resolution.Occurrences ?? Array.Empty<CanonicalActorOccurrence>();
                for (int occurrenceIndex = 0; occurrenceIndex < occurrences.Length; occurrenceIndex += 1)
                {
                    CanonicalActorOccurrence occurrence = occurrences[occurrenceIndex];
                    if (!occurrence.IsValid)
                    {
                        continue;
                    }

                    definitions.Add(new ActorDefinitionRecord(
                        occurrence.AxisActorId,
                        resolution.Role,
                        occurrence.IsRequired,
                        occurrence.SemanticParticipantId,
                        resolution.OperationalRecipeKind,
                        RuntimeActorId.None,
                        resolution.ActorSpecId,
                        resolution.SpawnArchetypeId,
                        resolution.ActorSetMemberId.Value,
                        occurrence.InstanceIndex,
                        resolution.RealizationMode,
                        resolution.ContinuityResetPolicy,
                        resolution.ActorSetRef.Value,
                        resolution.Source));
                }
            }

            return definitions.ToArray();
        }

        private static int ResolveNonParticipationOccurrenceCount(ActorCardinalitySpec cardinality)
        {
            switch (cardinality.Kind)
            {
                case ActorCardinalityKind.ExactlyOne:
                case ActorCardinalityKind.ZeroOrOne:
                    return 1;
                case ActorCardinalityKind.OneOrMore:
                    throw new InvalidOperationException(
                        "[FATAL][Config][ActorsSystem] Non-participation cardinality OneOrMore exige resolucao finita explicita no ciclo canonico.");
                case ActorCardinalityKind.Fixed:
                    return cardinality.FixedCount;
                case ActorCardinalityKind.Range:
                    throw new InvalidOperationException(
                        "[FATAL][Config][ActorsSystem] Non-participation cardinality Range exige resolvedCount explicito no ciclo canonico (sem fallback para min/max).");
                default:
                    throw new InvalidOperationException($"[FATAL][Config][ActorsSystem] Unsupported cardinality kind='{cardinality.Kind}' for non-participation resolution.");
            }
        }

        private static bool IsRequiredOccurrence(ActorCardinalitySpec cardinality, int occurrenceIndex, bool defaultRequired)
        {
            if (occurrenceIndex < 0)
            {
                return false;
            }

            switch (cardinality.Kind)
            {
                case ActorCardinalityKind.ZeroOrOne:
                    return false;
                case ActorCardinalityKind.Range:
                    return defaultRequired && occurrenceIndex < cardinality.MinCount;
                default:
                    return defaultRequired;
            }
        }

        private static void ValidateParticipantCardinalityOrFail(
            ActorCardinalitySpec cardinality,
            int participantCount,
            ActorSpecRecord spec,
            ActorSetRef actorSetRef,
            SceneRouteKind routeKind)
        {
            bool valid = cardinality.Kind switch
            {
                ActorCardinalityKind.ExactlyOne => participantCount == 1,
                ActorCardinalityKind.ZeroOrOne => participantCount <= 1,
                ActorCardinalityKind.OneOrMore => participantCount >= 1,
                ActorCardinalityKind.Fixed => participantCount == cardinality.FixedCount,
                ActorCardinalityKind.Range => participantCount >= cardinality.MinCount && participantCount <= cardinality.MaxCount,
                _ => false
            };

            if (valid)
            {
                return;
            }

            throw new InvalidOperationException(
                $"[FATAL][Config][ActorsSystem] Participant cardinality mismatch actorSpecId='{spec.ActorSpecId}' actorSetRef='{actorSetRef.Value}' routeKind='{routeKind}' cardinalityKind='{cardinality.Kind}' fixed='{cardinality.FixedCount}' min='{cardinality.MinCount}' max='{cardinality.MaxCount}' participants='{participantCount}'.");
        }

        private static List<ParticipantSnapshot> ResolveParticipantsForRoleGroup(
            ActorSpecRoleGroup roleGroup,
            ParticipationSnapshot participationSnapshot)
        {
            var participants = new List<ParticipantSnapshot>();
            ParticipantSnapshot[] source = participationSnapshot.Participants;
            if (source == null || source.Length == 0)
            {
                return participants;
            }

            ParticipantKind expectedKind = MapParticipantKind(roleGroup);
            if (expectedKind == ParticipantKind.Unknown)
            {
                return participants;
            }

            for (int index = 0; index < source.Length; index += 1)
            {
                ParticipantSnapshot current = source[index];
                if (!current.IsValid || current.Kind != expectedKind)
                {
                    continue;
                }

                participants.Add(current);
            }

            if (participants.Count == 0)
            {
                return participants;
            }

            participants.Sort(static (left, right) =>
            {
                int leftRank = GetParticipantRank(left);
                int rightRank = GetParticipantRank(right);
                if (leftRank != rightRank)
                {
                    return leftRank.CompareTo(rightRank);
                }

                return string.CompareOrdinal(left.ParticipantId.Value, right.ParticipantId.Value);
            });
            return participants;
        }

        private static int GetParticipantRank(ParticipantSnapshot participant)
        {
            if (participant.IsLocal)
            {
                return 0;
            }

            if (participant.IsPrimary)
            {
                return 1;
            }

            return 2;
        }

        private static ParticipantKind MapParticipantKind(ActorSpecRoleGroup roleGroup)
        {
            switch (roleGroup)
            {
                case ActorSpecRoleGroup.Player:
                    return ParticipantKind.Player;
                case ActorSpecRoleGroup.Actor:
                    return ParticipantKind.Actor;
                case ActorSpecRoleGroup.Spectator:
                    return ParticipantKind.Spectator;
                case ActorSpecRoleGroup.System:
                    return ParticipantKind.System;
                default:
                    return ParticipantKind.Unknown;
            }
        }

        private static ActorsDefinitionsSnapshot BuildEmptyDefinitionsSnapshot(
            SceneRouteKind routeKind,
            string source,
            string participationSignature,
            string reason)
        {
            return new ActorsDefinitionsSnapshot(
                $"actor-set-definitions|routeKind:{routeKind}|actorSetRef:<none>|source:{AsText(source)}|participation:{AsText(participationSignature)}|count:0|reason:{AsText(reason)}",
                Array.Empty<ActorDefinitionRecord>());
        }

        private bool TryGetCanonicalGameplayDefinitionsSnapshot(out ActorsDefinitionsSnapshot snapshot)
        {
            snapshot = ActorsDefinitionsSnapshot.Empty;

            CanonicalGameplayEntryContext canonicalContext;
            lock (_canonicalGameplayEntrySync)
            {
                if (!_hasCanonicalGameplayEntryContext)
                {
                    return false;
                }

                canonicalContext = _canonicalGameplayEntryContext;
            }

            if (!canonicalContext.RouteId.IsValid ||
                canonicalContext.RouteKind != SceneRouteKind.Gameplay ||
                string.IsNullOrWhiteSpace(canonicalContext.ActorSetRef))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Canonical gameplay entry context invalido ao derivar definitions. routeId='{canonicalContext.RouteId}' routeKind='{canonicalContext.RouteKind}' actorSetRef='{AsText(canonicalContext.ActorSetRef)}'.");
            }

            ParticipationReadinessSnapshot readiness = ParticipationReadinessSnapshot.Empty;
            bool hasReadiness = _participationFlowService.TryGetCurrentReadiness(out readiness);

            if (!_participationFlowService.TryGetCurrent(out ParticipationSnapshot participationSnapshot) ||
                !hasReadiness ||
                !participationSnapshot.IsValid ||
                !readiness.IsValid ||
                !readiness.CanEnterGameplay)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Participation snapshot invalida ao derivar definitions do rail phase-local-entry-ready actorSetRef='{AsText(canonicalContext.ActorSetRef)}' scene='{AsText(canonicalContext.SceneName)}' readinessState='{readiness.State}' canEnterGameplay='{readiness.CanEnterGameplay}'.");
            }

            if (!string.IsNullOrWhiteSpace(canonicalContext.ParticipationSignature) &&
                !string.Equals(participationSnapshot.Signature.Value, canonicalContext.ParticipationSignature, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Participation signature divergente ao derivar definitions do rail phase-local-entry-ready canonical='{AsText(canonicalContext.ParticipationSignature)}' current='{AsText(participationSnapshot.Signature.Value)}' actorSetRef='{AsText(canonicalContext.ActorSetRef)}'.");
            }

            ActorSetRef actorSetRef = new ActorSetRef(canonicalContext.ActorSetRef);
            if (!actorSetRef.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] ActorSetRef invalido ao derivar definitions do rail phase-local-entry-ready actorSetRef='{AsText(canonicalContext.ActorSetRef)}'.");
            }

            if (!_actorSetSelectionService.TryResolve(actorSetRef, out ActorSetResolvedSelection selection) || !selection.HasEntries)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] ActorSetRef canonico nao resolveu entries no rail phase-local-entry-ready actorSetRef='{actorSetRef.Value}' routeKind='{canonicalContext.RouteKind}' source='{AsText(canonicalContext.Source)}'.");
            }

            CanonicalActorResolutionSet resolutionSet = BuildCanonicalResolutionSetOrFail(
                selection,
                true,
                participationSnapshot,
                actorSetRef,
                canonicalContext.RouteKind,
                canonicalContext.Source,
                canonicalContext.CycleSignature);
            _currentResolutionSet = resolutionSet;
            ActorDefinitionRecord[] entries = BuildDefinitionsFromResolutionSet(resolutionSet);

            snapshot = new ActorsDefinitionsSnapshot(
                BuildDefinitionsSignature(
                    actorSetRef,
                    canonicalContext.RouteKind,
                    canonicalContext.Source,
                    canonicalContext.ParticipationSignature,
                    canonicalContext.CycleSignature,
                    entries.Length),
                entries);
            return snapshot.IsValid;
        }

        private static string BuildDefinitionsSignature(
            ActorSetRef actorSetRef,
            SceneRouteKind routeKind,
            string source,
            string participationSignature,
            string cycleSignature,
            int count)
        {
            return $"actor-set-definitions|routeKind:{routeKind}|actorSetRef:{actorSetRef.Value}|source:{AsText(source)}|participation:{AsText(participationSignature)}|cycle:{AsText(cycleSignature)}|count:{count}";
        }

        private static string BuildResolutionSignature(
            ActorSetRef actorSetRef,
            SceneRouteKind routeKind,
            string source,
            string cycleSignature,
            int count)
        {
            return $"canonical-resolution|routeKind:{routeKind}|actorSetRef:{actorSetRef.Value}|source:{AsText(source)}|cycle:{AsText(cycleSignature)}|count:{count}";
        }

        public readonly struct CanonicalGameplayEntryContext
        {
            public CanonicalGameplayEntryContext(
                SceneRouteId routeId,
                SceneRouteKind routeKind,
                string sceneName,
                string reason,
                string sessionSignature,
                string phaseSignature,
                string participationSignature,
                string actorSetRef,
                string cycleSignature,
                string source,
                PhaseEntryIdentity phaseEntryIdentity)
            {
                RouteId = routeId;
                RouteKind = routeKind;
                SceneName = Normalize(sceneName);
                Reason = Normalize(reason);
                SessionSignature = Normalize(sessionSignature);
                PhaseSignature = Normalize(phaseSignature);
                ParticipationSignature = Normalize(participationSignature);
                ActorSetRef = Normalize(actorSetRef);
                CycleSignature = Normalize(cycleSignature);
                Source = Normalize(source);
                PhaseEntryIdentity = phaseEntryIdentity;
            }

            public SceneRouteId RouteId { get; }
            public SceneRouteKind RouteKind { get; }
            public string SceneName { get; }
            public string Reason { get; }
            public string SessionSignature { get; }
            public string PhaseSignature { get; }
            public string ParticipationSignature { get; }
            public string ActorSetRef { get; }
            public string CycleSignature { get; }
            public string Source { get; }
            public PhaseEntryIdentity PhaseEntryIdentity { get; }

            private static string Normalize(string value)
            {
                return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
            }
        }

        public readonly struct CanonicalGameplayEntrySnapshot
        {
            public CanonicalGameplayEntrySnapshot(CanonicalGameplayEntryContext context)
            {
                RouteId = context.RouteId;
                RouteKind = context.RouteKind;
                SceneName = context.SceneName;
                Reason = context.Reason;
                SessionSignature = context.SessionSignature;
                PhaseSignature = context.PhaseSignature;
                ParticipationSignature = context.ParticipationSignature;
                ActorSetRef = context.ActorSetRef;
                CycleSignature = context.CycleSignature;
                PhaseEntryIdentity = context.PhaseEntryIdentity;
                SourceKind = ActorsOperationalMaterializationSourceKind.SessionTransitionPhaseLocalEntryReady;
                SourceId = context.Source;
            }

            public SceneRouteId RouteId { get; }
            public SceneRouteKind RouteKind { get; }
            public string SceneName { get; }
            public string Reason { get; }
            public string SessionSignature { get; }
            public string PhaseSignature { get; }
            public string ParticipationSignature { get; }
            public string ActorSetRef { get; }
            public string CycleSignature { get; }
            public PhaseEntryIdentity PhaseEntryIdentity { get; }
            public ActorsOperationalMaterializationSourceKind SourceKind { get; }
            public string SourceId { get; }

            public bool HasCanonicalPayload =>
                RouteId.IsValid &&
                RouteKind == SceneRouteKind.Gameplay &&
                !string.IsNullOrWhiteSpace(SceneName) &&
                !string.IsNullOrWhiteSpace(Reason) &&
                !string.IsNullOrWhiteSpace(SessionSignature) &&
                !string.IsNullOrWhiteSpace(PhaseSignature) &&
                !string.IsNullOrWhiteSpace(ParticipationSignature) &&
                !string.IsNullOrWhiteSpace(ActorSetRef) &&
                !string.IsNullOrWhiteSpace(CycleSignature) &&
                PhaseEntryIdentity.IsValid &&
                !string.IsNullOrWhiteSpace(SourceId);

            public bool IsValid =>
                HasCanonicalPayload &&
                SourceKind != ActorsOperationalMaterializationSourceKind.Unknown;
        }

        private static string AsText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }

        private static ActorRole MapRole(ActorSpecRoleGroup roleGroup)
        {
            switch (roleGroup)
            {
                case ActorSpecRoleGroup.Player:
                    return ActorRole.Player;
                case ActorSpecRoleGroup.Actor:
                    return ActorRole.Actor;
                case ActorSpecRoleGroup.Spectator:
                    return ActorRole.Spectator;
                case ActorSpecRoleGroup.System:
                    return ActorRole.System;
                default:
                    return ActorRole.Unknown;
            }
        }

        private static ActorRole MapRole(ParticipantKind kind)
        {
            switch (kind)
            {
                case ParticipantKind.Player:
                    return ActorRole.Player;
                case ParticipantKind.Actor:
                    return ActorRole.Actor;
                case ParticipantKind.Spectator:
                    return ActorRole.Spectator;
                case ParticipantKind.System:
                    return ActorRole.System;
                default:
                    return ActorRole.Unknown;
            }
        }
    }
}
