using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
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
                    evt.Source);
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

            var entries = new List<ActorDefinitionRecord>(selection.Count);
            for (int index = 0; index < selection.OrderedSpecs.Length; index += 1)
            {
                ActorSpecRecord spec = selection.OrderedSpecs[index];
                ActorDefinitionRecord definition = BuildDefinitionFromSpecOrFail(spec, hasParticipation, participationSnapshot, actorSetRef, routeKind, source);
                entries.Add(definition);

                DebugUtility.LogVerbose(typeof(SessionFlowActorsSemanticPortsAdapter),
                    $"[OBS][ActorsSystem] DefinitionsResolvedViaCanonicalActorSet actorSetRef='{actorSetRef.Value}' routeKind='{routeKind}' order='{index}' actorSpecId='{spec.ActorSpecId}' axisActorId='{definition.AxisActorId}' semanticParticipantId='{AsText(definition.SemanticParticipantId)}' recipe='{definition.OperationalRecipeKind}'.",
                    DebugUtility.Colors.Info);
            }

            snapshot = new ActorsDefinitionsSnapshot(
                BuildDefinitionsSignature(actorSetRef, routeKind, source, hasParticipation ? participationSnapshot.Signature.Value : string.Empty, string.Empty, entries.Count),
                entries.ToArray());
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

        private static ActorDefinitionRecord BuildDefinitionFromSpecOrFail(
            ActorSpecRecord spec,
            bool hasParticipation,
            ParticipationSnapshot participationSnapshot,
            ActorSetRef actorSetRef,
            SceneRouteKind routeKind,
            string source)
        {
            if (!spec.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Invalid ActorSpec while deriving definition actorSetRef='{actorSetRef.Value}' routeKind='{routeKind}'.");
            }

            ActorRole role = MapRole(spec.RoleGroup);
            if (role == ActorRole.Unknown)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Unsupported roleGroup='{spec.RoleGroup}' while deriving definition actorSpecId='{spec.ActorSpecId}'.");
            }

            AxisActorId axisActorId;
            string semanticParticipantId = string.Empty;
            bool isRequired = spec.IntegrationStage != ActorSpecIntegrationStage.RuntimeDynamic;

            if (spec.SourceKind == ActorSpecSourceKind.ParticipationDerived)
            {
                if (!hasParticipation)
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsSystem] Participation snapshot required for participation-derived actorSpecId='{spec.ActorSpecId}' actorSetRef='{actorSetRef.Value}' routeKind='{routeKind}'.");
                }

                if (!TryResolveParticipantForRoleGroup(spec.RoleGroup, participationSnapshot, out ParticipantSnapshot participant))
                {
                    throw new InvalidOperationException(
                        $"[FATAL][Config][ActorsSystem] Missing semantic participant for participation-derived actorSpecId='{spec.ActorSpecId}' roleGroup='{spec.RoleGroup}' actorSetRef='{actorSetRef.Value}' routeKind='{routeKind}' participationSignature='{participationSnapshot.Signature}'.");
                }

                semanticParticipantId = participant.ParticipantId.Value;
                axisActorId = AxisActorId.FromParticipantId(semanticParticipantId);
                isRequired = isRequired || participant.IsPrimary || participant.IsLocal;
            }
            else
            {
                axisActorId = AxisActorId.FromActorSpecId(spec.ActorSpecId);
            }

            var definition = new ActorDefinitionRecord(
                axisActorId,
                role,
                isRequired,
                semanticParticipantId,
                spec.OperationalRecipeKind,
                RuntimeActorId.None,
                spec.ActorSpecId,
                actorSetRef.Value,
                $"ActorSet/{actorSetRef.Value}/ActorSpec/{spec.ActorSpecId}");

            if (!definition.IsValid)
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][ActorsSystem] Invalid definition derived from ActorSpec actorSpecId='{spec.ActorSpecId}' actorSetRef='{actorSetRef.Value}' routeKind='{routeKind}' source='{AsText(source)}'.");
            }

            return definition;
        }

        private static bool TryResolveParticipantForRoleGroup(
            ActorSpecRoleGroup roleGroup,
            ParticipationSnapshot participationSnapshot,
            out ParticipantSnapshot participant)
        {
            participant = default;
            ParticipantSnapshot[] source = participationSnapshot.Participants;
            if (source == null || source.Length == 0)
            {
                return false;
            }

            ParticipantKind expectedKind = MapParticipantKind(roleGroup);
            if (expectedKind == ParticipantKind.Unknown)
            {
                return false;
            }

            if (roleGroup == ActorSpecRoleGroup.Player)
            {
                if (TryResolveLocal(source, expectedKind, out participant))
                {
                    return true;
                }

                if (TryResolvePrimary(source, expectedKind, out participant))
                {
                    return true;
                }
            }

            return TryResolveFirst(source, expectedKind, out participant);
        }

        private static bool TryResolveLocal(ParticipantSnapshot[] source, ParticipantKind kind, out ParticipantSnapshot participant)
        {
            participant = default;
            for (int index = 0; index < source.Length; index += 1)
            {
                ParticipantSnapshot current = source[index];
                if (!current.IsValid || current.Kind != kind || !current.IsLocal)
                {
                    continue;
                }

                participant = current;
                return true;
            }

            return false;
        }

        private static bool TryResolvePrimary(ParticipantSnapshot[] source, ParticipantKind kind, out ParticipantSnapshot participant)
        {
            participant = default;
            for (int index = 0; index < source.Length; index += 1)
            {
                ParticipantSnapshot current = source[index];
                if (!current.IsValid || current.Kind != kind || !current.IsPrimary)
                {
                    continue;
                }

                participant = current;
                return true;
            }

            return false;
        }

        private static bool TryResolveFirst(ParticipantSnapshot[] source, ParticipantKind kind, out ParticipantSnapshot participant)
        {
            participant = default;
            for (int index = 0; index < source.Length; index += 1)
            {
                ParticipantSnapshot current = source[index];
                if (!current.IsValid || current.Kind != kind)
                {
                    continue;
                }

                participant = current;
                return true;
            }

            return false;
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

            var entries = new List<ActorDefinitionRecord>(selection.Count);
            for (int index = 0; index < selection.OrderedSpecs.Length; index += 1)
            {
                ActorSpecRecord spec = selection.OrderedSpecs[index];
                if (!spec.IsValid)
                {
                    continue;
                }

                ActorDefinitionRecord definition = BuildDefinitionFromSpecOrFail(
                    spec,
                    true,
                    participationSnapshot,
                    actorSetRef,
                    canonicalContext.RouteKind,
                    canonicalContext.Source);
                entries.Add(definition);

                DebugUtility.LogVerbose(typeof(SessionFlowActorsSemanticPortsAdapter),
                    $"[OBS][ActorsSystem] DefinitionsRefreshedViaPhaseLocalEntryReady actorSetRef='{actorSetRef.Value}' routeKind='{canonicalContext.RouteKind}' routeId='{canonicalContext.RouteId}' actorSpecId='{spec.ActorSpecId}' axisActorId='{definition.AxisActorId}' semanticParticipantId='{AsText(definition.SemanticParticipantId)}' recipe='{definition.OperationalRecipeKind}'.",
                    DebugUtility.Colors.Info);
            }

            snapshot = new ActorsDefinitionsSnapshot(
                BuildDefinitionsSignature(
                    actorSetRef,
                    canonicalContext.RouteKind,
                    canonicalContext.Source,
                    canonicalContext.ParticipationSignature,
                    canonicalContext.CycleSignature,
                    entries.Count),
                entries.ToArray());
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
                string source)
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
