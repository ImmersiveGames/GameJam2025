using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Events;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using _ImmersiveGames.NewScripts.ActorsSystem.Semantic;
using _ImmersiveGames.NewScripts.SceneFlow.Authoring.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.Navigation;
using _ImmersiveGames.NewScripts.SceneFlow.Contracts.RuntimeCore;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.Events;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.Participation.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.SessionContext
{
    public readonly struct GameplayStartSnapshot
    {
        public static GameplayStartSnapshot FromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            if (evt.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayStartSnapshot),
                    "[FATAL][H1][GameplaySessionFlow] PhaseDefinitionSelectedEvent requires a valid phaseDefinitionRef to materialize the gameplay start snapshot.");
            }

            return new GameplayStartSnapshot(
                evt.PhaseDefinitionRef,
                evt.MacroRouteId,
                evt.MacroRouteRef,
                PhaseDefinitionId.BuildCanonicalIntroContentId(evt.PhaseDefinitionRef.PhaseId),
                evt.Reason,
                evt.SelectionVersion,
                evt.SelectionSignature);
        }

        public GameplayStartSnapshot(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId macroRouteId,
            SceneRouteDefinitionAsset macroRouteRef,
            string localContentId,
            string reason,
            int selectionVersion,
            string phaseSignature)
        {
            PhaseDefinitionRef = phaseDefinitionRef;
            MacroRouteId = macroRouteId;
            MacroRouteRef = macroRouteRef;
            LocalContentId = ResolveLocalContentId(phaseDefinitionRef, localContentId);
            Reason = Sanitize(reason);
            SelectionVersion = selectionVersion < 0 ? 0 : selectionVersion;
            PhaseSignature = NormalizeSignature(phaseDefinitionRef, macroRouteId, reason, phaseSignature);
        }

        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public SceneRouteId MacroRouteId { get; }
        public SceneRouteDefinitionAsset MacroRouteRef { get; }
        public string LocalContentId { get; }
        public string Reason { get; }
        public int SelectionVersion { get; }
        public string PhaseSignature { get; }

        public bool HasPhaseDefinitionRef => PhaseDefinitionRef != null;
        public bool HasLocalContentId => !string.IsNullOrWhiteSpace(LocalContentId);
        public bool IsValid => MacroRouteId.IsValid && MacroRouteRef != null && HasPhaseDefinitionRef;

        public static GameplayStartSnapshot Empty => new(
            null,
            SceneRouteId.None,
            null,
            string.Empty,
            string.Empty,
            0,
            string.Empty);

        public override string ToString()
        {
            return $"phaseRef='{(HasPhaseDefinitionRef ? PhaseDefinitionRef.name : "<none>")}', routeId='{MacroRouteId}', localContentId='{(HasLocalContentId ? LocalContentId : "<none>")}', reason='{(string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason)}', v='{SelectionVersion}', phaseSignature='{(string.IsNullOrWhiteSpace(PhaseSignature) ? "<none>" : PhaseSignature)}'";
        }

        private static string ResolveLocalContentId(PhaseDefinitionAsset phaseDefinitionRef, string localContentId)
        {
            if (!string.IsNullOrWhiteSpace(localContentId))
            {
                return localContentId.Trim();
            }

            if (phaseDefinitionRef != null)
            {
                return PhaseDefinitionId.BuildCanonicalIntroContentId(phaseDefinitionRef.PhaseId);
            }

            return string.Empty;
        }

        private static string NormalizeSignature(
            PhaseDefinitionAsset phaseDefinitionRef,
            SceneRouteId routeId,
            string reason,
            string phaseSignature)
        {
            string normalized = Sanitize(phaseSignature);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }

            if (phaseDefinitionRef != null)
            {
                string phaseName = phaseDefinitionRef.name;
                return $"phase:{phaseName}|route:{routeId}|reason:{reason}";
            }

            return $"phase:<none>|route:{routeId}|reason:{reason}";
        }

        private static string Sanitize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    public interface IGameplayParticipationFlowService
    {
        ParticipationSnapshot Current { get; }
        ParticipationReadinessSnapshot CurrentReadiness { get; }
        bool TryGetCurrent(out ParticipationSnapshot snapshot);
        bool TryGetCurrentReadiness(out ParticipationReadinessSnapshot readiness);
        bool TryGetLast(out ParticipationSnapshot snapshot);
        ParticipationSnapshot Update(ParticipationSnapshot snapshot);
        ParticipationSnapshot UpdateFromSemanticInput(ParticipationSemanticInput input);
        ParticipationSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt);
        void Clear(string reason = null);
        void Clear(ParticipationSnapshotClearKind clearKind, string reason = null);
    }

    public readonly struct ParticipationSemanticInput
    {
        public ParticipationSemanticInput(
            string sessionSignature,
            string phaseSignature,
            PhaseDefinitionAsset phaseDefinitionRef,
            PhaseDefinitionId phaseId)
        {
            SessionSignature = string.IsNullOrWhiteSpace(sessionSignature) ? string.Empty : sessionSignature.Trim();
            PhaseSignature = string.IsNullOrWhiteSpace(phaseSignature) ? string.Empty : phaseSignature.Trim();
            PhaseDefinitionRef = phaseDefinitionRef;
            PhaseId = phaseId;
        }

        public string SessionSignature { get; }
        public string PhaseSignature { get; }
        public PhaseDefinitionAsset PhaseDefinitionRef { get; }
        public PhaseDefinitionId PhaseId { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(SessionSignature) &&
            !string.IsNullOrWhiteSpace(PhaseSignature) &&
            PhaseDefinitionRef != null &&
            PhaseId.IsValid;

        public static ParticipationSemanticInput FromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            if (!evt.IsValid || evt.PhaseDefinitionRef == null)
            {
                HardFailFastH1.Trigger(typeof(ParticipationSemanticInput),
                    "[FATAL][H1][GameplaySessionFlow] Invalid PhaseDefinitionSelectedEvent received while building semantic participation input.");
            }

            GameplaySessionContextSnapshot sessionContext = GameplaySessionContextSnapshot.FromPhaseDefinitionSelectedEvent(evt);
            return new ParticipationSemanticInput(
                sessionContext.SessionSignature,
                evt.SelectionSignature,
                evt.PhaseDefinitionRef,
                evt.PhaseId);
        }
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameplayParticipationFlowService :
        IGameplayParticipationFlowService,
        IDisposable
    {
        private readonly object _sync = new();
        private readonly ISceneFlowRouteActorSetRefContext _routeActorSetContext;
        private readonly IActorSetSelectionService _actorSetSelectionService;
        private ParticipationSnapshot _current = ParticipationSnapshot.Empty;
        private ParticipationSnapshot _last = ParticipationSnapshot.Empty;

        public GameplayParticipationFlowService(
            ISceneFlowRouteActorSetRefContext routeActorSetContext,
            IActorSetSelectionService actorSetSelectionService)
        {
            _routeActorSetContext = routeActorSetContext ?? throw new ArgumentNullException(nameof(routeActorSetContext));
            _actorSetSelectionService = actorSetSelectionService ?? throw new ArgumentNullException(nameof(actorSetSelectionService));

            DebugUtility.LogVerbose<GameplayParticipationFlowService>(
                "[OBS][GameplaySessionFlow][Participation] owner='GameplayParticipationFlowService' role='semantic-roster-owner' boundary='semantic-only/no-operational-executor'.",
                DebugUtility.Colors.Info);
        }

        public ParticipationSnapshot Current
        {
            get
            {
                lock (_sync)
                {
                    return _current;
                }
            }
        }

        public ParticipationReadinessSnapshot CurrentReadiness
        {
            get
            {
                lock (_sync)
                {
                    return _current.Readiness;
                }
            }
        }

        public bool TryGetCurrent(out ParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _current;
                return _current.IsValid;
            }
        }

        public bool TryGetCurrentReadiness(out ParticipationReadinessSnapshot readiness)
        {
            lock (_sync)
            {
                readiness = _current.Readiness;
                return _current.IsValid && readiness.IsValid;
            }
        }

        public bool TryGetLast(out ParticipationSnapshot snapshot)
        {
            lock (_sync)
            {
                snapshot = _last;
                return _last.IsValid;
            }
        }

        public ParticipationSnapshot Update(ParticipationSnapshot snapshot)
        {
            return UpdateInternal(snapshot, source: "manual_update");
        }

        public ParticipationSnapshot UpdateFromSemanticInput(ParticipationSemanticInput input)
        {
            return UpdateInternal(FromSemanticInput(input), source: "semantic_input");
        }

        public ParticipationSnapshot UpdateFromPhaseDefinitionSelectedEvent(PhaseDefinitionSelectedEvent evt)
        {
            return UpdateFromSemanticInput(ParticipationSemanticInput.FromPhaseDefinitionSelectedEvent(evt));
        }

        public void Clear(string reason = null)
        {
            Clear(ParticipationSnapshotClearKind.Unknown, reason);
        }

        public void Clear(ParticipationSnapshotClearKind clearKind, string reason = null)
        {
            string normalizedReason = Normalize(reason);
            string lastSignature;
            ParticipationSnapshot clearedSnapshot = ParticipationSnapshot.Empty;

            lock (_sync)
            {
                _last = _current;
                _current = clearedSnapshot;
                lastSignature = _last.Signature.Value;
            }

            DebugUtility.Log<GameplayParticipationFlowService>(
                $"[OBS][GameplaySessionFlow][Participation] ParticipationCleared keepLast='true' lastParticipationSignature='{Normalize(lastSignature)}' reason='{normalizedReason}'.",
                DebugUtility.Colors.Info);

            EventBus<ParticipationSnapshotChangedEvent>.Raise(
                new ParticipationSnapshotChangedEvent(
                    clearedSnapshot,
                    source: "GameplayParticipationFlowService.Clear",
                    reason: normalizedReason,
                    isCleared: true,
                    clearKind: clearKind));
        }

        public void Dispose()
        {
        }

        private ParticipationSnapshot FromSemanticInput(ParticipationSemanticInput input)
        {
            if (!input.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplayParticipationFlowService),
                    "[FATAL][H1][GameplaySessionFlow] Invalid ParticipationSemanticInput received while building participation snapshot.");
            }

            ParticipantSnapshot[] participants = BuildParticipants(input);
            bool hasParticipants = participants.Length > 0;
            ParticipantId primaryParticipantId = ResolveParticipantId(participants, participant => participant.IsPrimary);

            ParticipationReadinessSnapshot readiness = new(
                hasParticipants ? ParticipationReadinessState.Ready : ParticipationReadinessState.NoContent,
                hasParticipants ? "actor_set_derived" : "actor_set_empty",
                participants.Length,
                primaryParticipantId);

            return new ParticipationSnapshot(
                input.SessionSignature,
                input.PhaseSignature,
                participants,
                readiness,
                ParticipationPublicationMode.SnapshotOnly);
        }

        private ParticipationSnapshot UpdateInternal(ParticipationSnapshot snapshot, string source)
        {
            lock (_sync)
            {
                if (!snapshot.IsValid)
                {
                    HardFailFastH1.Trigger(typeof(GameplayParticipationFlowService),
                        "[FATAL][H1][GameplaySessionFlow] Invalid participation snapshot received by participation owner.");
                }

                _last = _current;
                _current = snapshot;
            }

            DebugUtility.Log<GameplayParticipationFlowService>(
                $"[OBS][GameplaySessionFlow][Participation] ParticipationTruthUpdated owner='GameplayParticipationFlowService' source='{source}' sessionSignature='{snapshot.SessionSignature}' phaseSignature='{snapshot.PhaseSignature}' participantCount='{snapshot.ParticipantCount}' primaryId='{snapshot.PrimaryParticipantId}' readinessState='{snapshot.Readiness.State}' readinessCanEnter='{snapshot.Readiness.CanEnterGameplay}' signature='{snapshot.Signature}'.",
                DebugUtility.Colors.Info);

            EventBus<ParticipationSnapshotChangedEvent>.Raise(
                new ParticipationSnapshotChangedEvent(
                    snapshot,
                    source: $"GameplayParticipationFlowService.{source}",
                    reason: source));

            return snapshot;
        }

        private ParticipantSnapshot[] BuildParticipants(ParticipationSemanticInput input)
        {
            _ = input;

            if (_routeActorSetContext == null || _actorSetSelectionService == null)
            {
                HardFailFastH1.Trigger(typeof(GameplayParticipationFlowService),
                    "[FATAL][H1][GameplaySessionFlow] Participation dependencies are missing while building the canonical roster.");
            }

            if (!_routeActorSetContext.TryGetCurrent(out ActorSetRef actorSetRef, out SceneRouteKind routeKind, out string routeSource))
            {
                HardFailFastH1.Trigger(typeof(GameplayParticipationFlowService),
                    "[FATAL][H1][GameplaySessionFlow] Route actor-set context unavailable while building the canonical roster.");
            }

            if (routeKind != SceneRouteKind.Gameplay || !actorSetRef.IsValid)
            {
                HardFailFastH1.Trigger(typeof(GameplayParticipationFlowService),
                    $"[FATAL][H1][GameplaySessionFlow] Canonical gameplay participation requires a valid gameplay actorSetRef. routeKind='{routeKind}' actorSetRef='{actorSetRef.Value}' source='{Normalize(routeSource)}'.");
            }

            if (!_actorSetSelectionService.TryResolve(actorSetRef, out ActorSetResolvedSelection selection) || !selection.HasEntries)
            {
                return Array.Empty<ParticipantSnapshot>();
            }

            var participants = new List<ParticipantSnapshot>(selection.Count);
            int primaryIndex = ResolvePrimaryIndex(selection.Members);

            for (int index = 0; index < selection.Members.Length; index += 1)
            {
                ActorSetResolvedMember member = selection.Members[index];
                if (!member.IsValid)
                {
                    continue;
                }

                ActorSpecRecord spec = member.Spec;
                if (!spec.IsValid || spec.RoleGroup != ActorSpecRoleGroup.Player)
                {
                    continue;
                }

                bool isPrimary = index == primaryIndex;
                bool isLocal = isPrimary;
                ParticipantKind participantKind = ParticipantKind.Player;
                OwnershipKind ownershipKind = isPrimary ? OwnershipKind.Local : OwnershipKind.Shared;
                BindingHint bindingHint = isPrimary
                    ? new BindingHint(BindingHintKind.LocalPrimary)
                    : new BindingHint(BindingHintKind.Shared);
                string participantIdValue = ResolveParticipantIdValue(spec, index);

                participants.Add(new ParticipantSnapshot(
                    new ParticipantId(participantIdValue),
                    participantKind,
                    ownershipKind,
                    bindingHint,
                    ParticipantLifecycleState.Expected,
                    isPrimary,
                    isLocal,
                    spec.ActorSpecId));
            }

            return participants.ToArray();
        }

        private static int ResolvePrimaryIndex(IReadOnlyList<ActorSetResolvedMember> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return -1;
            }

            for (int index = 0; index < entries.Count; index += 1)
            {
                ActorSetResolvedMember entry = entries[index];
                if (entry.IsValid && entry.Spec.IsValid && entry.Spec.RoleGroup == ActorSpecRoleGroup.Player)
                {
                    return index;
                }
            }

            return 0;
        }

        private static string ResolveParticipantIdValue(ActorSpecRecord entry, int index)
        {
            if (entry.IsValid && !string.IsNullOrWhiteSpace(entry.ActorSpecId))
            {
                return entry.ActorSpecId.Trim();
            }

            return $"participant:{index + 1}";
        }

        private static ParticipantId ResolveParticipantId(ParticipantSnapshot[] participants, Func<ParticipantSnapshot, bool> predicate)
        {
            if (participants == null || predicate == null)
            {
                return ParticipantId.None;
            }

            for (int index = 0; index < participants.Length; index += 1)
            {
                ParticipantSnapshot participant = participants[index];
                if (participant.IsValid && predicate(participant))
                {
                    return participant.ParticipantId;
                }
            }

            return ParticipantId.None;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}

